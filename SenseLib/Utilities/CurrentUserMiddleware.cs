using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;

namespace SenseLib.Utilities
{
    public class CurrentUserMiddleware
    {
        private readonly RequestDelegate _next;

        public CurrentUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DataContext dbContext)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserID == userId);
                    
                    if (user != null)
                    {
                        // Thêm thông tin người dùng vào HttpContext.Items
                        context.Items["CurrentUser"] = user;
                        
                        // Lấy thông tin ví của người dùng
                        var wallet = await dbContext.Wallets.FirstOrDefaultAsync(w => w.UserID == userId);
                        if (wallet != null)
                        {
                            context.Items["CurrentUserWallet"] = wallet;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
} 