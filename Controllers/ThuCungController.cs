using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class ThuCungController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 5;

        public ThuCungController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? keyword, int? maLoaiDichVu, string? giongLoai, int? tuoi, string? trangThai, string sort = "name_asc", int page = 1)
        {
            if (page < 1) page = 1;

            var query = _context.ThuCungs
                .AsNoTracking()
                .Include(x => x.ChuyenMons)
                .ThenInclude(x => x.LoaiDichVu)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x =>
                    x.HoTen.Contains(keyword) ||
                    x.SoDienThoai.Contains(keyword) ||
                    x.Email.Contains(keyword) ||
                    x.ChuyenMons.Any(cm => cm.LoaiDichVu != null && cm.LoaiDichVu.TenLoaiDichVu.Contains(keyword)));
            }

            if (maLoaiDichVu.HasValue)
                query = query.Where(x => x.ChuyenMons.Any(cm => cm.MaLoaiDichVu == maLoaiDichVu.Value));

            if (!string.IsNullOrWhiteSpace(giongLoai))
                query = query.Where(x => x.GiongLoai == giongLoai);

            if (tuoi.HasValue)
                query = query.Where(x => x.Tuoi == tuoi.Value);

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.TrangThai == trangThai);

            sort = sort?.ToLowerInvariant() ?? "name_asc";
            query = sort switch
            {
                "name_desc" => query.OrderByDescending(x => x.HoTen).ThenBy(x => x.MaThuCung),
                "age_asc" => query.OrderBy(x => x.Tuoi).ThenBy(x => x.HoTen),
                "age_desc" => query.OrderByDescending(x => x.Tuoi).ThenBy(x => x.HoTen),
                "breed" => query.OrderBy(x => BreedOrder(x.GiongLoai)).ThenBy(x => x.GiongLoai).ThenBy(x => x.HoTen),
                _ => query.OrderBy(x => x.HoTen).ThenBy(x => x.MaThuCung)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new ThuCungIndexViewModel
            {
                Items = items,
                Keyword = keyword,
                MaLoaiDichVu = maLoaiDichVu,
                GiongLoai = giongLoai,
                Tuoi = tuoi,
                TrangThai = trangThai,
                Sort = sort,
                Page = page,
                PageSize = PageSize,
                TotalItems = totalItems,
                LoaiDichVus = await _context.LoaiDichVus.Where(x => x.TrangThai).OrderBy(x => x.TenLoaiDichVu).ToListAsync(),
                GiongLoais = await _context.ThuCungs.Select(x => x.GiongLoai).Distinct().OrderBy(x => x).ToListAsync()
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.ThuCungs
                .AsNoTracking()
                .Include(x => x.ChuyenMons)
                .ThenInclude(x => x.LoaiDichVu)
                .FirstOrDefaultAsync(x => x.MaThuCung == id);

            if (item == null) return NotFound();
            return View(item);
        }

        public IActionResult Create()
        {
            return View(new ThuCung { NgaySinh = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThuCung model)
        {
            model.Tuoi = CalculateAge(model.NgaySinh);

            if (await _context.ThuCungs.AnyAsync(x => x.Email == model.Email))
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");

            if (await _context.ThuCungs.AnyAsync(x => x.SoDienThoai == model.SoDienThoai))
                ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại đã tồn tại.");

            if (!ModelState.IsValid)
                return View(model);

            _context.ThuCungs.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Thêm thú cưng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ThuCungs.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ThuCung model)
        {
            if (id != model.MaThuCung) return NotFound();

            model.Tuoi = CalculateAge(model.NgaySinh);

            if (await _context.ThuCungs.AnyAsync(x => x.MaThuCung != id && x.Email == model.Email))
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");

            if (await _context.ThuCungs.AnyAsync(x => x.MaThuCung != id && x.SoDienThoai == model.SoDienThoai))
                ModelState.AddModelError(nameof(model.SoDienThoai), "Số điện thoại đã tồn tại.");

            if (!ModelState.IsValid)
                return View(model);

            var old = await _context.ThuCungs.FindAsync(id);
            if (old == null) return NotFound();

            old.MaTaiKhoan = model.MaTaiKhoan;
            old.HoTen = model.HoTen;
            old.NgaySinh = model.NgaySinh;
            old.GioiTinh = model.GioiTinh;
            old.SoDienThoai = model.SoDienThoai;
            old.Email = model.Email;
            old.DiaChi = model.DiaChi;
            old.GiongLoai = model.GiongLoai;
            old.Tuoi = model.Tuoi;
            old.GhiChuSucKhoe = model.GhiChuSucKhoe;
            old.TrangThai = model.TrangThai;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thú cưng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.ThuCungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaThuCung == id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.ThuCungs.FindAsync(id);
            if (item == null) return NotFound();

            var hasHistory = await _context.ChuyenMonThuCungs.AnyAsync(x => x.MaThuCung == id);
            if (hasHistory)
            {
                TempData["Error"] = "Không thể xóa thú cưng vì đã có lịch sử phân công/lịch phục vụ.";
                return RedirectToAction(nameof(Index));
            }

            _context.ThuCungs.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa thú cưng thành công.";
            return RedirectToAction(nameof(Index));
        }

        private static int CalculateAge(DateTime birthday)
        {
            var today = DateTime.Today;
            var age = today.Year - birthday.Year;
            if (birthday.Date > today.AddYears(-age)) age--;
            return Math.Max(age, 0);
        }

        private static int BreedOrder(string breed)
        {
            var value = breed.Trim().ToLowerInvariant();
            return value switch
            {
                "chó" or "cho" => 1,
                "mèo" or "meo" => 2,
                "chim" => 3,
                "thỏ" or "tho" => 4,
                "hamster" => 5,
                _ => 99
            };
        }
    }
}
