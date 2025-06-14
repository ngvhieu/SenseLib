using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SenseLib.Models;
using Microsoft.Extensions.Logging;

namespace SenseLib.Services
{
    public class WalletService
    {
        private readonly DataContext _context;
        private readonly IOptions<SystemConfig> _systemConfig;
        private readonly ILogger<WalletService> _logger;
        private readonly UserActivityService _userActivityService;

        public WalletService(DataContext context, IOptions<SystemConfig> systemConfig, ILogger<WalletService> logger, UserActivityService userActivityService)
        {
            _context = context;
            _systemConfig = systemConfig;
            _logger = logger;
            _userActivityService = userActivityService;
        }

        // Tạo ví mới cho người dùng
        public async Task<Wallet> CreateWalletAsync(int userId)
        {
            // Kiểm tra xem người dùng đã có ví chưa
            var existingWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserID == userId);
            if (existingWallet != null)
            {
                return existingWallet;
            }

            // Tạo ví mới
            var now = DateTime.Now;
            var wallet = new Wallet
            {
                UserID = userId,
                Balance = 0,
                CreatedDate = now,
                LastUpdatedDate = now
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        // Xử lý thanh toán - chuyển tiền vào ví người tải lên tài liệu
        public async Task ProcessPurchasePaymentAsync(Purchase purchase)
        {
            try
            {
                // Chỉ xử lý các giao dịch thành công
                if (purchase.Status != "Completed")
                {
                    return;
                }

                // Lấy thông tin tài liệu và người tạo
                var document = await _context.Documents
                    .Include(d => d.Author)
                    .FirstOrDefaultAsync(d => d.DocumentID == purchase.DocumentID);

                if (document == null)
                {
                    throw new Exception($"Không tìm thấy tài liệu ID: {purchase.DocumentID}");
                }

                // Lấy ID của người dùng đã đăng tải tài liệu
                // Document có trường UserID để lưu người đã tải lên
                if (!document.UserID.HasValue)
                {
                    Console.WriteLine($"Tài liệu ID {document.DocumentID} không có thông tin người tải lên.");
                    return;
                }

                var uploaderId = document.UserID.Value;

                // Nếu không có người đăng tải, không xử lý
                if (uploaderId <= 0)
                {
                    return;
                }

                // Lấy % hoa hồng cho người đăng tải từ cấu hình hệ thống, mặc định là 80%
                var commissionPercent = 80m;
                var commissionConfig = await _context.SystemConfigs
                    .FirstOrDefaultAsync(c => c.ConfigKey == "AuthorCommissionPercent");

                if (commissionConfig != null && decimal.TryParse(commissionConfig.ConfigValue, out var configValue))
                {
                    commissionPercent = configValue;
                }

                // Tính số tiền người đăng tải nhận được
                var commissionAmount = purchase.Amount * (commissionPercent / 100);

                // Lấy ví của người đăng tải
                var uploaderWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserID == uploaderId);
                
                // Nếu chưa có ví, tạo ví mới
                if (uploaderWallet == null)
                {
                    uploaderWallet = await CreateWalletAsync(uploaderId);
                }

                // Cập nhật số dư ví
                uploaderWallet.Balance += commissionAmount;
                uploaderWallet.LastUpdatedDate = DateTime.Now;

                // Tạo giao dịch ví
                var walletTransaction = new WalletTransaction
                {
                    WalletID = uploaderWallet.WalletID,
                    Amount = commissionAmount,
                    TransactionDate = DateTime.Now,
                    Type = "Credit",
                    Description = $"Nhận tiền từ việc bán tài liệu: {document.Title}",
                    DocumentID = document.DocumentID,
                    PurchaseID = purchase.PurchaseID
                };

                _context.WalletTransactions.Add(walletTransaction);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.WriteLine($"Lỗi khi xử lý thanh toán: {ex.Message}");
                throw;
            }
        }

        // Lấy thông tin ví của người dùng
        public async Task<Wallet> GetWalletAsync(int userId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserID == userId);

            // Nếu chưa có ví, tự động tạo mới
            if (wallet == null)
            {
                wallet = await CreateWalletAsync(userId);
            }

            return wallet;
        }

