using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SenseLib.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SenseLib.Controllers.Api
{
    [ApiController]
    [Route("api/documents")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public class DocumentsApiController : ControllerBase
    {
        private readonly DataContext _context;
        public DocumentsApiController(DataContext context)
        {
            _context = context;
        }

        // GET api/documents/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyDocuments(int page = 1, int pageSize = 20)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Người dùng không hợp lệ." });
            }

            var query = _context.Documents
                .Where(d => d.UserID == userId)
                .OrderByDescending(d => d.UploadDate);

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var docs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new {
                    id = d.DocumentID,
                    title = d.Title,
                    description = d.Description,
                    coverImageUrl = d.ImagePath,
                    price = d.Price,
                    isPaid = d.IsPaid,
                    status = d.Status,
                    categoryName = d.Category != null ? d.Category.CategoryName : null,
                    uploadDate = d.UploadDate
                })
                .ToListAsync();

            return Ok(new {
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                items = docs
            });
        }
    }
} 