using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Foods;
using JwtAuthApi.Dtos.Seller;
using JwtAuthApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Seller")]
    public class SellerProfileController : ControllerBase
    {
        private readonly ISellerProfileRepository _sellerRepo;
        private readonly ILogger<SellerProfileController> _logger;
        private string GetSellerId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public SellerProfileController(ISellerProfileRepository sellerRepo, ILogger<SellerProfileController> logger)
        {
            _sellerRepo = sellerRepo;
            _logger = logger;
        }

        [HttpGet("profile")]
        [SwaggerOperation(Summary = "Get Seller Profile Info (SELLER)")]
        public async Task<IActionResult> GetSellerProfile()
        {
            try
            {
                var sellerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _sellerRepo.GetSellerProfileAsync(sellerId!);

                if (!result.IsSuccess)
                    return NotFound(result.Error);

                return Ok(new { result.Value });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error retrieving profile" });
            }
        }

        [HttpPost("profile-update")]
        [SwaggerOperation(Summary = "Update Seller Profile Info (SELLER)")]
        public async Task<IActionResult> UpdateSeller([FromBody] UpdateSellerProfileDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _sellerRepo.UpdateSellerAsync(model, GetSellerId());
                if (!result.IsSuccess)
                    return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });
                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error updating profile" });
            }
        }

        [HttpPatch("toggle-status")]
        [SwaggerOperation(Summary = "Toggle Seller Active Status (SELLER)")]
        public async Task<IActionResult> ToggleStatus()
        {
            try
            {
                var result = await _sellerRepo.ToggleStatusAsync(GetSellerId());
                if (!result.IsSuccess)
                    return NotFound(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error updating status" });
            }
        }

        //todo: upload logo
        [HttpPost("logo")]
        [SwaggerOperation(Summary = "Upload Logo for Seller endpoint (SELLER)")]

        public async Task<IActionResult> UploadLogo([FromForm] UploadLogoDto model)
        {
            var logo = model.File;
            if (logo == null || logo.Length == 0)
                return BadRequest(new { message = "No file uploaded" });
            try
            {
                var result = await _sellerRepo.UploadLogoAsync(logo, GetSellerId());
                if (!result.IsSuccess)
                    return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error uploading logo" });
            }
        }
    }
}