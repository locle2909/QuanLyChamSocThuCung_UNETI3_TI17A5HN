using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

public class GoiDichVuController : Controller
{
    private readonly AppDbContext _context;

    public GoiDichVuController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (!IsAdmin()) return Forbid();

        var goiDichVus = await _context.GoiDichVus.AsNoTracking()
            .Include(goi => goi.ChiTietGoiDichVus)
                .ThenInclude(chiTiet => chiTiet.DichVu)
            .OrderBy(goi => goi.TenGoi)
            .ToListAsync();
        return View(goiDichVus);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsAdmin()) return Forbid();
        var model = new GoiDichVuFormViewModel();
        await PopulateServicesAsync(model);
        SetStatuses();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoiDichVuFormViewModel model)
    {
        if (!IsAdmin()) return Forbid();
        var selected = await GetSelectedServicesAsync(model);
        if (selected is null)
        {
            ModelState.AddModelError(nameof(model.DichVus), "Có dịch vụ được chọn không còn tồn tại.");
            await PopulateServicesAsync(model);
            SetStatuses();
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            await PopulateServicesAsync(model);
            SetStatuses();
            return View(model);
        }

        var goi = new GoiDichVu
        {
            TenGoi = model.TenGoi,
            MoTa = model.MoTa,
            GiaGoi = model.GiaGoi,
            GiaKhuyenMai = model.GiaKhuyenMai,
            TrangThai = model.TrangThai,
            ChiTietGoiDichVus = selected.Select(item => new ChiTietGoiDichVu
            {
                MaDichVu = item.MaDichVu,
                SoLuong = item.SoLuong
            }).ToList()
        };

        _context.GoiDichVus.Add(goi);
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã thêm gói dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin()) return Forbid();
        var goi = await _context.GoiDichVus.AsNoTracking()
            .Include(item => item.ChiTietGoiDichVus)
            .FirstOrDefaultAsync(item => item.MaGoi == id);
        if (goi is null) return NotFound();

        var model = new GoiDichVuFormViewModel
        {
            MaGoi = goi.MaGoi,
            TenGoi = goi.TenGoi,
            MoTa = goi.MoTa,
            GiaGoi = goi.GiaGoi,
            GiaKhuyenMai = goi.GiaKhuyenMai,
            TrangThai = goi.TrangThai,
            DichVus = goi.ChiTietGoiDichVus.Select(item => new GoiDichVuServiceItemViewModel
            {
                MaDichVu = item.MaDichVu,
                Chon = true,
                SoLuong = item.SoLuong
            }).ToList()
        };
        await PopulateServicesAsync(model);
        SetStatuses();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GoiDichVuFormViewModel model)
    {
        if (!IsAdmin()) return Forbid();
        if (id != model.MaGoi) return NotFound();

        var goi = await _context.GoiDichVus
            .Include(item => item.ChiTietGoiDichVus)
            .FirstOrDefaultAsync(item => item.MaGoi == id);
        if (goi is null) return NotFound();

        var selected = await GetSelectedServicesAsync(model);
        if (selected is null)
            ModelState.AddModelError(nameof(model.DichVus), "Có dịch vụ được chọn không còn tồn tại.");

        if (!ModelState.IsValid)
        {
            await PopulateServicesAsync(model);
            SetStatuses();
            return View(model);
        }

        goi.TenGoi = model.TenGoi;
        goi.MoTa = model.MoTa;
        goi.GiaGoi = model.GiaGoi;
        goi.GiaKhuyenMai = model.GiaKhuyenMai;
        goi.TrangThai = model.TrangThai;
        _context.ChiTietGoiDichVus.RemoveRange(goi.ChiTietGoiDichVus);
        goi.ChiTietGoiDichVus = selected!.Select(item => new ChiTietGoiDichVu
        {
            MaGoi = goi.MaGoi,
            MaDichVu = item.MaDichVu,
            SoLuong = item.SoLuong
        }).ToList();

        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật gói dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ngung(int id)
    {
        if (!IsAdmin()) return Forbid();
        var goi = await _context.GoiDichVus.FindAsync(id);
        if (goi is null) return NotFound();

        goi.TrangThai = HangSo.TrangThaiDichVu.NgungPhucVu;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã chuyển gói sang trạng thái ngừng phục vụ.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsAdmin() =>
        HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) == HangSo.VaiTro.Admin;

    private async Task<List<GoiDichVuServiceItemViewModel>?> GetSelectedServicesAsync(GoiDichVuFormViewModel model)
    {
        var selected = model.DichVus.Where(item => item.Chon).ToList();
        var ids = selected.Select(item => item.MaDichVu).ToArray();
        if (ids.Distinct().Count() != ids.Length) return null;
        var existingIds = await _context.DichVus
            .Where(item => ids.Contains(item.MaDichVu))
            .Select(item => item.MaDichVu)
            .ToListAsync();

        if (existingIds.Count != ids.Length) return null;
        return selected;
    }

    private async Task PopulateServicesAsync(GoiDichVuFormViewModel model)
    {
        var postedItems = model.DichVus
            .GroupBy(item => item.MaDichVu)
            .ToDictionary(group => group.Key, group => group.Last());
        var services = await _context.DichVus.AsNoTracking()
            .OrderBy(item => item.TenDichVu)
            .ToListAsync();

        model.DichVus = services.Select(service => new GoiDichVuServiceItemViewModel
        {
            MaDichVu = service.MaDichVu,
            TenDichVu = service.TenDichVu,
            Gia = service.Gia,
            Chon = postedItems.TryGetValue(service.MaDichVu, out var posted) && posted.Chon,
            SoLuong = postedItems.TryGetValue(service.MaDichVu, out posted) ? posted.SoLuong : 1
        }).ToList();
    }

    private void SetStatuses() => ViewBag.TrangThaiDichVu = new[]
    {
        HangSo.TrangThaiDichVu.DangMoDangKy,
        HangSo.TrangThaiDichVu.DaDu,
        HangSo.TrangThaiDichVu.DangPhucVu,
        HangSo.TrangThaiDichVu.TamNgung,
        HangSo.TrangThaiDichVu.NgungPhucVu
    };
}