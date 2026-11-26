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
    public class CaDichVuController : Controller
    {
        private readonly AppDbContext _context;

        public CaDichVuController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? tuKhoa, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangThai = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            ViewBag.DsTenDichVu = await _context.DichVus.AsNoTracking()
                .ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
            ViewBag.DsTenThuCung = await _context.ThuCungs.AsNoTracking()
                .ToDictionaryAsync(x => x.MaThuCung, x => x.HoTen);

            var truyVan = _context.CaDichVu.AsNoTracking();

            // Nhân viên chỉ thấy ca mình được phân công
            if (!HttpContext.LaAdmin())
            {
                var maTk = HttpContext.LayMaTaiKhoan();
                var caCuaToi = _context.CaCuaNhanVien(maTk);
                truyVan = truyVan.Where(c => caCuaToi.Contains(c.MaCaDichVu));
                ViewBag.ChuaLienKet = !await _context.CoHoSoNhanVienHopLe(maTk);
            }

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                var maLichHenTheoKhach = _context.LichHen
                    .Where(l => _context.TaiKhoan.Any(t => t.MaTaiKhoan == l.MaTaiKhoan && t.HoTen.Contains(kw)))
                    .Select(l => l.MaLichHen);
                var maDichVuTheoTen = _context.DichVus
                    .Where(d => d.TenDichVu.Contains(kw)).Select(d => d.MaDichVu);
                var maThuCungTheoTen = _context.ThuCungs
                    .Where(t => t.HoTen.Contains(kw)).Select(t => t.MaThuCung);
                var coMa = int.TryParse(kw, out var ma);
                truyVan = truyVan.Where(c =>
                    (coMa && (c.MaCaDichVu == ma || c.MaLichHen == ma))
                    || maDichVuTheoTen.Contains(c.MaDichVu)
                    || maThuCungTheoTen.Contains(c.MaThuCung)
                    || (c.MaLichHen != null && maLichHenTheoKhach.Contains(c.MaLichHen.Value)));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                truyVan = truyVan.Where(c => c.TrangThai == trangThai);
            if (tuNgay.HasValue)
                truyVan = truyVan.Where(c => c.ThoiGianBatDau >= tuNgay.Value.Date);
            if (denNgay.HasValue)
                truyVan = truyVan.Where(c => c.ThoiGianBatDau < denNgay.Value.Date.AddDays(1));

            var danhSach = await truyVan.OrderByDescending(x => x.ThoiGianBatDau).ToListAsync();

            // Tên khách hàng của từng ca (suy ra từ lịch hẹn)
            var maLichHens = danhSach.Where(c => c.MaLichHen != null).Select(c => c.MaLichHen!.Value).ToList();
            ViewBag.TenKhachTheoLichHen = await (
                from l in _context.LichHen.AsNoTracking()
                join tk in _context.TaiKhoan.AsNoTracking() on l.MaTaiKhoan equals tk.MaTaiKhoan
                where maLichHens.Contains(l.MaLichHen)
                select new { l.MaLichHen, tk.HoTen })
                .ToDictionaryAsync(x => x.MaLichHen, x => x.HoTen);

            return View(danhSach);
        }

        // Bước 1: chọn lịch hẹn. Bước 2 (khi đã có maLichHen): xem thông tin và nhập phòng, ghi chú.
        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Create(int? maLichHen)
        {
            await NapLichHenChuaCoCa();

            if (maLichHen == null)
                return View(new CaDichVu());

            var (lich, loi) = await KiemTraLichHen(maLichHen.Value);
            if (lich == null)
            {
                TempData["Loi"] = loi;
                return RedirectToAction(nameof(Create));
            }

            await NapThongTinLichHen(lich);
            return View(new CaDichVu
            {
                MaLichHen = lich.MaLichHen,
                MaDichVu = lich.MaDichVu,
                MaThuCung = lich.MaThuCung,
                ThoiGianBatDau = lich.ThoiGianBatDau,
                ThoiGianKetThuc = lich.ThoiGianKetThuc
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Create(
            [Bind("MaLichHen,PhongHocHoacHinhThuc,GhiChu")] CaDichVu model)
        {
            if (model.MaLichHen == null)
                return RedirectToAction(nameof(Create));

            var (lich, loi) = await KiemTraLichHen(model.MaLichHen.Value);
            if (lich == null)
            {
                TempData["Loi"] = loi;
                return RedirectToAction(nameof(Create));
            }

            // Dịch vụ, thú cưng, giờ lấy từ lịch hẹn, không nhận từ form
            var ca = new CaDichVu
            {
                MaLichHen = lich.MaLichHen,
                MaDichVu = lich.MaDichVu,
                MaThuCung = lich.MaThuCung,
                ThoiGianBatDau = lich.ThoiGianBatDau,
                ThoiGianKetThuc = lich.ThoiGianKetThuc,
                PhongHocHoacHinhThuc = string.IsNullOrWhiteSpace(model.PhongHocHoacHinhThuc)
                    ? null : model.PhongHocHoacHinhThuc.Trim(),
                GhiChu = model.GhiChu,
                TrangThai = HangSo.TrangThaiCaDichVu.DaLenLich
            };

            foreach (var (truong, thongBao) in await KiemTraLichHelper.KiemTraCa(_context, ca))
                ModelState.AddModelError(truong, thongBao);

            if (await KiemTraLichHelper.KhachBiTrungCa(_context, lich.MaTaiKhoan, ca.ThoiGianBatDau, ca.ThoiGianKetThuc))
                ModelState.AddModelError(string.Empty, "Khách hàng đã có ca dịch vụ khác trùng khoảng thời gian này.");

            if (ModelState.IsValid)
            {
                // Tạo ca và dòng kết quả ban đầu của khách trong cùng một giao dịch
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.CaDichVu.Add(ca);
                    await _context.SaveChangesAsync();

                    _context.KetQuaDichVu.Add(new KetQuaDichVu
                    {
                        MaCaDichVu = ca.MaCaDichVu,
                        MaKhachHang = lich.MaTaiKhoan,
                        TrangThaiKetQuaDichVu = HangSo.TrangThaiKetQua.ChoThucHien,
                        ThoiGianGhiNhan = DateTime.Now,
                        MaNguoiCapNhat = HttpContext.LayMaTaiKhoan()
                    });
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["ThongBao"] = "Tạo ca dịch vụ thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty,
                        "Không thể tạo ca. Có thể lịch hẹn này vừa được tạo ca bởi người khác.");
                }
            }

            await NapLichHenChuaCoCa();
            await NapThongTinLichHen(lich);
            return View(ca);
        }

        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Edit(int id)
        {
            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == id);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ sửa được ca đang ở trạng thái 'Đã lên lịch'.");

            await NapTenHienThi(ca);
            return View(ca);
        }

        // Ca tạo từ lịch hẹn: chỉ sửa phòng và ghi chú. Ca cũ (chưa có lịch hẹn): sửa được cả giờ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Edit(int id,
            [Bind("MaCaDichVu,ThoiGianBatDau,ThoiGianKetThuc,PhongHocHoacHinhThuc,GhiChu")] CaDichVu model)
        {
            if (id != model.MaCaDichVu) return NotFound();

            var ca = await _context.CaDichVu.FindAsync(id);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ sửa được ca đang ở trạng thái 'Đã lên lịch'.");

            model.MaLichHen = ca.MaLichHen;
            model.MaDichVu = ca.MaDichVu;
            model.MaThuCung = ca.MaThuCung;
            model.TrangThai = ca.TrangThai;
            model.PhongHocHoacHinhThuc = string.IsNullOrWhiteSpace(model.PhongHocHoacHinhThuc)
                ? null : model.PhongHocHoacHinhThuc.Trim();

            if (ca.MaLichHen != null)
            {
                model.ThoiGianBatDau = ca.ThoiGianBatDau;
                model.ThoiGianKetThuc = ca.ThoiGianKetThuc;
            }

            bool doiGio = model.ThoiGianBatDau != ca.ThoiGianBatDau
                          || model.ThoiGianKetThuc != ca.ThoiGianKetThuc;
            bool doiPhong = model.PhongHocHoacHinhThuc != ca.PhongHocHoacHinhThuc;

            if (doiGio)
            {
                if (model.ThoiGianBatDau <= DateTime.Now)
                    ModelState.AddModelError(nameof(model.ThoiGianBatDau), "Không thể dời ca về quá khứ.");

                bool coPhanCong = await _context.PhanCongNhanVien.AnyAsync(x =>
                    x.MaCaDichVu == id && x.TrangThai != HangSo.TrangThaiPhanCong.DaHuy);
                if (coPhanCong)
                    ModelState.AddModelError(string.Empty,
                        "Ca đã có nhân viên được phân công. Hãy hủy phân công trước khi đổi thời gian.");
            }

            if (doiGio || doiPhong)
            {
                foreach (var (truong, thongBao) in await KiemTraLichHelper.KiemTraCa(_context, model, boQuaMaCa: id))
                    ModelState.AddModelError(truong, thongBao);
            }

            if (!ModelState.IsValid)
            {
                await NapTenHienThi(ca);
                return View(model);
            }

            ca.ThoiGianBatDau = model.ThoiGianBatDau;
            ca.ThoiGianKetThuc = model.ThoiGianKetThuc;
            ca.PhongHocHoacHinhThuc = model.PhongHocHoacHinhThuc;
            ca.GhiChu = model.GhiChu;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Cập nhật ca dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // Hủy ca: lịch hẹn vẫn "Đã xác nhận" để Admin có thể tạo ca khác
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Huy(int id)
        {
            var ca = await _context.CaDichVu.FindAsync(id);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ hủy được ca đang ở trạng thái 'Đã lên lịch'.");

            ca.TrangThai = HangSo.TrangThaiCaDichVu.DaHuy;

            var phanCongs = await _context.PhanCongNhanVien
                .Where(x => x.MaCaDichVu == id && x.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc)
                .ToListAsync();
            foreach (var pc in phanCongs)
                pc.TrangThai = HangSo.TrangThaiPhanCong.DaHuy;

            await _context.SaveChangesAsync();
            TempData["ThongBao"] = "Đã hủy ca dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        // Hoàn thành ca là bước dự phòng của Admin: vẫn bắt buộc khách hàng đã xác nhận mọi kết quả
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> HoanThanh(int id)
        {
            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == id);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ hoàn thành được ca đang ở trạng thái 'Đã lên lịch'.");

            if (ca.ThoiGianBatDau > DateTime.Now)
                return Loi("Ca chưa bắt đầu nên chưa thể hoàn thành.");

            if (!await KiemTraLichHelper.CoNhanVienPhuTrach(_context, id))
                return Loi("Ca chưa có nhân viên phụ trách (phân công còn hiệu lực) nên chưa thể hoàn thành.");

            var ketQuas = await _context.KetQuaDichVu
                .AsNoTracking()
                .Where(x => x.MaCaDichVu == id)
                .ToListAsync();

            if (ketQuas.Count == 0)
                return Loi("Ca chưa có kết quả dịch vụ nào để hoàn thành.");

            bool conChuaXacNhan = ketQuas.Any(x =>
                x.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.HoanThanh
                || x.ThoiGianKhachXacNhanHoanThanh == null);

            if (conChuaXacNhan)
                return Loi("Chưa thể hoàn thành ca. Cần khách hàng xác nhận tất cả kết quả dịch vụ trước.");

            await HoanThanhCaHelper.HoanThanhCaNeuDuDieuKien(_context, id);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Ca dịch vụ đã hoàn thành.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult Loi(string thongBao)
        {
            TempData["Loi"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        // Lịch hẹn phải tồn tại, đã xác nhận và chưa có ca nào chưa hủy
        private async Task<(LichHen? Lich, string? Loi)> KiemTraLichHen(int maLichHen)
        {
            var lich = await _context.LichHen.AsNoTracking().FirstOrDefaultAsync(x => x.MaLichHen == maLichHen);
            if (lich == null)
                return (null, "Lịch hẹn không tồn tại.");

            if (lich.TrangThai != HangSo.TrangThaiLichHen.DaXacNhan)
                return (null, "Chỉ tạo ca từ lịch hẹn đã xác nhận.");

            bool daCoCa = await _context.CaDichVu.AnyAsync(c =>
                c.MaLichHen == maLichHen && c.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy);
            if (daCoCa)
                return (null, "Lịch hẹn này đã có ca dịch vụ.");

            return (lich, null);
        }

        // Dropdown lịch hẹn đã xác nhận mà chưa có ca
        private async Task NapLichHenChuaCoCa()
        {
            var coCa = _context.CaDichVu
                .Where(c => c.MaLichHen != null && c.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy)
                .Select(c => c.MaLichHen);

            var ds = await (
                from l in _context.LichHen.AsNoTracking()
                join tk in _context.TaiKhoan.AsNoTracking() on l.MaTaiKhoan equals tk.MaTaiKhoan
                join tc in _context.ThuCungs.AsNoTracking() on l.MaThuCung equals tc.MaThuCung
                join dv in _context.DichVus.AsNoTracking() on l.MaDichVu equals dv.MaDichVu
                where l.TrangThai == HangSo.TrangThaiLichHen.DaXacNhan && !coCa.Contains(l.MaLichHen)
                orderby l.ThoiGianBatDau
                select new
                {
                    l.MaLichHen,
                    KhachHang = tk.HoTen,
                    ThuCung = tc.HoTen,
                    dv.TenDichVu,
                    l.ThoiGianBatDau
                }).ToListAsync();

            ViewBag.DsLichHen = ds.Select(x => new SelectListItem(
                $"LH {x.MaLichHen} | {x.KhachHang} | {x.ThuCung} | {x.TenDichVu} | {x.ThoiGianBatDau:dd/MM/yyyy HH:mm}",
                x.MaLichHen.ToString())).ToList();
        }

        private async Task NapThongTinLichHen(LichHen lich)
        {
            ViewBag.TenKhachHang = await _context.TaiKhoan.AsNoTracking()
                .Where(x => x.MaTaiKhoan == lich.MaTaiKhoan).Select(x => x.HoTen).FirstOrDefaultAsync()
                ?? ("#" + lich.MaTaiKhoan);
            ViewBag.TenDichVu = await _context.DichVus.AsNoTracking()
                .Where(x => x.MaDichVu == lich.MaDichVu).Select(x => x.TenDichVu).FirstOrDefaultAsync()
                ?? ("#" + lich.MaDichVu);
            ViewBag.TenThuCung = await _context.ThuCungs.AsNoTracking()
                .Where(x => x.MaThuCung == lich.MaThuCung).Select(x => x.HoTen).FirstOrDefaultAsync()
                ?? ("#" + lich.MaThuCung);
        }

        private async Task NapTenHienThi(CaDichVu ca)
        {
            ViewBag.TenDichVu = await _context.DichVus.AsNoTracking()
                .Where(x => x.MaDichVu == ca.MaDichVu).Select(x => x.TenDichVu).FirstOrDefaultAsync()
                ?? ("Dịch vụ #" + ca.MaDichVu);
            ViewBag.TenThuCung = await _context.ThuCungs.AsNoTracking()
                .Where(x => x.MaThuCung == ca.MaThuCung).Select(x => x.HoTen).FirstOrDefaultAsync()
                ?? ("Thú cưng #" + ca.MaThuCung);

            if (ca.MaLichHen != null)
            {
                ViewBag.TenKhachHang = await (
                    from l in _context.LichHen.AsNoTracking()
                    join tk in _context.TaiKhoan.AsNoTracking() on l.MaTaiKhoan equals tk.MaTaiKhoan
                    where l.MaLichHen == ca.MaLichHen
                    select tk.HoTen).FirstOrDefaultAsync() ?? "";
            }
        }
    }
}
