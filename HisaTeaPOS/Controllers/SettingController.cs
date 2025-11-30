using System.Linq;
using System.Web.Mvc;
using HisaTeaPOS.Models;
using HisaTeaPOS.Filters;

namespace HisaTeaPOS.Controllers
{
    [AdminOnly] // Chỉ quản lý mới được chỉnh
    public class SettingController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        public ActionResult Index()
        {
            // Lấy dòng cấu hình đầu tiên (vì chỉ có 1 quán)
            var config = db.CauHinhs.FirstOrDefault();
            if (config == null)
            {
                config = new CauHinh(); // Tạo mới nếu chưa có
                db.CauHinhs.Add(config);
                db.SaveChanges();
            }
            return View(config);
        }

        [HttpPost]
        public ActionResult Save(CauHinh model)
        {
            var config = db.CauHinhs.FirstOrDefault();
            if (config != null)
            {
                config.TenQuan = model.TenQuan;
                config.DiaChi = model.DiaChi;
                config.SoDienThoai = model.SoDienThoai;
                config.WifiPass = model.WifiPass;
                config.PrinterIP = model.PrinterIP;
                config.LoiChao = model.LoiChao;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}