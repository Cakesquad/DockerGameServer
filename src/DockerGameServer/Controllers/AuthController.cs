using DockerGameServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DockerGameServer.Controllers
{
    public record LoginUIResponse(bool success, string errorMessage = "");

    [ApiController]
    [Route("[controller]")]
    public class AuthController(UserService userService) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginModel model)
        {
            var user = await userService.LoginAsync(model);
            if (user == null)
            {
                return Ok(new LoginUIResponse(false, "Invalid email or password."));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Data.FullName),
                new Claim(ClaimTypes.Email, user.Data.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            return Ok(new LoginUIResponse(true));
        }

        [HttpGet("logout")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }

    }
}
