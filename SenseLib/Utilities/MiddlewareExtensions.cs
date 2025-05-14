using Microsoft.AspNetCore.Builder;

namespace SenseLib.Utilities
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseCurrentUser(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CurrentUserMiddleware>();
        }
    }
} 