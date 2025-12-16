using System;
using System.Linq;
using System.Web.Mvc;
using HisaTeaPOS.Models;
using System.Data.Entity;
using System.Collections.Generic; // Required for List<>

namespace HisaTeaPOS.Controllers
{
    public class ShiftController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        // 1. Shift History & Check-In/Out
        public ActionResult Index()
        {
            int maNV = (int?)Session["UserID"] ?? 0;
            if (maNV == 0) return RedirectToAction("Login", "Account");

            var shiftHistory = db.CaLamViecs
                .Where(c => c.MaNV == maNV)
                .OrderByDescending(c => c.GioBatDau)
                .ToList();

            var currentShift = db.CaLamViecs
                .FirstOrDefault(c => c.MaNV == maNV && c.GioKetThuc == null);

            ViewBag.CurrentShift = currentShift;

            // Calculate Personal Stats for current month
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finishedShifts = db.CaLamViecs
                .Where(c => c.MaNV == maNV && c.GioBatDau >= startOfMonth && c.GioKetThuc != null)
                .ToList();

            decimal totalHours = finishedShifts.Sum(s => s.TongGio ?? 0);
            var emp = db.NhanViens.Find(maNV);
            decimal hourlyRate = emp != null ? emp.LuongGio : 0;
            decimal estimatedSalary = totalHours * hourlyRate;

            ViewBag.TotalHours = totalHours;
            ViewBag.EstimatedSalary = estimatedSalary;
            ViewBag.HourlyRate = hourlyRate;

            // Get Schedule
            var today = DateTime.Today;
            var mySchedules = db.LichLamViecs
                .Where(l => l.MaNV == maNV && l.NgayLam >= today)
                .OrderBy(l => l.NgayLam)
                .Take(7)
                .ToList();
            ViewBag.MySchedules = mySchedules;

            return View(shiftHistory);
        }

        [HttpPost]
        public ActionResult CheckIn()
        {
            int maNV = (int?)Session["UserID"] ?? 0;
            if (maNV == 0) return RedirectToAction("Login", "Account");

            var openShift = db.CaLamViecs.Any(c => c.MaNV == maNV && c.GioKetThuc == null);
            if (!openShift)
            {
                var newShift = new CaLamViec
                {
                    MaNV = maNV,
                    GioBatDau = DateTime.Now,
                    TongGio = 0
                };
                db.CaLamViecs.Add(newShift);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult CheckOut(int maCa)
        {
            var shift = db.CaLamViecs.Find(maCa);
            if (shift != null && shift.GioKetThuc == null)
            {
                shift.GioKetThuc = DateTime.Now;
                TimeSpan duration = shift.GioKetThuc.Value - shift.GioBatDau;
                shift.TongGio = (decimal)Math.Round(duration.TotalHours, 2);

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

  
        public ActionResult Schedule()
        {
            var schedules = db.LichLamViecs
                              .Include(l => l.NhanVien)
                              .OrderByDescending(l => l.NgayLam)
                              .ToList();

            ViewBag.StaffList = db.NhanViens.ToList();
            return View(schedules);
        }

        [HttpPost]
        public ActionResult AddSchedule(DateTime ngayLam, string caLam, int maNV)
        {
            bool exists = db.LichLamViecs.Any(l => l.NgayLam == ngayLam && l.CaLam == caLam && l.MaNV == maNV);

            if (!exists)
            {
                var lich = new LichLamViec();
                lich.NgayLam = ngayLam;
                lich.CaLam = caLam;
                lich.MaNV = maNV;
                db.LichLamViecs.Add(lich);
                db.SaveChanges();
            }
            return RedirectToAction("Schedule");
        }

        public ActionResult DeleteSchedule(int id)
        {
            var lich = db.LichLamViecs.Find(id);
            if (lich != null)
            {
                db.LichLamViecs.Remove(lich);
                db.SaveChanges();
            }
            return RedirectToAction("Schedule");
        }

        // 3. Payroll Calculation (Logic TÍNH LƯƠNG CHI TIẾT)
        // [AdminOnly] // Uncomment if you have the filter
        public ActionResult Payroll(int? month, int? year)
        {
            int m = month ?? DateTime.Now.Month;
            int y = year ?? DateTime.Now.Year;

            // Cấu hình: 26 ngày * 8 tiếng = 208 giờ
            const double DINH_MUC_GIO = 208.0;
            const decimal TIEN_THUONG = 300000;

            var shifts = db.CaLamViecs
                .Where(c => c.GioBatDau.Year == y && c.GioBatDau.Month == m && c.GioKetThuc != null)
                .ToList();

            var employees = db.NhanViens.ToList();
            var payrollList = new List<PayrollViewModel>();

            foreach (var emp in employees)
            {
                var empShifts = shifts.Where(s => s.MaNV == emp.MaNV).ToList();

                if (empShifts.Count == 0) continue;

                double totalHours = 0;
                double otHours = 0;
                decimal salaryTotal = 0;

                // A. Tính Lương & OT
                foreach (var s in empShifts)
                {
                    double h = (double)(s.TongGio ?? 0);

                    if (h > 8)
                    {
                        double normalH = 8;
                        double ot = h - 8;

                        // 8 tiếng đầu nhân hệ số 1.0, OT nhân 1.5
                        salaryTotal += (decimal)(normalH * (double)emp.LuongGio) +
                                       (decimal)(ot * (double)emp.LuongGio * 1.5);

                        totalHours += h;
                        otHours += ot;
                    }
                    else
                    {
                        salaryTotal += (decimal)(h * (double)emp.LuongGio);
                        totalHours += h;
                    }
                }

                // B. Tính Thưởng Chuyên Cần
                decimal bonus = 0;
                if (totalHours >= DINH_MUC_GIO)
                {
                    bonus = TIEN_THUONG;
                }

                var row = new PayrollViewModel
                {
                    MaNV = emp.TaiKhoan,
                    HoTen = emp.HoTen,
                    LuongCoBan = emp.LuongGio,
                    TongGioLam = totalHours,
                    GioTangCa = otHours,
                    SoNgayLam = empShifts.Count,
                    TienLuongChinh = salaryTotal,
                    ThuongChuyenCan = bonus,
                    TongNhan = salaryTotal + bonus
                };

                payrollList.Add(row);
            }

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.DinhMuc = DINH_MUC_GIO;

            return View(payrollList);
        }
    }

    // --- VIEW MODEL CHO BẢNG LƯƠNG ---
    public class PayrollViewModel
    {
        public string MaNV { get; set; }
        public string HoTen { get; set; }
        public decimal LuongCoBan { get; set; }
        public double TongGioLam { get; set; }
        public double GioTangCa { get; set; }
        public int SoNgayLam { get; set; }
        public decimal TienLuongChinh { get; set; }
        public decimal TienTangCa { get; set; } // (Optional field based on your logic)
        public decimal ThuongChuyenCan { get; set; }
        public decimal TongNhan { get; set; }
    }
}