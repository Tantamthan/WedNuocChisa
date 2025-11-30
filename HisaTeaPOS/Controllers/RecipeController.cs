using HisaTeaPOS.Filters;
using HisaTeaPOS.Models;
using System; // Required for DateTime and Exception
using System.IO; // Required for Path
using System.Linq;
using System.Web; // Required for HttpPostedFileBase
using System.Web.Mvc;
[AdminOnly]
public class RecipeController : Controller
{
    private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

    // Hiển thị danh sách sản phẩm để chọn cấu hình công thức
    public ActionResult Index()
    {
        var products = db.SanPhams.ToList();
        ViewBag.NguyenLieuList = new SelectList(db.NguyenLieux.ToList(), "MaNL", "Ten");
        return View(products);
    }

    // Load công thức của 1 sản phẩm (cho AJAX Modal)
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

    // Thêm nguyên liệu vào công thức
    [HttpPost]
    public ActionResult AddIngredient(int maSP, int maNL, decimal dinhLuong)
    {
        var ct = new CongThuc { MaSP = maSP, MaNL = maNL, DinhLuong = dinhLuong };
        db.CongThucs.Add(ct);
        db.SaveChanges();
        return Json(new { success = true });
    }

    // Xóa nguyên liệu khỏi công thức
    [HttpPost]
    public ActionResult RemoveIngredient(int maCongThuc)
    {
        var ct = db.CongThucs.Find(maCongThuc);
        db.CongThucs.Remove(ct);
        db.SaveChanges();
        return Json(new { success = true });
    }

    [HttpPost]
    public ActionResult SaveProduct(SanPham model, HttpPostedFileBase imageFile)
    {
        try
        {
            // 1. Xử lý lưu ảnh nếu có upload
            if (imageFile != null && imageFile.ContentLength > 0)
            {
                // Tạo tên file độc nhất để tránh trùng
                string fileName = Path.GetFileNameWithoutExtension(imageFile.FileName);
                string extension = Path.GetExtension(imageFile.FileName);
                fileName = fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension;

                // Đường dẫn lưu file (Tạo thư mục /Content/Images/Products trong dự án trước nhé)
                string folderPath = Server.MapPath("~/Pictures");

                // Tự động tạo thư mục "Pictures" nếu chưa có
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string path = Path.Combine(folderPath, fileName);
                imageFile.SaveAs(path);

                // Lưu đường dẫn vào DB (Bắt đầu bằng /Pictures/...)
                model.HinhAnh = "/Pictures/" + fileName;
            }

            if (model.MaSP == 0)
            {
                // --- THÊM MỚI ---
                if (string.IsNullOrEmpty(model.HinhAnh)) model.HinhAnh = "https://via.placeholder.com/150"; // Ảnh mặc định
                db.SanPhams.Add(model);
            }
            else
            {
                // --- CẬP NHẬT ---
                var sp = db.SanPhams.Find(model.MaSP);
                if (sp != null)
                {
                    sp.Ten = model.Ten;
                    sp.GiaBan = model.GiaBan;
                    sp.DanhMuc = model.DanhMuc;

                    // Chỉ cập nhật ảnh nếu người dùng có upload ảnh mới
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
            // Ghi log lỗi nếu cần
            return RedirectToAction("Index");
        }
    }

    // Hàm xóa sản phẩm (Nếu cần)
    public ActionResult DeleteProduct(int id)
    {
        var sp = db.SanPhams.Find(id);
        if (sp != null)
        {
            db.SanPhams.Remove(sp);
            db.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}