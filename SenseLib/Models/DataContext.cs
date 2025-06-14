﻿using Microsoft.EntityFrameworkCore;
namespace SenseLib.Models
{
	public class DataContext : DbContext
	{
		public DataContext(DbContextOptions<DataContext> options) : base(options){

		}
		public DbSet<User> Users {get; set;}
		public DbSet<Document> Documents {get; set;}
		public DbSet<Author> Authors {get; set;}
		public DbSet<Category> Categories {get; set;}
		public DbSet<Publisher> Publishers {get; set;}
		public DbSet<Comment> Comments {get; set;}
		public DbSet<CommentLike> CommentLikes {get; set;}
		public DbSet<Rating> Ratings {get; set;}
		public DbSet<Favorite> Favorites {get; set;}
		public DbSet<Download> Downloads {get; set;}
		public DbSet<Transaction> Transactions {get; set;}
		public DbSet<Menu> Menu {get; set;}
		public DbSet<AdminMenu> AdminMenus {get; set;}
		public DbSet<Slideshow> Slideshows {get; set;}
		public DbSet<DocumentStatistics> DocumentStatistics {get; set;}
		public DbSet<ContactMessage> ContactMessages {get; set;}
		public DbSet<PasswordResetToken> PasswordResetTokens {get; set;}
		public DbSet<Purchase> Purchases {get; set;}
		public DbSet<SystemConfig> SystemConfigs {get; set;}
		public DbSet<Wallet> Wallets {get; set;}
		public DbSet<WalletTransaction> WalletTransactions {get; set;}
		public DbSet<UserActivity> UserActivities {get; set;}
		
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			
			// Cấu hình cho bảng Favorites
			modelBuilder.Entity<Favorite>(entity =>
			{
				entity.ToTable("Favorites");
				
				entity.HasKey(e => e.FavoriteID);
				
				entity.Property(e => e.FavoriteID)
					.HasColumnName("FavoriteID")
					.ValueGeneratedOnAdd();
				
				entity.Property(e => e.UserID)
					.HasColumnName("UserID")
					.IsRequired();
				
				entity.Property(e => e.DocumentID)
					.HasColumnName("DocumentID")
					.IsRequired();
				
				// Thiết lập quan hệ với User
				entity.HasOne(e => e.User)
					.WithMany(u => u.Favorites)
					.HasForeignKey(e => e.UserID)
					.OnDelete(DeleteBehavior.Cascade);
				
				// Thiết lập quan hệ với Document
				entity.HasOne(e => e.Document)
					.WithMany(d => d.Favorites)
					.HasForeignKey(e => e.DocumentID)
					.OnDelete(DeleteBehavior.Cascade);
				
				// Thêm chỉ mục cho tìm kiếm nhanh hơn
				entity.HasIndex(e => new { e.UserID, e.DocumentID }).IsUnique();
			});
			
			// Cấu hình cho bảng UserActivities
			modelBuilder.Entity<UserActivity>(entity =>
			{
				entity.ToTable("UserActivities");
				
				entity.HasKey(e => e.ActivityID);
				
				entity.Property(e => e.UserID).IsRequired();
				entity.Property(e => e.ActivityDate).IsRequired();
				entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(50);
				entity.Property(e => e.Description).HasMaxLength(500);
				entity.Property(e => e.AdditionalData).HasMaxLength(1000);
				
				// Thiết lập quan hệ với User
				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserID)
					.OnDelete(DeleteBehavior.Cascade);
				
				// Thiết lập quan hệ với Document (nếu có)
				entity.HasOne(e => e.Document)
					.WithMany()
					.HasForeignKey(e => e.DocumentID)
					.OnDelete(DeleteBehavior.SetNull);
				
				// Tạo chỉ mục cho tìm kiếm nhanh
				entity.HasIndex(e => e.UserID);
				entity.HasIndex(e => e.ActivityDate);
				entity.HasIndex(e => e.ActivityType);
			});
			
