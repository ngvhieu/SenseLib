using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SenseLib.Models;

namespace SenseLib.Services
{
    public interface IFavoriteService
    {
        Task<bool> IsFavorite(int userId, int documentId);
        Task<bool> AddFavorite(int userId, int documentId);
        Task<bool> RemoveFavorite(int userId, int documentId);
        Task<bool> ToggleFavorite(int userId, int documentId);
        Task<List<Document>> GetUserFavorites(int userId, int page, int pageSize);
        Task<int> GetUserFavoritesCount(int userId);
    }
    
    public class FavoriteService : IFavoriteService
    {
        private readonly DataContext _context;
        private readonly ILogger<FavoriteService> _logger;
        
        public FavoriteService(DataContext context, ILogger<FavoriteService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        // Kiểm tra xem tài liệu có được yêu thích bởi người dùng hay không
        public async Task<bool> IsFavorite(int userId, int documentId)
        {
            try
            {
                return await _context.Favorites
                    .AnyAsync(f => f.UserID == userId && f.DocumentID == documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái yêu thích. UserId: {UserId}, DocumentId: {DocumentId}", userId, documentId);
                throw new Exception("Không thể kiểm tra trạng thái yêu thích", ex);
            }
        }
        
        // Thêm một tài liệu vào danh sách yêu thích
        public async Task<bool> AddFavorite(int userId, int documentId)
        {
            try
            {
                _logger.LogInformation("Bắt đầu thêm yêu thích - UserId: {UserId}, DocumentId: {DocumentId}", userId, documentId);
                
                // Kiểm tra xem đã tồn tại chưa
                var exists = await IsFavorite(userId, documentId);
                if (exists)
                {
                    _logger.LogInformation("Tài liệu {DocumentId} đã được yêu thích bởi người dùng {UserId}", documentId, userId);
                    return true; // Đã tồn tại, không cần thêm nữa
                }
                
                // Kiểm tra tài liệu có tồn tại không
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Không thể thêm yêu thích - Tài liệu {DocumentId} không tồn tại", documentId);
                    return false; // Tài liệu không tồn tại
                }
                
                // Kiểm tra người dùng có tồn tại không
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Không thể thêm yêu thích - Người dùng {UserId} không tồn tại", userId);
                    return false; // Người dùng không tồn tại
                }
                
                // Log trạng thái DbContext trước khi tạo đối tượng Favorite
                _logger.LogDebug("Trạng thái DbContext trước khi thêm: CanConnect={CanConnect}", _context.Database.CanConnect());
                
                // Thử thêm trực tiếp bằng SQL trước
                try
                {
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = $"IF NOT EXISTS (SELECT 1 FROM Favorites WHERE UserID = {userId} AND DocumentID = {documentId}) " +
                                            $"INSERT INTO Favorites (UserID, DocumentID) VALUES ({userId}, {documentId})";
                        
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                            await command.Connection.OpenAsync();
                            
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        _logger.LogInformation("Thêm yêu thích bằng SQL trực tiếp: {Result} rows affected", rowsAffected);
                        
                        // Kiểm tra xem đã tồn tại chưa
                        command.CommandText = $"SELECT COUNT(1) FROM Favorites WHERE UserID = {userId} AND DocumentID = {documentId}";
                        var result = await command.ExecuteScalarAsync();
                        var count = Convert.ToInt32(result);
                        
                        if (count > 0)
                        {
                            _logger.LogInformation("Đã thêm yêu thích thành công bằng SQL trực tiếp");
                            return true;
                        }
                    }
                }
                catch (Exception sqlEx)
                {
                    _logger.LogWarning(sqlEx, "Không thể thêm yêu thích bằng SQL trực tiếp: {Message}. Thử bằng EF Core", sqlEx.Message);
                    // Tiếp tục thực hiện bằng Entity Framework
                }
                
                // Tạo đối tượng yêu thích mới
                var favorite = new Favorite
                {
                    UserID = userId,
                    DocumentID = documentId
                };
                
                _logger.LogDebug("Đã tạo đối tượng Favorite - UserID={UserID}, DocumentID={DocumentID}", favorite.UserID, favorite.DocumentID);
                
                // Thêm vào database và lưu thay đổi
                _context.Favorites.Add(favorite);
                
                // Theo dõi thay đổi trước khi lưu
                var entries = _context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();
                _logger.LogDebug("Số lượng entities đang được theo dõi để thêm: {Count}", entries.Count);
                
                try
                {
                    var rowsAffected = await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("SaveChanges đã hoàn thành - Rows affected: {RowsAffected}", rowsAffected);
                    
                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Đã thêm tài liệu {DocumentId} vào yêu thích của người dùng {UserId}", documentId, userId);
                    }
                    else
                    {
                        _logger.LogWarning("Không có thay đổi nào được lưu khi thêm tài liệu {DocumentId} vào yêu thích", documentId);
                    }
                    
                    // Xác thực trực tiếp bằng SQL thuần
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(1) FROM Favorites WHERE UserID = {userId} AND DocumentID = {documentId}";
                        
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                            await command.Connection.OpenAsync();
                            
                        var result = await command.ExecuteScalarAsync();
                        var count = Convert.ToInt32(result);
                        
                        _logger.LogInformation("Kiểm tra SQL trực tiếp - Số bản ghi tìm thấy: {Count}", count);
                        
                        return count > 0;
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "DbUpdateException khi lưu yêu thích - Message: {Message}", dbEx.Message);
                    
