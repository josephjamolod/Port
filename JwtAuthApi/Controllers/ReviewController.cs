using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Reviews;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepo;
        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private string? GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value;
        public ReviewController(IReviewRepository reviewRepo)
        {
            _reviewRepo = reviewRepo;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get reviews with optional filters [Non-admin: must filter by foodItemId or sellerId, Admin: all reviews] (ALL USER)")]
        public async Task<IActionResult> GetReviews([FromQuery] ReviewQuery query)
        {
            var result = await _reviewRepo.GetReviewsAsync(query, GetUserId(), GetUserRole());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpPost]
        [Authorize]
        [SwaggerOperation(Summary = "Create a review for delivered order [Can Be also use to raview the seller/restaurant ] (CUSTOMER)")]
        public async Task<IActionResult> CreateReview(CreateReviewRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reviewRepo.CreateReviewAsync(request, GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return CreatedAtAction(nameof(GetReviews), result.Value);
        }

        [HttpGet("low-rated")]
        [Authorize(Roles = "Seller")]
        [SwaggerOperation(Summary = "Get Low Rated Reviews (SELLER)")]
        public async Task<IActionResult> GetLowRatedReviews([FromQuery] LowRatedReviewQuery query)
        {
            var result = await _reviewRepo.GetLowRatedReviewsAsync(query, GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }
    }
}