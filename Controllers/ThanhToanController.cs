// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: SV5 - Thanh toán: tính chi phí còn lại, trạng thái chi phí, ghi nhận thanh toán,
// tìm kiếm - lọc - sắp xếp - phân trang, hóa đơn; khách hàng chỉ xem chi phí của mình.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

// Admin xem tất cả, Khách hàng chỉ xem của mình; ghi nhận/xóa thanh toán chỉ Admin (đặt trên từng action).
[PhanQuyen(HangSo.VaiTro.Admin, HangSo.VaiTro.KhachHang)]
public class ThanhToanController : Controller
{
    private const int PageSize = 10;
    private readonly AppDbContext _context;

    public ThanhToanController(AppDbContext context)
    {
        _context = context;
    }

    private int MaTaiKhoanHienTai => HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan) ?? 0;
    private bool LaAdmin => HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) == HangSo.VaiTro.Admin;

    // Mỗi lịch hẹn (chưa hủy) kèm đơn giá dịch vụ và tổng tiền đã thanh toán.
    // LichHen không có navigation property nên nối bảng bằng join.
    private IQueryable<ChiPhiLichHenViewModel> TruyVanChiPhi()
    {
        return
            from l in _context.LichHen.AsNoTracking()
            join tk in _context.TaiKhoan.AsNoTracking() on l.MaTaiKhoan equals tk.MaTaiKhoan
            join dv in _context.DichVus.AsNoTracking() on l.MaDichVu equals dv.MaDichVu
            where l.TrangThai != HangSo.TrangThaiLichHen.DaHuy
            select new ChiPhiLichHenViewModel
            {
                MaLichHen = l.MaLichHen,
                MaTaiKhoan = l.MaTaiKhoan,
                TenKhachHang = tk.HoTen,
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

    // Truy vấn -> (khách hàng chỉ thấy của mình) -> Tìm kiếm -> Lọc -> Sắp xếp -> Phân trang -> Hiển thị
    public async Task<IActionResult> Index(string? tuKhoa, string? trangThaiChiPhi, string? sapXep, int page = 1)
    {
        var query = TruyVanChiPhi();

        if (!LaAdmin)
        {
            var maTk = MaTaiKhoanHienTai;
            query = query.Where(x => x.MaTaiKhoan == maTk);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var kw = tuKhoa.Trim();
            query = query.Where(x => x.TenKhachHang.Contains(kw)
                                  || x.TenDichVu.Contains(kw)
                                  || x.MaLichHen.ToString() == kw);
        }

        switch (trangThaiChiPhi)
        {
            case HangSo.TrangThaiChiPhi.ChuaThanhToan:
                query = query.Where(x => x.TongDaThanhToan == 0m);
                break;
            case HangSo.TrangThaiChiPhi.ThanhToanMotPhan:
                query = query.Where(x => x.TongDaThanhToan > 0m && x.TongDaThanhToan < x.DonGia);
                break;
            case HangSo.TrangThaiChiPhi.DaThanhToan:
                query = query.Where(x => x.TongDaThanhToan >= x.DonGia);
                break;
        }

        IOrderedQueryable<ChiPhiLichHenViewModel> sapXepQuery = sapXep switch
        {
            "ten_asc" => query.OrderBy(x => x.TenKhachHang).ThenBy(x => x.MaLichHen),
            "ten_desc" => query.OrderByDescending(x => x.TenKhachHang).ThenBy(x => x.MaLichHen),
            "conlai_desc" => query.OrderByDescending(x => x.DonGia - x.TongDaThanhToan).ThenBy(x => x.MaLichHen),
            "conlai_asc" => query.OrderBy(x => x.DonGia - x.TongDaThanhToan).ThenBy(x => x.MaLichHen),
            "ngay_asc" => query.OrderBy(x => x.ThoiGianBatDau).ThenBy(x => x.MaLichHen),
            _ => query.OrderByDescending(x => x.ThoiGianBatDau).ThenByDescending(x => x.MaLichHen)
        };

        var total = await sapXepQuery.CountAsync();
        var totalPages = Math.Max((int)Math.Ceiling(total / (double)PageSize), 1);
        page = Math.Clamp(page, 1, totalPages);

        var items = await sapXepQuery.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        // Tổng còn phải thu của toàn bộ kết quả lọc (không chỉ trang hiện tại)
        var tongConLai = (await query
            .Select(x => new { x.DonGia, x.TongDaThanhToan })
            .ToListAsync())
            .Sum(x => ChiPhiHelper.TinhConLai(x.DonGia, x.TongDaThanhToan));

        return View(new ThanhToanIndexViewModel
        {
            Items = items,
            LaAdmin = LaAdmin,
            TuKhoa = tuKhoa,
            TrangThaiChiPhi = trangThaiChiPhi,
            SapXep = sapXep,
            PageIndex = page,
            PageSize = PageSize,
            TotalItems = total,
            TongConLai = tongConLai
        });
    }

    private async Task<ThanhToanCreateViewModel?> TaiThongTinThanhToan(int maLichHen)
    {
        var tt = await TruyVanChiPhi().FirstOrDefaultAsync(x => x.MaLichHen == maLichHen);
        if (tt == null) return null;
        return new ThanhToanCreateViewModel
        {
            MaLichHen = tt.MaLichHen,
            TenKhachHang = tt.TenKhachHang,
            TenDichVu = tt.TenDichVu,
            TrangThaiLichHen = tt.TrangThaiLichHen,
            DonGia = tt.DonGia,
            TongDaThanhToan = tt.TongDaThanhToan
        };
    }

    private static string? KiemTraDieuKienThanhToan(ThanhToanCreateViewModel vm)
    {
        if (vm.TrangThaiLichHen == HangSo.TrangThaiLichHen.DaHuy)
            return "Lịch hẹn đã hủy, không thể ghi nhận thanh toán.";
        if (vm.SoTienConLai <= 0)
            return "Lịch hẹn này đã thanh toán đủ chi phí dịch vụ.";
        return null;
    }

    [HttpGet]
    [PhanQuyen(HangSo.VaiTro.Admin)]
    public async Task<IActionResult> Create(int maLichHen)
    {
        var vm = await TaiThongTinThanhToan(maLichHen);
        if (vm == null) return NotFound();

        var loi = KiemTraDieuKienThanhToan(vm);
        if (loi != null)
        {
            TempData["Loi"] = loi;
            return RedirectToAction(nameof(LichSu), new { id = maLichHen });
        }

        ViewBag.DsPhuongThuc = HangSo.PhuongThucThanhToan.DanhSach;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [PhanQuyen(HangSo.VaiTro.Admin)]
    public async Task<IActionResult> Create(ThanhToanCreateViewModel model)
    {
        ViewBag.DsPhuongThuc = HangSo.PhuongThucThanhToan.DanhSach;

        // Dữ liệu hiển thị luôn lấy lại từ CSDL, không tin dữ liệu gửi lên
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

        if (!HangSo.PhuongThucThanhToan.DanhSach.Contains(model.PhuongThuc))
            ModelState.AddModelError(nameof(model.PhuongThuc), "Phương thức thanh toán không hợp lệ.");

        // 0 < số tiền mới <= số tiền còn lại
        if (model.SoTien.HasValue && model.SoTien.Value > thongTin.SoTienConLai)
            ModelState.AddModelError(nameof(model.SoTien),
                $"Số tiền vượt quá số tiền còn lại ({thongTin.SoTienConLai.ToVnd()}).");

        if (!ModelState.IsValid) return View(model);

        var thanhToan = new ThanhToan
        {
            MaLichHen = model.MaLichHen,
            NgayThanhToan = DateTime.Now,                                        // do hệ thống xác định
            SoTien = model.SoTien!.Value,
            PhuongThuc = model.PhuongThuc,
            NguoiGhiNhan = HttpContext.Session.GetString(HangSo.SessionKey.HoTen), // lấy từ Session
            GhiChu = model.GhiChu
        };
        _context.ThanhToan.Add(thanhToan);
        await _context.SaveChangesAsync();

        TempData["ThongBao"] = "Đã ghi nhận thanh toán " + thanhToan.SoTien.ToVnd();
        return RedirectToAction(nameof(LichSu), new { id = model.MaLichHen });
    }

    // Hóa đơn / lịch sử thanh toán: Admin xem tất cả, Khách hàng chỉ xem lịch hẹn của mình.
    public async Task<IActionResult> LichSu(int id)
    {
        var thongTin = await TruyVanChiPhi().FirstOrDefaultAsync(x => x.MaLichHen == id);
        if (thongTin == null) return NotFound();

        // Đổi mã trên URL cũng không xem được dữ liệu của người khác
        if (!LaAdmin && thongTin.MaTaiKhoan != MaTaiKhoanHienTai) return NotFound();

        var lichSu = await _context.ThanhToan.AsNoTracking()
            .Where(t => t.MaLichHen == id)
            .OrderByDescending(t => t.NgayThanhToan)
            .ToListAsync();

        return View(new HoaDonViewModel { ThongTin = thongTin, LichSu = lichSu, LaAdmin = LaAdmin });
    }

    // Xóa khoản thanh toán nhập nhầm (chỉ Admin)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [PhanQuyen(HangSo.VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var tt = await _context.ThanhToan.FindAsync(id);
        if (tt == null) return NotFound();

        var maLichHen = tt.MaLichHen;
        _context.ThanhToan.Remove(tt);
        await _context.SaveChangesAsync();

        TempData["ThongBao"] = "Đã xóa khoản thanh toán nhập nhầm.";
        return RedirectToAction(nameof(LichSu), new { id = maLichHen });
    }
}
