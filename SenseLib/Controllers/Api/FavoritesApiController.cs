using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using SenseLib.Services;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SenseLib.Controllers.Api
{
    [Route("api/favorites")]
    [ApiController]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    [EnableCors("AllowAndroid")]
    public class FavoritesApiController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<FavoritesApiController> _logger;
        private readonly DataContext _context;

        public FavoritesApiController(IFavoriteService favoriteService, ILogger<FavoritesApiController> logger, DataContext context)
        {
            _favoriteService = favoriteService;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Ok(new { items = new List<object>() }); // Trả về danh sách rỗng nếu không có người dùng
            }
            var userId = int.Parse(userIdClaim.Value);

            try
            {
                var favoritesQuery = _context.Favorites
                    .Where(f => f.UserID == userId)
                    .Include(f => f.Document)
                        .ThenInclude(d => d.Author)
                    .Include(f => f.Document)
                        .ThenInclude(d => d.Category)
                    .Include(f => f.Document)
                        .ThenInclude(d => d.Ratings)
                    .OrderByDescending(f => f.FavoriteID);

                int totalItems = await favoritesQuery.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var favorites = await favoritesQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    totalItems,
                    totalPages,
                    currentPage = page,
                    pageSize,
                    items = favorites.Select(f => new
                    {
                        id = f.Document.DocumentID,
                        title = f.Document.Title,
                        description = f.Document.Description,
                        coverImageUrl = f.Document.ImagePath,
                        authorName = f.Document.Author?.AuthorName,
                        categoryName = f.Document.Category?.CategoryName,
                        uploadDate = f.Document.UploadDate,
                        price = f.Document.Price,
                        isPaid = f.Document.IsPaid,
                        averageRating = f.Document.Ratings.Any() ? f.Document.Ratings.Average(r => r.RatingValue) : 0,
                        ratingCount = f.Document.Ratings.Count()
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách yêu thích của người dùng {UserId}", userId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpPost("toggle")]
        public async Task<IActionResult> ToggleFavorite([FromBody] FavoriteRequest request)
        {
            // Log request body để debug
            _logger.LogInformation("Nhận request toggle favorite với body: {RequestBody}", 
                JsonSerializer.Serialize(request));

            // Log thông tin header để debug
            foreach (var header in Request.Headers)
            {
                _logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Không tìm thấy thông tin người dùng.");
            }

            var userId = int.Parse(userIdClaim.Value);
            var documentId = request.DocumentId;

            // Log các thông tin quan trọng
            _logger.LogInformation("Toggle favorite - UserId: {UserId}, DocumentId: {DocumentId}", 
                userId, documentId);

            try
            {
                var newFavoriteState = await _favoriteService.ToggleFavorite(userId, documentId);
                _logger.LogInformation("User {UserId} toggled favorite status for document {DocumentId} to {Status}", userId, documentId, newFavoriteState);
                return Ok(new { isFavorite = newFavoriteState });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thay đổi trạng thái yêu thích cho tài liệu {DocumentId} của người dùng {UserId}", documentId, userId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }
    }

    public class FavoriteRequest
    {
        // Thêm thuộc tính JsonPropertyName để đảm bảo binding đúng
        public int DocumentId { get; set; }
    }
}