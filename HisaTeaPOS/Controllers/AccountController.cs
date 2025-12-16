using HisaTeaPOS.Models;
using System.Web.Mvc;
using System.Linq;

public class AccountController : Controller
{
    private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

    public ActionResult Login()
    {
        var config = db.CauHinhs.FirstOrDefault();

        // Nếu database chưa có cấu hình, tạo một cái mặc định để tránh lỗi null
        if (config == null)
        {
            config = new CauHinh
            {
                TenQuan = "CHISA TEA SYSTEM",
                LoiChao = "Vui lòng đăng nhập"
            };
        }

        // Truyền model sang View
        return View(config);
       
    }

    [HttpPost]
    public ActionResult Login(string username, string password)
    {
        // Lưu ý: Trong thực tế password nên được mã hóa (Hash). 
        // Code này so sánh plain-text theo DB script hiện tại.
        var user = db.NhanViens.FirstOrDefault(u => u.TaiKhoan == username && u.MatKhau == password);

        if (user != null)
        {
            Session["UserID"] = user.MaNV;
            Session["UserName"] = user.HoTen;
            Session["Role"] = user.VaiTro;
            return RedirectToAction("Index", "Home"); // Chuyển về Dashboard
        }

        ViewBag.Error = "Sai tên đăng nhập hoặc mật khẩu!";
        return View();
    }

    public ActionResult Logout()
    {
      
        Session.Clear();
        Session.Abandon();

        return RedirectToAction("Login");
    }
 
    public ActionResult ChangePassword()
    {
        if (Session["UserID"] == null)
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    // POST: Xử lý đổi mật khẩu
    [HttpPost]
    public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        // 1. Kiểm tra đăng nhập
        if (Session["UserID"] == null) return RedirectToAction("Login");

        int userId = (int)Session["UserID"];
        var user = db.NhanViens.Find(userId); // Giả sử bảng nhân viên tên là NhanViens

        if (user == null) return HttpNotFound();

       
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
            return View();
        }

        if (user.MatKhau != currentPassword)
        {
            ViewBag.Error = "Mật khẩu hiện tại không đúng!";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp!";
            return View();
        }

        // 5. Lưu mật khẩu mới
        user.MatKhau = newPassword;
        db.SaveChanges();

        ViewBag.Success = "Đổi mật khẩu thành công!";
        return View();
    }
}