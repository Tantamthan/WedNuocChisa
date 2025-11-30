using HisaTeaPOS.Filters;
using HisaTeaPOS.Models;
using System;
using System.Linq;
using System.Web.Mvc;
[AdminOnly]
public class ReportController : Controller
{
    private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

    public ActionResult Index(DateTime? startDate, DateTime? endDate)
    {
        // Mặc định xem hôm nay
        var from = startDate ?? DateTime.Today;
        var to = endDate ?? DateTime.Today.AddDays(1).AddTicks(-1);

        var orders = db.DonHangs
            .Where(d => d.NgayTao >= from && d.NgayTao <= to)
            .ToList();

        // Tính toán các chỉ số
        ViewBag.TotalRevenue = orders.Sum(d => d.TongTien) ?? 0;
        ViewBag.TotalOrders = orders.Count;
        ViewBag.CashTotal = orders.Where(d => d.HinhThucTT == "cash").Sum(d => d.TongTien) ?? 0;
        ViewBag.TransferTotal = orders.Where(d => d.HinhThucTT == "transfer").Sum(d => d.TongTien) ?? 0;

        ViewBag.StartDate = from.ToString("yyyy-MM-dd");
        ViewBag.EndDate = to.ToString("yyyy-MM-dd");

        return View(orders);
    }
}