using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Data;
using JwtAuthApi.Dtos.Orders;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Helpers.QueryBuilders;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Mappers;
using JwtAuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApi.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDBContext _context;
        public OrderRepository(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<OperationResult<CheckoutSelectedResponse, ErrorResult>> CheckoutSelectedSellersAsync(CheckoutSelectedRequest request, string userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.FoodItem)
                        .ThenInclude(fi => fi.Seller)
                .FirstOrDefaultAsync(c => c.CustomerId == userId);

            if (cart == null || cart.CartItems.Count == 0)
                return OperationResult<CheckoutSelectedResponse, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status400BadRequest,
                    ErrDescription = "Cart is empty"
                });

            // Filter only selected sellers
            var selectedItems = cart.CartItems
                .Where(ci => request.SellerIds.Contains(ci.FoodItem.SellerId))
                .ToList();

            if (selectedItems.Count == 0)
                return OperationResult<CheckoutSelectedResponse, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status400BadRequest,
                    ErrDescription = "No items found for selected sellers"
                });

            // Group by seller
            var sellerGroups = selectedItems
                .GroupBy(ci => ci.FoodItem.SellerId)
                .ToList();

            var createdOrders = new List<OrderDto>();
            var errors = new List<CheckoutError>();

            // Create separate order for each selected seller
            foreach (var sellerGroup in sellerGroups)
            {
                var sellerId = sellerGroup.Key;
                var sellerItems = sellerGroup.ToList();
                var sellerName = sellerItems.First().FoodItem.Seller.BusinessName;

                // Validate seller items
                var validationError = ValidateSellerItems(sellerItems, sellerId, sellerName!);
                if (validationError != null)
                {
                    errors.Add(validationError);
                    continue;
                }

                try
                {
                    // Create order
                    var order = CreateOrderForSeller(sellerItems, userId, sellerId, request);

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    var orderDto = await GetOrderDtoById(order.Id);
                    createdOrders.Add(orderDto!);

                    // Remove items from cart
                    _context.CartItems.RemoveRange(sellerItems);
                }
                catch (Exception ex)
                {
                    errors.Add(new CheckoutError
                    {
                        SellerId = sellerId,
                        SellerName = sellerName,
                        ErrorType = "system_error",
                        Message = $"Failed to create order for {sellerName}: {ex.Message}"
                    });
                }
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = new CheckoutSelectedResponse
            {
                Success = createdOrders.Count != 0,
                OrdersCreated = createdOrders.Count,
                TotalOrders = sellerGroups.Count,
                Orders = createdOrders,
                Errors = errors
            };

            if (createdOrders.Count == 0 && errors.Count > 0)
            {
                return OperationResult<CheckoutSelectedResponse, ErrorResult>.Failure(
                    new ErrorResult
                    {
                        ErrCode = StatusCodes.Status400BadRequest,
                        ErrDescription = "Checkout failed for all selected sellers",
                    }
                );
            }

            // CASE 2: Partial success → still Success but includes errors
            if (createdOrders.Count > 0 && errors.Count > 0)
            {
                return OperationResult<CheckoutSelectedResponse, ErrorResult>.Success(response);
            }

            return OperationResult<CheckoutSelectedResponse, ErrorResult>.Success(response);
        }

        public async Task<OperationResult<Order, ErrorResult>> GetOrderByIdAsync(int id)
        {
            try
            {
                var order = await _context.Orders
                     .Include(o => o.Customer)
                     .Include(o => o.Seller)
                     .Include(o => o.OrderItems)
                         .ThenInclude(oi => oi.FoodItem)
                             .ThenInclude(fi => fi.ImageUrls)
                     .FirstOrDefaultAsync(o => o.Id == id);
                if (order == null)
                    return OperationResult<Order, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status404NotFound,
                        ErrDescription = "Order not found"
                    });

                return OperationResult<Order, ErrorResult>.Success(order);

            }
            catch (Exception)
            {
                return OperationResult<Order, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Please try again later."
                });
            }
        }

        public async Task<OperationResult<OrderDto, ErrorResult>> BuyNowAsync(BuyNowRequest request, string userId)
        {
            try
            {
                // Validate food item exists and is available
                var foodItem = await _context.FoodItems
                    .Include(f => f.Seller)
                    .Include(f => f.ImageUrls)
                    .FirstOrDefaultAsync(f => f.Id == request.FoodItemId);

                if (foodItem == null)
                    return OperationResult<OrderDto, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status404NotFound,
                        ErrDescription = "Food item not found"
                    });

                if (!foodItem.IsAvailable)
                    return OperationResult<OrderDto, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status400BadRequest,
                        ErrDescription = "This item is currently unavailable"
                    });


                if (request.Quantity <= 0)
                    return OperationResult<OrderDto, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status400BadRequest,
                        ErrDescription = "Quantity must be greater than 0"
                    });


                // Calculate totals
                var subTotal = foodItem.Price * request.Quantity;
                var estimatedDeliveryTime = foodItem.PreparationTime + 30;

                // Create order using shared helper
                var createOrder = new CreateOrderDto()
                {
                    UserId = userId,
                    SellerId = foodItem.SellerId,
                    SubTotal = subTotal,
                    DeliveryFee = request.DeliveryFee,
                    EstimatedDeliveryTime = estimatedDeliveryTime,
                    DeliveryAddress = request.DeliveryAddress,
                    PhoneNumber = request.PhoneNumber,
                    Notes = request.Notes
                };

                var order = createOrder.CreateOrderDtoToOrder();
                order.OrderNumber = GenerateOrderNumber();

                // Create single order item
                var orderItem = new OrderItem
                {
                    FoodItemId = request.FoodItemId,
                    Quantity = request.Quantity,
                    Price = foodItem.Price,
                    SpecialInstructions = request.SpecialInstructions
                };
                order.OrderItems.Add(orderItem);

                // Update food item statistics
                foodItem.TotalSold += request.Quantity;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                var finalOrder = await GetOrderDtoById(order.Id);
                // Return order details
                return OperationResult<OrderDto, ErrorResult>.Success(finalOrder!);
            }
            catch (Exception)
            {
                return OperationResult<OrderDto, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Buying the item"
                });
            }
        }

        public async Task<OperationResult<PaginatedResponse<OrderDto>, ErrorResult>> GetMyOrdersAsync(MyOrdersQuery queryObject, string userId)
        {
            try
            {
                var query = _context.Orders
                   .Include(o => o.Customer)
                   .Include(o => o.Seller)
                   .Include(o => o.OrderItems)
                       .ThenInclude(oi => oi.FoodItem)
                           .ThenInclude(fi => fi.ImageUrls)
                   .Where(o => o.CustomerId == userId);

                // Apply filters
                query = UserOrderQueryBuilder.ApplyFilters(query, queryObject);
                // Apply sorting
                query = UserOrderQueryBuilder.ApplySorting(query, queryObject);

                // Get total count before pagination
                var totalCount = await query.CountAsync();
                // Apply pagination
                var skip = (queryObject.PageNumber - 1) * queryObject.PageSize;

                //  Materialize the data from database
                var ordersFromDb = await query
                    .Skip(skip)
                    .Take(queryObject.PageSize)
                    .ToListAsync();
                //  Apply the mapper in-memory
                var orders = ordersFromDb
                    .Select(o => o.OrderToOrderDto())
                    .ToList();

                return OperationResult<PaginatedResponse<OrderDto>, ErrorResult>.Success(new PaginatedResponse<OrderDto>()
                {
                    Total = totalCount,
                    PageNumber = queryObject.PageNumber,
                    PageSize = queryObject.PageSize,
                    Items = orders
                });
            }
            catch (Exception)
            {
                return OperationResult<PaginatedResponse<OrderDto>, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Retrieving Orders"
                });
            }
        }

        public async Task<OperationResult<object, ErrorResult>> UpdateOrderStatusAsync(UpdateOrderStatusParams prop)
        {
            try
            {
                var order = await _context.Orders.FindAsync(prop.OrderId);

                if (order == null)
                    return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status404NotFound,
                        ErrDescription = "Order not found"
                    });

                // Authorization checks
                if (!prop.IsAdmin)
                {
                    // Customers can only cancel
                    if (order.CustomerId == prop.UserId && prop.Status != OrderStatus.Cancelled)
                        return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                        {
                            ErrCode = StatusCodes.Status403Forbidden,
                            ErrDescription = "Forbidden"
                        });

                    // Sellers can update their own orders (except cancellation)
                    if (prop.IsSeller && order.SellerId != prop.UserId)
                        return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                        {
                            ErrCode = StatusCodes.Status403Forbidden,
                            ErrDescription = "Forbidden"
                        });

                    // Non-sellers/non-customers cannot update
                    if (order.CustomerId != prop.UserId && order.SellerId != prop.UserId)
                        return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                        {
                            ErrCode = StatusCodes.Status403Forbidden,
                            ErrDescription = "Forbidden"
                        });
                }

                // Validate status transitions
                if (!IsValidStatusTransition(order.Status, prop.Status, order.CustomerId == prop.UserId))
                    return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status400BadRequest,
                        ErrDescription = $"Cannot transition from {order.Status} to {prop.Status}"
                    });

                // Update status and timestamps
                order.Status = prop.Status;

                switch (prop.Status)
                {
                    case OrderStatus.Confirmed:
                        order.ConfirmedAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Preparing:
                        order.PreparingAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Ready:
                        order.ReadyAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Delivered:
                        order.DeliveredAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAt = DateTime.UtcNow;
                        break;
                }

                await _context.SaveChangesAsync();
                return OperationResult<object, ErrorResult>.Success(new { message = $"Order status updated to {prop.Status}" });
            }
            catch (Exception)
            {
                return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Updating order status"
                });
            }
        }

        public async Task<OperationResult<object, ErrorResult>> DeleteOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status404NotFound,
                        ErrDescription = "Order not found"
                    });
                // return NotFound(new { message = "Order not found" });

                _context.OrderItems.RemoveRange(order.OrderItems);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return OperationResult<object, ErrorResult>.Success(new { message = "Order deleted successfully" });
            }
            catch (Exception)
            {
                return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Deleting order "
                });
            }
        }

        private static string GenerateOrderNumber()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"ORD-{date}-{random}";
        }

        private async Task<OrderDto?> GetOrderDtoById(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Seller)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.FoodItem)
                        .ThenInclude(fi => fi.ImageUrls)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return null;

            return order.OrderToOrderDto();
        }

        private static CheckoutError? ValidateSellerItems(List<CartItem> sellerItems, string sellerId, string sellerName)
        {
            // Check for unavailable items
            var unavailableItems = sellerItems
                .Where(ci => !ci.FoodItem.IsAvailable)
                .ToList();

            if (unavailableItems.Count != 0)
            {
                return new CheckoutError
                {
                    SellerId = sellerId,
                    SellerName = sellerName,
                    ErrorType = "unavailable_items",
                    Message = $"Some items from {sellerName} are no longer available",
                    Items = unavailableItems.Select(ci => ci.FoodItem.Name).ToList()
                };
            }

            // Check for price changes
            var priceChangedItems = sellerItems
                .Where(ci => ci.PriceSnapshot != ci.FoodItem.Price)
                .ToList();

            if (priceChangedItems.Count != 0)
            {
                return new CheckoutError
                {
                    SellerId = sellerId,
                    SellerName = sellerName,
                    ErrorType = "price_changes",
                    Message = $"Prices have changed for items from {sellerName}",
                    PriceChanges = priceChangedItems.Select(ci => ci.CartItemToPriceChange()).ToList()
                };
            }

            return null;
        }

        private static Order CreateOrderForSeller(List<CartItem> sellerItems, string userId, string sellerId, CheckoutSelectedRequest request)
        {
            // Calculate totals
            var subTotal = sellerItems.Sum(ci => ci.Quantity * ci.FoodItem.Price);
            var maxPrepTime = sellerItems.Max(ci => ci.FoodItem.PreparationTime);
            var estimatedDeliveryTime = maxPrepTime + 30;

            // Create order using shared helper
            var createOrder = new CreateOrderDto()
            {
                UserId = userId,
                SellerId = sellerId,
                SubTotal = subTotal,
                DeliveryFee = request.DeliveryFee,
                EstimatedDeliveryTime = estimatedDeliveryTime,
                DeliveryAddress = request.DeliveryAddress,
                PhoneNumber = request.PhoneNumber,
                Notes = request.Notes
            };

            var order = createOrder.CreateOrderDtoToOrder();
            order.OrderNumber = GenerateOrderNumber();
            // Create order items
            foreach (var cartItem in sellerItems)
            {
                var orderItem = new OrderItem
                {
                    FoodItemId = cartItem.FoodItemId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.FoodItem.Price,
                    SpecialInstructions = cartItem.SpecialInstructions
                };
                order.OrderItems.Add(orderItem);

                cartItem.FoodItem.TotalSold += cartItem.Quantity;
            }

            return order;
        }


        private static bool IsValidStatusTransition(OrderStatus current, OrderStatus next, bool isCustomer)
        {
            // Customers can only cancel orders in Pending or Confirmed status
            if (isCustomer)
                return next == OrderStatus.Cancelled && (current == OrderStatus.Pending || current == OrderStatus.Confirmed);

            // Valid transitions for sellers
            return (current, next) switch
            {
                (OrderStatus.Pending, OrderStatus.Confirmed) => true,
                (OrderStatus.Pending, OrderStatus.Cancelled) => true,
                (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
                (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
                (OrderStatus.Preparing, OrderStatus.Ready) => true,
                (OrderStatus.Ready, OrderStatus.OutForDelivery) => true,
                (OrderStatus.OutForDelivery, OrderStatus.Delivered) => true,
                _ => false
            };
        }


    }
}