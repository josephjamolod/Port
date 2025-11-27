using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JwtAuthApi.Data;
using JwtAuthApi.Dtos;
using JwtAuthApi.Dtos.Auth;
using JwtAuthApi.Dtos.Seller;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        private readonly IAuthRepository _authRepo;
        public AuthController(
            ILogger<AuthController> logger,
            IAuthRepository authRepo
            )
        {
            _logger = logger;
            _authRepo = authRepo;
        }
        [HttpPost("register-seller")]
        [SwaggerOperation(Summary = "Register As Seller (Seller)")]
        public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.RegisterSellerAsync(model, GenerateConfirmationLink);
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });
                _logger.LogInformation($"New seller registered: {model.BusinessName}");

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }
        [HttpPost("register")]
        [SwaggerOperation(Summary = "Create Customer Account (User)")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.CreateUserAsync(model, GenerateConfirmationLink);

                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });
                _logger.LogInformation($"User '{model.Username}' registered successfully");

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpGet("confirm-email")]
        [SwaggerOperation(Summary = "Confirm Email After User Registration (User)")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] ConfirmEmailDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.ConfirmEmailAsync(model);

                if (result.IsSuccess)
                    return Ok(result.Value);

                return BadRequest(new { message = result.Error });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("resend-confirmation")]
        [SwaggerOperation(Summary = "Resend email confirmation link (User)")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.ResendEmailConfirmationAsync(model, GenerateConfirmationLink);
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });

                var user = result.Value;

                if (user == null)
                    return Ok(new { messsage = "If the email exists, a confirmation link has been sent" });

                return Ok(new
                {
                    message = "Confirmation email has been resent",
                    canResendAgainIn = "2 minutes"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login an account (ALL USER)")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.LoginAsync(model);
                if (!result.IsSuccess)
                    return Unauthorized(new { message = result.Error });

                var user = result.Value!;
                // Check if 2FA is enabled for this user
                if (user.TwoFactorEnabled)
                {
                    _logger.LogInformation($"2FA code sent to {model.Email}");
                    return Ok(new AuthResponseDto
                    {
                        RequiresTwoFactor = true,
                        Username = user.UserName!,
                        Email = user.Email!
                    });
                }

                var authResponse = await _authRepo.SaveRefreshToken(user, GetIpAddress());
                _logger.LogInformation($"User '{model.Email}' logged in successfully from {GetIpAddress()}");

                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during login for user: {model.Email}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }


        [HttpPost("verify-2fa")]
        [SwaggerOperation(Summary = "Verify 2fa and complete the login process (ALL USER)")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.Verify2FAAsync(model.Username, model.Code);
                if (!result.IsSuccess)
                    return Unauthorized(new { message = result.Error });

                var user = result.Value!;

                // Generate tokens
                var authResponse = await _authRepo.SaveRefreshToken(user, GetIpAddress());

                _logger.LogInformation($"2FA verification successful for user '{model.Username}'");

                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during login for user: {model.Username}");
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        // RESEND 2FA CODE ENDPOINT 
        [HttpPost("resend-2fa-code")]
        [SwaggerOperation(Summary = "RESEND 2FA CODE ENDPOINT  (ALL USER)")]
        public async Task<IActionResult> Resend2FACode([FromBody] Resend2FACodeDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.Resend2FACodeAsync(model.Username);
                if (!result.IsSuccess)
                    return Unauthorized(new { message = result.Error });

                var user = result.Value;

                if (user == null)
                    return Ok(new { message = "If your account has 2FA enabled, a new code has been sent" });

                // await SendNew2FACodeAsync(user);
                _logger.LogInformation($"2FA code resent to {model.Username}");

                return Ok(new
                {
                    message = "A new verification code has been sent to your email",
                    expiresIn = "5 minutes"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("enable-2fa")]
        [Authorize]
        [SwaggerOperation(Summary = "ENABLE 2FA ENDPOINT  (ALL USER)")]
        public async Task<IActionResult> Enable2FA()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                var result = await _authRepo.Enable2FAAsync(userId);
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });
                var user = result.Value;

                if (user == null)
                    return NotFound(new { message = "User not found" });

                _logger.LogInformation($"2FA enabled for user '{user.UserName}'");

                return Ok(new
                {
                    message = "Two-factor authentication has been enabled successfully"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("disable-2fa")]
        [Authorize]
        [SwaggerOperation(Summary = "DISABLE 2FA ENDPOINT  (ALL USER)")]
        public async Task<IActionResult> Disable2FA()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid authentication token" });

                var result = await _authRepo.Disable2FAAsync(userId);
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });
                var user = result.Value;

                if (user == null)
                    return NotFound(new { message = "User not found" });

                _logger.LogInformation($"2FA disabled for user '{user.UserName}'");
                return Ok(new
                {
                    message = "Two-factor authentication has been disabled successfully"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("refresh")]
        [SwaggerOperation(Summary = "Refresh expired access token using refresh token  (ALL USER)")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var token = await _authRepo.RefreshTokenAsync(model.RefreshToken);
                if (token == null || !token.IsActive)
                {
                    _logger.LogWarning("Invalid refresh token attempt");
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                // Generate new tokens
                var authResponse = await _authRepo.SaveRefreshToken(token.User, GetIpAddress(), token);
                _logger.LogInformation($"Token refreshed for user '{token.User.UserName}'");

                return Ok(authResponse);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("revoke")]
        [SwaggerOperation(Summary = "Logout User/Revoke a token  (ALL USER)")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.RevokeTokenAsync(model.RefreshToken, GetIpAddress());
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });

                return Ok(result.Value);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("forgot-password")]
        [SwaggerOperation(Summary = "Request password reset(ALL USER)")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var resetToken = await _authRepo.ForgotPasswordAsync(model.Email);
                if (resetToken == null)
                    return Ok(new
                    {
                        message = "If the email exists, a password reset link has been sent"
                    });

                _logger.LogInformation($"Password reset requested for email: {model.Email}");
                return Ok(new
                {
                    message = "  password reset link has been sent"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(Summary = "Reset password with token(ALL USER)")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _authRepo.ResetPasswordAsync(model);
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Error });

                return Ok(new
                {
                    message = result.Value
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred. Please try again." });
            }
        }

        // HELPER METHODS
        private async Task<string?> GenerateConfirmationLink(AppUser user, string token)
        {
            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Auth",
                new { userId = user.Id, token },
                Request.Scheme
            );

            return confirmationLink;
        }

        private string GetIpAddress()
        {
            // Check for forwarded IP (if behind proxy/load balancer)
            if (Request.Headers.TryGetValue("X-Forwarded-For", out Microsoft.Extensions.Primitives.StringValues value))
                return value!;

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

    }
}