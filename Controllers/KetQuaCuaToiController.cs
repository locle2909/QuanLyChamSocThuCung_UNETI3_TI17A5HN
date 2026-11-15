using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    // Khách hàng chỉ được xem (không sửa) kết quả dịch vụ của chính mình
    [PhanQuyen(HangSo.VaiTro.KhachHang)]
    public class KetQuaCuaToiController : Controller
    {
        private readonly AppDbContext _context;

        public KetQuaCuaToiController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var maTk = HttpContext.LayMaTaiKhoan();

            var rows = await (
                from kq in _context.KetQuaDichVu.AsNoTracking()
                join ca in _context.CaDichVu.AsNoTracking() on kq.MaCaDichVu equals ca.MaCaDichVu
                where kq.MaKhachHang == maTk
                orderby ca.ThoiGianBatDau descending
                select new { kq, ca }).ToListAsync();

            var tenDv = await _context.DichVus.AsNoTracking().ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
            var tenTc = await _context.ThuCungs.AsNoTracking().ToDictionaryAsync(x => x.MaThuCung, x => x.HoTen);

            var ds = rows.Select(x => new KetQuaHienThi
            {
                MaKetQuaDichVu = x.kq.MaKetQuaDichVu,
                MaCaDichVu = x.ca.MaCaDichVu,
                MaKhachHang = x.kq.MaKhachHang,
                TenDichVu = tenDv.GetValueOrDefault(x.ca.MaDichVu, "#" + x.ca.MaDichVu),
                TenThuCung = tenTc.GetValueOrDefault(x.ca.MaThuCung, "#" + x.ca.MaThuCung),
                ThoiGianBatDau = x.ca.ThoiGianBatDau,
                TrangThaiCa = x.ca.TrangThai,
                TrangThai = x.kq.TrangThaiKetQuaDichVu,
                ThoiGianGhiNhan = x.kq.ThoiGianGhiNhan,
                GhiChu = x.kq.GhiChu
            }).ToList();

            return View(ds);
        }
    }
}