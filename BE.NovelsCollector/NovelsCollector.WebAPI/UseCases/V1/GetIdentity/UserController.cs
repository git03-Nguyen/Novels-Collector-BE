using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Infrastructure.Persistence.Entities;
using System.Security.Claims;

namespace NovelsCollector.WebAPI.UseCases.V1.GetIdentity
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

            return Ok(new { data = users });
        }

        [HttpDelete("delete/{id}")]
        [EndpointSummary("Delete a user")]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) throw new Exception("Người dùng không tồn tại");

            // Cannot delete the own account
            var email = user.Email;
            ClaimsPrincipal currentUser = User;
            var currentUserEmail = currentUser.FindFirst(ClaimTypes.Email).Value;
            if (currentUserEmail == email) throw new BadHttpRequestException("Không thể xóa tài khoản của chính mình");

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded) throw new Exception("Xóa người dùng thất bại");

            return Ok(new { message = "Xóa người dùng thành công" });
        }

    }
}
