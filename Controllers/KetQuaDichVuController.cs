using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    [PhanQuyen(HangSo.VaiTro.Admin, HangSo.VaiTro.NhanVien)]
    public class KetQuaDichVuController : Controller
    {
        private static readonly string[] TrangThaiHopLe =
        {
            HangSo.TrangThaiKetQua.ChoThucHien,
            HangSo.TrangThaiKetQua.DangThucHien,
            HangSo.TrangThaiKetQua.HoanThanh
        };

        private readonly AppDbContext _context;

        public KetQuaDichVuController(AppDbContext context)
        {
            _context = context;
        }

        private bool LaAdmin =>
            HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) == HangSo.VaiTro.Admin;

        private int MaTaiKhoanHienTai =>
            HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan) ?? 0;

        // ===== Giới hạn theo phân công (chỉ áp dụng cho tài khoản Nhân viên) =====

        // Các ca mà nhân viên đang đăng nhập được phân công (phân công chưa hủy)
        private IQueryable<int> CaCuaToi()
        {
            var maTk = MaTaiKhoanHienTai;
            return from pc in _context.PhanCongNhanVien
                   join nv in _context.NhanVien on pc.MaNhanVien equals nv.MaNhanVien
                   where nv.MaTaiKhoan == maTk
                         && nv.TrangThai
                         && pc.TrangThai != HangSo.TrangThaiPhanCong.DaHuy
                   select pc.MaCaDichVu;
        }

        private async Task<bool> DuocPhanCongVaoCa(int maCa)
        {
            if (LaAdmin) return true;
            return await CaCuaToi().AnyAsync(c => c == maCa);
        }

        private async Task<bool> DaLienKetNhanVien()
        {
            var maTk = MaTaiKhoanHienTai;
            return await _context.NhanVien.AnyAsync(n => n.MaTaiKhoan == maTk && n.TrangThai);
        }

        // ===== Danh sách =====

        public async Task<IActionResult> Index()
        {
            var laAdmin = LaAdmin;

            var query = from kq in _context.KetQuaDichVu.AsNoTracking()
                        join ca in _context.CaDichVu.AsNoTracking() on kq.MaCaDichVu equals ca.MaCaDichVu
                        select new { kq, ca };

            if (!laAdmin)
            {
                var caCuaToi = CaCuaToi();
                query = query.Where(x => caCuaToi.Contains(x.ca.MaCaDichVu));
                ViewBag.ChuaLienKet = !await DaLienKetNhanVien();
            }

            var rows = await query.OrderByDescending(x => x.kq.ThoiGianGhiNhan).ToListAsync();

            var tenKhach = await _context.TaiKhoan.AsNoTracking().ToDictionaryAsync(x => x.MaTaiKhoan, x => x.HoTen);
            var tenDv = await _context.DichVus.AsNoTracking().ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
            var tenTc = await _context.ThuCungs.AsNoTracking().ToDictionaryAsync(x => x.MaThuCung, x => x.HoTen);

            var ds = rows.Select(x => new KetQuaHienThi
            {
                MaKetQuaDichVu = x.kq.MaKetQuaDichVu,
                MaCaDichVu = x.ca.MaCaDichVu,
                MaKhachHang = x.kq.MaKhachHang,
                TenKhachHang = tenKhach.GetValueOrDefault(x.kq.MaKhachHang, "#" + x.kq.MaKhachHang),
                TenDichVu = tenDv.GetValueOrDefault(x.ca.MaDichVu, "#" + x.ca.MaDichVu),
                TenThuCung = tenTc.GetValueOrDefault(x.ca.MaThuCung, "#" + x.ca.MaThuCung),
                ThoiGianBatDau = x.ca.ThoiGianBatDau,
                TrangThaiCa = x.ca.TrangThai,
                TrangThai = x.kq.TrangThaiKetQuaDichVu,
                ThoiGianGhiNhan = x.kq.ThoiGianGhiNhan,
                GhiChu = x.kq.GhiChu,
                // Ca đã lên lịch: Admin/Nhân viên được phân công sửa được.
                // Ca đã hoàn thành: chỉ Admin. Ca đã hủy: không sửa.
                CoTheSua = x.ca.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich
                           || (x.ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh && laAdmin)
            }).ToList();

            return View(ds);
        }

        // ===== Thêm =====

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await NapLuaChon();
            return View(new KetQuaDichVu());
        }

        // Danh sách khách đủ điều kiện của ca (dùng cho dropdown phụ thuộc)
        [HttpGet]
        public async Task<IActionResult> KhachTheoCa(int maCaDichVu)
        {
            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == maCaDichVu);
            if (ca == null || ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Json(new List<object>());

            if (!await DuocPhanCongVaoCa(maCaDichVu))
                return Json(new List<object>());

            var daCoKetQua = _context.KetQuaDichVu.Where(k => k.MaCaDichVu == maCaDichVu).Select(k => k.MaKhachHang);

            var ds = await KhachDaXacNhan(ca)
                .Where(t => !daCoKetQua.Contains(t.MaTaiKhoan))
                .Distinct()
                .OrderBy(t => t.HoTen)
                .Select(t => new { t.MaTaiKhoan, t.HoTen, t.TenDangNhap })
                .ToListAsync();

            return Json(ds);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("MaCaDichVu,MaKhachHang,TrangThaiKetQuaDichVu,GhiChu")] KetQuaDichVu kq)
        {
            var ca = await _context.CaDichVu.FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);

            if (ca == null)
            {
                ModelState.AddModelError(nameof(kq.MaCaDichVu), "Ca dịch vụ không tồn tại.");
            }
            else if (ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHuy)
            {
                ModelState.AddModelError(nameof(kq.MaCaDichVu), "Không thể ghi nhận kết quả cho ca đã hủy.");
            }
            else if (ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh)
            {
                ModelState.AddModelError(nameof(kq.MaCaDichVu), "Ca đã hoàn thành, không thể thêm kết quả mới.");
            }
            else if (!await DuocPhanCongVaoCa(ca.MaCaDichVu))
            {
                // 8.7: nhân viên chỉ ghi kết quả cho ca mình được phân công
                ModelState.AddModelError(nameof(kq.MaCaDichVu),
                    "Bạn chưa được phân công vào ca này nên không thể ghi nhận kết quả.");
            }

            if (!TrangThaiHopLe.Contains(kq.TrangThaiKetQuaDichVu))
                ModelState.AddModelError(nameof(kq.TrangThaiKetQuaDichVu), "Trạng thái không hợp lệ.");

            if (ca != null && ModelState.IsValid)
            {
                // 8.7: chỉ khách có đăng ký "Đã xác nhận" đúng dịch vụ và đúng thú cưng của ca
                bool duDieuKien = await KhachDaXacNhan(ca).AnyAsync(t => t.MaTaiKhoan == kq.MaKhachHang);
                if (!duDieuKien)
                    ModelState.AddModelError(nameof(kq.MaKhachHang),
                        "Khách hàng không có đăng ký đã xác nhận cho dịch vụ và thú cưng của ca này.");

                // 8.6: mỗi khách chỉ một kết quả cho mỗi ca
                bool daCo = await _context.KetQuaDichVu.AnyAsync(x =>
                    x.MaCaDichVu == kq.MaCaDichVu && x.MaKhachHang == kq.MaKhachHang);
                if (daCo)
                    ModelState.AddModelError(nameof(kq.MaKhachHang),
                        "Khách hàng này đã có kết quả cho ca. Hãy sửa kết quả hiện có.");

                // 8.5: khách không được có hai ca trùng khoảng thời gian
                if (duDieuKien && !daCo &&
                    await KiemTraLichHelper.KhachBiTrungCa(_context, kq.MaKhachHang,
                        ca.ThoiGianBatDau, ca.ThoiGianKetThuc, ca.MaCaDichVu))
                    ModelState.AddModelError(nameof(kq.MaKhachHang),
                        "Khách hàng đã có ca dịch vụ khác trùng khoảng thời gian này.");

                if (kq.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.ChoThucHien
                    && ca.ThoiGianBatDau > DateTime.Now)
                    ModelState.AddModelError(nameof(kq.TrangThaiKetQuaDichVu),
                        "Ca chưa bắt đầu, chỉ được ghi trạng thái 'Chờ thực hiện'.");
            }

            if (!ModelState.IsValid)
            {
                await NapLuaChon();
                return View(kq);
            }

            kq.ThoiGianGhiNhan = DateTime.Now;
            _context.KetQuaDichVu.Add(kq);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã ghi nhận kết quả dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Sửa =====

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var kq = await _context.KetQuaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id);
            if (kq == null) return NotFound();

            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);
            if (ca == null) return NotFound();

            var loi = await KiemTraQuyenSua(ca);
            if (loi != null) return loi;

            await NapThongTin(kq, ca);
            return View(kq);
        }

        // 8.8: sau khi ca hoàn thành chỉ Admin được sửa và bắt buộc ghi lý do vào ghi chú
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("TrangThaiKetQuaDichVu,GhiChu")] KetQuaDichVu model)
        {
            var kq = await _context.KetQuaDichVu.FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id);
            if (kq == null) return NotFound();

            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);
            if (ca == null) return NotFound();

            var loi = await KiemTraQuyenSua(ca);
            if (loi != null) return loi;

            if (!TrangThaiHopLe.Contains(model.TrangThaiKetQuaDichVu))
                ModelState.AddModelError(nameof(model.TrangThaiKetQuaDichVu), "Trạng thái không hợp lệ.");

            if (model.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.ChoThucHien
                && ca.ThoiGianBatDau > DateTime.Now)
                ModelState.AddModelError(nameof(model.TrangThaiKetQuaDichVu),
                    "Ca chưa bắt đầu, chỉ được ghi trạng thái 'Chờ thực hiện'.");

            if (ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh && string.IsNullOrWhiteSpace(model.GhiChu))
                ModelState.AddModelError(nameof(model.GhiChu), "Sửa kết quả của ca đã hoàn thành phải ghi lý do.");

            if (!ModelState.IsValid)
            {
                model.MaKetQuaDichVu = kq.MaKetQuaDichVu;
                model.MaCaDichVu = kq.MaCaDichVu;
                model.MaKhachHang = kq.MaKhachHang;
                await NapThongTin(kq, ca);
                return View(model);
            }

            kq.TrangThaiKetQuaDichVu = model.TrangThaiKetQuaDichVu;
            kq.GhiChu = model.GhiChu;
            kq.ThoiGianGhiNhan = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật kết quả dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Hàm dùng chung =====

        // Khách hàng (đang hoạt động) có lịch hẹn đã xác nhận đúng dịch vụ và đúng thú cưng của ca
        private IQueryable<TaiKhoan> KhachDaXacNhan(CaDichVu ca)
        {
            var maDichVu = ca.MaDichVu;
            var maThuCung = ca.MaThuCung;

            return from l in _context.LichHen
                   join tk in _context.TaiKhoan on l.MaTaiKhoan equals tk.MaTaiKhoan
                   where l.MaDichVu == maDichVu
                         && l.MaThuCung == maThuCung
                         && l.TrangThai == HangSo.TrangThaiLichHen.DaXacNhan
                         && tk.VaiTro == HangSo.VaiTro.KhachHang
                         && tk.TrangThai
                   select tk;
        }

        private async Task<IActionResult?> KiemTraQuyenSua(CaDichVu ca)
        {
            if (ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHuy)
            {
                TempData["Loi"] = "Ca đã hủy, không thể sửa kết quả.";
                return RedirectToAction(nameof(Index));
            }

            // Nhân viên chỉ sửa kết quả của ca mình được phân công
            if (!await DuocPhanCongVaoCa(ca.MaCaDichVu))
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");

            if (ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh && !LaAdmin)
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");

            return null;
        }

        private async Task NapThongTin(KetQuaDichVu kq, CaDichVu ca)
        {
            ViewBag.TenKhachHang = await _context.TaiKhoan.AsNoTracking()
                .Where(x => x.MaTaiKhoan == kq.MaKhachHang).Select(x => x.HoTen).FirstOrDefaultAsync()
                ?? ("#" + kq.MaKhachHang);
            ViewBag.TenDichVu = await _context.DichVus.AsNoTracking()
                .Where(x => x.MaDichVu == ca.MaDichVu).Select(x => x.TenDichVu).FirstOrDefaultAsync()
                ?? ("#" + ca.MaDichVu);
            ViewBag.TenThuCung = await _context.ThuCungs.AsNoTracking()
                .Where(x => x.MaThuCung == ca.MaThuCung).Select(x => x.HoTen).FirstOrDefaultAsync()
                ?? ("#" + ca.MaThuCung);
            ViewBag.CaDaHoanThanh = ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh;
        }

        private async Task NapLuaChon()
        {
            var truyVan = _context.CaDichVu.AsNoTracking()
                .Where(x => x.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich);

            // Nhân viên chỉ thấy các ca mình được phân công
            if (!LaAdmin)
            {
                var caCuaToi = CaCuaToi();
                truyVan = truyVan.Where(c => caCuaToi.Contains(c.MaCaDichVu));
                ViewBag.ChuaLienKet = !await DaLienKetNhanVien();
            }

            var cas = await truyVan.OrderBy(x => x.ThoiGianBatDau).ToListAsync();
            var tenDv = await _context.DichVus.AsNoTracking().ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
            var tenTc = await _context.ThuCungs.AsNoTracking().ToDictionaryAsync(x => x.MaThuCung, x => x.HoTen);

            ViewBag.CaDichVus = cas.Select(c => new SelectListItem(
                $"Ca {c.MaCaDichVu} | {tenDv.GetValueOrDefault(c.MaDichVu, "?")} | {tenTc.GetValueOrDefault(c.MaThuCung, "?")} | {c.ThoiGianBatDau:dd/MM/yyyy HH:mm}",
                c.MaCaDichVu.ToString())).ToList();
        }
    }

    public class KetQuaHienThi
    {
        public int MaKetQuaDichVu { get; set; }
        public int MaCaDichVu { get; set; }
        public int MaKhachHang { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public string TenThuCung { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public string TrangThaiCa { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public DateTime ThoiGianGhiNhan { get; set; }
        public string? GhiChu { get; set; }
        public bool CoTheSua { get; set; }
    }
}