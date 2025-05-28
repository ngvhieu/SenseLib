using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SenseLib.Services;
using SenseLib.Utilities;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Thiết lập biến môi trường GOOGLE_APPLICATION_CREDENTIALS
string googleCredentialsPath = Path.Combine(Directory.GetCurrentDirectory(), 
    builder.Configuration["Google:CredentialsFile"] ?? "google-credentials.json");

if (File.Exists(googleCredentialsPath))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", googleCredentialsPath);
    Console.WriteLine($"GOOGLE_APPLICATION_CREDENTIALS đã được thiết lập: {googleCredentialsPath}");
}
else
{
    Console.WriteLine($"CẢNH BÁO: File credentials Google không tìm thấy tại: {googleCredentialsPath}");
    Console.WriteLine("Vui lòng tạo file credentials theo hướng dẫn trong README.md");
}

// Đăng ký DbContext
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký dịch vụ Email
builder.Services.AddTransient<IEmailService, EmailService>();

// Đăng ký dịch vụ Favorite
builder.Services.AddScoped<IFavoriteService, FavoriteService>();

// Đăng ký dịch vụ VNPay
builder.Services.Configure<VNPayConfig>(builder.Configuration.GetSection("VNPay"));
builder.Services.AddScoped<VNPayService>();

// Đăng ký dịch vụ Wallet
builder.Services.AddScoped<WalletService>();

// Đăng ký dịch vụ Summary (Tóm tắt AI)
builder.Services.AddScoped<ISummaryService, SummaryService>();

// Đăng ký dịch vụ Chatbot
builder.Services.AddScoped<IChatbotService, ChatbotService>();

// Đăng ký dịch vụ xử lý file DOCX
builder.Services.AddScoped<IDocxService, DocxService>();

// Đăng ký dịch vụ chuyển đổi tài liệu sang PDF
builder.Services.AddScoped<IDocumentConverterService, DocumentConverterService>();

// Đăng ký dịch vụ đọc PDF
builder.Services.AddScoped<PdfService>();

// Đăng ký dịch vụ Text-to-Speech
builder.Services.AddScoped<TextToSpeechService>();

// Đăng ký dịch vụ UserActivity
builder.Services.AddScoped<UserActivityService>();

// Cấu hình xác thực cookie và JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Sử dụng fallback khi config Jwt:Issuer/Audience null
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SenseLib",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SenseLibApp",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? "SenseLibSecretKeyForJwtAuthenticationAndAuthorization123"))
        };
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

// Thêm CORS để cho phép ứng dụng Android kết nối
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAndroid", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
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

// Cấu hình phục vụ file tĩnh với các tùy chọn nâng cao
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Thêm cache cho file tĩnh
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
        
        // Đặt Content-Disposition cho file PDF để đảm bảo hiển thị trực tiếp
        if (ctx.File.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.Append("Content-Type", "application/pdf");
            ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            
            // Đảm bảo file PDF được mở trong trình duyệt mà không tải xuống
            // ctx.Context.Response.Headers.Append("Content-Disposition", "inline");
        }
    }
});

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
            "uploads/slideshow",
            "audio"
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
    Console.WriteLine($"Lỗi khi tạo thư mục: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
}

app.UseRouting();

// Kích hoạt CORS (đặt trước UseAuthentication và UseAuthorization)
app.UseCors("AllowAndroid");

// Kích hoạt Session trước Authentication
app.UseSession();

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
