using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

public class DichVuController : Controller
{
    private readonly AppDbContext _context;

    public DichVuController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) is null)
            return RedirectToAction("DangNhap", "TaiKhoan");

        var dichVus = await _context.DichVus.AsNoTracking()
            .OrderBy(dichVu => dichVu.TenDichVu)
            .ToListAsync();
        return View(dichVus);
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!IsAdmin()) return Forbid();
        SetOptions();
        return View(new DichVu());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenDichVu,MoTa,Gia,NhomDichVu,TrangThai")] DichVu dichVu)
    {
        if (!IsAdmin()) return Forbid();
        if (!ModelState.IsValid)
        {
            SetOptions();
            return View(dichVu);
        }

        _context.Add(dichVu);
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã thêm dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin()) return Forbid();
        var dichVu = await _context.DichVus.FindAsync(id);
        if (dichVu is null) return NotFound();
        SetOptions();
        return View(dichVu);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("MaDichVu,TenDichVu,MoTa,Gia,NhomDichVu,TrangThai")] DichVu input)
    {
        if (!IsAdmin()) return Forbid();
        if (id != input.MaDichVu) return NotFound();

        var dichVu = await _context.DichVus.FindAsync(id);
        if (dichVu is null) return NotFound();

        if (!ModelState.IsValid)
        {
            SetOptions();
            return View(input);
        }

        dichVu.TenDichVu = input.TenDichVu;
        dichVu.MoTa = input.MoTa;
        dichVu.Gia = input.Gia;
        dichVu.NhomDichVu = input.NhomDichVu;
        dichVu.TrangThai = input.TrangThai;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật dịch vụ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ngung(int id)
    {
        if (!IsAdmin()) return Forbid();
        var dichVu = await _context.DichVus.FindAsync(id);
        if (dichVu is null) return NotFound();

        dichVu.TrangThai = HangSo.TrangThaiDichVu.NgungPhucVu;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã chuyển dịch vụ sang trạng thái ngừng phục vụ.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsAdmin() =>
        HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) == HangSo.VaiTro.Admin;

    private void SetOptions()
    {
        ViewBag.NhomDichVu = HangSo.NhomDichVu.DanhSach;
        ViewBag.TrangThaiDichVu = new[]
        {
            HangSo.TrangThaiDichVu.DangMoDangKy,
            HangSo.TrangThaiDichVu.DaDu,
            HangSo.TrangThaiDichVu.DangPhucVu,
            HangSo.TrangThaiDichVu.TamNgung,
            HangSo.TrangThaiDichVu.NgungPhucVu
        };
    }
}