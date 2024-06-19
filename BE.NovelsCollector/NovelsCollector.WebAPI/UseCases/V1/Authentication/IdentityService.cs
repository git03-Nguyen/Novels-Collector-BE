using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NovelsCollector.Infrastructure.Identity;
using NovelsCollector.WebAPI.UseCases.V1.Authentication.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NovelsCollector.Infrastructure.Services
{
    public class IdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public IdentityService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<RegisterResponse> CreateUserAsync(RegisterRequest request)
        {
            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null) throw new Exception("Email đã tồn tại");

            var roleExists = await _roleManager.FindByNameAsync(request.Role);
            if (roleExists is null) throw new Exception("Vai trò không tồn tại");

            // Create user
            userExists = new ApplicationUser
            {
                Email = request.Email,
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                UserName = request.Email,
            };

            var createUserResult = await _userManager.CreateAsync(userExists, request.Password);
            if (!createUserResult.Succeeded)
                throw new Exception($"Đăng ký thất bại: {createUserResult?.Errors?.First()?.Description}");

            //user is created...
            //then add user to a role...
            var addUserToRoleResult = await _userManager.AddToRoleAsync(userExists, request.Role);
            if (!addUserToRoleResult.Succeeded)
                throw new Exception($"Đăng ký thành công nhưng không thể gán Vai trò {request.Role}: {addUserToRoleResult?.Errors?.First()?.Description}");

            return new RegisterResponse("Đăng ký thành công", userExists.Email, request.Role);
        }

        public async Task<DeleteResponse> DeleteUserAsync(DeleteRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<LoginResponse> GetTokenAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null) throw new Exception("Email hoặc mật khẩu không đúng");

            var passwordCheck = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordCheck) throw new Exception("Email hoặc mật khẩu không đúng");

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(x => new Claim("role", x));
            claims.AddRange(roleClaims);

            // Create token
            var _SECRET = "FBE2968244C56E34E98dsa_ahahah_dasdasdasd3B5C54E319123";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_SECRET));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(1);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                    );

            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            return new LoginResponse("Đăng nhập thành công", accessToken, user.Email);
        }
    }
}
