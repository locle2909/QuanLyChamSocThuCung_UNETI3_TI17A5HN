
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class CaDichVuController : Controller
    {
        private readonly AppDbContext _context;

        public CaDichVuController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var danhSach = await _context.CaDichVu
                .OrderByDescending(x => x.ThoiGianBatDau)
                .ToListAsync();

            return View(danhSach);
        }

        public IActionResult Create()
        {
            return View(new CaDichVu
            {
                ThoiGianBatDau = DateTime.Now.AddHours(1),
                ThoiGianKetThuc = DateTime.Now.AddHours(2),
                TrangThai = "Đã lên lịch"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CaDichVu ca)
        {
            if (ca.ThoiGianKetThuc <= ca.ThoiGianBatDau)
            {
                ModelState.AddModelError(
                    nameof(ca.ThoiGianKetThuc),
                    "Thời gian kết thúc phải sau thời gian bắt đầu.");
            }

            if (!ModelState.IsValid)
                return View(ca);

            ca.TrangThai = "Đã lên lịch";
            _context.CaDichVu.Add(ca);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Tạo ca dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var ca = await _context.CaDichVu.FindAsync(id);

            if (ca == null)
                return NotFound();

            return View(ca);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CaDichVu ca)
        {
            if (id != ca.MaCaDichVu)
                return NotFound();

            if (ca.ThoiGianKetThuc <= ca.ThoiGianBatDau)
            {
                ModelState.AddModelError(
                    nameof(ca.ThoiGianKetThuc),
                    "Thời gian kết thúc phải sau thời gian bắt đầu.");
            }

            var caCu = await _context.CaDichVu
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaCaDichVu == id);

            if (caCu == null)
                return NotFound();

            // Không cho đổi thời gian ca đã có phân công.
            bool daCoPhanCong = await _context.PhanCongNhanVien
                .AnyAsync(x => x.MaCaDichVu == id);

            if (daCoPhanCong &&
                (ca.ThoiGianBatDau != caCu.ThoiGianBatDau ||
                 ca.ThoiGianKetThuc != caCu.ThoiGianKetThuc))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ca đã có phân công. Hãy xử lý phân công trước khi đổi thời gian.");
            }

            if (!ModelState.IsValid)
                return View(ca);

            ca.TrangThai = caCu.TrangThai;
            _context.Entry(ca).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Cập nhật ca dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id)
        {
            var ca = await _context.CaDichVu.FindAsync(id);

            if (ca == null)
                return NotFound();

            if (ca.TrangThai == "Hoàn thành")
            {
                TempData["ThongBao"] =
                    "Không thể hủy ca đã hoàn thành.";
                return RedirectToAction(nameof(Index));
            }

            ca.TrangThai = "Đã hủy";
            var ketQua = await _context.KetQuaDichVu.FirstOrDefaultAsync(x => x.MaCaDichVu == ca.MaCaDichVu);

            if (ketQua != null)
            {
                ketQua.TrangThaiKetQuaDichVu = "Đã hủy";
            }

            var phanCongs = await _context.PhanCongNhanVien
                .Where(x => x.MaCaDichVu == id)
                .ToListAsync();

            foreach (var pc in phanCongs)
                pc.TrangThai = "Đã hủy";

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã hủy ca dịch vụ.";
            return RedirectToAction(nameof(Index));
        }
    }
}