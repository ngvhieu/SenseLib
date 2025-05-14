using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;
using SenseLib.Areas.Admin.Models.ViewModels;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly DataContext _context;

        public UserController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/User
        public async Task<IActionResult> Index(string searchString, string currentFilter, int? pageNumber)
        {
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var users = from u in _context.Users
                        select u;

            if (!String.IsNullOrEmpty(searchString))
            {
                users = users.Where(s => s.Username.Contains(searchString) 
                                       || s.Email.Contains(searchString)
                                       || s.FullName.Contains(searchString));
            }

            users = users.OrderBy(u => u.UserID);

            int pageSize = 10;
            return View(await PaginatedList<User>.CreateAsync(users.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Admin/User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Admin/User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,Password,Email,FullName,Role,Status")] User user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem username đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                    return View(user);
                }

                // Kiểm tra xem email đã tồn tại chưa
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    return View(user);
                }

                // Mã hóa mật khẩu trước khi lưu
                user.Password = HashPassword(user.Password);
                
                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo tài khoản thành công";
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Tạo ViewModel từ model User
            var viewModel = new UserEditViewModel(user);
            
            // Đặt một giá trị mặc định cho trường mật khẩu
            viewModel.Password = "********"; // Giá trị placeholder
            
            return View(viewModel);
        }

        // POST: Admin/User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            // Lấy thông tin user hiện tại
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Cập nhật thông tin từ form
                var username = Request.Form["Username"].ToString();
                var email = Request.Form["Email"].ToString();
                var fullName = Request.Form["FullName"].ToString();
                var role = Request.Form["Role"].ToString();
                var status = Request.Form["Status"].ToString();
                var password = Request.Form["Password"].ToString();

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(role) ||
                    string.IsNullOrEmpty(status))
                {
                    TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc";
                    return RedirectToAction(nameof(Edit), new { id = id });
                }

                // Kiểm tra xem username đã tồn tại chưa (nếu thay đổi)
                if (user.Username != username && await _context.Users.AnyAsync(u => u.Username == username))
                {
                    TempData["ErrorMessage"] = "Tên đăng nhập đã tồn tại";
                    return RedirectToAction(nameof(Edit), new { id = id });
                }

                // Kiểm tra xem email đã tồn tại chưa (nếu thay đổi)
                if (user.Email != email && await _context.Users.AnyAsync(u => u.Email == email))
                {
                    TempData["ErrorMessage"] = "Email đã tồn tại";
                    return RedirectToAction(nameof(Edit), new { id = id });
                }

                // Cập nhật các trường
                user.Username = username;
                user.Email = email;
                user.FullName = fullName;
                user.Role = role;
                user.Status = status;
                
                // Cập nhật mật khẩu nếu có thay đổi và không phải là placeholder
                if (!string.IsNullOrEmpty(password) && password != "********")
                {
                    user.Password = HashPassword(password);
                    TempData["PasswordChanged"] = "Mật khẩu đã được cập nhật";
                }
                else
                {
                    TempData["PasswordInfo"] = "Mật khẩu không thay đổi";
                }

                _context.Update(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật tài khoản thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id = id });
            }
        }

        // GET: Admin/User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa tài khoản thành công";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/User/ChangeStatus/5
        public async Task<IActionResult> ChangeStatus(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Đổi trạng thái người dùng
            user.Status = user.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thay đổi trạng thái tài khoản thành {user.Status}";

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }

    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalItems { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            TotalItems = count;

            this.AddRange(items);
        }

        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
} 