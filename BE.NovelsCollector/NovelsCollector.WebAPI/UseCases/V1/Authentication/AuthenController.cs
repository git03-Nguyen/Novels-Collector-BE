using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Domain.Constants;
using NovelsCollector.Infrastructure.Services;
using NovelsCollector.WebAPI.UseCases.V1.Authentication.Models;

namespace NovelsCollector.WebAPI.UseCases.V1.Authentication
{
    [Authorize(Roles = Roles.Administrator)]
    [Route("api/v1/auth")]
    [Tags("00. Admin")]
    [ApiController]
    public class AuthenController : ControllerBase
    {
        #region Injected Services
        private readonly IdentityService _identityService;

        public AuthenController(IdentityService identityService) => _identityService = identityService;
        #endregion

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request) =>
            Ok(await _identityService.CreateUserAsync(request));

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request) =>
            Ok(await _identityService.GetTokenAsync(request));

    }
}
