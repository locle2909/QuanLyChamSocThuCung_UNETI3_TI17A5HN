// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: SV5 - Dashboard và thống kê bằng LINQ (Count, Sum, GroupBy, Distinct, OrderBy, Take),
// tính tỷ lệ chuyên cần; khách hàng xem chuyên cần của chính mình.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

// Phân quyền đặt trên từng action: Dashboard/Index cho Admin, ChuyenCanCuaToi cho Khách hàng.
public class ThongKeController : Controller
{
    private readonly AppDbContext _context;

    // Ca đã xong: hệ thống có nơi ghi "Đã hoàn thành", có nơi ghi "Hoàn thành" nên chấp nhận cả hai.
    private static readonly string[] CaDaXong =
        { HangSo.TrangThaiCaDichVu.DaHoanThanh, "Hoàn thành" };

    public ThongKeController(AppDbContext context)
    {
        _context = context;
    }

    // Tổng tiền đã thanh toán của từng lịch hẹn chưa hủy, kèm đơn giá dịch vụ
    private IQueryable<ChiPhiLichHenViewModel> TruyVanChiPhi()
    {
        return
            from l in _context.LichHen.AsNoTracking()
            join dv in _context.DichVus.AsNoTracking() on l.MaDichVu equals dv.MaDichVu
            where l.TrangThai != HangSo.TrangThaiLichHen.DaHuy
            select new ChiPhiLichHenViewModel
            {
                MaLichHen = l.MaLichHen,
                MaTaiKhoan = l.MaTaiKhoan,
                MaDichVu = dv.MaDichVu,
                TenDichVu = dv.TenDichVu,
                ThoiGianBatDau = l.ThoiGianBatDau,
                TrangThaiLichHen = l.TrangThai,
                DonGia = dv.Gia,
                TongDaThanhToan = _context.ThanhToan
                    .Where(t => t.MaLichHen == l.MaLichHen)
                    .Sum(t => (decimal?)t.SoTien) ?? 0m
            };
    }

    // ===================== DASHBOARD =====================
    [PhanQuyen(HangSo.VaiTro.Admin)]
    public async Task<IActionResult> Dashboard(DateTime? tuNgay, DateTime? denNgay)
    {
        var homNay = DateTime.Today;
        var tu = tuNgay?.Date ?? new DateTime(homNay.Year, homNay.Month, 1);
        var den = denNgay?.Date ?? homNay;
        if (den < tu)
        {
            TempData["Loi"] = "Đến ngày phải sau hoặc bằng Từ ngày.";
            den = tu;
        }
        var denKhongBaoGom = den.AddDays(1);   // lấy trọn ngày cuối kỳ
        var bayGio = DateTime.Now;

        var vm = new DashboardViewModel
        {
            TuNgay = tu,
            DenNgay = den,
            TongLoaiDichVu = await _context.LoaiDichVu.CountAsync(),
            TongThuCungHoatDong = await _context.ThuCungs.CountAsync(t => t.TrangThai == "Hoạt động"),
            TongKhachHang = await _context.TaiKhoan.CountAsync(t => t.VaiTro == HangSo.VaiTro.KhachHang),
            TongDichVu = await _context.DichVus.CountAsync(),
            TongGoiDichVu = await _context.GoiDichVus.CountAsync(),
            DichVuDangMoDangKy = await _context.DichVus.CountAsync(d => d.TrangThai == HangSo.TrangThaiDichVu.DangMoDangKy),
            DichVuDangPhucVu = await _context.DichVus.CountAsync(d => d.TrangThai == HangSo.TrangThaiDichVu.DangPhucVu),
            DichVuDaDu = await _context.DichVus.CountAsync(d => d.TrangThai == HangSo.TrangThaiDichVu.DaDu),
            LichHenChoXacNhan = await _context.LichHen.CountAsync(l => l.TrangThai == HangSo.TrangThaiLichHen.ChoXacNhan),
            CaDichVuSapToi = await _context.CaDichVu.CountAsync(c =>
                c.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich && c.ThoiGianBatDau >= bayGio),
            TongThuTrongKy = await _context.ThanhToan
                .Where(t => t.NgayThanhToan >= tu && t.NgayThanhToan < denKhongBaoGom)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m,
            // Lịch hẹn (chưa hủy) có tổng đã thanh toán < đơn giá dịch vụ
            LichHenConThieuChiPhi = await TruyVanChiPhi().CountAsync(x => x.TongDaThanhToan < x.DonGia)
        };

        return View(vm);
    }

