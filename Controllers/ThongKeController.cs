// Họ và tên: Vũ Duy Cường
// Mã sinh viên: 23103100305
// Nội dung thực hiện: Dashboard, thống kê bằng LINQ (Count, Sum, GroupBy, Average, OrderBy, Any),
// tính tỷ lệ chuyên cần và tỷ lệ lấp đầy số lịch tiếp nhận.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly AppDbContext _context;

        public ThongKeController(AppDbContext context)
        {
            _context = context;
        }

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

        // ===================== DASHBOARD =====================
        public async Task<IActionResult> Dashboard(DateTime? tuNgay, DateTime? denNgay)
        {
            var chan = YeuCauVaiTro(VaiTroConst.Admin);
            if (chan != null) return chan;

            var homNay = DateTime.Today;
            var tu = tuNgay?.Date ?? new DateTime(homNay.Year, homNay.Month, 1);
            var den = denNgay?.Date ?? homNay;
            if (den < tu)
            {
                ModelState.AddModelError(string.Empty, "Đến ngày phải sau hoặc bằng Từ ngày.");
                den = tu;
            }
            var denKhongBaoGom = den.AddDays(1);   // lấy trọn ngày cuối kỳ

            var vm = new DashboardViewModel { TuNgay = tu, DenNgay = den };

            vm.TongLoaiDichVu = await _context.Set<LoaiDichVu>().CountAsync();

            vm.TongThuCungHoatDong = await _context.Set<ThuCung>()
                .CountAsync(t => t.TrangThai == TrangThaiHoatDong.DangHoatDong);

            vm.TongKhachHang = await _context.Set<KhachHang>().CountAsync();

            vm.DichVuDangMoDangKy = await _context.Set<DichVu>()
                .CountAsync(d => d.TrangThai == TrangThaiDichVu.DangMoDangKy);

            vm.DichVuDangPhucVu = await _context.Set<DichVu>()
                .CountAsync(d => d.TrangThai == TrangThaiDichVu.DangPhucVu);

            vm.LichHenChoXacNhan = await _context.Set<LichHen>()
                .CountAsync(l => l.TrangThai == TrangThaiLichHen.ChoXacNhan);

            // Dịch vụ đã đủ: số đăng ký Đã xác nhận >= số lịch tiếp nhận tối đa
            vm.DichVuDaDuLich = await _context.Set<DichVu>().CountAsync(d =>
                _context.Set<LichHen>().Count(l => l.MaDichVu == d.MaDichVu
                                          && l.TrangThai == TrangThaiLichHen.DaXacNhan)
                >= d.SoLichToiDaMoiKhungGio);

            vm.CaDichVuSapToi = await _context.Set<CaDichVu>()
                .CountAsync(c => c.TrangThai == TrangThaiCa.DaLenLich && c.ThoiGianBatDau >= DateTime.Now);

            vm.TongThuTrongKy = await _context.ThanhToan
                .Where(t => t.NgayThanhToan >= tu && t.NgayThanhToan < denKhongBaoGom)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            // Lịch hẹn (chưa hủy) có tổng đã thanh toán < đơn giá dịch vụ
            vm.LichHenConThieuChiPhi = await _context.Set<LichHen>()
                .Where(l => l.TrangThai != TrangThaiLichHen.DaHuy)
                .CountAsync(l => (_context.ThanhToan
                                    .Where(t => t.MaLichHen == l.MaLichHen)
                                    .Sum(t => (decimal?)t.SoTien) ?? 0) < l.DichVu!.DonGia);

            return View(vm);
        }

        // ===================== THỐNG KÊ =====================
        public async Task<IActionResult> Index(int? nam)
        {
            var chan = YeuCauVaiTro(VaiTroConst.Admin);
            if (chan != null) return chan;

            int namChon = nam ?? DateTime.Today.Year;
            var vm = new ThongKeViewModel { Nam = namChon };

            // 1. Số dịch vụ theo loại dịch vụ
            vm.DichVuTheoLoai = await _context.Set<LoaiDichVu>().AsNoTracking()
                .Select(l => new ThongKeItem
                {
                    Ten = l.TenLoaiDichVu,
                    SoLuong = _context.Set<DichVu>().Count(d => d.MaLoaiDichVu == l.MaLoaiDichVu)
                })
                .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
                .ToListAsync();

            // 2. Số khách hàng (không trùng) theo từng dịch vụ
            vm.KhachHangTheoDichVu = await _context.Set<DichVu>().AsNoTracking()
                .Select(d => new ThongKeItem
                {
                    Ten = d.TenDichVu,
                    SoLuong = _context.Set<LichHen>()
                        .Where(l => l.MaDichVu == d.MaDichVu
                                 && (l.TrangThai == TrangThaiLichHen.DaXacNhan
                                  || l.TrangThai == TrangThaiLichHen.DaHoanThanh))
                        .Select(l => l.MaKhachHang).Distinct().Count()
                })
                .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
                .ToListAsync();

            // 3. Số đăng ký theo trạng thái
            vm.LichHenTheoTrangThai = await _context.Set<LichHen>().AsNoTracking()
                .GroupBy(l => l.TrangThai)
                .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            // 4. Loại dịch vụ có nhiều lượt đăng ký nhất (top 5)
            vm.LoaiDichVuNhieuDangKy = await _context.Set<LoaiDichVu>().AsNoTracking()
                .Select(l => new ThongKeItem
                {
                    Ten = l.TenLoaiDichVu,
                    SoLuong = _context.Set<LichHen>().Count(h => h.DichVu!.MaLoaiDichVu == l.MaLoaiDichVu
                                                        && h.TrangThai != TrangThaiLichHen.DaHuy)
                })
                .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
                .Take(5)
                .ToListAsync();

            // 5. Thú cưng đang phụ trách nhiều dịch vụ nhất (top 5)
            vm.ThuCungPhuTrachNhieu = (await _context.Set<ThuCung>().AsNoTracking()
                .Select(t => new ThongKeItem
                {
                    Ten = t.HoTen,
                    SoLuong = _context.Set<PhanCongNhanVien>()
                        .Where(p => p.MaThuCung == t.MaThuCung)
                        .Select(p => p.MaDichVu).Distinct().Count()
                })
                .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
                .Take(5)
                .ToListAsync())
                .Where(x => x.SoLuong > 0).ToList();

            // 6. Số buổi phục vụ theo thú cưng / tháng (ca đã hoàn thành trong năm)
            vm.BuoiPhucVuTheoThang = await _context.Set<CaDichVu>().AsNoTracking()
                .Where(c => c.TrangThai == TrangThaiCa.DaHoanThanh && c.ThoiGianBatDau.Year == namChon)
                .GroupBy(c => new { Ten = c.ThuCung!.HoTen, Thang = c.ThoiGianBatDau.Month })
                .Select(g => new BuoiPhucVuRow
                {
                    TenThuCung = g.Key.Ten,
                    Thang = g.Key.Thang,
                    SoBuoi = g.Count()
                })
                .OrderBy(x => x.TenThuCung).ThenBy(x => x.Thang)
                .ToListAsync();

            // 7. Doanh thu chi phí dịch vụ theo tháng
            var thu = await _context.ThanhToan.AsNoTracking()
                .Where(t => t.NgayThanhToan.Year == namChon)
                .GroupBy(t => t.NgayThanhToan.Month)
                .Select(g => new { Thang = g.Key, Tien = g.Sum(x => x.SoTien) })
                .ToListAsync();

            vm.DoanhThuTheoThang = Enumerable.Range(1, 12)
                .Select(m => new ThongKeTienThang
                {
                    Thang = m,
                    SoTien = thu.FirstOrDefault(x => x.Thang == m)?.Tien ?? 0
                }).ToList();

            // 8 + 9 + 10. Còn phải thu, tỷ lệ lấp đầy, tỷ lệ chuyên cần theo từng dịch vụ
            var raw = await _context.Set<DichVu>().AsNoTracking()
                .Select(d => new
                {
                    d.TenDichVu,
                    d.DonGia,
                    SoToiDa = d.SoLichToiDaMoiKhungGio,
                    DaXacNhan = _context.Set<LichHen>().Count(l => l.MaDichVu == d.MaDichVu
                                    && l.TrangThai == TrangThaiLichHen.DaXacNhan),
                    SoLichHieuLuc = _context.Set<LichHen>().Count(l => l.MaDichVu == d.MaDichVu
                                    && l.TrangThai != TrangThaiLichHen.DaHuy),
                    DaThu = _context.ThanhToan
                                .Where(t => t.LichHen!.MaDichVu == d.MaDichVu
                                         && t.LichHen!.TrangThai != TrangThaiLichHen.DaHuy)
                                .Sum(t => (decimal?)t.SoTien) ?? 0,
                    TongKetQua = _context.Set<KetQuaDichVu>().Count(k => k.CaDichVu!.MaDichVu == d.MaDichVu
                                    && k.CaDichVu!.TrangThai == TrangThaiCa.DaHoanThanh),
                    CoMat = _context.Set<KetQuaDichVu>().Count(k => k.CaDichVu!.MaDichVu == d.MaDichVu
                                    && k.CaDichVu!.TrangThai == TrangThaiCa.DaHoanThanh
                                    && k.TrangThaiKetQuaDichVu == TrangThaiKetQua.HoanThanh)
                })
                .ToListAsync();

            // Tính tỷ lệ ở bộ nhớ (tránh chia 0)
            vm.ChiTietDichVu = raw.Select(r => new DichVuThongKeRow
            {
                TenDichVu = r.TenDichVu,
                DaXacNhan = r.DaXacNhan,
                SoLichToiDa = r.SoToiDa,
                // TyLeLapDay = SoLuongDaXacNhan / SoLichToiDa * 100
                TyLeLapDay = r.SoToiDa > 0 ? r.DaXacNhan * 100.0 / r.SoToiDa : 0,
                PhaiThu = r.SoLichHieuLuc * r.DonGia,
                DaThu = r.DaThu,
                TongKetQua = r.TongKetQua,
                SoBuoiCoMat = r.CoMat,
                // TyLeChuyenCan = SoBuoiCoMat / TongSoBuoiDaKetQuaDichVu * 100
                TyLeChuyenCan = r.TongKetQua > 0 ? r.CoMat * 100.0 / r.TongKetQua : 0
            })
            .OrderByDescending(x => x.ConPhaiThu)
            .ToList();

            return View(vm);
        }

        // ===================== KHÁCH HÀNG: CHUYÊN CẦN CỦA TÔI =====================
        public async Task<IActionResult> ChuyenCanCuaToi()
        {
            var chan = YeuCauVaiTro(VaiTroConst.KhachHang);
            if (chan != null) return chan;

            int maTk = MaTaiKhoanHienTai!.Value;
            var khachHang = await _context.Set<KhachHang>().AsNoTracking()
                .FirstOrDefaultAsync(k => k.MaTaiKhoan == maTk);
            if (khachHang == null) return NotFound();

            // Chỉ lấy kết quả của chính khách hàng này, thuộc ca đã hoàn thành
            var ds = await _context.Set<KetQuaDichVu>().AsNoTracking()
                .Where(k => k.MaKhachHang == khachHang.MaKhachHang
                         && k.CaDichVu!.TrangThai == TrangThaiCa.DaHoanThanh)
                .GroupBy(k => k.CaDichVu!.DichVu!.TenDichVu)
                .Select(g => new ChuyenCanRow
                {
                    TenDichVu = g.Key,
                    TongBuoi = g.Count(),
                    SoBuoiCoMat = g.Count(k => k.TrangThaiKetQuaDichVu == TrangThaiKetQua.HoanThanh)
                })
                .ToListAsync();

            return View(ds);
        }
    }
}
