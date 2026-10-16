
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly AppDbContext _context;

        public NhanVienController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.NhanVien
                .OrderBy(x => x.MaNhanVien)
                .ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new NhanVien());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVien nhanVien)
        {
            if (!ModelState.IsValid)
                return View(nhanVien);

            _context.NhanVien.Add(nhanVien);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _context.NhanVien.FindAsync(id);

            if (nhanVien == null)
                return NotFound();

            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nhanVien)
        {
            if (id != nhanVien.MaNhanVien)
                return NotFound();

            if (!ModelState.IsValid)
                return View(nhanVien);

            _context.NhanVien.Update(nhanVien);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var nhanVien = await _context.NhanVien.FindAsync(id);

            if (nhanVien == null)
                return NotFound();

            bool daPhanCong = await _context.PhanCongNhanVien
                .AnyAsync(x => x.MaNhanVien == id);

            if (daPhanCong)
            {
                TempData["ThongBao"] =
                    "Nhân viên đã có lịch sử phân công, không thể xóa. Hãy chuyển trạng thái ngừng hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            _context.NhanVien.Remove(nhanVien);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}