    // ===================== THỐNG KÊ =====================
    [PhanQuyen(HangSo.VaiTro.Admin)]
    public async Task<IActionResult> Index(int? nam)
    {
        var namChon = Math.Clamp(nam ?? DateTime.Today.Year, 2000, 2100);
        var dauNam = new DateTime(namChon, 1, 1);
        var dauNamSau = dauNam.AddYears(1);
        var vm = new ThongKeViewModel { Nam = namChon };

        // 1. Số dịch vụ theo loại dịch vụ (nhóm dịch vụ)
        vm.DichVuTheoLoai = await _context.DichVus.AsNoTracking()
            .GroupBy(d => d.NhomDichVu)
            .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
            .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
            .ToListAsync();

        // 2. Số khách hàng (không trùng) theo từng dịch vụ (lịch đã xác nhận hoặc hoàn thành)
        vm.KhachHangTheoDichVu = await (
            from l in _context.LichHen.AsNoTracking()
            join dv in _context.DichVus.AsNoTracking() on l.MaDichVu equals dv.MaDichVu
            where l.TrangThai == HangSo.TrangThaiLichHen.DaXacNhan
               || l.TrangThai == HangSo.TrangThaiLichHen.DaHoanThanh
            group l by dv.TenDichVu into g
            select new ThongKeItem
            {
                Ten = g.Key,
                SoLuong = g.Select(x => x.MaTaiKhoan).Distinct().Count()
            })
            .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
            .ToListAsync();

        // 3. Số lịch hẹn theo trạng thái
        vm.LichHenTheoTrangThai = await _context.LichHen.AsNoTracking()
            .GroupBy(l => l.TrangThai)
            .Select(g => new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
            .OrderByDescending(x => x.SoLuong)
            .ToListAsync();

        // 4. Loại dịch vụ có nhiều lượt đặt nhất (top 5, không tính lịch đã hủy)
        vm.LoaiDichVuNhieuDangKy = await (
            from l in _context.LichHen.AsNoTracking()
            join dv in _context.DichVus.AsNoTracking() on l.MaDichVu equals dv.MaDichVu
            where l.TrangThai != HangSo.TrangThaiLichHen.DaHuy
            group l by dv.NhomDichVu into g
            select new ThongKeItem { Ten = g.Key, SoLuong = g.Count() })
            .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
            .Take(5)
            .ToListAsync();

        // 5. Thú cưng đặt nhiều dịch vụ nhất (top 5)
        vm.ThuCungDatNhieu = await (
            from l in _context.LichHen.AsNoTracking()
            join tc in _context.ThuCungs.AsNoTracking() on l.MaThuCung equals tc.MaThuCung
            where l.TrangThai != HangSo.TrangThaiLichHen.DaHuy
            group l by new { tc.MaThuCung, tc.HoTen } into g
            select new ThongKeItem { Ten = g.Key.HoTen, SoLuong = g.Count() })
            .OrderByDescending(x => x.SoLuong).ThenBy(x => x.Ten)
            .Take(5)
            .ToListAsync();

        // 6. Số buổi phục vụ theo thú cưng và theo tháng (ca đã hoàn thành trong năm)
        vm.BuoiPhucVuTheoThang = await (
            from c in _context.CaDichVu.AsNoTracking()
            join tc in _context.ThuCungs.AsNoTracking() on c.MaThuCung equals tc.MaThuCung
            where CaDaXong.Contains(c.TrangThai)
               && c.ThoiGianBatDau >= dauNam && c.ThoiGianBatDau < dauNamSau
            group c by new { tc.HoTen, Thang = c.ThoiGianBatDau.Month } into g
            select new BuoiPhucVuRow { TenThuCung = g.Key.HoTen, Thang = g.Key.Thang, SoBuoi = g.Count() })
            .OrderBy(x => x.TenThuCung).ThenBy(x => x.Thang)
            .ToListAsync();

        // 7. Tiền thu theo tháng
        var thu = await _context.ThanhToan.AsNoTracking()
            .Where(t => t.NgayThanhToan >= dauNam && t.NgayThanhToan < dauNamSau)
            .GroupBy(t => t.NgayThanhToan.Month)
            .Select(g => new { Thang = g.Key, Tien = g.Sum(x => x.SoTien) })
            .ToListAsync();

        vm.DoanhThuTheoThang = Enumerable.Range(1, 12)
            .Select(m => new ThongKeTienThang
            {
                Thang = m,
                SoTien = thu.FirstOrDefault(x => x.Thang == m)?.Tien ?? 0m
            }).ToList();

        // 8. Phải thu / đã thu / còn phải thu theo từng dịch vụ
        var chiPhi = await TruyVanChiPhi()
            .Select(x => new { x.MaDichVu, x.TenDichVu, x.DonGia, x.TongDaThanhToan })
            .ToListAsync();

        // 9. Chuyên cần theo từng dịch vụ: số kết quả "Hoàn thành" / tổng kết quả (ca chưa hủy)
        var chuyenCan = await (
            from kq in _context.KetQuaDichVu.AsNoTracking()
            join ca in _context.CaDichVu.AsNoTracking() on kq.MaCaDichVu equals ca.MaCaDichVu
            where ca.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy
            group kq by ca.MaDichVu into g
            select new
            {
                MaDichVu = g.Key,
                Tong = g.Count(),
                CoMat = g.Count(x => x.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.HoanThanh)
            })
            .ToListAsync();

        vm.ChiTietDichVu = chiPhi
            .GroupBy(x => new { x.MaDichVu, x.TenDichVu })
            .Select(g =>
            {
                var cc = chuyenCan.FirstOrDefault(c => c.MaDichVu == g.Key.MaDichVu);
                return new DichVuThongKeRow
                {
                    TenDichVu = g.Key.TenDichVu,
                    SoLichHieuLuc = g.Count(),
                    PhaiThu = g.Sum(x => x.DonGia),
                    DaThu = g.Sum(x => x.TongDaThanhToan),
                    TongKetQua = cc?.Tong ?? 0,
                    SoBuoiCoMat = cc?.CoMat ?? 0
                };
            })
            .OrderByDescending(x => x.ConPhaiThu).ThenBy(x => x.TenDichVu)
            .ToList();

        return View(vm);
    }

    // ===================== KHÁCH HÀNG: CHUYÊN CẦN CỦA TÔI =====================
    // Quy ước: KetQuaDichVu.MaKhachHang lưu mã tài khoản của khách hàng.
    [PhanQuyen(HangSo.VaiTro.KhachHang)]
    public async Task<IActionResult> ChuyenCanCuaToi()
    {
        var maTk = HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan) ?? 0;

        var ds = await (
            from kq in _context.KetQuaDichVu.AsNoTracking()
            join ca in _context.CaDichVu.AsNoTracking() on kq.MaCaDichVu equals ca.MaCaDichVu
            join dv in _context.DichVus.AsNoTracking() on ca.MaDichVu equals dv.MaDichVu
            where kq.MaKhachHang == maTk && ca.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy
            group kq by dv.TenDichVu into g
            select new ChuyenCanRow
            {
                TenDichVu = g.Key,
                TongBuoi = g.Count(),
                SoBuoiCoMat = g.Count(x => x.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.HoanThanh)
            })
            .OrderBy(x => x.TenDichVu)
            .ToListAsync();

        return View(ds);
    }
}
