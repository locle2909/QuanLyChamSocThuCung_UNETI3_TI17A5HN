using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

public static class PhanQuyenHelper
{
    public static bool LaAdmin(this HttpContext ctx) =>
        ctx.Session.GetString(HangSo.SessionKey.VaiTro) == HangSo.VaiTro.Admin;

    public static int LayMaTaiKhoan(this HttpContext ctx) =>
        ctx.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan) ?? 0;

    // Các ca mà nhân viên (theo tài khoản đăng nhập) được phân công, phân công chưa hủy.
    // Chỉ tính khi tài khoản còn hoạt động, còn vai trò Nhân viên và hồ sơ nhân viên còn hoạt động.
    public static IQueryable<int> CaCuaNhanVien(this AppDbContext db, int maTaiKhoan)
    {
        return from pc in db.PhanCongNhanVien
               join nv in db.NhanVien on pc.MaNhanVien equals nv.MaNhanVien
               join tk in db.TaiKhoan on nv.MaTaiKhoan equals (int?)tk.MaTaiKhoan
               where tk.MaTaiKhoan == maTaiKhoan
                     && tk.TrangThai
                     && tk.VaiTro == HangSo.VaiTro.NhanVien
                     && nv.TrangThai
                     && pc.TrangThai != HangSo.TrangThaiPhanCong.DaHuy
               select pc.MaCaDichVu;
    }

    // Tài khoản có hồ sơ nhân viên hợp lệ (hồ sơ hoạt động, tài khoản hoạt động, đúng vai trò)
    public static Task<bool> CoHoSoNhanVienHopLe(this AppDbContext db, int maTaiKhoan)
    {
        return (from nv in db.NhanVien
                join tk in db.TaiKhoan on nv.MaTaiKhoan equals (int?)tk.MaTaiKhoan
                where tk.MaTaiKhoan == maTaiKhoan
                      && tk.TrangThai
                      && tk.VaiTro == HangSo.VaiTro.NhanVien
                      && nv.TrangThai
                select nv.MaNhanVien).AnyAsync();
    }
}