			// Cấu hình cho bảng PasswordResetToken
			modelBuilder.Entity<PasswordResetToken>(entity =>
			{
				entity.ToTable("PasswordResetTokens");
				
				entity.HasKey(e => e.Id);
				
				entity.Property(e => e.UserId).IsRequired();
				entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
				entity.Property(e => e.ExpiryDate).IsRequired();
				entity.Property(e => e.IsUsed).HasDefaultValue(false);
				
				// Thiết lập quan hệ với User
				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserId)
					.OnDelete(DeleteBehavior.Cascade);
			});
			
			// Cấu hình cho bảng Purchases
			modelBuilder.Entity<Purchase>(entity =>
			{
				entity.ToTable("Purchases");
				
				entity.HasKey(e => e.PurchaseID);
				
				entity.Property(e => e.UserID).IsRequired();
				entity.Property(e => e.DocumentID).IsRequired();
				entity.Property(e => e.PurchaseDate).IsRequired();
				entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
				entity.Property(e => e.Status).HasMaxLength(20);
				
				// Thiết lập quan hệ với User
				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserID)
					.OnDelete(DeleteBehavior.Cascade);
				
				// Thiết lập quan hệ với Document
				entity.HasOne(e => e.Document)
					.WithMany()
					.HasForeignKey(e => e.DocumentID)
					.OnDelete(DeleteBehavior.Cascade);
				
				// Tạo chỉ mục cho tìm kiếm nhanh
				entity.HasIndex(e => new { e.UserID, e.DocumentID });
			});
			
			// Cấu hình cho bảng Wallets
			modelBuilder.Entity<Wallet>(entity =>
			{
				entity.ToTable("Wallets");
				
				entity.HasKey(e => e.WalletID);
				
				entity.Property(e => e.UserID).IsRequired();
				entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)").HasDefaultValue(0);
				entity.Property(e => e.CreatedDate).IsRequired();
				entity.Property(e => e.LastUpdatedDate).IsRequired();
				
				// Thiết lập quan hệ với User
				entity.HasOne(e => e.User)
					.WithMany()
					.HasForeignKey(e => e.UserID)
					.OnDelete(DeleteBehavior.Cascade);
				
				// Tạo chỉ mục cho tìm kiếm nhanh
				entity.HasIndex(e => e.UserID).IsUnique();
			});
			
			// Cấu hình cho bảng WalletTransactions
			modelBuilder.Entity<WalletTransaction>(entity =>
			{
				entity.ToTable("WalletTransactions");
				
				entity.HasKey(e => e.TransactionID);
				
				entity.Property(e => e.WalletID).IsRequired();
				entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").IsRequired();
				entity.Property(e => e.TransactionDate).IsRequired();
				entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
				entity.Property(e => e.Description).HasMaxLength(255);
				
				// Thiết lập quan hệ với Wallet
				entity.HasOne(e => e.Wallet)
					.WithMany(w => w.Transactions)
					.HasForeignKey(e => e.WalletID)
					.OnDelete(DeleteBehavior.Cascade);
				
				// Thiết lập quan hệ với Document (nếu có)
				entity.HasOne(e => e.Document)
					.WithMany()
					.HasForeignKey(e => e.DocumentID)
					.OnDelete(DeleteBehavior.SetNull);
				
				// Thiết lập quan hệ với Purchase (nếu có) - Thay thành SetNull để cho phép xóa Purchase
				entity.HasOne(e => e.Purchase)
					.WithMany()
					.HasForeignKey(e => e.PurchaseID)
					.OnDelete(DeleteBehavior.SetNull);
				
				// Tạo chỉ mục cho tìm kiếm nhanh
				entity.HasIndex(e => e.WalletID);
			});
			
			// Cấu hình cho bảng SystemConfig
			modelBuilder.Entity<SystemConfig>(entity =>
			{
				entity.ToTable("SystemConfigs");
				
				entity.HasKey(e => e.ConfigID);
				
				entity.Property(e => e.ConfigKey).IsRequired().HasMaxLength(100);
				entity.Property(e => e.ConfigValue).IsRequired().HasMaxLength(1000);
				entity.Property(e => e.Description).HasMaxLength(500);
				
				// Tạo chỉ mục cho tìm kiếm nhanh
				entity.HasIndex(e => e.ConfigKey).IsUnique();
				
				// Thêm dữ liệu mặc định
				entity.HasData(
					new SystemConfig { ConfigID = 1, ConfigKey = "MaxLoginAttempts", ConfigValue = "5", Description = "Số lần đăng nhập sai tối đa trước khi khóa tài khoản" },
					new SystemConfig { ConfigID = 2, ConfigKey = "LockoutTimeMinutes", ConfigValue = "30", Description = "Thời gian khóa tài khoản tạm thời (phút)" },
					new SystemConfig { ConfigID = 3, ConfigKey = "HomePagePaidDocuments", ConfigValue = "4", Description = "Số lượng tài liệu có phí hiển thị trên trang chủ" },
					new SystemConfig { ConfigID = 4, ConfigKey = "HomePageFreeDocuments", ConfigValue = "4", Description = "Số lượng tài liệu miễn phí hiển thị trên trang chủ" },
					new SystemConfig { ConfigID = 5, ConfigKey = "AuthorCommissionPercent", ConfigValue = "80", Description = "Phần trăm hoa hồng cho tác giả khi bán tài liệu" }
				);
			});
			
			// Các cấu hình khác nếu cần...
		}
	}
}
