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
        // Trạng thái được chọn tay. "Chờ khách xác nhận" và "Hoàn thành" chỉ đi qua
        // luồng đề xuất (nhân viên) và xác nhận (khách hàng).
        private static readonly string[] TrangThaiHopLe =
        {
            HangSo.TrangThaiKetQua.ChoThucHien,
            HangSo.TrangThaiKetQua.DangThucHien
        };

        private readonly AppDbContext _context;

        public KetQuaDichVuController(AppDbContext context)
        {
            _context = context;
        }

        private bool LaAdmin => HttpContext.LaAdmin();

        private int MaTaiKhoanHienTai => HttpContext.LayMaTaiKhoan();

        // Các ca mà nhân viên đang đăng nhập được phân công
        private IQueryable<int> CaCuaToi() => _context.CaCuaNhanVien(MaTaiKhoanHienTai);

        private async Task<bool> DuocPhanCongVaoCa(int maCa)
        {
            if (LaAdmin) return true;
            return await CaCuaToi().AnyAsync(c => c == maCa);
        }

        private static string GhiThem(string? cu, string them)
        {
            return string.IsNullOrWhiteSpace(cu) ? them : cu + "\n" + them;
        }

        // ===== Danh sách =====

        public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangThai = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            var laAdmin = LaAdmin;

            var query = from kq in _context.KetQuaDichVu.AsNoTracking()
                        join ca in _context.CaDichVu.AsNoTracking() on kq.MaCaDichVu equals ca.MaCaDichVu
                        select new { kq, ca };

            if (!laAdmin)
            {
                var caCuaToi = CaCuaToi();
                query = query.Where(x => caCuaToi.Contains(x.ca.MaCaDichVu));
                ViewBag.ChuaLienKet = !await _context.CoHoSoNhanVienHopLe(MaTaiKhoanHienTai);
            }

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                var coMa = int.TryParse(kw, out var ma);
                query = query.Where(x =>
                    (coMa && (x.kq.MaKetQuaDichVu == ma || x.ca.MaCaDichVu == ma))
                    || _context.TaiKhoan.Any(t => t.MaTaiKhoan == x.kq.MaKhachHang && t.HoTen.Contains(kw))
                    || _context.DichVus.Any(d => d.MaDichVu == x.ca.MaDichVu && d.TenDichVu.Contains(kw))
                    || _context.ThuCungs.Any(t => t.MaThuCung == x.ca.MaThuCung && t.HoTen.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(x => x.kq.TrangThaiKetQuaDichVu == trangThai);
            if (tuNgay.HasValue)
                query = query.Where(x => x.ca.ThoiGianBatDau >= tuNgay.Value.Date);
            if (denNgay.HasValue)
                query = query.Where(x => x.ca.ThoiGianBatDau < denNgay.Value.Date.AddDays(1));

            var rows = await query.OrderByDescending(x => x.kq.ThoiGianGhiNhan).ToListAsync();

            var tenTaiKhoan = await _context.TaiKhoan.AsNoTracking().ToDictionaryAsync(x => x.MaTaiKhoan, x => x.HoTen);
            var tenDv = await _context.DichVus.AsNoTracking().ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
            var tenTc = await _context.ThuCungs.AsNoTracking().ToDictionaryAsync(x => x.MaThuCung, x => x.HoTen);

            var ds = rows.Select(x => new KetQuaHienThi
            {
                MaKetQuaDichVu = x.kq.MaKetQuaDichVu,
                MaCaDichVu = x.ca.MaCaDichVu,
                MaKhachHang = x.kq.MaKhachHang,
                TenKhachHang = tenTaiKhoan.GetValueOrDefault(x.kq.MaKhachHang, "#" + x.kq.MaKhachHang),
                TenDichVu = tenDv.GetValueOrDefault(x.ca.MaDichVu, "#" + x.ca.MaDichVu),
                TenThuCung = tenTc.GetValueOrDefault(x.ca.MaThuCung, "#" + x.ca.MaThuCung),
                ThoiGianBatDau = x.ca.ThoiGianBatDau,
                TrangThaiCa = x.ca.TrangThai,
                TrangThai = x.kq.TrangThaiKetQuaDichVu,
                ThoiGianGhiNhan = x.kq.ThoiGianGhiNhan,
                GhiChu = x.kq.GhiChu,
                GhiChuNoiBo = x.kq.GhiChuNoiBo,
                TenNguoiCapNhat = x.kq.MaNguoiCapNhat != null
                    ? tenTaiKhoan.GetValueOrDefault(x.kq.MaNguoiCapNhat.Value, "#" + x.kq.MaNguoiCapNhat.Value)
                    : "",
                ThoiGianDeXuatHoanThanh = x.kq.ThoiGianDeXuatHoanThanh,
                ThoiGianKhachXacNhanHoanThanh = x.kq.ThoiGianKhachXacNhanHoanThanh,
                // Ca đã lên lịch: Admin/Nhân viên được phân công sửa được.
                // Ca đã hoàn thành: chỉ Admin (và chỉ sửa ghi chú). Ca đã hủy: không sửa.
                CoTheSua = x.ca.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich
                           || (x.ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh && laAdmin),
                CoTheDeXuatHoanThanh = x.ca.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich
                                       && x.kq.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.DangThucHien,
                ChoKhachXacNhan = x.ca.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich
                                  && x.kq.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.ChoKhachXacNhan
            }).ToList();

            return View(ds);
        }

        // ===== Thêm (chỉ dùng cho ca cũ chưa gắn lịch hẹn) =====

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

            // Ca tạo từ lịch hẹn đã có sẵn kết quả của khách, không thêm khách khác vào
            if (ca.MaLichHen != null)
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
            else if (ca.MaLichHen != null)
            {
                ModelState.AddModelError(nameof(kq.MaCaDichVu),
                    "Ca tạo từ lịch hẹn đã có sẵn kết quả của khách. Hãy sửa kết quả hiện có.");
            }
            else if (!await DuocPhanCongVaoCa(ca.MaCaDichVu))
            {
                // Nhân viên chỉ ghi kết quả cho ca mình được phân công
                ModelState.AddModelError(nameof(kq.MaCaDichVu),
                    "Bạn chưa được phân công vào ca này nên không thể ghi nhận kết quả.");
            }
            else if (!await KiemTraLichHelper.CoNhanVienPhuTrach(_context, ca.MaCaDichVu))
            {
                // Ca phải có nhân viên phụ trách trước khi ghi kết quả (áp dụng cả Admin)
                ModelState.AddModelError(nameof(kq.MaCaDichVu),
                    "Ca chưa có nhân viên phụ trách. Hãy phân công nhân viên trước khi ghi kết quả.");
            }

            if (!TrangThaiHopLe.Contains(kq.TrangThaiKetQuaDichVu))
                ModelState.AddModelError(nameof(kq.TrangThaiKetQuaDichVu),
                    "Trạng thái không hợp lệ. Kết quả chỉ chuyển sang Hoàn thành khi khách hàng xác nhận.");

            if (ca != null && ModelState.IsValid)
            {
                // Chỉ khách có đăng ký "Đã xác nhận" đúng dịch vụ và đúng thú cưng của ca
                bool duDieuKien = await KhachDaXacNhan(ca).AnyAsync(t => t.MaTaiKhoan == kq.MaKhachHang);
                if (!duDieuKien)
                    ModelState.AddModelError(nameof(kq.MaKhachHang),
                        "Khách hàng không có đăng ký đã xác nhận cho dịch vụ và thú cưng của ca này.");

                // Mỗi khách chỉ một kết quả cho mỗi ca
                bool daCo = await _context.KetQuaDichVu.AnyAsync(x =>
                    x.MaCaDichVu == kq.MaCaDichVu && x.MaKhachHang == kq.MaKhachHang);
                if (daCo)
                    ModelState.AddModelError(nameof(kq.MaKhachHang),
                        "Khách hàng này đã có kết quả cho ca. Hãy sửa kết quả hiện có.");

                // Khách không được có hai ca trùng khoảng thời gian
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
            kq.MaNguoiCapNhat = MaTaiKhoanHienTai;
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

        // Trạng thái chỉ chọn tay giữa "Chờ thực hiện" và "Đang thực hiện".
        // Khi đã đề xuất, đã hoàn thành hoặc ca đã hoàn thành thì khóa trạng thái, chỉ sửa ghi chú.
        // Ca đã hoàn thành: chỉ Admin, bắt buộc ghi lý do vào ghi chú nội bộ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("TrangThaiKetQuaDichVu,GhiChu,GhiChuNoiBo")] KetQuaDichVu model)
        {
            var kq = await _context.KetQuaDichVu.FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id);
            if (kq == null) return NotFound();

            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);
            if (ca == null) return NotFound();

            var loi = await KiemTraQuyenSua(ca);
            if (loi != null) return loi;

            bool caDaHoanThanh = ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh;
            bool khoaTrangThai = caDaHoanThanh
                                 || kq.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.ChoKhachXacNhan
                                 || kq.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.HoanThanh;

            model.GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim();
            model.GhiChuNoiBo = string.IsNullOrWhiteSpace(model.GhiChuNoiBo) ? null : model.GhiChuNoiBo.Trim();

            if (khoaTrangThai)
            {
                // Không đổi trạng thái khi đã đề xuất, đã hoàn thành hoặc ca đã hoàn thành
                model.TrangThaiKetQuaDichVu = kq.TrangThaiKetQuaDichVu;

                if (caDaHoanThanh && model.GhiChuNoiBo == null)
                    ModelState.AddModelError(nameof(model.GhiChuNoiBo),
                        "Sửa kết quả của ca đã hoàn thành phải ghi lý do vào ghi chú nội bộ.");
            }
            else
            {
                if (!TrangThaiHopLe.Contains(model.TrangThaiKetQuaDichVu))
                    ModelState.AddModelError(nameof(model.TrangThaiKetQuaDichVu),
                        "Không được tự chuyển kết quả sang Hoàn thành. Hãy dùng chức năng đề xuất hoàn thành.");

                if (model.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.ChoThucHien)
                {
                    if (ca.ThoiGianBatDau > DateTime.Now)
                        ModelState.AddModelError(nameof(model.TrangThaiKetQuaDichVu),
                            "Ca chưa bắt đầu, chỉ được ghi trạng thái 'Chờ thực hiện'.");

                    if (!await KiemTraLichHelper.CoNhanVienPhuTrach(_context, ca.MaCaDichVu))
                        ModelState.AddModelError(nameof(model.TrangThaiKetQuaDichVu),
                            "Ca chưa có nhân viên phụ trách. Hãy phân công nhân viên trước.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.MaKetQuaDichVu = kq.MaKetQuaDichVu;
                model.MaCaDichVu = kq.MaCaDichVu;
                model.MaKhachHang = kq.MaKhachHang;
                await NapThongTin(kq, ca);
                return View(model);
            }

            var trangThaiCu = kq.TrangThaiKetQuaDichVu;

            kq.TrangThaiKetQuaDichVu = model.TrangThaiKetQuaDichVu;
            kq.GhiChu = model.GhiChu;
            kq.GhiChuNoiBo = model.GhiChuNoiBo;
            kq.ThoiGianGhiNhan = DateTime.Now;
            kq.MaNguoiCapNhat = MaTaiKhoanHienTai;

            // Ghi nhật ký: ai sửa, lúc nào, sửa gì
            var noiDung = (caDaHoanThanh ? "Sửa sau khi ca hoàn thành. " : "")
                          + $"Trạng thái: {trangThaiCu} → {kq.TrangThaiKetQuaDichVu}.";
            if (model.GhiChuNoiBo != null)
                noiDung += " Ghi chú nội bộ: " + model.GhiChuNoiBo;
            if (noiDung.Length > 1000)
                noiDung = noiDung[..1000];

            _context.NhatKyKetQuaDichVu.Add(new NhatKyKetQuaDichVu
            {
                MaKetQuaDichVu = kq.MaKetQuaDichVu,
                MaNguoiSua = MaTaiKhoanHienTai,
                ThoiGian = DateTime.Now,
                NoiDung = noiDung
            });

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật kết quả dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Nhân viên đề xuất hoàn thành =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeXuatHoanThanh(int id)
        {
            var kq = await _context.KetQuaDichVu.FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id);
            if (kq == null) return NotFound();

            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
            {
                TempData["Loi"] = "Ca đã hủy hoặc đã hoàn thành.";
                return RedirectToAction(nameof(Index));
            }

            // Nhân viên chỉ được thao tác trên ca mình phụ trách
            if (!await DuocPhanCongVaoCa(ca.MaCaDichVu))
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");

            if (ca.ThoiGianBatDau > DateTime.Now)
            {
                TempData["Loi"] = "Ca dịch vụ chưa bắt đầu.";
                return RedirectToAction(nameof(Index));
            }

            if (!await KiemTraLichHelper.CoNhanVienPhuTrach(_context, ca.MaCaDichVu))
            {
                TempData["Loi"] = "Ca chưa có nhân viên phụ trách. Hãy phân công nhân viên trước.";
                return RedirectToAction(nameof(Index));
            }

            if (kq.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.DangThucHien)
            {
                TempData["Loi"] = "Chỉ được đề xuất hoàn thành khi kết quả đang ở trạng thái 'Đang thực hiện'.";
                return RedirectToAction(nameof(Index));
            }

            kq.TrangThaiKetQuaDichVu = HangSo.TrangThaiKetQua.ChoKhachXacNhan;
            kq.ThoiGianDeXuatHoanThanh = DateTime.Now;
            kq.ThoiGianGhiNhan = DateTime.Now;
            kq.MaNguoiCapNhat = MaTaiKhoanHienTai;

            _context.NhatKyKetQuaDichVu.Add(new NhatKyKetQuaDichVu
            {
                MaKetQuaDichVu = kq.MaKetQuaDichVu,
                MaNguoiSua = MaTaiKhoanHienTai,
                ThoiGian = DateTime.Now,
                NoiDung = "Đề xuất hoàn thành dịch vụ, chờ khách hàng xác nhận."
            });

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã gửi đề xuất hoàn thành đến khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Admin xác nhận thay khách hàng (khi khách không phản hồi) =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> XacNhanThay(int id, string? lyDo)
        {
            var kq = await _context.KetQuaDichVu.FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id);
            if (kq == null) return NotFound();

            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
            {
                TempData["Loi"] = "Ca đã hủy hoặc đã hoàn thành.";
                return RedirectToAction(nameof(Index));
            }

            if (kq.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.ChoKhachXacNhan)
            {
                TempData["Loi"] = "Chỉ xác nhận thay khi kết quả đang chờ khách hàng xác nhận.";
                return RedirectToAction(nameof(Index));
            }

            lyDo = lyDo?.Trim();
            if (string.IsNullOrEmpty(lyDo))
            {
                TempData["Loi"] = "Phải ghi lý do khi xác nhận thay khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            if (lyDo.Length > 500)
            {
                TempData["Loi"] = "Lý do tối đa 500 ký tự.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bayGio = DateTime.Now;

                kq.TrangThaiKetQuaDichVu = HangSo.TrangThaiKetQua.HoanThanh;
                kq.ThoiGianKhachXacNhanHoanThanh = bayGio;
                kq.ThoiGianGhiNhan = bayGio;
                kq.MaNguoiCapNhat = MaTaiKhoanHienTai;
                kq.GhiChuNoiBo = GhiThem(kq.GhiChuNoiBo,
                    $"[{bayGio:dd/MM/yyyy HH:mm}] Admin xác nhận thay khách hàng. Lý do: {lyDo}");

                _context.NhatKyKetQuaDichVu.Add(new NhatKyKetQuaDichVu
                {
                    MaKetQuaDichVu = kq.MaKetQuaDichVu,
                    MaNguoiSua = MaTaiKhoanHienTai,
                    ThoiGian = bayGio,
                    NoiDung = "Admin xác nhận hoàn thành thay khách hàng. Lý do: " + lyDo
                });

                await _context.SaveChangesAsync();

                bool caHoanThanh = await HoanThanhCaHelper.HoanThanhCaNeuDuDieuKien(_context, ca.MaCaDichVu);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["ThongBao"] = caHoanThanh
                    ? "Đã xác nhận thay khách hàng. Ca dịch vụ đã hoàn thành."
                    : "Đã xác nhận thay khách hàng.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Loi"] = "Có lỗi xảy ra, vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== Lịch sử sửa (chỉ Admin) =====

        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> LichSu(int id)
        {
            var kq = await _context.KetQuaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id);
            if (kq == null) return NotFound();

            var tenTaiKhoan = await _context.TaiKhoan.AsNoTracking()
                .ToDictionaryAsync(x => x.MaTaiKhoan, x => x.HoTen);

            var nhatKy = await _context.NhatKyKetQuaDichVu.AsNoTracking()
                .Where(n => n.MaKetQuaDichVu == id)
                .OrderByDescending(n => n.ThoiGian)
                .ToListAsync();

            ViewBag.MaKetQuaDichVu = id;
            ViewBag.TenKhachHang = tenTaiKhoan.GetValueOrDefault(kq.MaKhachHang, "#" + kq.MaKhachHang);

            return View(nhatKy.Select(n => new NhatKyHienThi
            {
                ThoiGian = n.ThoiGian,
                NguoiSua = tenTaiKhoan.GetValueOrDefault(n.MaNguoiSua, "#" + n.MaNguoiSua),
                NoiDung = n.NoiDung
            }).ToList());
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
            ViewBag.KhoaTrangThai = ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh
                                    || kq.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.ChoKhachXacNhan
                                    || kq.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.HoanThanh;
        }

        private async Task NapLuaChon()
        {
            var truyVan = _context.CaDichVu.AsNoTracking()
                .Where(x => x.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich && x.MaLichHen == null);

            // Nhân viên chỉ thấy các ca mình được phân công
            if (!LaAdmin)
            {
                var caCuaToi = CaCuaToi();
                truyVan = truyVan.Where(c => caCuaToi.Contains(c.MaCaDichVu));
                ViewBag.ChuaLienKet = !await _context.CoHoSoNhanVienHopLe(MaTaiKhoanHienTai);
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
        public string? GhiChuNoiBo { get; set; }
        public string TenNguoiCapNhat { get; set; } = string.Empty;
        public DateTime? ThoiGianDeXuatHoanThanh { get; set; }
        public DateTime? ThoiGianKhachXacNhanHoanThanh { get; set; }
        public bool CoTheSua { get; set; }
        public bool CoTheDeXuatHoanThanh { get; set; }
        public bool ChoKhachXacNhan { get; set; }
    }

    public class NhatKyHienThi
    {
        public DateTime ThoiGian { get; set; }
        public string NguoiSua { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
    }
}