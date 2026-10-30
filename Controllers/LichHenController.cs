using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

[PhanQuyen(HangSo.VaiTro.Admin, HangSo.VaiTro.NhanVien, HangSo.VaiTro.KhachHang)]
public class LichHenController : Controller
{
    private readonly AppDbContext _context;

    public LichHenController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? trangThai)
    {
        var vaiTro = HttpContext.Session.GetString(HangSo.SessionKey.VaiTro);
        var maTaiKhoan = HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan);
        if (maTaiKhoan is null)
            return RedirectToAction("DangNhap", "TaiKhoan");

        var query =
            from lichHen in _context.LichHen.AsNoTracking()
            join taiKhoan in _context.TaiKhoan.AsNoTracking()
                on lichHen.MaTaiKhoan equals taiKhoan.MaTaiKhoan
            join thuCung in _context.ThuCungs.AsNoTracking()
                on lichHen.MaThuCung equals thuCung.MaThuCung
            join dichVu in _context.DichVus.AsNoTracking()
                on lichHen.MaDichVu equals dichVu.MaDichVu
            select new { lichHen, taiKhoan, thuCung, dichVu };

        if (vaiTro == HangSo.VaiTro.KhachHang)
            query = query.Where(x => x.lichHen.MaTaiKhoan == maTaiKhoan.Value);

        var trangThaiHopLe = new[]
        {
            HangSo.TrangThaiLichHen.ChoXacNhan,
            HangSo.TrangThaiLichHen.DaXacNhan,
            HangSo.TrangThaiLichHen.DaHuy,
            HangSo.TrangThaiLichHen.DaHoanThanh
        };
        if (trangThaiHopLe.Contains(trangThai))
            query = query.Where(x => x.lichHen.TrangThai == trangThai);

        var now = DateTime.Now;
        var items = await query
            .OrderByDescending(x => x.lichHen.ThoiGianBatDau)
            .Select(x => new LichHenIndexItemViewModel
            {
                MaLichHen = x.lichHen.MaLichHen,
                TenKhachHang = x.taiKhoan.HoTen,
                TenThuCung = x.thuCung.HoTen,
                TenDichVu = x.dichVu.TenDichVu,
                ThoiGianBatDau = x.lichHen.ThoiGianBatDau,
                ThoiGianKetThuc = x.lichHen.ThoiGianKetThuc,
                TrangThai = x.lichHen.TrangThai,
                GhiChu = x.lichHen.GhiChu,
                CoTheHuy = (x.lichHen.TrangThai == HangSo.TrangThaiLichHen.ChoXacNhan
                    || x.lichHen.TrangThai == HangSo.TrangThaiLichHen.DaXacNhan)
                    && x.lichHen.ThoiGianBatDau > now,
                CoTheXacNhan = x.lichHen.TrangThai == HangSo.TrangThaiLichHen.ChoXacNhan
                    && x.lichHen.ThoiGianBatDau > now,
                CoTheHoanThanh = x.lichHen.TrangThai == HangSo.TrangThaiLichHen.DaXacNhan
                    && x.lichHen.ThoiGianKetThuc <= now
            })
            .ToListAsync();

        ViewBag.TrangThai = trangThai;
        ViewBag.DanhSachTrangThai = new SelectList(trangThaiHopLe, trangThai);
        ViewBag.VaiTro = vaiTro;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsKhachHang())
            return Forbid();

        var maTaiKhoan = HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan)!.Value;
        await NapLuaChon(maTaiKhoan);

        var batDau = DateTime.Now.AddHours(1);
        return View(new LichHenFormViewModel
        {
            ThoiGianBatDau = new DateTime(batDau.Year, batDau.Month, batDau.Day, batDau.Hour, 0, 0),
            ThoiGianKetThuc = new DateTime(batDau.Year, batDau.Month, batDau.Day, batDau.Hour, 0, 0).AddHours(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LichHenFormViewModel model)
    {
        if (!IsKhachHang())
            return Forbid();

        var maTaiKhoan = HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan)!.Value;

        if (model.ThoiGianBatDau <= DateTime.Now)
            ModelState.AddModelError(nameof(model.ThoiGianBatDau), "Thời gian hẹn phải ở trong tương lai.");

        if (model.ThoiGianKetThuc <= model.ThoiGianBatDau)
            ModelState.AddModelError(nameof(model.ThoiGianKetThuc), "Thời gian kết thúc phải sau thời gian bắt đầu.");

        var thuCung = await _context.ThuCungs
            .FirstOrDefaultAsync(x => x.MaThuCung == model.MaThuCung
                && x.MaTaiKhoan == maTaiKhoan.ToString()
                && x.TrangThai == "Hoạt động");
        if (thuCung == null)
            ModelState.AddModelError(nameof(model.MaThuCung), "Thú cưng không tồn tại, không thuộc tài khoản của bạn hoặc đã ngừng hoạt động.");

        var dichVu = await _context.DichVus
            .FirstOrDefaultAsync(x => x.MaDichVu == model.MaDichVu
                && x.TrangThai == HangSo.TrangThaiDichVu.DangMoDangKy);
        if (dichVu == null)
            ModelState.AddModelError(nameof(model.MaDichVu), "Dịch vụ không tồn tại hoặc hiện không mở đăng ký.");

        if (thuCung != null && model.ThoiGianKetThuc > model.ThoiGianBatDau)
        {
            var biTrungLich = await _context.LichHen.AnyAsync(x =>
                x.MaThuCung == thuCung.MaThuCung
                && x.TrangThai != HangSo.TrangThaiLichHen.DaHuy
                && x.ThoiGianBatDau < model.ThoiGianKetThuc
                && x.ThoiGianKetThuc > model.ThoiGianBatDau);
            if (biTrungLich)
                ModelState.AddModelError(string.Empty, "Thú cưng đã có một lịch hẹn khác trong khoảng thời gian này.");
        }

        if (!ModelState.IsValid)
        {
            await NapLuaChon(maTaiKhoan);
            return View(model);
        }

        _context.LichHen.Add(new LichHen
        {
            MaTaiKhoan = maTaiKhoan,
            MaThuCung = thuCung!.MaThuCung,
            MaDichVu = dichVu!.MaDichVu,
            ThoiGianBatDau = model.ThoiGianBatDau,
            ThoiGianKetThuc = model.ThoiGianKetThuc,
            GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim(),
            TrangThai = HangSo.TrangThaiLichHen.ChoXacNhan,
            NgayTao = DateTime.Now
        });
        await _context.SaveChangesAsync();

        TempData["ThongBao"] = "Đã gửi yêu cầu đặt lịch. Vui lòng theo dõi trạng thái xác nhận.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int id)
    {
        if (!IsNhanVienHoacAdmin())
            return Forbid();

        var lichHen = await _context.LichHen.FindAsync(id);
        if (lichHen == null)
            return NotFound();

        if (lichHen.TrangThai != HangSo.TrangThaiLichHen.ChoXacNhan
            || lichHen.ThoiGianBatDau <= DateTime.Now)
        {
            TempData["Loi"] = "Chỉ có thể xác nhận lịch đang chờ và chưa đến giờ hẹn.";
            return RedirectToAction(nameof(Index));
        }

        var biTrungLich = await _context.LichHen.AnyAsync(x =>
            x.MaLichHen != id
            && x.MaThuCung == lichHen.MaThuCung
            && x.TrangThai != HangSo.TrangThaiLichHen.DaHuy
            && x.ThoiGianBatDau < lichHen.ThoiGianKetThuc
            && x.ThoiGianKetThuc > lichHen.ThoiGianBatDau);
        if (biTrungLich)
        {
            TempData["Loi"] = "Không thể xác nhận vì thú cưng đã có lịch hẹn khác trùng thời gian.";
            return RedirectToAction(nameof(Index));
        }

        lichHen.TrangThai = HangSo.TrangThaiLichHen.DaXacNhan;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã xác nhận lịch hẹn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id)
    {
        var vaiTro = HttpContext.Session.GetString(HangSo.SessionKey.VaiTro);
        if (vaiTro != HangSo.VaiTro.Admin
            && vaiTro != HangSo.VaiTro.NhanVien
            && vaiTro != HangSo.VaiTro.KhachHang)
            return Forbid();

        var lichHen = await _context.LichHen.FindAsync(id);
        if (lichHen == null)
            return NotFound();

        if (vaiTro == HangSo.VaiTro.KhachHang
            && lichHen.MaTaiKhoan != HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan))
            return Forbid();

        if ((lichHen.TrangThai != HangSo.TrangThaiLichHen.ChoXacNhan
             && lichHen.TrangThai != HangSo.TrangThaiLichHen.DaXacNhan)
            || lichHen.ThoiGianBatDau <= DateTime.Now)
        {
            TempData["Loi"] = "Không thể hủy lịch hẹn đã bắt đầu hoặc đã kết thúc.";
            return RedirectToAction(nameof(Index));
        }

        lichHen.TrangThai = HangSo.TrangThaiLichHen.DaHuy;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã hủy lịch hẹn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanThanh(int id)
    {
        if (!IsNhanVienHoacAdmin())
            return Forbid();

        var lichHen = await _context.LichHen.FindAsync(id);
        if (lichHen == null)
            return NotFound();

        if (lichHen.TrangThai != HangSo.TrangThaiLichHen.DaXacNhan
            || lichHen.ThoiGianKetThuc > DateTime.Now)
        {
            TempData["Loi"] = "Chỉ có thể hoàn tất lịch đã xác nhận sau khi kết thúc thời gian hẹn.";
            return RedirectToAction(nameof(Index));
        }

        lichHen.TrangThai = HangSo.TrangThaiLichHen.DaHoanThanh;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật lịch hẹn hoàn thành.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsKhachHang() =>
        HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) == HangSo.VaiTro.KhachHang
        && HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan).HasValue;

    private bool IsNhanVienHoacAdmin()
    {
        var vaiTro = HttpContext.Session.GetString(HangSo.SessionKey.VaiTro);
        return vaiTro == HangSo.VaiTro.Admin || vaiTro == HangSo.VaiTro.NhanVien;
    }

    private async Task NapLuaChon(int maTaiKhoan)
    {
        var maTaiKhoanChuoi = maTaiKhoan.ToString();
        var thuCungs = await _context.ThuCungs.AsNoTracking()
            .Where(x => x.MaTaiKhoan == maTaiKhoanChuoi && x.TrangThai == "Hoạt động")
            .OrderBy(x => x.HoTen)
            .Select(x => new { x.MaThuCung, x.HoTen })
            .ToListAsync();
        var dichVus = await _context.DichVus.AsNoTracking()
            .Where(x => x.TrangThai == HangSo.TrangThaiDichVu.DangMoDangKy)
            .OrderBy(x => x.TenDichVu)
            .Select(x => new { x.MaDichVu, x.TenDichVu, x.Gia })
            .ToListAsync();

        ViewBag.ThuCungs = new SelectList(thuCungs, "MaThuCung", "HoTen");
        ViewBag.DichVus = new SelectList(
            dichVus.Select(x => new
            {
                x.MaDichVu,
                TenHienThi = $"{x.TenDichVu} - {x.Gia:N0} đ"
            }),
            "MaDichVu",
            "TenHienThi");
        ViewBag.CoThuCung = thuCungs.Count > 0;
        ViewBag.CoDichVu = dichVus.Count > 0;
    }
}
