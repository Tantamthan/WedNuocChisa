using HisaTeaPOS.Filters;
using HisaTeaPOS.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HisaTeaPOS.Controllers
{
    [AdminOnly]
    public class StaffController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        // 1. Hiển thị danh sách nhân viên
        public ActionResult Index()
        {
            var employees = db.NhanViens.ToList();
            return View(employees);
        }
        [HttpPost]
        public ActionResult Save(NhanVien model)
        {
            if (model.MaNV == 0)
            {
        
                if (db.NhanViens.Any(x => x.TaiKhoan == model.TaiKhoan))
                {
                   
                    return RedirectToAction("Index");
                }
                db.NhanViens.Add(model);
            }
            else
            {
            
                var emp = db.NhanViens.Find(model.MaNV);
                if (emp != null)
                {
                    emp.HoTen = model.HoTen;
                    emp.VaiTro = model.VaiTro;
                    emp.LuongGio = model.LuongGio;

                    // Chỉ cập nhật mật khẩu nếu người dùng nhập mới
                    if (!string.IsNullOrEmpty(model.MatKhau))
                    {
                        emp.MatKhau = model.MatKhau;
                    }
                 
                }
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // 3. Xóa nhân viên
        public ActionResult Delete(int id)
        {
            var emp = db.NhanViens.Find(id);
            if (emp != null)
            {
                db.NhanViens.Remove(emp);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}