using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.User;
using JwtAuthApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfile : ControllerBase
    {
        private readonly IUserProfileRepository _userRepo;
        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        public UserProfile(IUserProfileRepository userRepo)
        {
            _userRepo = userRepo;
        }
        [HttpGet("profile")]
        [SwaggerOperation(Summary = "Get Current User Profile (All User)")]

        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var result = await _userRepo.GetUserProfileAsync(GetUserId());

                if (!result.IsSuccess)
                    return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error retrieving profile" });
            }
        }

        [HttpPost("update-profile")]
        [SwaggerOperation(Summary = "Update Current User Profile (All User)")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _userRepo.UpdateProfileAsync(GetUserId(), model);
                if (!result.IsSuccess)
                    return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error updating profile" });
            }
        }

        [HttpDelete]
        [SwaggerOperation(Summary = "Delete Current User Account (All User)")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (model.ConfirmationText != "DELETE")
                return BadRequest(new
                {
                    message = "Please type 'DELETE' to confirm account deletion"
                });
            try
            {
                var result = await _userRepo.DeleteAccountAsync(GetUserId(), model);
                if (!result.IsSuccess)
                    return StatusCode(result.Error!.ErrCode, new { message = result.Error.ErrDescription });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error updating profile" });
            }
        }
    }
}