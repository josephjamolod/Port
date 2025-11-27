using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Admin;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminRepository _adminRepo;
        private readonly ILogger<AdminController> _logger;
        public AdminController(IAdminRepository adminRepo, ILogger<AdminController> logger)
        {
            _adminRepo = adminRepo;
            _logger = logger;
        }

        [HttpPost("approve")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Approve Seller registered account(ADMIN)")]
        public async Task<IActionResult> ApproveSeller([FromBody] SellerApprovalDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _adminRepo.ApproveSellerAsync(model, adminId!);
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });
                _logger.LogInformation($"Seller approved by admin {adminId}");
                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get all pending sellers(ADMIN)")]
        public async Task<IActionResult> GetPendingSellers([FromQuery] PendingSellerQueryObj queryObject)
        {
            try
            {
                var result = await _adminRepo.GetPendingSellersAsync(queryObject);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error retrieving sellers" });
            }
        }
    }
}