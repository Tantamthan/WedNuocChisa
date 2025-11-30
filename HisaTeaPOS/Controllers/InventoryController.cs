using HisaTeaPOS.Filters;
using HisaTeaPOS.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HisaTeaPOS.Controllers
{
    [AdminOnly]
    public class InventoryController : Controller
    {
        private HisaTeaDB_VNEntities db = new HisaTeaDB_VNEntities();

        // 1. Show Inventory List
        public ActionResult Index()
        {
            var ingredients = db.NguyenLieux.ToList();
            return View(ingredients);
        }


        [HttpPost]
        public ActionResult QuickUpdate(int id, decimal amount)
        {
            var item = db.NguyenLieux.Find(id);
            if (item != null)
            {
                item.TonKho = (item.TonKho ?? 0) + amount;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // 3. Create Ingredient (and optionally a Topping)
        [HttpPost]
        public ActionResult Create(NguyenLieu nl, bool IsTopping = false, decimal GiaBanTopping = 0, decimal DinhLuongTopping = 0)
        {
            if (ModelState.IsValid)
            {
                // 1. Create the Ingredient (Always happens)
                if (nl.TonKho == null) nl.TonKho = 0;
                db.NguyenLieux.Add(nl);
                db.SaveChanges(); // Save immediately to generate the new MaNL (ID)

                // 2. If checkbox is checked, create the Topping
                if (IsTopping)
                {
                    var topping = new Topping();
                    topping.Ten = nl.Ten; // Topping name matches Ingredient name
                    topping.GiaBan = GiaBanTopping;
                    topping.MaNL = nl.MaNL; // Link to the ingredient we just created
                    topping.DinhLuong = DinhLuongTopping;

                    db.Toppings.Add(topping);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }

        // 4. Edit Ingredient
        [HttpPost]
        public ActionResult Edit(NguyenLieu model, decimal? DinhLuongTopping, decimal? GiaBanTopping)
        {
            if (ModelState.IsValid)
            {
                var item = db.NguyenLieux.Find(model.MaNL);
                if (item != null)
                {
                    // 1. Cập nhật thông tin Nguyên liệu (Như cũ)
                    item.Ten = model.Ten;
                    item.DonVi = model.DonVi;
                    item.DinhMucToiThieu = model.DinhMucToiThieu;
                    item.XuatXu = model.XuatXu;
                    item.ChungNhan = model.ChungNhan;

                    // 2. Cập nhật thông tin Topping (PHẦN MỚI THÊM)
                    // Tìm xem nguyên liệu này có gắn với Topping nào không
                    var topping = db.Toppings.FirstOrDefault(t => t.MaNL == model.MaNL);

                    if (topping != null)
                    {
                        // Nếu có, cập nhật luôn tên topping cho khớp tên nguyên liệu
                        topping.Ten = model.Ten;

                        // Cập nhật định lượng và giá bán nếu người dùng có nhập
                        if (DinhLuongTopping.HasValue) topping.DinhLuong = DinhLuongTopping.Value;
                        if (GiaBanTopping.HasValue) topping.GiaBan = GiaBanTopping.Value;
                    }

                    db.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }

    }
}