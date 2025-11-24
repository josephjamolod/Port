using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewRepository _reviewRepo;
        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public ReviewController(IReviewRepository reviewRepo)
        {
            _reviewRepo = reviewRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews([FromQuery] ReviewQuery query)
        {
            var result = await _reviewRepo.GetReviewsAsync(query);
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }
    }
}