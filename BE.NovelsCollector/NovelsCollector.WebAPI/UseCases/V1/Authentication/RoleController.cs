using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NovelsCollector.Domain.Constants;
using NovelsCollector.Infrastructure.Identity;

namespace NovelsCollector.WebAPI.UseCases.V1.Authentication
{
    [Authorize(Roles = Roles.Administrator)]
    [Tags("00. Admin")]
    [Route("api/v1/role")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        #region Injected Services

        private readonly RoleManager<ApplicationRole> _roleManager;

        public RoleController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        #endregion
        [HttpGet]
        public IActionResult Get()
        {
            var roles = _roleManager.Roles.Select(x => x.Name).ToList();
            return Ok(roles);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Create([FromBody] string role)
        {
            var appRole = new ApplicationRole { Name = role };
            var createRole = await _roleManager.CreateAsync(appRole);
            if (!createRole.Succeeded) throw new BadHttpRequestException("Tạo Vai trò thất bại");

            return Ok(new { message = "Tạo Vai trò thành công" });
        }
    }
}
