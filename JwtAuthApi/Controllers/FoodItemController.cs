using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Foods;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodItemController : ControllerBase
    {
        private readonly IFoodItemRespository _foodItemRepo;
        public FoodItemController(IFoodItemRespository foodItemRepo)
        {
            _foodItemRepo = foodItemRepo;

        }
        [HttpGet]
        [SwaggerOperation(Summary = "Get All Food Items (ALL USER)")]
        public async Task<ActionResult> GetAllFoodItems([FromQuery] AllFoodsQuery queryObject)
        {
            try
            {
                var result = await _foodItemRepo.GetAllFoodItemsAsync(queryObject);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error retrieving food items" });
            }
        }

        [HttpGet("{id:int}")]
        [SwaggerOperation(Summary = "Get specific Food Item by ID (ALL USER)")]
        public async Task<IActionResult> GetFoodItemById([FromRoute] int id)
        {
            try
            {
                var result = await _foodItemRepo.GetFoodItemByIdAsync(id);
                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpGet("featured")]
        [SwaggerOperation(Summary = "Get Featured Items (ALL USER)")]
        public async Task<ActionResult<List<FoodResponseDto>>> GetFeaturedItems()
        {
            try
            {
                var result = await _foodItemRepo.GetFeaturedItemsAsync();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error retrieving featured food items" });
            }
        }

        [HttpGet("seller/{sellerId}")]
        [SwaggerOperation(Summary = "Get All food Items on seller (ALL USER)")]
        public async Task<IActionResult> GetSellerMenu([FromRoute] string sellerId, [FromQuery] AllFoodsQuery queryObject)
        {
            try
            {
                var result = await _foodItemRepo.GetSellerMenuAsync(queryObject, sellerId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error retrieving food items" });
            }
        }

    }
}