                    if (dbEx.InnerException != null)
                    {
                        _logger.LogError(dbEx.InnerException, "Inner exception: {Message}", dbEx.InnerException.Message);
                    }
                    
                    // Cố gắng thực hiện thêm bằng SQL thuần
                    try
                    {
                        _logger.LogWarning("Thử thêm bằng SQL thuần sau khi EF Core thất bại");
                        using (var command = _context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = $"IF NOT EXISTS (SELECT 1 FROM Favorites WHERE UserID = {userId} AND DocumentID = {documentId}) " +
                                                $"INSERT INTO Favorites (UserID, DocumentID) VALUES ({userId}, {documentId})";
                            
                            if (command.Connection.State != System.Data.ConnectionState.Open)
                                await command.Connection.OpenAsync();
                                
                            var result = await command.ExecuteNonQueryAsync();
                            _logger.LogInformation("Kết quả thêm bằng SQL thuần: {Result} rows affected", result);
                            
                            // Kiểm tra xem đã tồn tại chưa
                            command.CommandText = $"SELECT COUNT(1) FROM Favorites WHERE UserID = {userId} AND DocumentID = {documentId}";
                            var checkResult = await command.ExecuteScalarAsync();
                            var count = Convert.ToInt32(checkResult);
                            
                            return count > 0;
                        }
                    }
                    catch (Exception sqlEx)
                    {
                        _logger.LogError(sqlEx, "Lỗi khi thêm bằng SQL thuần: {Message}", sqlEx.Message);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm tài liệu vào yêu thích. UserId: {UserId}, DocumentId: {DocumentId}", userId, documentId);
                throw new Exception("Không thể thêm tài liệu vào yêu thích", ex);
            }
        }
        
        // Xóa một tài liệu khỏi danh sách yêu thích
        public async Task<bool> RemoveFavorite(int userId, int documentId)
        {
            try
            {
                // Tìm bản ghi yêu thích
                var favorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserID == userId && f.DocumentID == documentId);
                    
                if (favorite == null)
                {
                    _logger.LogInformation("Tài liệu {DocumentId} không có trong yêu thích của người dùng {UserId}", documentId, userId);
                    return true; // Không tồn tại, coi như đã xóa thành công
                }
                
                // Xóa bản ghi
                _context.Favorites.Remove(favorite);
                var rowsAffected = await _context.SaveChangesAsync();
                
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Đã xóa tài liệu {DocumentId} khỏi yêu thích của người dùng {UserId}", documentId, userId);
                }
                else
                {
                    _logger.LogWarning("Không có thay đổi nào được lưu khi xóa tài liệu {DocumentId} khỏi yêu thích", documentId);
                }
                
