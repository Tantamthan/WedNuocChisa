using System;
using System.Linq;
using System.Web.Mvc;
using HisaTeaPOS.Models;
using HisaTeaPOS.Filters;

namespace HisaTeaPOS.Controllers
{
    [AdminOnly]
    public class PromotionController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        public ActionResult Index()
        {
            return View(db.KhuyenMais.ToList());
        }

        [HttpPost]
        public ActionResult Save(KhuyenMai km)
        {
            if (km.MaKM == 0)
            {
                km.TrangThai = true; // Mặc định bật
                db.KhuyenMais.Add(km);
            }
            else
            {
                var exist = db.KhuyenMais.Find(km.MaKM);
                if (exist != null)
                {
                    exist.MaCode = km.MaCode;
                    exist.TenCT = km.TenCT;
                    exist.LoaiKM = km.LoaiKM;
                    exist.GiaTri = km.GiaTri;
                    exist.NgayBatDau = km.NgayBatDau;
                    exist.NgayKetThuc = km.NgayKetThuc;
                }
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            var km = db.KhuyenMais.Find(id);
            db.KhuyenMais.Remove(km);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult ToggleStatus(int id)
        {
            var km = db.KhuyenMais.Find(id);
            if (km != null)
            {
                km.TrangThai = !km.TrangThai; // Đảo trạng thái
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}