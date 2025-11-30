using HisaTeaPOS.Filters;
using HisaTeaPOS.Models;
using System;
using System.Data.Entity; // REQUIRED for .Include()
using System.Linq;
using System.Web.Mvc;

namespace HisaTeaPOS.Controllers
{
    public class ShiftController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        public ActionResult Index()
        {
            int maNV = (int?)Session["UserID"] ?? 0;
            if (maNV == 0) return RedirectToAction("Login", "Account");

            // 1. Lấy lịch sử (Code cũ)
            var shiftHistory = db.CaLamViecs
                .Where(c => c.MaNV == maNV)
                .OrderByDescending(c => c.GioBatDau)
                .ToList();

            var currentShift = db.CaLamViecs
                .FirstOrDefault(c => c.MaNV == maNV && c.GioKetThuc == null);

            ViewBag.CurrentShift = currentShift;

            // 2. Lấy lịch làm việc (Code cũ)
            var today = DateTime.Today;
            var mySchedules = db.LichLamViecs
                .Where(l => l.MaNV == maNV && l.NgayLam >= today)
                .OrderBy(l => l.NgayLam)
                .Take(7)
                .ToList();
            ViewBag.MySchedules = mySchedules;

            // --- 3. PHẦN MỚI: TÍNH LƯƠNG & GIỜ LÀM THÁNG NÀY ---

            // Lấy ngày đầu tháng (Ví dụ: 01/11/2025)
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Lấy các ca đã hoàn thành trong tháng
            var finishedShifts = db.CaLamViecs
                .Where(c => c.MaNV == maNV && c.GioBatDau >= startOfMonth && c.GioKetThuc != null)
                .ToList();

            // Tổng giờ làm
            decimal totalHours = finishedShifts.Sum(s => s.TongGio ?? 0);

            // Lấy mức lương/giờ của nhân viên
            var emp = db.NhanViens.Find(maNV);
            decimal hourlyRate = emp != null ? emp.LuongGio : 0;

            // Tính lương ước tính = Giờ * Lương/Giờ
            decimal estimatedSalary = totalHours * hourlyRate;

            // Truyền qua ViewBag
            ViewBag.TotalHours = totalHours;
            ViewBag.EstimatedSalary = estimatedSalary;
            ViewBag.HourlyRate = hourlyRate;
            // -----------------------------------------------------

            return View(shiftHistory);
        }

        [HttpPost]
        public ActionResult CheckIn()
        {
            int maNV = (int?)Session["UserID"] ?? 0;
            if (maNV == 0) return RedirectToAction("Login", "Account");

            // Prevent starting a new shift if one is already open
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
                // Calculate total hours (rounded to 2 decimal places)
                TimeSpan duration = shift.GioKetThuc.Value - shift.GioBatDau;
                shift.TongGio = (decimal)Math.Round(duration.TotalHours, 2);

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // 2. Schedule Management (For Managers)
        [AdminOnly]
        public ActionResult Schedule()
        {
            // List schedules, include Employee info
            var schedules = db.LichLamViecs
                              .Include(l => l.NhanVien) // Requires System.Data.Entity
                              .OrderByDescending(l => l.NgayLam)
                              .ToList();

            // Dropdown list for Staff
            ViewBag.StaffList = db.NhanViens.ToList();

            return View(schedules);
        }

        [HttpPost]
        public ActionResult AddSchedule(DateTime ngayLam, string caLam, int maNV)
        {
            // Prevent duplicate schedule for the same person, same day, same shift
            bool exists = db.LichLamViecs.Any(l => l.NgayLam == ngayLam && l.CaLam == caLam && l.MaNV == maNV);

            if (!exists)
            {
                var lich = new LichLamViec();
                lich.NgayLam = ngayLam;
                lich.CaLam = caLam;
                lich.MaNV = maNV;
                // lich.GhiChu = ""; 

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

        // 3. Payroll Calculation (Optional/Advanced)
        [AdminOnly]
        public ActionResult Payroll()
        {
            var payrollData = db.CaLamViecs
                .GroupBy(c => c.NhanVien)
                .Select(g => new
                {
                    NhanVien = g.Key,
                    TongGio = g.Sum(x => x.TongGio),
                    LuongTamTinh = g.Sum(x => x.TongGio * x.NhanVien.LuongGio)
                }).ToList();

            return View(payrollData);
        }
    }
}