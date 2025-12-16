using HisaTeaPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity; // BẮT BUỘC: Thêm thư viện này để dùng .Include

namespace HisaTeaPOS.Controllers
{
    public class InvoiceController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        // Hiển thị view in hóa đơn (Pop-up)
        public ActionResult Print(int id)
        {
            // SỬA: Dùng Include để lấy kèm Chi Tiết Đơn Hàng (Món + Topping)
            // Thay "ChiTietDonHangs" bằng tên bảng chi tiết thực tế trong Model của bạn nếu khác
            var order = db.DonHangs
                    .Include("ChiTietDonHangs")                        // Lấy món
                    .Include("ChiTietDonHangs.SanPham")                // Lấy tên món
                    .Include("ChiTietDonHangs.ChiTietToppings")        // Lấy danh sách topping kèm theo
                    .Include("ChiTietDonHangs.ChiTietToppings.Topping")// Lấy tên thật của Topping
                    .FirstOrDefault(x => x.MaDon == id);

            if (order == null)
            {
                return HttpNotFound("Không tìm thấy đơn hàng");
            }

            // Lấy thông tin cấu hình từ Database
            var config = db.CauHinhs.FirstOrDefault();

            if (config != null)
            {
                ViewBag.TenQuan = config.TenQuan;
                ViewBag.DiaChi = config.DiaChi;
                ViewBag.WifiPass = config.WifiPass;
                ViewBag.LoiChao = config.LoiChao;
            }
            else
            {
                ViewBag.TenQuan = "HISA TEA";
                ViewBag.DiaChi = "Chưa cập nhật địa chỉ";
                ViewBag.WifiPass = "Không có";
                ViewBag.LoiChao = "Cảm ơn quý khách!";
            }

            return View(order);
        }
    }
}