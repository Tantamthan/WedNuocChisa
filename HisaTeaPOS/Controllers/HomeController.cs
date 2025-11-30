using HisaTeaPOS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity; // Cần thêm cái này để dùng DbFunctions nếu cần
using System.Linq;
using System.Web.Mvc;

namespace HisaTeaPOS.Controllers
{
    public class HomeController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        public ActionResult Index()
        {
            var today = DateTime.Today; // 00:00:00 of today

            // 1. Overview Stats (Today)
            // Using DbFunctions.TruncateTime ensures we ignore hours/minutes in DB
            var revenue = db.DonHangs
                .Where(d => DbFunctions.TruncateTime(d.NgayTao) == today)
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            var totalOrders = db.DonHangs
                .Count(d => DbFunctions.TruncateTime(d.NgayTao) == today);

            var totalCups = db.ChiTietDonHangs
                .Where(ct => DbFunctions.TruncateTime(ct.DonHang.NgayTao) == today)
                .Sum(ct => (int?)ct.SoLuong) ?? 0;

            // 2. Top Products (Today)
            var topProducts = db.ChiTietDonHangs
                   .Where(ct => DbFunctions.TruncateTime(ct.DonHang.NgayTao) == today)
                   .GroupBy(ct => new { ct.SanPham.MaSP, ct.SanPham.Ten, ct.SanPham.HinhAnh })
                   .Select(g => new TopProductViewModel
                   {
                       TenSP = g.Key.Ten,
                       HinhAnh = g.Key.HinhAnh,
                       SoLuong = g.Sum(x => x.SoLuong) ?? 0
                   })
                   .OrderByDescending(x => x.SoLuong)
                   .Take(3)
                   .ToList();

            // --- 3. REVENUE CHART (Last 7 Days) ---
            var sevenDaysAgo = today.AddDays(-6);

            var rawData = db.DonHangs
                .Where(d => d.NgayTao != null && d.NgayTao >= sevenDaysAgo)
                .Select(d => new {
                    NgayTao = d.NgayTao.Value,
                    TongTien = d.TongTien ?? 0
                })
                .ToList();

            // DEBUG: In ra console để kiểm tra
            System.Diagnostics.Debug.WriteLine($"=== KIỂM TRA DỮ LIỆU ===");
            System.Diagnostics.Debug.WriteLine($"Hôm nay: {today:dd/MM/yyyy}");
            System.Diagnostics.Debug.WriteLine($"Từ ngày: {sevenDaysAgo:dd/MM/yyyy}");
            System.Diagnostics.Debug.WriteLine($"Tổng đơn hàng tìm thấy: {rawData.Count}");

            var chartData = new List<decimal>();
            var chartLabels = new List<string>();

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);

                decimal dailyTotal = rawData
                    .Where(d => d.NgayTao.Date == targetDate)
                    .Sum(d => d.TongTien);

                chartData.Add(dailyTotal);
                chartLabels.Add(targetDate.ToString("dd/MM"));

                // DEBUG: In từng ngày
                var ordersForDay = rawData.Where(d => d.NgayTao.Date == targetDate).ToList();
                System.Diagnostics.Debug.WriteLine($"{targetDate:dd/MM/yyyy}: {dailyTotal:N0}đ (Số đơn: {ordersForDay.Count})");
            }

            ViewBag.ChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartData);
            ViewBag.ChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(chartLabels);

            ViewBag.Revenue = revenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalCups = totalCups;

            return View(topProducts);
        }
    }
}
