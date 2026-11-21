using Microsoft.AspNetCore.Http;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

public static class PhanQuyenHelper
{
    public static bool LaAdmin(this HttpContext ctx) =>
        ctx.Session.GetString(HangSo.SessionKey.VaiTro) == HangSo.VaiTro.Admin;

    public static int LayMaTaiKhoan(this HttpContext ctx) =>
        ctx.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan) ?? 0;

    // Các ca mà nhân viên (theo tài khoản đăng nhập) được phân công, phân công chưa hủy
    public static IQueryable<int> CaCuaNhanVien(this AppDbContext db, int maTaiKhoan)
    {
        return from pc in db.PhanCongNhanVien
               join nv in db.NhanVien on pc.MaNhanVien equals nv.MaNhanVien
               where nv.MaTaiKhoan == maTaiKhoan
                     && nv.TrangThai
                     && pc.TrangThai != HangSo.TrangThaiPhanCong.DaHuy
               select pc.MaCaDichVu;
    }
}