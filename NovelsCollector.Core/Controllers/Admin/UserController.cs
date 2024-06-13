using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Core.Models;

namespace NovelsCollector.Core.Controllers.Admin
{
    [Authorize(Roles = "Quản trị viên")]
    [Route("api/v1/user")]
    [Tags("00. Admin")]
    [ApiController]
    public class UserController : ControllerBase
    {
        #region Injected Services

        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        #endregion

        [HttpGet]
        [EndpointSummary("Get a list of all users")]
        public IActionResult Get()
        {
            var users = _userManager.Users.ToList();
            return Ok(users);
        }

    }
}
