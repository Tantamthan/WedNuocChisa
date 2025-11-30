using System;
using System.Linq;
using System.Web.Mvc;
using HisaTeaPOS.Models;
using System.Data.Entity;

namespace HisaTeaPOS.Controllers
{
    public class KitchenController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        // 1. Trang chính của Bếp
        public ActionResult Index()
        {
            return View(); // Chỉ trả về khung HTML, dữ liệu sẽ load bằng AJAX
        }

        // 2. API lấy danh sách đơn chờ làm (Load tự động mỗi 10s)
        public ActionResult GetPendingOrders()
        {
            var orders = db.DonHangs
                           // 1. Lấy chi tiết đơn hàng + Sản phẩm
                           .Include(d => d.ChiTietDonHangs.Select(ct => ct.SanPham.CongThucs.Select(c => c.NguyenLieu)))

                           // 2. QUAN TRỌNG: Lấy thông tin Topping và Nguyên liệu của Topping
                           .Include(d => d.ChiTietDonHangs.Select(ct => ct.ChiTietToppings.Select(t => t.Topping.NguyenLieu)))

                           .Where(d => d.TrangThai == "Pending" || d.TrangThai == null)
                           .OrderBy(d => d.NgayTao)
                           .ToList();

            return PartialView("_PendingOrdersPartial", orders);
        }

        // 3. API Báo làm xong
        [HttpPost]
        public ActionResult CompleteOrder(int id)
        {
            var order = db.DonHangs.Find(id);
            if (order != null)
            {
                order.TrangThai = "Completed"; // Đổi trạng thái thành Đã xong
                db.SaveChanges();
            }
            return Json(new { success = true });
        }
    }
}