using System.Web.Mvc;

namespace HisaTeaPOS.Filters
{
    // Kế thừa từ ActionFilterAttribute để chặn request trước khi vào Controller
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Lấy Role từ Session (đã lưu lúc đăng nhập)
            var role = filterContext.HttpContext.Session["Role"] as string;

            // Nếu không phải là "Quản lý"
            if (role != "Quản lý")
            {
                // Đá về trang chủ (Tổng quan) hoặc trang lỗi
                filterContext.Result = new RedirectResult("/Home/Index");
            }

            base.OnActionExecuting(filterContext);
        }
    }
}