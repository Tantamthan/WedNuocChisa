using HisaTeaPOS.Filters;
using HisaTeaPOS.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

[AdminOnly]
public class RecipeController : Controller
{
    private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

    // Hiển thị danh sách sản phẩm
    public ActionResult Index()
    {
        var products = db.SanPhams.ToList();
        ViewBag.NguyenLieuList = new SelectList(db.NguyenLieux.ToList(), "MaNL", "Ten");
        return View(products);
    }

    // Load công thức (AJAX)
    public ActionResult GetRecipe(int productId)
    {
        var recipes = db.CongThucs.Where(c => c.MaSP == productId).Select(c => new {
            c.MaCongThuc,
            c.MaNL,
            TenNL = c.NguyenLieu.Ten,
            c.DinhLuong,
            DonVi = c.NguyenLieu.DonVi
        }).ToList();
        return Json(recipes, JsonRequestBehavior.AllowGet);
    }

    // Thêm nguyên liệu
    [HttpPost]
    public ActionResult AddIngredient(int maSP, int maNL, decimal dinhLuong)
    {
        var ct = new CongThuc { MaSP = maSP, MaNL = maNL, DinhLuong = dinhLuong };
        db.CongThucs.Add(ct);
        db.SaveChanges();
        return Json(new { success = true });
    }

    // Xóa nguyên liệu
    [HttpPost]
    public ActionResult RemoveIngredient(int maCongThuc)
    {
        var ct = db.CongThucs.Find(maCongThuc);
        if (ct != null)
        {
            db.CongThucs.Remove(ct);
            db.SaveChanges();
        }
        return Json(new { success = true });
    }

    [HttpPost]
    public ActionResult SaveProduct(SanPham model, HttpPostedFileBase imageFile)
    {
        try
        {
            // --- XỬ LÝ ẢNH ---
            // Nếu người dùng có upload ảnh mới
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                // 1. Tạo tên file độc nhất (tránh trùng lặp)
                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                string extension = Path.GetExtension(imageFile.FileName);
                fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;

             
                string folderPath = Server.MapPath("~/Pictures");

                // Nếu thư mục chưa có thì tạo mới
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 3. Lưu file vào server
                string path = Path.Combine(folderPath, fileName);
                imageFile.SaveAs(path);

                // 4. Cập nhật đường dẫn vào Model để lưu xuống DB
                model.HinhAnh = "/Pictures/" + fileName;
            }

            // --- LƯU DATABASE ---
            if (model.MaSP == 0)
            {
                // THÊM MỚI
                if (string.IsNullOrEmpty(model.HinhAnh))
                    model.HinhAnh = "https://via.placeholder.com/150"; // Ảnh mặc định

                model.NgungBan = false; // Mặc định là đang bán
                db.SanPhams.Add(model);
            }
            else
            {
                // CẬP NHẬT
                var sp = db.SanPhams.Find(model.MaSP);
                if (sp != null)
                {
                    sp.Ten = model.Ten;
                    sp.GiaBan = model.GiaBan;
                    sp.DanhMuc = model.DanhMuc;
                    sp.NgungBan = model.NgungBan; // Cập nhật trạng thái Ngừng Bán

                    // Chỉ cập nhật ảnh nếu có upload ảnh mới
                    if (!string.IsNullOrEmpty(model.HinhAnh))
                    {
                        sp.HinhAnh = model.HinhAnh;
                    }
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
           
            return RedirectToAction("Index");
        }
    }


    [HttpPost]
  
    public ActionResult ToggleStatus(int id)
    {
        var sp = db.SanPhams.Find(id);
        if (sp != null)
        {
            sp.NgungBan = !sp.NgungBan;

            db.SaveChanges();
            return Json(new { success = true, newStatus = sp.NgungBan });
        }
        return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
    }

    // Xóa sản phẩm (Xóa mềm)
    public ActionResult DeleteProduct(int id)
    {
        var sp = db.SanPhams.Find(id);
        if (sp != null)
        {
            // Đánh dấu ngừng bán thay vì xóa vĩnh viễn
            sp.NgungBan = true;
            db.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}