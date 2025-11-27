using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Cart;
using JwtAuthApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartRepository _cartRepo;
        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public CartController(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }
        [HttpPost("add")]
        [SwaggerOperation(Summary = "Add an Item to cart (ALL USER)")]
        public async Task<ActionResult<CartItemDto>> AddToCart(AddToCartRequest model)
        {
            try
            {
                var result = await _cartRepo.AddToCartAsync(model, GetUserId());
                if (!result.IsSuccess)
                    return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error adding item to cart" });
            }
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get User Cart Items (ALL USER)")]
        public async Task<ActionResult<CartResponseDto>> GetCart()
        {
            var result = await _cartRepo.GetCartAsync(GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpPut("items/{id}")]
        [SwaggerOperation(Summary = "Update a Cart Item (ALL USER)")]
        public async Task<IActionResult> UpdateCartItem(int id, UpdateCartItemRequest request)
        {
            if (request.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0" });

            var result = await _cartRepo.UpdateCartItemAsync(id, GetUserId(), request);
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpDelete("items/{id}")]
        [SwaggerOperation(Summary = "Remove a single Item in Cart (ALL USER)")]
        public async Task<IActionResult> RemoveItemFromCart(int id)
        {
            var result = await _cartRepo.RemoveItemFromCartAsync(id, GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpDelete]
        [SwaggerOperation(Summary = "Clear Cart Items (ALL USER)")]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _cartRepo.ClearCartAsync(GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpDelete("seller/{sellerId}")]
        [SwaggerOperation(Summary = "Allows CUSTOMERS to remove all items from a specific seller (ALL USER)")]
        public async Task<IActionResult> ClearSellerCart(string sellerId)
        {
            var result = await _cartRepo.ClearSellerCartAsync(sellerId, GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpPost("validate/seller/{sellerId}")]
        [SwaggerOperation(Summary = " Validate Items In Seller Cart (ALL USER)")]
        public async Task<ActionResult<CartValidationResult>> ValidateItemsInSellerCart(string sellerId)
        {
            var result = await _cartRepo.ValidateItemsInSellerCartAsync(sellerId, GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }

        [HttpPost("validate")]
        [SwaggerOperation(Summary = " Validate All Items (ALL USER)")]
        public async Task<ActionResult<CartValidationResult>> ValidateCart()
        {
            var result = await _cartRepo.ValidateAllCartItemsAsync(GetUserId());
            if (!result.IsSuccess)
                return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

            return Ok(result.Value);
        }
    }
}