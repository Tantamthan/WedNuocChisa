using System;
using System.Linq;
using System.Web.Mvc;
using HisaTeaPOS.Models;
using System.Collections.Generic;

namespace HisaTeaPOS.Controllers
{
    public class OrderController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        // GET: POS Page
        public ActionResult POS()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            // Dùng Include để lấy kèm thông tin Công thức và Nguyên liệu
            var products = db.SanPhams.Include("CongThucs.NguyenLieu").ToList();
            var toppings = db.Toppings.Include("NguyenLieu").ToList();

            ViewBag.Toppings = toppings;
            return View(products);
        }

        // POST: Checkout API
        // Thay thế toàn bộ hàm Checkout trong OrderController.cs

        [HttpPost]
        public ActionResult Checkout(OrderViewModel orderData)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // --- BƯỚC 1: KIỂM TRA TỒN KHO (VALIDATION) ---
                    // Tạo một danh sách tạm để cộng dồn nguyên liệu cần dùng
                    // Key: Mã Nguyên Liệu, Value: Tổng số lượng cần
                    var ingredientsNeeded = new Dictionary<int, decimal>();

                    foreach (var item in orderData.Items)
                    {
                        // 1.1. Tính nguyên liệu cho món chính
                        var congThuc = db.CongThucs.Where(ct => ct.MaSP == item.ProductId).ToList();

                        double sizeMult = 1.0;
                        if (item.Size == "L") sizeMult = 1.3;
                        else if (item.Size == "S") sizeMult = 0.8;
                        double sugarMult = item.Sugar / 100.0;

                        foreach (var ct in congThuc)
                        {
                            double amountPerCup = (double)ct.DinhLuong * sizeMult;

                            // Kiểm tra tên nguyên liệu để áp dụng đường (Cách này tạm ổn, tốt nhất nên có cờ IsSugar trong DB)
                            var nlName = db.NguyenLieux.Find(ct.MaNL)?.Ten.ToLower() ?? "";
                            if (nlName.Contains("đường")) amountPerCup *= sugarMult;

                            decimal totalNeed = (decimal)(amountPerCup * item.Quantity);

                            if (ingredientsNeeded.ContainsKey(ct.MaNL))
                                ingredientsNeeded[ct.MaNL] += totalNeed;
                            else
                                ingredientsNeeded.Add(ct.MaNL, totalNeed);
                        }

                        // 1.2. Tính nguyên liệu cho Topping
                        if (item.Toppings != null)
                        {
                            foreach (var topId in item.Toppings)
                            {
                                var top = db.Toppings.Find(topId);
                                if (top != null && top.MaNL != null)
                                {
                                    decimal topNeed = (top.DinhLuong ?? 0) * item.Quantity;

                                    if (ingredientsNeeded.ContainsKey(top.MaNL.Value))
                                        ingredientsNeeded[top.MaNL.Value] += topNeed;
                                    else
                                        ingredientsNeeded.Add(top.MaNL.Value, topNeed);
                                }
                            }
                        }
                    }

                    // 1.3. So sánh với kho hiện tại
                    foreach (var kvp in ingredientsNeeded)
                    {
                        var nl = db.NguyenLieux.Find(kvp.Key);
                        if (nl == null) continue;

                        // Nếu tồn kho hiện tại < Số lượng cần
                        if ((nl.TonKho ?? 0) < kvp.Value)
                        {
                            // Trả về lỗi ngay lập tức
                            return Json(new
                            {
                                success = false,
                                message = $"Không đủ nguyên liệu: {nl.Ten}.\nCần: {kvp.Value:N0}{nl.DonVi}.\nTrong kho còn: {nl.TonKho:N0}{nl.DonVi}."
                            });
                        }
                    }

                    // --- BƯỚC 2: NẾU ĐỦ HÀNG -> TIẾN HÀNH LƯU & TRỪ KHO ---
                    // (Code tạo đơn hàng, chi tiết đơn, và trừ kho giống hệt phiên bản trước)

                    var dh = new DonHang();
                    dh.NgayTao = DateTime.Now;
                    dh.MaNV = (Session["UserID"] != null) ? (int)Session["UserID"] : 1002;
                    dh.HinhThucTT = orderData.PaymentMethod;
                    dh.TongTien = orderData.Total;
                    dh.TrangThai = "Pending";

                    if (orderData.DiscountAmount > 0)
                    {
                        dh.SoTienGiam = orderData.DiscountAmount;
                        dh.MaKM = orderData.CouponId;
                    }

                    db.DonHangs.Add(dh);
                    db.SaveChanges();

                    foreach (var item in orderData.Items)
                    {
                        // Lưu chi tiết đơn hàng
                        var ctdh = new ChiTietDonHang();
                        ctdh.MaDon = dh.MaDon;
                        ctdh.MaSP = item.ProductId;
                        ctdh.SoLuong = item.Quantity;
                        ctdh.KichCo = (item.Size != null && item.Size.Length > 5) ? item.Size.Substring(0, 5) : item.Size;
                        ctdh.MucDuong = item.Sugar;
                        ctdh.DonGia = item.Price;
                        ctdh.ThanhTien = item.Price * item.Quantity;
                        db.ChiTietDonHangs.Add(ctdh);
                        db.SaveChanges();

                        // Lưu Topping
                        if (item.Toppings != null)
                        {
                            foreach (var topId in item.Toppings)
                            {
                                var ctt = new ChiTietTopping { MaChiTietDon = ctdh.MaChiTiet, MaTop = topId };
                                db.ChiTietToppings.Add(ctt);
                            }
                        }
                    }

                    // TRỪ KHO CHÍNH THỨC (Dựa vào danh sách đã tính ở Bước 1)
                    foreach (var kvp in ingredientsNeeded)
                    {
                        var nl = db.NguyenLieux.Find(kvp.Key);
                        if (nl != null) nl.TonKho -= kvp.Value;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, orderId = dh.MaDon });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }

        // API Check mã giảm giá
        [HttpPost]
        public ActionResult CheckCoupon(string code, decimal total)
        {
            var km = db.KhuyenMais.FirstOrDefault(k => k.MaCode == code && k.TrangThai == true);

            if (km == null) return Json(new { success = false, message = "Mã không tồn tại hoặc đã hết hạn!" });

            var today = DateTime.Today;
            if ((km.NgayBatDau != null && today < km.NgayBatDau) || (km.NgayKetThuc != null && today > km.NgayKetThuc))
            {
                return Json(new { success = false, message = "Mã chưa đến hoặc đã quá hạn sử dụng!" });
            }

            decimal discountAmount = 0;
            if (km.LoaiKM == "phantram")
                discountAmount = total * (km.GiaTri / 100);
            else
                discountAmount = km.GiaTri;

            if (discountAmount > total) discountAmount = total;

            return Json(new { success = true, discount = discountAmount, kmId = km.MaKM });
        }
    }

    // View Models (Cập nhật đầy đủ)
    public class OrderViewModel
    {
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }

        // Thêm trường cho Khuyến mãi
        public int? CouponId { get; set; }
        public decimal DiscountAmount { get; set; }

        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Size { get; set; }
        public int Sugar { get; set; }
        public decimal Price { get; set; }

        // QUAN TRỌNG: Phải có dòng này để nhận Topping
        public List<int> Toppings { get; set; }
    }
}