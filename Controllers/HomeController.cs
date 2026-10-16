// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Trang chủ sau đăng nhập: yêu cầu đăng nhập, số liệu tổng quan cho Admin.
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using System.Diagnostics;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        [PhanQuyen(HangSo.VaiTro.Admin, HangSo.VaiTro.NhanVien, HangSo.VaiTro.KhachHang)]
        public async Task<IActionResult> Index()
        {
            var vaiTro = HttpContext.Session.GetString(HangSo.SessionKey.VaiTro);
            ViewBag.HoTen = HttpContext.Session.GetString(HangSo.SessionKey.HoTen);
            ViewBag.VaiTro = vaiTro;

            if (vaiTro == HangSo.VaiTro.Admin)
            {
                ViewBag.TongTaiKhoan = await _context.TaiKhoan.CountAsync();
                ViewBag.TaiKhoanHoatDong = await _context.TaiKhoan.CountAsync(t => t.TrangThai);
                ViewBag.TongLoaiDichVu = await _context.LoaiDichVu.CountAsync();
                ViewBag.LoaiDichVuDangBat = await _context.LoaiDichVu.CountAsync(l => l.TrangThai);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}