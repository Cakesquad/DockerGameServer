using DockerGameServer.Models.Database;
using System.Security.Claims;

namespace DockerGameServer.Services
{
    public class UserContext(IHttpContextAccessor httpContextAccessor)
    {
        public User? GetSignedInUser()
        {
            var isAuthenticated = httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            if (!isAuthenticated)
            {
                return null;
            }

            var user = new User
            {
                Id = Guid.Parse(httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                PasswordHash = string.Empty,
                EmailHash = string.Empty,
                Data = new UserInfo
                {
                    FullName = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "",
                    Email = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? ""
                }
            };

            return user;
        }
    }
}
