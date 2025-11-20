using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Orders;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerAnalyticsController : ControllerBase
    {
        private readonly ISellerAnalyticsRepository _sellerAnalyticsRepo;
        private string GetSellerId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public SellerAnalyticsController(ISellerAnalyticsRepository sellerAnalyticsRepo)
        {
            _sellerAnalyticsRepo = sellerAnalyticsRepo;
        }

        [HttpGet("seller-orders")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<List<OrderDto>>> GetSellerOrders([FromQuery] MyOrdersQuery queryObject)
        {
            var result = await _sellerAnalyticsRepo.GetSellerOrdersAsync(queryObject, GetSellerId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpGet("top-items")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetTopSellingItems([FromQuery] int limit = 10)
        {
            var result = await _sellerAnalyticsRepo.GetTopSellingItemsAsync(limit, GetSellerId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var result = await _sellerAnalyticsRepo.GetDashboardStatsAsync(GetSellerId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }
    }
}