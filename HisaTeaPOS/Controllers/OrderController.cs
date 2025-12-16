using System;
using System.Linq;
using System.Web.Mvc;
using HisaTeaPOS.Models;
using System.Collections.Generic;
using System.Data.Entity; // Required for .Include()

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

            var products = db.SanPhams
                      .Where(p => p.NgungBan != true) // Lọc bỏ món ngừng bán
                      .Include("CongThucs.NguyenLieu")
                      .ToList();
            var toppings = db.Toppings.Include("NguyenLieu").ToList();

            ViewBag.Toppings = toppings;
            return View(products);
        }

        // POST: Checkout API
        [HttpPost]
        public ActionResult Checkout(OrderViewModel orderData)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // --- STEP 1: VALIDATE INVENTORY ---
                    var ingredientsNeeded = new Dictionary<int, decimal>();

                    foreach (var item in orderData.Items)
                    {
                        // 1.1. Main Product Ingredients
                        var productCheck = db.SanPhams.Find(item.ProductId);
                        if (productCheck != null && productCheck.NgungBan == true)
                        {
                            return Json(new { success = false, message = $"Món '{productCheck.Ten}' đã ngừng kinh doanh. Vui lòng tải lại trang!" });
                        }
                        var congThuc = db.CongThucs.Where(ct => ct.MaSP == item.ProductId).ToList();

                        double sizeMult = 1.0;
                        if (item.Size == "L") sizeMult = 1.3;
                        else if (item.Size == "S") sizeMult = 0.8;

                        double sugarMult = item.Sugar / 100.0;

                        foreach (var ct in congThuc)
                        {
                            double amountPerCup = (double)ct.DinhLuong * sizeMult;
                            var nlName = db.NguyenLieux.Find(ct.MaNL)?.Ten.ToLower() ?? "";
                            if (nlName.Contains("đường")) amountPerCup *= sugarMult;

                            decimal totalNeed = (decimal)(amountPerCup * item.Quantity);

                            if (ingredientsNeeded.ContainsKey(ct.MaNL))
                                ingredientsNeeded[ct.MaNL] += totalNeed;
                            else
                                ingredientsNeeded.Add(ct.MaNL, totalNeed);
                        }

                        if (item.Toppings != null && item.Toppings.Count > 0)
                        {
                            foreach (var topId in item.Toppings)
                            {
                                var top = db.Toppings.Find(topId);
                                if (top != null && top.MaNL != null)
                                {
                                    // For toppings, we just add the base amount for each instance in the list
                                    decimal topNeed = (top.DinhLuong ?? 0);

                                    if (ingredientsNeeded.ContainsKey(top.MaNL.Value))
                                        ingredientsNeeded[top.MaNL.Value] += topNeed;
                                    else
                                        ingredientsNeeded.Add(top.MaNL.Value, topNeed);
                                }
                            }
                        }
                    }

                    // 1.3. Check against Stock
                    foreach (var kvp in ingredientsNeeded)
                    {
                        var nl = db.NguyenLieux.Find(kvp.Key);
                        if (nl == null) continue;

                        if ((nl.TonKho ?? 0) < kvp.Value)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"Không đủ nguyên liệu: {nl.Ten}.\nCần: {kvp.Value:N0}{nl.DonVi}.\nTrong kho còn: {nl.TonKho:N0}{nl.DonVi}."
                            });
                        }
                    }

                    // --- STEP 2: SAVE ORDER ---
                    var dh = new DonHang();
                    dh.NgayTao = DateTime.Now;
                    dh.MaNV = (Session["UserID"] != null) ? (int)Session["UserID"] : 1002;
                    dh.HinhThucTT = orderData.PaymentMethod;
                    dh.TongTien = orderData.Total;
                    dh.TrangThai = "Pending";
                    dh.NguonDon = orderData.OrderSource ?? "Tại quán"; // Save Order Source

                    if (orderData.DiscountAmount > 0)
                    {
                        dh.SoTienGiam = orderData.DiscountAmount;
                        dh.MaKM = orderData.CouponId;
                    }

                    db.DonHangs.Add(dh);
                    db.SaveChanges(); // Get Order ID

                    // --- STEP 3: SAVE DETAILS & TOPPINGS ---
                    foreach (var item in orderData.Items)
                    {
                        // Save Order Detail
                        var ctdh = new ChiTietDonHang();
                        ctdh.MaDon = dh.MaDon;
                        ctdh.MaSP = item.ProductId;
                        ctdh.SoLuong = item.Quantity;
                        ctdh.KichCo = (item.Size != null && item.Size.Length > 5) ? item.Size.Substring(0, 5) : item.Size;
                        ctdh.MucDuong = item.Sugar;
                        ctdh.DonGia = item.Price;
                        ctdh.ThanhTien = item.Price * item.Quantity;

                        db.ChiTietDonHangs.Add(ctdh);
                        db.SaveChanges(); // Get Detail ID

                        // Save Toppings
                        if (item.Toppings != null && item.Toppings.Count > 0)
                        {
                            foreach (var topId in item.Toppings)
                            {
                                var ctt = new ChiTietTopping { MaChiTietDon = ctdh.MaChiTiet, MaTop = topId };
                                db.ChiTietToppings.Add(ctt);
                            }
                        }
                    }

                    // --- STEP 4: DEDUCT INVENTORY ---
                    foreach (var kvp in ingredientsNeeded)
                    {
                        var nl = db.NguyenLieux.Find(kvp.Key);
                        if (nl != null)
                        {
                            nl.TonKho -= kvp.Value;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, orderId = dh.MaDon });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    string msg = ex.Message;
                    if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                    return Json(new { success = false, message = "Lỗi hệ thống: " + msg });
                }
            }
        }

        // API Check Coupon
        [HttpPost]
        public ActionResult CheckCoupon(string code, decimal total)
        {
            var km = db.KhuyenMais.FirstOrDefault(k => k.MaCode == code && k.TrangThai == true);

            if (km == null) return Json(new { success = false, message = "Mã không tồn tại hoặc hết hạn!" });

            var today = DateTime.Today;
            if ((km.NgayBatDau != null && today < km.NgayBatDau) || (km.NgayKetThuc != null && today > km.NgayKetThuc))
                return Json(new { success = false, message = "Mã chưa đến hoặc quá hạn!" });

            decimal discountAmount = (km.LoaiKM == "phantram") ? total * (km.GiaTri / 100) : km.GiaTri;
            if (discountAmount > total) discountAmount = total;

            return Json(new { success = true, discount = discountAmount, kmId = km.MaKM });
        }
    }

    // View Models
    public class OrderViewModel
    {
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public int? CouponId { get; set; }
        public decimal DiscountAmount { get; set; }
        public string OrderSource { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Size { get; set; }
        public int Sugar { get; set; }
        public decimal Price { get; set; }
        public List<int> Toppings { get; set; }
    }
}