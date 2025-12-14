using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Orders;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepo;
        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        private bool IsInRole(string role) => User.IsInRole(role);
        public OrderController(IOrderRepository orderRepo)
        {
            _orderRepo = orderRepo;
        }

        [HttpPost("checkout-selected")]
        [SwaggerOperation(Summary = "Create an order from cart from selected sellers (User, Seller)")]
        public async Task<ActionResult<CheckoutSelectedResponse>> CheckoutSelectedSellers(CheckoutSelectedRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            // Call repository method
            var result = await _orderRepo.CheckoutSelectedSellersAsync(request, GetUserId());

            if (!result.IsSuccess)
            {
                // All sellers failed
                return StatusCode(result.Error!.ErrCode, result.Error.ErrDescription);
            }

            // Partial success or full success
            var response = result.Value!; // guaranteed to be non-null if IsSuccess == true

            if (response.Orders.Count > 0 && response.Errors.Count > 0)
            {
                // Partial success: some orders created, some errors
                return StatusCode(207, response); // Multi-status
            }

            // Full success: all orders created, no errors
            return Ok(response);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get Order by ID (User, Seller)")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id)
        {
            var userId = GetUserId();
            var result = await _orderRepo.GetOrderByIdAsync(id);

            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            var order = result.Value;
            if (order!.CustomerId != userId && order.SellerId != userId && !IsInRole("Admin"))
                return Forbid();

            return Ok(order.OrderToOrderDto());
        }

        [HttpPost("buy-now")]
        [SwaggerOperation(Summary = "Direct order/Buy 1 item directly (User, Seller)")]
        public async Task<ActionResult<OrderDto>> BuyNow(BuyNowRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _orderRepo.BuyNowAsync(request, GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return CreatedAtAction(nameof(GetOrderById), new { id = result.Value!.Id }, result.Value);
        }

        [HttpGet("my-orders")]
        [SwaggerOperation(Summary = "Get current User Orders (User, Seller)")]
        public async Task<ActionResult<List<OrderDto>>> GetMyOrders([FromQuery] MyOrdersQuery queryObject)
        {
            var result = await _orderRepo.GetMyOrdersAsync(queryObject, GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpPut("{id}/status")]
        [SwaggerOperation(Summary = "Update order status [Customers can only cancel,Sellers can update their own orders, Admin can do everything] (All User)")]
        public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusRequest request)
        {
            var props = new UpdateOrderStatusParams()
            {
                IsAdmin = IsInRole("Admin"),
                IsSeller = IsInRole("Seller"),
                OrderId = id,
                Status = request.Status,
                UserId = GetUserId()
            };

            var result = await _orderRepo.UpdateOrderStatusAsync(props);

            if (!result.IsSuccess)
            {
                if (result.Error!.ErrCode == 403)
                    return Forbid();
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });
            }

            return Ok(result.Value);
        }

        // DELETE: api/Order/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Seller")]
        [SwaggerOperation(Summary = "Delete an order [Seller can only delete their own orders, Admin can delete any] (Admin,Seller)")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var result = await _orderRepo.DeleteOrderAsync(id, GetUserId(), IsInRole("Admin"));

            if (!result.IsSuccess)
            {
                if (result.Error!.ErrCode == 403)
                    return Forbid();
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });
            }

            return Ok(result.Value);
        }
    }
}