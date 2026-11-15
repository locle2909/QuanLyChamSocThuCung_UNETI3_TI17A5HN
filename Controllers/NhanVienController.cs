using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using System.Globalization;
using System.Text;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    [PhanQuyen(HangSo.VaiTro.Admin)]
    public class NhanVienController : Controller
    {
        private readonly AppDbContext _context;

        public NhanVienController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? tuKhoa, string? chuyenMon, string? trangThai, bool chuaLienKet = false)
        {
            var tenTaiKhoan = await _context.TaiKhoan.AsNoTracking()
                .ToDictionaryAsync(x => x.MaTaiKhoan, x => x.TenDangNhap);

            var chuyenMonTheoNv = (await _context.ChuyenMonNhanVien.AsNoTracking().ToListAsync())
                .GroupBy(x => x.MaNhanVien)
                .ToDictionary(g => g.Key, g => g.Select(x => x.TenChuyenMon).ToList());

            var danhSach = await _context.NhanVien.AsNoTracking()
                .OrderBy(x => x.MaNhanVien)
                .ToListAsync();
            var tongSo = danhSach.Count;

            // Từ khóa: tìm theo mã, họ tên, số điện thoại, email, tên tài khoản (không phân biệt hoa thường và dấu)
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = BoDau(tuKhoa.Trim());
                danhSach = danhSach.Where(n =>
                    n.MaNhanVien.ToString() == kw
                    || BoDau(n.HoTen).Contains(kw)
                    || BoDau(n.SoDienThoai).Contains(kw)
                    || BoDau(n.Email).Contains(kw)
                    || (n.MaTaiKhoan != null
                        && tenTaiKhoan.TryGetValue(n.MaTaiKhoan.Value, out var tk)
                        && BoDau(tk).Contains(kw))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(chuyenMon))
            {
                danhSach = danhSach.Where(n =>
                    chuyenMonTheoNv.TryGetValue(n.MaNhanVien, out var ds) && ds.Contains(chuyenMon)
                ).ToList();
            }

            if (trangThai == "hoat-dong")
                danhSach = danhSach.Where(n => n.TrangThai).ToList();
            else if (trangThai == "ngung")
                danhSach = danhSach.Where(n => !n.TrangThai).ToList();

            if (chuaLienKet)
                danhSach = danhSach.Where(n => n.MaTaiKhoan == null).ToList();

            ViewBag.TenTaiKhoan = tenTaiKhoan;
            ViewBag.ChuyenMon = chuyenMonTheoNv.ToDictionary(x => x.Key, x => string.Join(", ", x.Value));
            ViewBag.DanhSachChuyenMon = HangSo.NhomDichVu.DanhSach.ToList();
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.ChuyenMonChon = chuyenMon;
            ViewBag.TrangThai = trangThai;
            ViewBag.ChuaLienKet = chuaLienKet;
            ViewBag.TongSo = tongSo;

            return View(danhSach);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            NapChuyenMon(new List<string>());
            await NapTaiKhoan(null);
            return View(new NhanVien());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("HoTen,SoDienThoai,Email,TrangThai,MaTaiKhoan")] NhanVien nhanVien,
            string[]? tenChuyenMons)
        {
            var chuyenMons = LocChuyenMon(tenChuyenMons);

            if (chuyenMons.Count == 0)
                ModelState.AddModelError("tenChuyenMons", "Vui lòng chọn ít nhất một chuyên môn.");

            await KiemTraTaiKhoan(nhanVien, 0);

            if (!ModelState.IsValid)
            {
                NapChuyenMon(chuyenMons);
                await NapTaiKhoan(nhanVien.MaTaiKhoan);
                return View(nhanVien);
            }

            // Lưu nhân viên và chuyên môn trong cùng một giao dịch.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.NhanVien.Add(nhanVien);
                await _context.SaveChangesAsync();

                foreach (var tenChuyenMon in chuyenMons)
                {
                    _context.ChuyenMonNhanVien.Add(new ChuyenMonNhanVien
                    {
                        MaNhanVien = nhanVien.MaNhanVien,
                        TenChuyenMon = tenChuyenMon,
                        GhiChu = "Chuyên môn của nhân viên"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["ThongBao"] = "Thêm nhân viên thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Không thể thêm nhân viên. Vui lòng kiểm tra lại dữ liệu.");
                NapChuyenMon(chuyenMons);
                await NapTaiKhoan(nhanVien.MaTaiKhoan);
                return View(nhanVien);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _context.NhanVien.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaNhanVien == id);
            if (nhanVien == null) return NotFound();

            var daChon = await _context.ChuyenMonNhanVien.AsNoTracking()
                .Where(x => x.MaNhanVien == id)
                .Select(x => x.TenChuyenMon)
                .ToListAsync();

            NapChuyenMon(daChon);
            await NapTaiKhoan(nhanVien.MaTaiKhoan);
            return View(nhanVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("MaNhanVien,HoTen,SoDienThoai,Email,TrangThai,MaTaiKhoan")] NhanVien model,
            string[]? tenChuyenMons)
        {
            if (id != model.MaNhanVien) return NotFound();

            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null) return NotFound();

            var chuyenMons = LocChuyenMon(tenChuyenMons);
            if (chuyenMons.Count == 0)
                ModelState.AddModelError("tenChuyenMons", "Vui lòng chọn ít nhất một chuyên môn.");

            await KiemTraTaiKhoan(model, id);

            var hienCo = await _context.ChuyenMonNhanVien.Where(x => x.MaNhanVien == id).ToListAsync();
            var biBo = hienCo.Where(x => !chuyenMons.Contains(x.TenChuyenMon)).ToList();

            // Không cho bỏ chuyên môn nếu nhân viên đang có phân công còn hiệu lực thuộc nhóm đó
            if (biBo.Count > 0)
            {
                var dangDung = await (
                    from pc in _context.PhanCongNhanVien
                    join ca in _context.CaDichVu on pc.MaCaDichVu equals ca.MaCaDichVu
                    join dv in _context.DichVus on ca.MaDichVu equals dv.MaDichVu
                    where pc.MaNhanVien == id && pc.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc
                    select dv.NhomDichVu).Distinct().ToListAsync();

                var biChan = biBo
                    .Where(x => dangDung.Any(d => d.Trim() == x.TenChuyenMon.Trim()))
                    .Select(x => x.TenChuyenMon)
                    .ToList();

                if (biChan.Count > 0)
                    ModelState.AddModelError("tenChuyenMons",
                        "Không thể bỏ chuyên môn đang có phân công còn hiệu lực: " + string.Join(", ", biChan));
            }

            if (!ModelState.IsValid)
            {
                NapChuyenMon(chuyenMons);
                await NapTaiKhoan(model.MaTaiKhoan);
                return View(model);
            }

            nhanVien.HoTen = model.HoTen;
            nhanVien.SoDienThoai = model.SoDienThoai;
            nhanVien.Email = model.Email;
            nhanVien.TrangThai = model.TrangThai;
            nhanVien.MaTaiKhoan = model.MaTaiKhoan;

            // Đồng bộ chuyên môn: xóa cái bị bỏ, thêm cái mới chọn
            _context.ChuyenMonNhanVien.RemoveRange(biBo);
            foreach (var ten in chuyenMons.Where(t => !hienCo.Any(x => x.TenChuyenMon == t)))
            {
                _context.ChuyenMonNhanVien.Add(new ChuyenMonNhanVien
                {
                    MaNhanVien = id,
                    TenChuyenMon = ten,
                    GhiChu = "Chuyên môn của nhân viên"
                });
            }

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Cập nhật nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var nhanVien = await _context.NhanVien.FindAsync(id);
            if (nhanVien == null) return NotFound();

            bool daPhanCong = await _context.PhanCongNhanVien.AnyAsync(x => x.MaNhanVien == id);
            if (daPhanCong)
            {
                TempData["ThongBao"] =
                    "Nhân viên đã có lịch sử phân công, không thể xóa. Hãy chuyển trạng thái ngừng hoạt động.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa chuyên môn trước khi xóa nhân viên.
            var chuyenMons = await _context.ChuyenMonNhanVien.Where(x => x.MaNhanVien == id).ToListAsync();
            _context.ChuyenMonNhanVien.RemoveRange(chuyenMons);
            _context.NhanVien.Remove(nhanVien);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã xóa nhân viên.";
            return RedirectToAction(nameof(Index));
        }
        // Bỏ dấu tiếng Việt và chuyển về chữ thường để so sánh: "Đức" -> "duc"
        private static string BoDau(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var chuoi = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(chuoi.Length);
            foreach (var c in chuoi)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .ToLowerInvariant();
        }
        // Chỉ giữ các chuyên môn nằm trong danh sách nhóm dịch vụ, bỏ giá trị trùng
        private static List<string> LocChuyenMon(string[]? tenChuyenMons)
        {
            var hopLe = HangSo.NhomDichVu.DanhSach;
            return (tenChuyenMons ?? Array.Empty<string>())
                .Where(x => hopLe.Contains(x))
                .Distinct()
                .ToList();
        }

        // Tài khoản được gán phải tồn tại, có vai trò Nhân viên và chưa gán cho nhân viên khác
        private async Task KiemTraTaiKhoan(NhanVien nv, int maNhanVienHienTai)
        {
            if (nv.MaTaiKhoan == null) return;

            var tk = await _context.TaiKhoan.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaTaiKhoan == nv.MaTaiKhoan);

            if (tk == null || tk.VaiTro != HangSo.VaiTro.NhanVien)
            {
                ModelState.AddModelError(nameof(nv.MaTaiKhoan),
                    "Tài khoản không tồn tại hoặc không có vai trò Nhân viên.");
            }
            else if (await _context.NhanVien.AnyAsync(x =>
                         x.MaTaiKhoan == nv.MaTaiKhoan && x.MaNhanVien != maNhanVienHienTai))
            {
                ModelState.AddModelError(nameof(nv.MaTaiKhoan),
                    "Tài khoản này đã được gán cho nhân viên khác.");
            }
        }

        // Danh sách tài khoản vai trò Nhân viên còn trống (cộng thêm tài khoản đang gán)
        private async Task NapTaiKhoan(int? maTaiKhoanHienTai)
        {
            var daGan = _context.NhanVien
                .Where(n => n.MaTaiKhoan != null && n.MaTaiKhoan != maTaiKhoanHienTai)
                .Select(n => n.MaTaiKhoan);

            ViewBag.DsTaiKhoan = await _context.TaiKhoan.AsNoTracking()
                .Where(t => t.VaiTro == HangSo.VaiTro.NhanVien
                            && (t.TrangThai || t.MaTaiKhoan == maTaiKhoanHienTai)
                            && !daGan.Contains(t.MaTaiKhoan))
                .OrderBy(t => t.HoTen)
                .ToListAsync();
        }

        private void NapChuyenMon(List<string> daChon)
        {
            ViewBag.DanhSachChuyenMon = HangSo.NhomDichVu.DanhSach.ToList();
            ViewBag.DaChon = daChon;
        }
    }
}