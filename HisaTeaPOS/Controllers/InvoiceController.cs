using HisaTeaPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HisaTeaPOS.Controllers
{
    public class InvoiceController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        // Hiển thị view in hóa đơn (Pop-up)
        public ActionResult Print(int id)
        {
            var order = db.DonHangs.Find(id);

            // Lấy thông tin cấu hình từ Database
            var config = db.CauHinhs.FirstOrDefault();

            // Truyền sang View qua ViewBag
            if (config != null)
            {
                ViewBag.TenQuan = config.TenQuan;
                ViewBag.DiaChi = config.DiaChi;
                ViewBag.WifiPass = config.WifiPass;
                ViewBag.LoiChao = config.LoiChao;
            }
            else
            {
                // Giá trị mặc định nếu chưa cài đặt
                ViewBag.TenQuan = "HISA TEA";
                ViewBag.DiaChi = "Chưa cập nhật địa chỉ";
                ViewBag.WifiPass = "Không có";
                ViewBag.LoiChao = "Cảm ơn quý khách!";
            }

            return View(order);
        }
    }
}