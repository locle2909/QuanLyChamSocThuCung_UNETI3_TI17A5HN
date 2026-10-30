
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

        // GET: NhanVien/Create
        public IActionResult Create()
        {
            NapDanhSachChuyenMon();
            return View(new NhanVien());
        }

        // POST: NhanVien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            NhanVien nhanVien,
            string[]? tenChuyenMons)
        {
            // Lọc chuyên môn hợp lệ và loại bỏ giá trị trùng.
            var danhSachHopLe = HangSo.NhomDichVu.DanhSach;

            var chuyenMons = (tenChuyenMons ?? Array.Empty<string>())
                .Where(x => danhSachHopLe.Contains(x))
                .Distinct()
                .ToList();

            if (chuyenMons.Count == 0)
            {
                ModelState.AddModelError(
                    "tenChuyenMons",
                    "Vui lòng chọn ít nhất một chuyên môn.");
            }

            if (!ModelState.IsValid)
            {
                NapDanhSachChuyenMon();
                return View(nhanVien);
            }

            // Lưu nhân viên và chuyên môn trong cùng một giao dịch.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.NhanVien.Add(nhanVien);
                await _context.SaveChangesAsync();

                foreach (var tenChuyenMon in chuyenMons)
                {
                    var chuyenMon = new ChuyenMonNhanVien
                    {
                        MaNhanVien = nhanVien.MaNhanVien,
                        TenChuyenMon = tenChuyenMon,
                        GhiChu = "Chuyên môn của nhân viên"
                    };

                    _context.ChuyenMonNhanVien.Add(chuyenMon);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["ThongBao"] = "Thêm nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Không thể thêm nhân viên. Vui lòng kiểm tra lại dữ liệu.");

                NapDanhSachChuyenMon();
                return View(nhanVien);
            }
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

            // Xóa chuyên môn trước khi xóa nhân viên.
            var chuyenMons = await _context.ChuyenMonNhanVien
                .Where(x => x.MaNhanVien == id)
                .ToListAsync();

            _context.ChuyenMonNhanVien.RemoveRange(chuyenMons);
            _context.NhanVien.Remove(nhanVien);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Nạp danh sách chuyên môn lên giao diện.
        private void NapDanhSachChuyenMon()
        {
            ViewBag.DanhSachChuyenMon =
                HangSo.NhomDichVu.DanhSach.ToList();
        }
    }
}
