// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Quản lý loại dịch vụ (chỉ Admin): xem, chi tiết, tìm kiếm, thêm, sửa, xóa, ẩn/hiện.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using ClosedXML.Excel;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

[PhanQuyen(HangSo.VaiTro.Admin)]
public class LoaiDichVuController : Controller
{
    private readonly AppDbContext _context;

    public LoaiDichVuController(AppDbContext context)
    {
        _context = context;
    }
    private bool LaAjax() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    // Dùng chung cho Index và XuatExcel: tìm theo tên, lọc theo nhóm, sắp xếp
    private IQueryable<LoaiDichVu> Loc(string? q, string? nhom, string? sapXep)
    {
        var ds = _context.LoaiDichVu.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            ds = ds.Where(l => l.TenLoaiDichVu.Contains(q));
        }
        if (!string.IsNullOrEmpty(nhom))
            ds = ds.Where(l => l.NhomDichVu == nhom);

        return sapXep switch
        {
            "ten_tang" => ds.OrderBy(l => l.TenLoaiDichVu),
            "ten_giam" => ds.OrderByDescending(l => l.TenLoaiDichVu),
            _ => ds.OrderBy(l => l.NhomDichVu).ThenBy(l => l.TenLoaiDichVu)
        };
    }

    public async Task<IActionResult> Index(string? q, string? nhom, string? sapXep, int trang = 1)
    {
        const int kichThuoc = 5;
        var ds = Loc(q, nhom, sapXep);

        int tong = await ds.CountAsync();
        int tongTrang = Math.Max(1, (int)Math.Ceiling(tong / (double)kichThuoc));
        trang = Math.Clamp(trang, 1, tongTrang);

        var dsTrang = await ds.Skip((trang - 1) * kichThuoc).Take(kichThuoc).ToListAsync();

        ViewBag.Q = q?.Trim();
        ViewBag.Nhom = nhom;
        ViewBag.SapXep = sapXep;
        ViewBag.Trang = trang;
        ViewBag.TongTrang = tongTrang;
        ViewBag.Tong = tong;
        NapNhom(nhom);

        // Yêu cầu AJAX chỉ cần phần bảng, không cần cả trang
        if (LaAjax()) return PartialView("_DanhSach", dsTrang);
        return View(dsTrang);
    }

    // Xuất Excel theo đúng bộ lọc đang chọn (không phân trang)
    public async Task<IActionResult> XuatExcel(string? q, string? nhom, string? sapXep)
    {
        var ds = await Loc(q, nhom, sapXep).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("LoaiDichVu");
        string[] tieuDe = { "Mã", "Tên loại dịch vụ", "Nhóm dịch vụ", "Mô tả", "Trạng thái" };
        for (int i = 0; i < tieuDe.Length; i++) ws.Cell(1, i + 1).Value = tieuDe[i];
        ws.Row(1).Style.Font.Bold = true;

        int dong = 2;
        foreach (var l in ds)
        {
            ws.Cell(dong, 1).Value = l.MaLoaiDichVu;
            ws.Cell(dong, 2).Value = l.TenLoaiDichVu;
            ws.Cell(dong, 3).Value = l.NhomDichVu;
            ws.Cell(dong, 4).Value = l.MoTa ?? "";
            ws.Cell(dong, 5).Value = l.TrangThai ? "Hoạt động" : "Đã tắt";
            dong++;
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "LoaiDichVu.xlsx");
    }

    // Dữ liệu cho biểu đồ Chart.js (GroupBy + Count bằng LINQ)
    [HttpGet]
    public async Task<IActionResult> DuLieuBieuDo()
    {
        var nhom = await _context.LoaiDichVu
            .GroupBy(l => l.NhomDichVu)
            .Select(g => new { ten = g.Key, soLuong = g.Count() })
            .ToListAsync();
        int hoatDong = await _context.LoaiDichVu.CountAsync(l => l.TrangThai);
        int daTat = await _context.LoaiDichVu.CountAsync(l => !l.TrangThai);
        return Json(new { nhom, hoatDong, daTat });
    }

    [HttpGet]
    public IActionResult Create()
    {
        NapNhom(null);
        return View(new LoaiDichVu());
    }

    //Thêm , chống trùng , kiểm tra nhóm hợp lệ
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
        string tb = (l.TrangThai ? "Đã bật " : "Đã tắt ") + l.TenLoaiDichVu + ".";

        if (LaAjax()) return Json(new { ok = true, thongBao = tb });

        TempData["ThongBao"] = tb;
        return RedirectToAction(nameof(Index));
    }
    //Chi tiết
    public async Task<IActionResult> Details(int id)
    {
        var l = await _context.LoaiDichVu.FindAsync(id);
        if (l == null) return NotFound();


        ViewBag.SoDangDung = await DemSoDangDung(id);
        return View(l);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var l = await _context.LoaiDichVu.FindAsync(id);
        if (l == null) return NotFound();

        ViewBag.SoDangDung = await DemSoDangDung(id);
        return View(l);
    }

    
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var l = await _context.LoaiDichVu.FindAsync(id);
        if (l == null) return NotFound();

        if (await DemSoDangDung(id) > 0)
        {
            TempData["Loi"] = "Không thể xóa \"" + l.TenLoaiDichVu + "\" vì đang được sử dụng. Hãy tắt (ẩn) thay vì xóa.";
            return RedirectToAction(nameof(Index));
        }

        _context.LoaiDichVu.Remove(l);
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã xóa loại dịch vụ " + l.TenLoaiDichVu + ".";
        return RedirectToAction(nameof(Index));
    }

    // Đếm số bản ghi đang tham chiếu tới loại dịch vụ.
    private async Task<int> DemSoDangDung(int ma)
    {
        int n = await _context.ChuyenMonThuCungs.CountAsync(c => c.MaLoaiDichVu == ma);
        
        // n += await _context.DichVus.CountAsync(d => d.MaLoaiDichVu == ma);
        return n;
    }
    private void NapNhom(string? chon)
    {
        ViewBag.DsNhom = new SelectList(HangSo.NhomDichVu.DanhSach, chon);
    }
}