// Họ và tên: Vũ Duy Cường
// Mã sinh viên: 23103100305
// Nội dung thực hiện: Quản lý thanh toán, tính chi phí dịch vụ còn lại, trạng thái chi phí,
// tìm kiếm - lọc - sắp xếp - phân trang danh sách chi phí, phân quyền dữ liệu khách hàng.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 10;

        public ThanhToanController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== PHÂN QUYỀN (kiểm tra tại Controller) =====================
        private int? MaTaiKhoanHienTai => HttpContext.Session.GetInt32("MaTaiKhoan");
        private string? VaiTroHienTai => HttpContext.Session.GetString("VaiTro");

        private IActionResult? YeuCauVaiTro(string vaiTro)
        {
            if (MaTaiKhoanHienTai == null)
                return RedirectToAction("DangNhap", "TaiKhoan");   // đổi theo action đăng nhập của SV1
            if (VaiTroHienTai != vaiTro)
            {
                TempData["Loi"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }
            return null;
        }

        // ===================== TRUY VẤN DÙNG CHUNG =====================
        // Mỗi lịch hẹn (chưa hủy) kèm đơn giá và tổng tiền đã thanh toán
        private IQueryable<ChiPhiLichHenViewModel> TruyVanChiPhi()
        {
            return _context.Set<LichHen>().AsNoTracking()
                .Where(l => l.TrangThai != TrangThaiLichHen.DaHuy)
                .Select(l => new ChiPhiLichHenViewModel
                {
                    MaLichHen = l.MaLichHen,
                    MaKhachHang = l.MaKhachHang,
                    TenKhachHang = l.KhachHang!.HoTen,
                    TenDichVu = l.DichVu!.TenDichVu,
                    NgayDat = l.NgayDat,
                    TrangThaiLichHen = l.TrangThai,
                    DonGia = l.DichVu!.DonGia,
                    TongDaThanhToan = _context.ThanhToan
                        .Where(t => t.MaLichHen == l.MaLichHen)
                        .Sum(t => (decimal?)t.SoTien) ?? 0
                });
        }

        private async Task<ThanhToanCreateViewModel?> TaiThongTinThanhToan(int maLichHen)
        {
            return await _context.Set<LichHen>().AsNoTracking()
                .Where(l => l.MaLichHen == maLichHen)
                .Select(l => new ThanhToanCreateViewModel
                {
                    MaLichHen = l.MaLichHen,
                    TenKhachHang = l.KhachHang!.HoTen,
                    TenDichVu = l.DichVu!.TenDichVu,
                    TrangThaiLichHen = l.TrangThai,
                    DonGia = l.DichVu!.DonGia,
                    TongDaThanhToan = _context.ThanhToan
                        .Where(t => t.MaLichHen == l.MaLichHen)
                        .Sum(t => (decimal?)t.SoTien) ?? 0
                })
                .FirstOrDefaultAsync();
        }

        // ===================== ADMIN: DANH SÁCH CHI PHÍ =====================
        // Truy vấn -> Tìm kiếm -> Lọc -> Sắp xếp -> Phân trang -> Hiển thị
        public async Task<IActionResult> Index(string? tuKhoa, string? trangThaiChiPhi, string? sapXep, int page = 1)
        {
            var chan = YeuCauVaiTro(VaiTroConst.Admin);
            if (chan != null) return chan;

            var query = TruyVanChiPhi();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                query = query.Where(x => x.TenKhachHang.Contains(kw)
                                      || x.TenDichVu.Contains(kw)
                                      || x.MaLichHen.ToString() == kw);
            }

            // Lọc theo trạng thái chi phí
            switch (trangThaiChiPhi)
            {
                case TrangThaiChiPhi.ChuaThanhToan:
                    query = query.Where(x => x.TongDaThanhToan == 0); break;
                case TrangThaiChiPhi.ThanhToanMotPhan:
                    query = query.Where(x => x.TongDaThanhToan > 0 && x.TongDaThanhToan < x.DonGia); break;
                case TrangThaiChiPhi.DaThanhToan:
                    query = query.Where(x => x.TongDaThanhToan >= x.DonGia); break;
            }

            // Sắp xếp
            IOrderedQueryable<ChiPhiLichHenViewModel> sapXepQuery = sapXep switch
            {
                "ten_asc" => query.OrderBy(x => x.TenKhachHang).ThenBy(x => x.MaLichHen),
                "ten_desc" => query.OrderByDescending(x => x.TenKhachHang).ThenBy(x => x.MaLichHen),
                "conlai_desc" => query.OrderByDescending(x => x.DonGia - x.TongDaThanhToan).ThenBy(x => x.MaLichHen),
                "conlai_asc" => query.OrderBy(x => x.DonGia - x.TongDaThanhToan).ThenBy(x => x.MaLichHen),
                "ngay_asc" => query.OrderBy(x => x.NgayDat).ThenBy(x => x.MaLichHen),
                _ => query.OrderByDescending(x => x.NgayDat).ThenByDescending(x => x.MaLichHen)
            };

            // Phân trang (Skip/Take trên truy vấn)
            int total = await sapXepQuery.CountAsync();
            int totalPages = Math.Max((int)Math.Ceiling(total / (double)PageSize), 1);
            page = Math.Clamp(page, 1, totalPages);

            var items = await sapXepQuery.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            return View(new ThanhToanIndexViewModel
            {
                Items = items,
                TuKhoa = tuKhoa,
                TrangThaiChiPhi = trangThaiChiPhi,
                SapXep = sapXep,
                PageIndex = page,
                PageSize = PageSize,
                TotalItems = total
            });
        }

        // ===================== ADMIN: GHI NHẬN THANH TOÁN =====================
        [HttpGet]
        public async Task<IActionResult> Create(int maLichHen)
        {
            var chan = YeuCauVaiTro(VaiTroConst.Admin);
            if (chan != null) return chan;

            var vm = await TaiThongTinThanhToan(maLichHen);
            if (vm == null) return NotFound();

            var loi = KiemTraDieuKienThanhToan(vm);
            if (loi != null)
            {
                TempData["Loi"] = loi;
                return RedirectToAction(nameof(LichSu), new { id = maLichHen });
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThanhToanCreateViewModel model)
        {
            var chan = YeuCauVaiTro(VaiTroConst.Admin);
            if (chan != null) return chan;

            // Dữ liệu hiển thị luôn lấy lại từ DB, không tin dữ liệu gửi lên
            var thongTin = await TaiThongTinThanhToan(model.MaLichHen);
            if (thongTin == null) return NotFound();

            model.TenKhachHang = thongTin.TenKhachHang;
            model.TenDichVu = thongTin.TenDichVu;
            model.TrangThaiLichHen = thongTin.TrangThaiLichHen;
            model.DonGia = thongTin.DonGia;
            model.TongDaThanhToan = thongTin.TongDaThanhToan;

            var loi = KiemTraDieuKienThanhToan(thongTin);
            if (loi != null)
            {
                ModelState.AddModelError(string.Empty, loi);
                return View(model);
            }

            if (!PhuongThucThanhToan.DanhSach.Contains(model.PhuongThuc))
                ModelState.AddModelError(nameof(model.PhuongThuc), "Phương thức thanh toán không hợp lệ.");

            // 0 < SoTienThanhToanMoi <= SoTienConLai
            if (model.SoTien.HasValue)
            {
                if (model.SoTien.Value <= 0)
                    ModelState.AddModelError(nameof(model.SoTien), "Số tiền phải lớn hơn 0.");
                else if (model.SoTien.Value > thongTin.SoTienConLai)
                    ModelState.AddModelError(nameof(model.SoTien),
                        $"Số tiền vượt quá số tiền còn lại ({thongTin.SoTienConLai.ToVnd()}).");
            }

            if (!ModelState.IsValid) return View(model);

            var thanhToan = new ThanhToan
            {
                MaLichHen = model.MaLichHen,
                NgayThanhToan = DateTime.Now,                                   // do hệ thống xác định
                SoTien = model.SoTien!.Value,
                PhuongThuc = model.PhuongThuc,
                NguoiGhiNhan = HttpContext.Session.GetString("HoTen"),          // lấy từ Session
                GhiChu = model.GhiChu
            };
            _context.ThanhToan.Add(thanhToan);
            await _context.SaveChangesAsync();

            TempData["ThanhCong"] = "Đã ghi nhận thanh toán " + thanhToan.SoTien.ToVnd();
            return RedirectToAction(nameof(LichSu), new { id = model.MaLichHen });
        }

        private static string? KiemTraDieuKienThanhToan(ThanhToanCreateViewModel vm)
        {
            if (vm.TrangThaiLichHen == TrangThaiLichHen.DaHuy)
                return "Lịch hẹn đã hủy, không thể ghi nhận thanh toán.";
            if (vm.SoTienConLai <= 0)
                return "Lịch hẹn này đã thanh toán đủ chi phí dịch vụ.";
            return null;
        }

        // ===================== HÓA ĐƠN / LỊCH SỬ THANH TOÁN =====================
        // Admin xem được tất cả; Khách hàng chỉ xem lịch hẹn của chính mình
        public async Task<IActionResult> LichSu(int id)
        {
            if (MaTaiKhoanHienTai == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            bool laAdmin = VaiTroHienTai == VaiTroConst.Admin;
            bool laKhach = VaiTroHienTai == VaiTroConst.KhachHang;
            if (!laAdmin && !laKhach)
            {
                TempData["Loi"] = "Bạn không có quyền truy cập chức năng này.";
                return RedirectToAction("Index", "Home");
            }

            var thongTin = await TruyVanChiPhi().FirstOrDefaultAsync(x => x.MaLichHen == id);
            if (thongTin == null) return NotFound();

            if (laKhach)
            {
                int maTk = MaTaiKhoanHienTai.Value;
                bool laCuaToi = await _context.Set<KhachHang>()
                    .AnyAsync(k => k.MaKhachHang == thongTin.MaKhachHang && k.MaTaiKhoan == maTk);
                if (!laCuaToi) return NotFound();   // đổi mã trên URL không làm lộ dữ liệu người khác
            }

            var lichSu = await _context.ThanhToan.AsNoTracking()
                .Where(t => t.MaLichHen == id)
                .OrderByDescending(t => t.NgayThanhToan)
                .ToListAsync();

            return View(new HoaDonViewModel { ThongTin = thongTin, LichSu = lichSu, LaAdmin = laAdmin });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var chan = YeuCauVaiTro(VaiTroConst.Admin);
            if (chan != null) return chan;

            var tt = await _context.ThanhToan.FindAsync(id);
            if (tt == null) return NotFound();

            int maLichHen = tt.MaLichHen;
            _context.ThanhToan.Remove(tt);
            await _context.SaveChangesAsync();

            TempData["ThanhCong"] = "Đã xóa khoản thanh toán nhập nhầm.";
            return RedirectToAction(nameof(LichSu), new { id = maLichHen });
        }

        // ===================== KHÁCH HÀNG: CHI PHÍ CỦA TÔI =====================
        public async Task<IActionResult> CuaToi()
        {
            var chan = YeuCauVaiTro(VaiTroConst.KhachHang);
            if (chan != null) return chan;

            int maTk = MaTaiKhoanHienTai!.Value;
            var khachHang = await _context.Set<KhachHang>().AsNoTracking()
                .FirstOrDefaultAsync(k => k.MaTaiKhoan == maTk);
            if (khachHang == null) return NotFound();

            var ds = await TruyVanChiPhi()
                .Where(x => x.MaKhachHang == khachHang.MaKhachHang)
                .OrderByDescending(x => x.NgayDat)
                .ToListAsync();

            return View(ds);
        }
    }
}
