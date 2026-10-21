using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class ChuyenMonThuCungController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 10;

        public ChuyenMonThuCungController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, string? trangThai, int page = 1)
        {
            if (page < 1) page = 1;

            var query = _context.ChuyenMonThuCungs
                .AsNoTracking()
                .Include(x => x.ThuCung)
                .Include(x => x.LoaiDichVu)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    (x.ThuCung != null && x.ThuCung.HoTen.Contains(keyword)) ||
                    (x.LoaiDichVu != null && x.LoaiDichVu.TenLoaiDichVu.Contains(keyword)) ||
                    x.GiongLoaiChuyenMon.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);

            query = query.OrderBy(x => x.ThuCung!.HoTen).ThenBy(x => x.LoaiDichVu!.TenLoaiDichVu);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            return View(new ChuyenMonIndexViewModel
            {
                Items = items,
                Keyword = keyword,
                TrangThai = trangThai,
                Page = page,
                PageSize = PageSize,
                TotalItems = total
            });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ChuyenMonThuCungs
                .AsNoTracking()
                .Include(x => x.ThuCung)
                .Include(x => x.LoaiDichVu)
                .FirstOrDefaultAsync(x => x.MaChuyenMon == id);
            if (item == null) return NotFound();
            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ThuCungs = await _context.ThuCungs.Where(x => x.TrangThai == "Hoạt động").OrderBy(x => x.HoTen).ToListAsync();
            ViewBag.LoaiDichVus = await _context.LoaiDichVus.Where(x => x.TrangThai).OrderBy(x => x.TenLoaiDichVu).ToListAsync();
            return View(new ChuyenMonThuCung());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChuyenMonThuCung model)
        {
            var pet = await _context.ThuCungs.FindAsync(model.MaThuCung);
            var service = await _context.LoaiDichVus.FindAsync(model.MaLoaiDichVu);

            if (pet == null)
                ModelState.AddModelError(nameof(model.MaThuCung), "Thú cưng không tồn tại.");
            else
            {
                if (pet.TrangThai != "Hoạt động")
                    ModelState.AddModelError(nameof(model.MaThuCung), "Thú cưng ngừng hoạt động không được phân công mới.");
                model.GiongLoaiChuyenMon = pet.GiongLoai;
            }

            if (service == null || !service.TrangThai)
                ModelState.AddModelError(nameof(model.MaLoaiDichVu), "Loại dịch vụ không hợp lệ hoặc đã ngừng hoạt động.");

            var duplicate = await _context.ChuyenMonThuCungs.AnyAsync(x =>
                x.MaThuCung == model.MaThuCung &&
                x.MaLoaiDichVu == model.MaLoaiDichVu &&
                x.TrangThai == "Hiệu lực");

            if (duplicate)
                ModelState.AddModelError(nameof(model.MaLoaiDichVu), "Đã có bản ghi chăm sóc đang hiệu lực cho thú cưng và loại dịch vụ này.");

            if (pet != null && model.TuoiMon == 0)
                model.TuoiMon = pet.Tuoi;

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return View(model);
            }

            _context.ChuyenMonThuCungs.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm thông tin chăm sóc thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ChuyenMonThuCungs.FindAsync(id);
            if (item == null) return NotFound();
            await LoadSelectLists();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChuyenMonThuCung model)
        {
            if (id != model.MaChuyenMon) return NotFound();

            var pet = await _context.ThuCungs.FindAsync(model.MaThuCung);
            var service = await _context.LoaiDichVus.FindAsync(model.MaLoaiDichVu);

            if (pet == null)
                ModelState.AddModelError(nameof(model.MaThuCung), "Thú cưng không tồn tại.");
            else if (pet.TrangThai != "Hoạt động" && model.TrangThai == "Hiệu lực")
                ModelState.AddModelError(nameof(model.MaThuCung), "Thú cưng ngừng hoạt động không được có phân công hiệu lực mới.");

            if (service == null || !service.TrangThai)
                ModelState.AddModelError(nameof(model.MaLoaiDichVu), "Loại dịch vụ không hợp lệ hoặc đã ngừng hoạt động.");

            var duplicate = await _context.ChuyenMonThuCungs.AnyAsync(x =>
                x.MaChuyenMon != id &&
                x.MaThuCung == model.MaThuCung &&
                x.MaLoaiDichVu == model.MaLoaiDichVu &&
                x.TrangThai == "Hiệu lực");

            if (duplicate)
                ModelState.AddModelError(nameof(model.MaLoaiDichVu), "Đã có bản ghi hiệu lực trùng thú cưng và loại dịch vụ.");

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return View(model);
            }

            var old = await _context.ChuyenMonThuCungs.FindAsync(id);
            if (old == null) return NotFound();

            old.MaThuCung = model.MaThuCung;
            old.MaLoaiDichVu = model.MaLoaiDichVu;
            old.GiongLoaiChuyenMon = pet!.GiongLoai;
            old.TuoiMon = model.TuoiMon;
            old.GhiChu = model.GhiChu;
            old.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông tin chăm sóc thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ChuyenMonThuCungs
                .AsNoTracking()
                .Include(x => x.ThuCung)
                .Include(x => x.LoaiDichVu)
                .FirstOrDefaultAsync(x => x.MaChuyenMon == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.ChuyenMonThuCungs.FindAsync(id);
            if (item == null) return NotFound();
            _context.ChuyenMonThuCungs.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa thông tin chăm sóc thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists()
        {
            ViewBag.ThuCungs = await _context.ThuCungs.Where(x => x.TrangThai == "Hoạt động").OrderBy(x => x.HoTen).ToListAsync();
            ViewBag.LoaiDichVus = await _context.LoaiDichVus.Where(x => x.TrangThai).OrderBy(x => x.TenLoaiDichVu).ToListAsync();
        }
    }
}
