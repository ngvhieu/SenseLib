using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using SenseLib.Services;
using SenseLib.Utilities;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký DbContext
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký dịch vụ Email
builder.Services.AddTransient<IEmailService, EmailService>();

// Đăng ký dịch vụ Favorite
builder.Services.AddScoped<IFavoriteService, FavoriteService>();

// Cấu hình xác thực cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    });

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Tăng giới hạn kích thước file upload
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50MB in bytes
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50MB in bytes
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB in bytes
});

var app = builder.Build();

// Kiểm tra kết nối database lúc khởi động
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<DataContext>();
        if (dbContext.Database.CanConnect())
        {
            Console.WriteLine("Kết nối database thành công!");
            Console.WriteLine($"Số lượng Favorites hiện có: {dbContext.Favorites.Count()}");
            
            // Liệt kê các bảng trong cơ sở dữ liệu
            Console.WriteLine("Danh sách bảng trong database:");
            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                dbContext.Database.OpenConnection();
                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        Console.WriteLine($"- {result.GetString(0)}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Không thể kết nối tới database!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi khi kiểm tra kết nối database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Tạo thư mục uploads/documents khi khởi động ứng dụng
try
{
    string webRootPath = app.Environment.WebRootPath;
    if (!string.IsNullOrEmpty(webRootPath))
    {
        // Danh sách các thư mục cần tạo
        string[] folders = {
            "uploads",
            "uploads/documents",
            "uploads/images",
            "uploads/profiles",
            "uploads/slideshow"
        };
        
        // Tạo các thư mục nếu chưa tồn tại
        foreach (var folder in folders)
        {
            string folderPath = Path.Combine(webRootPath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                Console.WriteLine($"Đã tạo thư mục {folder}");
            }
            else
            {
                Console.WriteLine($"Thư mục {folder} đã tồn tại");
            }
            
            // Kiểm tra quyền ghi vào thư mục
            try
            {
                string testFile = Path.Combine(folderPath, "permission_test.txt");
                File.WriteAllText(testFile, "Testing write permissions");
                File.Delete(testFile);
                Console.WriteLine($"Thư mục {folder} có quyền ghi");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CẢNH BÁO: Không có quyền ghi vào thư mục {folder}. Lỗi: {ex.Message}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Lỗi khi tạo thư mục uploads/documents: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Thêm middleware để lấy thông tin người dùng hiện tại
app.UseCurrentUser();

// Route cho khu vực Admin
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