                // Kiểm tra xem đã xóa thành công chưa
                return !(await IsFavorite(userId, documentId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa tài liệu khỏi yêu thích. UserId: {UserId}, DocumentId: {DocumentId}", userId, documentId);
                throw new Exception("Không thể xóa tài liệu khỏi yêu thích", ex);
            }
        }
        
        // Thêm/xóa yêu thích (toggle)
        public async Task<bool> ToggleFavorite(int userId, int documentId)
        {
            try
            {
                // Kiểm tra trạng thái hiện tại
                var isFavorite = await IsFavorite(userId, documentId);
                _logger.LogInformation("ToggleFavorite - Trạng thái hiện tại: {IsFavorite} cho UserId: {UserId}, DocumentId: {DocumentId}", 
                    isFavorite ? "Đã yêu thích" : "Chưa yêu thích", userId, documentId);
                
                bool result;
                if (isFavorite)
                {
                    // Nếu đã yêu thích, xóa bỏ
                    result = await RemoveFavorite(userId, documentId);
                    _logger.LogInformation("Đã xóa yêu thích - Kết quả: {Result}", result ? "Thành công" : "Thất bại");
                    return false; // Trả về false vì đã xóa
                }
                else
                {
                    // Nếu chưa yêu thích, thêm vào
                    result = await AddFavorite(userId, documentId);
                    _logger.LogInformation("Đã thêm yêu thích - Kết quả: {Result}", result ? "Thành công" : "Thất bại");
                    return result; // Trả về true nếu thêm thành công
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi toggle yêu thích. UserId: {UserId}, DocumentId: {DocumentId}", userId, documentId);
                throw;
            }
        }
        
        // Lấy danh sách tài liệu yêu thích của người dùng
        public async Task<List<Document>> GetUserFavorites(int userId, int page, int pageSize)
        {
            try
            {
                _logger.LogInformation("Bắt đầu GetUserFavorites - UserId: {UserId}, Page: {Page}, PageSize: {PageSize}", 
                    userId, page, pageSize);
                
                var favorites = await _context.Favorites
                    .Where(f => f.UserID == userId)
                    .Join(_context.Documents,
                        f => f.DocumentID,
                        d => d.DocumentID,
                        (f, d) => d)
                    .Where(d => d.Status == "Published")
                    .Include(d => d.Author)
                    .Include(d => d.Category)
                    .Include(d => d.Publisher)
                    .Include(d => d.Statistics)
                    .Include(d => d.Downloads)
                    .Include(d => d.Favorites)
                    .OrderByDescending(d => d.UploadDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                _logger.LogInformation("Đã lấy {Count} tài liệu yêu thích của người dùng {UserId} (trang {Page})", favorites.Count, userId, page);
                
                // Kiểm tra trực tiếp xem có bản ghi yêu thích nào không
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(1) FROM Favorites WHERE UserID = {userId}";
                    
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();
                        
                    var result = await command.ExecuteScalarAsync();
                    var count = Convert.ToInt32(result);
                    
                    _logger.LogInformation("Kiểm tra SQL trực tiếp - Tổng số yêu thích của người dùng {UserId}: {Count}", userId, count);
                }
                
                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách yêu thích. UserId: {UserId}, Page: {Page}", userId, page);
                throw new Exception("Không thể lấy danh sách tài liệu yêu thích", ex);
            }
        }
        
        // Đếm tổng số tài liệu yêu thích của người dùng
        public async Task<int> GetUserFavoritesCount(int userId)
        {
            try
            {
                var count = await _context.Favorites
                    .Where(f => f.UserID == userId)
                    .Join(_context.Documents,
                        f => f.DocumentID,
                        d => d.DocumentID,
                        (f, d) => d)
                    .Where(d => d.Status == "Published")
                    .CountAsync();
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đếm số tài liệu yêu thích. UserId: {UserId}", userId);
                throw new Exception("Không thể đếm số lượng tài liệu yêu thích", ex);
            }
        }
    }
} 