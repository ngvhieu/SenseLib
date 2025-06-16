using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SenseLib.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SenseLib.Controllers.Api
{
    [Route("api/user")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
    public class ProfileApiController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<ProfileApiController> _logger;
        private readonly IWebHostEnvironment _env;

        public ProfileApiController(DataContext context, ILogger<ProfileApiController> logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        // GET api/user/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }
            return Ok(MapUserDto(user));
        }

        // PUT api/user/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "Người dùng không tồn tại" });

            user.FullName = model.FullName ?? user.FullName;
            user.Email = model.Email ?? user.Email;
            await _context.SaveChangesAsync();
            return Ok(MapUserDto(user));
        }

        public class UpdateProfileRequest
        {
            public string FullName { get; set; }
            public string Email { get; set; }
        }

        // POST api/user/avatar
        [HttpPost("avatar")]
        [RequestSizeLimit(5_242_880)] // 5MB
        public async Task<IActionResult> UpdateAvatar([FromForm] IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(new { message = "Không có tệp ảnh" });
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "Người dùng không tồn tại" });

            var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "profiles");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var fileExt = Path.GetExtension(avatar.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            // Xóa file cũ (nếu không phải mặc định)
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                var oldPath = Path.Combine(uploadsDir, user.ProfileImage);
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { }
                }
            }

            user.ProfileImage = fileName;
            await _context.SaveChangesAsync();
            return Ok(MapUserDto(user));
        }

        private object MapUserDto(User user)
        {
            return new
            {
                id = user.UserID,
                fullName = user.FullName,
                email = user.Email,
                avatarUrl = string.IsNullOrEmpty(user.ProfileImage) ? null : $"/uploads/profiles/{user.ProfileImage}"
            };
        }
    }
} 