// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Quản lý loại dịch vụ (chỉ Admin): xem, tìm kiếm, thêm, sửa, ẩn/hiện.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

[PhanQuyen(HangSo.VaiTro.Admin)]
public class LoaiDichVuController : Controller
{
    private readonly AppDbContext _context;

    public LoaiDichVuController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? q, string? nhom)
    {
        var ds = _context.LoaiDichVu.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            ds = ds.Where(l => l.TenLoaiDichVu.Contains(q));
        }
        if (!string.IsNullOrEmpty(nhom))
            ds = ds.Where(l => l.NhomDichVu == nhom);

        ViewBag.Q = q;
        NapNhom(nhom);

        return View(await ds.OrderBy(l => l.NhomDichVu).ThenBy(l => l.TenLoaiDichVu).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create()
    {
        NapNhom(null);
        return View(new LoaiDichVu());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenLoaiDichVu,MoTa,NhomDichVu,TrangThai")] LoaiDichVu model)
    {
        model.TenLoaiDichVu = (model.TenLoaiDichVu ?? "").Trim();

        if (await _context.LoaiDichVu.AnyAsync(l => l.TenLoaiDichVu == model.TenLoaiDichVu))
            ModelState.AddModelError(nameof(LoaiDichVu.TenLoaiDichVu), "Tên loại dịch vụ đã tồn tại.");

        if (!HangSo.NhomDichVu.DanhSach.Contains(model.NhomDichVu ?? ""))
            ModelState.AddModelError(nameof(LoaiDichVu.NhomDichVu), "Nhóm dịch vụ không hợp lệ.");

        if (!ModelState.IsValid)
        {
            NapNhom(model.NhomDichVu);
            return View(model);
        }

        _context.LoaiDichVu.Add(model);
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã thêm loại dịch vụ " + model.TenLoaiDichVu + ".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var l = await _context.LoaiDichVu.FindAsync(id);
        if (l == null) return NotFound();

        NapNhom(l.NhomDichVu);
        return View(l);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("MaLoaiDichVu,TenLoaiDichVu,MoTa,NhomDichVu,TrangThai")] LoaiDichVu model)
    {
        if (id != model.MaLoaiDichVu) return NotFound();
        var l = await _context.LoaiDichVu.FindAsync(id);
        if (l == null) return NotFound();

        model.TenLoaiDichVu = (model.TenLoaiDichVu ?? "").Trim();

        if (await _context.LoaiDichVu.AnyAsync(x => x.TenLoaiDichVu == model.TenLoaiDichVu && x.MaLoaiDichVu != id))
            ModelState.AddModelError(nameof(LoaiDichVu.TenLoaiDichVu), "Tên loại dịch vụ đã tồn tại.");

        if (!HangSo.NhomDichVu.DanhSach.Contains(model.NhomDichVu ?? ""))
            ModelState.AddModelError(nameof(LoaiDichVu.NhomDichVu), "Nhóm dịch vụ không hợp lệ.");

        if (!ModelState.IsValid)
        {
            NapNhom(model.NhomDichVu);
            return View(model);
        }

        l.TenLoaiDichVu = model.TenLoaiDichVu;
        l.MoTa = model.MoTa;
        l.NhomDichVu = model.NhomDichVu;
        l.TrangThai = model.TrangThai;

        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật loại dịch vụ " + l.TenLoaiDichVu + ".";
        return RedirectToAction(nameof(Index));
    }

    // Ẩn / hiện loại dịch vụ. Không xóa cứng vì DichVu và ChuyenMonThuCung sẽ tham chiếu tới.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var l = await _context.LoaiDichVu.FindAsync(id);
        if (l == null) return NotFound();

        l.TrangThai = !l.TrangThai;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = (l.TrangThai ? "Đã bật " : "Đã tắt ") + l.TenLoaiDichVu + ".";
        return RedirectToAction(nameof(Index));
    }

    private void NapNhom(string? chon)
    {
        ViewBag.DsNhom = new SelectList(HangSo.NhomDichVu.DanhSach, chon);
    }
}