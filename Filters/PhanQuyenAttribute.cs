// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Phân quyền: kiểm tra Session và vai trò trước khi Action chạy.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;

// Dùng: [PhanQuyen(HangSo.VaiTro.Admin)] hoặc [PhanQuyen(HangSo.VaiTro.Admin, HangSo.VaiTro.NhanVien)]
// Đặt trên Controller hoặc Action. Chạy ở server trước khi Action xử lý.
public class PhanQuyenAttribute : ActionFilterAttribute
{
    private readonly string[] _vaiTroChoPhep;

    public PhanQuyenAttribute(params string[] vaiTroChoPhep)
    {
        _vaiTroChoPhep = vaiTroChoPhep;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var vaiTro = context.HttpContext.Session.GetString(HangSo.SessionKey.VaiTro);

        if (vaiTro == null)
        {
            // Chưa đăng nhập
            context.Result = new RedirectToActionResult("DangNhap", "TaiKhoan", null);
            return;
        }

        if (!_vaiTroChoPhep.Contains(vaiTro))
        {
            // Đã đăng nhập nhưng sai vai trò
            context.Result = new RedirectToActionResult("KhongCoQuyen", "TaiKhoan", null);
        }
    }
}