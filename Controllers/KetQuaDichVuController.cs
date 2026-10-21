
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class KetQuaDichVuController : Controller
    {
        private readonly AppDbContext _context;

        public KetQuaDichVuController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ketQua = await (
                from kq in _context.KetQuaDichVu
                join ca in _context.CaDichVu
                    on kq.MaCaDichVu equals ca.MaCaDichVu
                orderby kq.ThoiGianGhiNhan descending
                select new KetQuaHienThi
                {
                    MaKetQuaDichVu = kq.MaKetQuaDichVu,
                    MaCaDichVu = ca.MaCaDichVu,
                    MaKhachHang = kq.MaKhachHang,
                    ThoiGianBatDau = ca.ThoiGianBatDau,
                    TrangThai = ca.TrangThai == "Đã hủy" || ca.TrangThai == "Hoàn thành" 
                        ? ca.TrangThai
                        : kq.TrangThaiKetQuaDichVu,
                    ThoiGianGhiNhan = kq.ThoiGianGhiNhan,
                    GhiChu = kq.GhiChu
                }).ToListAsync();

            return View(ketQua);
        }

        public async Task<IActionResult> Create()
        {
            await NapDanhSachCa();
            return View(new KetQuaDichVu());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KetQuaDichVu kq)
        {
            ModelState.Remove(nameof(kq.ThoiGianGhiNhan));

            var ca = await _context.CaDichVu
                .FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);

            if (ca == null)
            {
                ModelState.AddModelError(
                    nameof(kq.MaCaDichVu), "Ca dịch vụ không tồn tại.");
            }
            else if (ca.TrangThai == "Đã hủy" || ca.TrangThai == "Hoàn thành")
            {
                ModelState.AddModelError(
                    nameof(kq.MaCaDichVu),
                    "Không thể ghi nhận kết quả cho ca đã hủy hoặc hoàn thành.");
            }

            var trangThaiHopLe = new[]
            {
                "Chờ thực hiện",
                "Đang thực hiện",
                "Hoàn thành",
                "Đã hủy"
            };

            if (!trangThaiHopLe.Contains(kq.TrangThaiKetQuaDichVu))
            {
                ModelState.AddModelError(
                    nameof(kq.TrangThaiKetQuaDichVu),
                    "Trạng thái không hợp lệ.");
            }

            bool daCoKetQua = await _context.KetQuaDichVu
                .AnyAsync(x => x.MaCaDichVu == kq.MaCaDichVu);

            if (daCoKetQua)
            {
                ModelState.AddModelError(
                    nameof(kq.MaCaDichVu),
                    "Ca này đã có kết quả. Hãy cập nhật kết quả hiện có.");
            }

            if (!ModelState.IsValid || ca == null)
            {
                await NapDanhSachCa();
                return View(kq);
            }

            kq.ThoiGianGhiNhan = DateTime.Now;

            if (kq.TrangThaiKetQuaDichVu == "Hoàn thành")
            {
                ca.TrangThai = "Hoàn thành";
            }
            else if (kq.TrangThaiKetQuaDichVu == "Đã hủy")
            {
                ca.TrangThai = "Đã hủy";
            }
            else if (kq.TrangThaiKetQuaDichVu == "Đang thực hiện")
            {
                ca.TrangThai = "Đang thực hiện";
            }

            if (kq.TrangThaiKetQuaDichVu == "Đã hủy")
            {
                var phanCongs = await _context.PhanCongNhanVien
                    .Where(x => x.MaCaDichVu == ca.MaCaDichVu)
                    .ToListAsync();

                foreach (var pc in phanCongs) { 
                    pc.TrangThai = "Đã hủy";
                }
            }

            if (kq.TrangThaiKetQuaDichVu == "Hoàn thành")
            {
                var phanCongs = await _context.PhanCongNhanVien
                    .Where(x => x.MaCaDichVu == ca.MaCaDichVu)
                    .ToListAsync();

                foreach (var pc in phanCongs)
                {
                    pc.TrangThai = "Hoàn thành";
                }
            }

            _context.KetQuaDichVu.Add(kq);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã ghi nhận kết quả dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        private async Task NapDanhSachCa()
        {
            ViewBag.CaDichVus = await _context.CaDichVu
                .Where(x => x.TrangThai != "Đã hủy"
                         && x.TrangThai != "Hoàn thành")
                .OrderByDescending(x => x.ThoiGianBatDau)
                .ToListAsync();
        }
    }

    public class KetQuaHienThi
    {
        public int MaKetQuaDichVu { get; set; }
        public int MaCaDichVu { get; set; }
        public int MaKhachHang { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime ThoiGianGhiNhan { get; set; }
        public string? GhiChu { get; set; }
    }
}