        // Lấy lịch sử giao dịch của ví
        public async Task<List<WalletTransaction>> GetTransactionsAsync(int walletId, int page = 1, int pageSize = 10)
        {
            return await _context.WalletTransactions
                .Where(t => t.WalletID == walletId)
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Thực hiện rút tiền (cần phát triển thêm)
        public async Task<bool> WithdrawAsync(int walletId, decimal amount, string description)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            
            if (wallet == null || wallet.Balance < amount)
            {
                return false;
            }

            wallet.Balance -= amount;
            wallet.LastUpdatedDate = DateTime.Now;

            var transaction = new WalletTransaction
            {
                WalletID = walletId,
                Amount = amount,
                TransactionDate = DateTime.Now,
                Type = "Debit",
                Description = description ?? "Rút tiền từ ví"
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }
        
        // Nạp tiền vào ví người dùng
        public async Task<bool> DepositAsync(int walletId, decimal amount, string transactionCode, string description = null)
        {
            var wallet = await _context.Wallets.FindAsync(walletId);
            
            if (wallet == null || amount <= 0)
            {
                return false;
            }

            wallet.Balance += amount;
            wallet.LastUpdatedDate = DateTime.Now;

            var transaction = new WalletTransaction
            {
                WalletID = walletId,
                Amount = amount,
                TransactionDate = DateTime.Now,
                Type = "Credit",
                Description = description ?? $"Nạp {amount:N0} POINT vào ví - Mã GD: {transactionCode}"
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return true;
        }
        
        // Thanh toán tài liệu từ ví
        public async Task<(bool success, string message)> PayForDocumentFromWalletAsync(int userId, int documentId)
        {
            var wallet = await GetWalletAsync(userId);
            var document = await _context.Documents.FindAsync(documentId);
            
            if (document == null)
            {
                return (false, "Không tìm thấy tài liệu");
            }
            
            if (!document.Price.HasValue || document.Price <= 0)
            {
                return (false, "Tài liệu không có giá hoặc miễn phí");
            }
            
            // Kiểm tra số dư
            if (wallet.Balance < document.Price.Value)
            {
                return (false, "Số dư không đủ để thanh toán tài liệu này");
            }
            
            // Tạo giao dịch mua tài liệu
            var purchase = new Purchase
            {
                UserID = userId,
                DocumentID = documentId,
                PurchaseDate = DateTime.Now,
                Amount = document.Price.Value,
                TransactionCode = $"WALLET-{DateTime.Now.Ticks}",
                Status = "Completed"
            };
            
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
            
            // Trừ tiền từ ví người mua
            wallet.Balance -= document.Price.Value;
            wallet.LastUpdatedDate = DateTime.Now;
            
            // Tạo giao dịch ví cho người mua
            var walletTransaction = new WalletTransaction
            {
                WalletID = wallet.WalletID,
                Amount = document.Price.Value,
                TransactionDate = DateTime.Now,
                Type = "Debit",
                Description = $"Thanh toán {document.Price.Value:N0} POINT cho tài liệu: {document.Title}",
                DocumentID = documentId,
                PurchaseID = purchase.PurchaseID
            };
            
            _context.WalletTransactions.Add(walletTransaction);
            
            // Ghi lại hoạt động mua tài liệu
            await _userActivityService.LogPurchaseActivityAsync(userId, documentId, purchase.Amount);
            
            // Xử lý chuyển tiền cho người đăng tải
            await ProcessPurchasePaymentAsync(purchase);
            
            return (true, "Thanh toán thành công");
        }

        // Cộng điểm khi tài liệu được duyệt
        public async Task<bool> AddPointsForApprovedDocumentAsync(int documentId)
        {
            try
            {
                // Lấy thông tin tài liệu và người tạo
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.DocumentID == documentId);

                if (document == null || !document.UserID.HasValue)
                {
                    _logger.LogWarning($"Không thể cộng điểm: Tài liệu không tồn tại hoặc không có thông tin người đăng tải (DocumentID: {documentId})");
                    return false;
                }

                var uploaderId = document.UserID.Value;

                // Lấy số điểm thưởng từ cấu hình hệ thống, mặc định là 10 điểm
                decimal pointsToAdd = 10m;
                var pointsConfig = await _context.SystemConfigs
                    .FirstOrDefaultAsync(c => c.ConfigKey == "PointsForApprovedDocument");

                if (pointsConfig != null && decimal.TryParse(pointsConfig.ConfigValue, out var configPoints))
                {
                    pointsToAdd = configPoints;
                }

                // Lấy ví của người đăng tải
                var uploaderWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserID == uploaderId);
                
                // Nếu chưa có ví, tạo ví mới
                if (uploaderWallet == null)
                {
                    uploaderWallet = await CreateWalletAsync(uploaderId);
                }

                // Cập nhật số dư ví
                uploaderWallet.Balance += pointsToAdd;
                uploaderWallet.LastUpdatedDate = DateTime.Now;

                // Tạo giao dịch ví
                var walletTransaction = new WalletTransaction
                {
                    WalletID = uploaderWallet.WalletID,
                    Amount = pointsToAdd,
                    TransactionDate = DateTime.Now,
                    Type = "Credit",
                    Description = $"Thưởng {pointsToAdd:N0} POINT cho việc đăng tải tài liệu: {document.Title}",
                    DocumentID = document.DocumentID
                };

                _context.WalletTransactions.Add(walletTransaction);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Đã cộng {pointsToAdd} điểm cho người dùng {uploaderId} cho tài liệu {documentId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cộng điểm cho tài liệu được duyệt: {ex.Message}");
                return false;
            }
        }

        // Đếm tổng số giao dịch của ví
        public async Task<int> CountTransactionsAsync(int walletId)
        {
            return await _context.WalletTransactions
                .Where(t => t.WalletID == walletId)
                .CountAsync();
        }
    }
} 