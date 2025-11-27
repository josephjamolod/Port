using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Data;
using JwtAuthApi.Dtos.Foods;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Seller")]
    public class SellerFoodItemController : ControllerBase
    {
        private readonly ISellerFoodItemRepository _sellerFoodItemRepo;
        private readonly ILogger<SellerFoodItemController> _logger;
        private string GetSellerId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public SellerFoodItemController(ISellerFoodItemRepository sellerFoodItemRepo, ILogger<SellerFoodItemController> logger)
        {
            _sellerFoodItemRepo = sellerFoodItemRepo;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get Seller All Food Items (SELLER)")]
        public async Task<IActionResult> GetSellerAllFoodItems([FromQuery] AllFoodsQuery queryObject)
        {
            try
            {
                var result = await _sellerFoodItemRepo.GetSellerAllFoodItemsAsync(queryObject, GetSellerId());
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error retrieving food items" });
            }
        }

        [HttpGet("{id:int}")]
        [SwaggerOperation(Summary = "Get Seller Food Item by ID (SELLER)")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            try
            {
                var result = await _sellerFoodItemRepo.GetByIdAsync(id, GetSellerId());
                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("create")]
        [SwaggerOperation(Summary = "Create Seller Food Item (SELLER)")]
        public async Task<IActionResult> Create([FromBody] CreateFoodItemDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _sellerFoodItemRepo.CreateAsync(model, GetSellerId());
                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Value!.Id },
                    new
                    {
                        message = "Food item created successfully",
                        id = result.Value.Id,
                        name = result.Value.Name
                    });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }
        [HttpPost("{id}/images")]
        [SwaggerOperation(Summary = "Upload Maximum 5 Food Images (SELLER)")]
        public async Task<IActionResult> UploadFoodImages(
           int id,
           [FromForm] List<IFormFile> images,
           [FromForm] bool setFirstAsMain = true)
        {
            if (images == null || images.Count != 0)
                return BadRequest(new { message = "No images uploaded" });

            // Validate max 5 images
            if (images.Count > 5)
                return BadRequest(new { message = "Maximum 5 images allowed" });
            try
            {
                var result = await _sellerFoodItemRepo.UploadFoodImagesAsync(id, images, setFirstAsMain, GetSellerId());
                if (!result.IsSuccess)
                    return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });
                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading images for item {id}");
                return StatusCode(500, new { message = "Error uploading images" });
            }
        }

        [HttpDelete("images/{imageId}")]
        [SwaggerOperation(Summary = "Delete Food Image By ID (SELLER)")]
        public async Task<IActionResult> DeleteFoodImage(int imageId)
        {
            try
            {
                var result = await _sellerFoodItemRepo.DeleteFoodImageAsync(imageId, GetSellerId());

                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image {imageId}");
                return StatusCode(500, new { message = "Error deleting image" });
            }
        }

        [HttpPatch("images/{imageId}/set-main")]
        [SwaggerOperation(Summary = "Set Main Food Image (SELLER)")]
        public async Task<IActionResult> SetMainFoodImage(int imageId)
        {
            try
            {
                var result = await _sellerFoodItemRepo.SetMainFoodImageAsync(imageId, GetSellerId());
                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting main image {imageId}");
                return StatusCode(500, new { message = "Error updating main image" });
            }
        }

        [HttpPatch("{id}/availability")]
        [SwaggerOperation(Summary = "Toggle Food Availability (SELLER)")]
        public async Task<IActionResult> SetAvailability(int id, [FromBody] bool isAvailable)
        {
            try
            {
                var result = await _sellerFoodItemRepo.SetAvailabilityAsync(id, isAvailable, GetSellerId());

                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting availability for item {id}");
                return StatusCode(500, new { message = "Error updating availability" });
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update Food Item By Food ID [You can update such as name, price, etc] (SELLER)")]
        public async Task<IActionResult> UpdateFoodItem(int id, [FromBody] UpdateFoodItemDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _sellerFoodItemRepo.UpdateFoodItemAsync(id, GetSellerId(), model);
                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating food item {id}");
                return StatusCode(500, new { message = "Error updating food item" });
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete Food Item By FoodId (SELLER)")]
        public async Task<IActionResult> DeleteFoodItem(int id)
        {
            try
            {
                var result = await _sellerFoodItemRepo.DeleteFoodItemAsync(id, GetSellerId());
                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting food item {id}");
                return StatusCode(500, new { message = "Error deleting food item" });
            }
        }
    }
}