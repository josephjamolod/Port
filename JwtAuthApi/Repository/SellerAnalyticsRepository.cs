using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Data;
using JwtAuthApi.Dtos.Orders;
using JwtAuthApi.Dtos.SellerAnalytics;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Helpers.QueryBuilders;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Mappers;
using JwtAuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApi.Repository
{
    public class SellerAnalyticsRepository : ISellerAnalyticsRepository
    {
        private readonly ApplicationDBContext _context;
        public SellerAnalyticsRepository(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<OperationResult<PaginatedResponse<OrderDto>, ErrorResult>> GetSellerOrdersAsync(MyOrdersQuery queryObject, string sellerId)
        {
            try
            {
                var query = _context.Orders
                   .Include(o => o.Customer)
                   .Include(o => o.Seller)
                   .Include(o => o.OrderItems)
                       .ThenInclude(oi => oi.FoodItem)
                           .ThenInclude(fi => fi.ImageUrls)
                   .Where(o => o.SellerId == sellerId);

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

        public async Task<OperationResult<object, ErrorResult>> GetTopSellingItemsAsync(int limit, string sellerId)
        {
            try
            {
                // First, get the aggregated data
                var topItemsData = await _context.OrderItems
                         .Include(oi => oi.Order)
                         .Where(oi => oi.FoodItem.SellerId == sellerId &&
                                      oi.Order.Status == OrderStatus.Delivered)
                         .GroupBy(oi => oi.FoodItemId)
                         .Select(g => new
                         {
                             foodItemId = g.Key,
                             totalQuantitySold = g.Sum(oi => oi.Quantity),
                             totalRevenue = g.Sum(oi => oi.Quantity * oi.Price),
                             orderCount = g.Count()
                         })
                         .OrderByDescending(x => x.totalQuantitySold)
                         .Take(limit)
                         .ToListAsync();

                // Get the food item IDs
                var foodItemIds = topItemsData.Select(x => x.foodItemId).ToList();

                // Fetch the food items with images
                var foodItems = await _context.FoodItems
                    .Include(fi => fi.ImageUrls)
                    .Where(fi => foodItemIds.Contains(fi.Id))
                    .ToListAsync();

                // Combine the data
                var topItems = topItemsData.Select(data =>
                {
                    var foodItem = foodItems.First(fi => fi.Id == data.foodItemId);
                    return new
                    {
                        foodItemId = data.foodItemId,
                        name = foodItem.Name,
                        category = foodItem.Category,
                        price = foodItem.Price,
                        imageUrls = foodItem.ImageUrls.Select(img => img.ImageUrl).ToList(),
                        totalQuantitySold = data.totalQuantitySold,
                        totalRevenue = data.totalRevenue,
                        orderCount = data.orderCount
                    };
                }).ToList();

                return OperationResult<object, ErrorResult>.Success(new
                {
                    count = topItems.Count,
                    items = topItems
                });
            }
            catch (Exception)
            {
                return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Retrieving Items"
                });
            }
        }

        public async Task<OperationResult<DashboardStatsResponseDto, ErrorResult>> GetDashboardStatsAsync(string sellerId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

                // Today's stats
                var todayOrders = await _context.Orders
                    .Where(o => o.SellerId == sellerId && o.CreatedAt >= today)
                    .ToListAsync();

                var todayRevenue = todayOrders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .Sum(o => o.Total);

                // This month's stats
                var monthOrders = await _context.Orders
                    .Where(o => o.SellerId == sellerId && o.CreatedAt >= thisMonth)
                    .ToListAsync();

                var monthRevenue = monthOrders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .Sum(o => o.Total);

                // Pending orders
                var pendingOrders = await _context.Orders
                    .CountAsync(o => o.SellerId == sellerId &&
                                     (o.Status == OrderStatus.Pending ||
                                      o.Status == OrderStatus.Confirmed ||
                                      o.Status == OrderStatus.Preparing));

                // Total items
                var totalItems = await _context.FoodItems
                    .CountAsync(f => f.SellerId == sellerId);

                var availableItems = await _context.FoodItems
                    .CountAsync(f => f.SellerId == sellerId && f.IsAvailable);

                // Total customers
                var totalCustomers = await _context.Orders
                    .Where(o => o.SellerId == sellerId)
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .CountAsync();

                // Average rating
                var averageRating = await _context.Reviews
                    .Where(r => r.SellerId == sellerId)
                    .AverageAsync(r => (decimal?)r.Rating) ?? 0;

                var totalReviews = await _context.Reviews
                    .CountAsync(r => r.SellerId == sellerId);

                var dashboardStats = new DashboardStatsResponseDto()
                {
                    Today = new Today()
                    {
                        Orders = todayOrders.Count,
                        TodayRevenue = todayRevenue,
                        Pending = todayOrders.Count(o => o.Status == OrderStatus.Pending)
                    },
                    ThisMonth = new ThisMonth()
                    {
                        MonthOrders = monthOrders.Count,
                        MonthRevenue = monthRevenue,
                    },
                    Overview = new Overview()
                    {
                        PendingOrders = pendingOrders,
                        TotalItems = totalItems,
                        AvailableItems = availableItems,
                        TotalCustomers = totalCustomers,
                        AverageRating = averageRating,
                        TotalReviews = totalReviews
                    }
                };

                return OperationResult<DashboardStatsResponseDto, ErrorResult>.Success(dashboardStats);
            }
            catch (Exception)
            {
                return OperationResult<DashboardStatsResponseDto, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Error retrieving analytics"
                });
            }
        }
    }
}