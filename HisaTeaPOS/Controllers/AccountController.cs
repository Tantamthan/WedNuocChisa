using HisaTeaPOS.Models;
using System.Web.Mvc;
using System.Linq;

public class AccountController : Controller
{
    private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

    public ActionResult Login()
    {
        return View();
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
        // Xóa toàn bộ session
        Session.Clear();
        Session.Abandon();

        // Quay về trang đăng nhập
        return RedirectToAction("Login");
    }
}