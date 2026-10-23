// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Đăng nhập, đăng xuất, Session, quản lý tài khoản (Admin).
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

public class TaiKhoanController : Controller
{
    private readonly AppDbContext _context;

    public TaiKhoanController(AppDbContext context)
    {
        _context = context;
    }

    // ================= ĐĂNG NHẬP / ĐĂNG XUẤT (không yêu cầu quyền) =================

    [HttpGet]
    public IActionResult DangNhap()
    {
        if (HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) != null)
            return RedirectToAction("Index", "Home");

        return View(new DangNhapViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var tenDangNhap = model.TenDangNhap.Trim();
        var tk = await _context.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

        if (tk == null)
        {
            ModelState.AddModelError("", "Tài khoản không tồn tại.");
            return View(model);
        }
        if (!PasswordHasher.Verify(model.MatKhau, tk.MatKhau))
        {
            ModelState.AddModelError("", "Sai mật khẩu.");
            return View(model);
        }
        if (!tk.TrangThai)
        {
            ModelState.AddModelError("", "Tài khoản đã bị khóa.");
            return View(model);
        }

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(HangSo.SessionKey.MaTaiKhoan, tk.MaTaiKhoan);
        HttpContext.Session.SetString(HangSo.SessionKey.HoTen, tk.HoTen);
        HttpContext.Session.SetString(HangSo.SessionKey.VaiTro, tk.VaiTro);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult DangXuat()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(DangNhap));
    }

    public IActionResult KhongCoQuyen()
    {
        Response.StatusCode = 403;
        return View();
    }

    // ================= QUẢN LÝ TÀI KHOẢN (chỉ Admin) =================

    [PhanQuyen(HangSo.VaiTro.Admin)]
    public async Task<IActionResult> Index(string? q, string? vaiTro)
    {
        var ds = _context.TaiKhoan.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            ds = ds.Where(t => t.TenDangNhap.Contains(q) || t.HoTen.Contains(q));
        }
        if (!string.IsNullOrEmpty(vaiTro))
            ds = ds.Where(t => t.VaiTro == vaiTro);

        ViewBag.Q = q;
        ViewBag.DsVaiTro = new SelectList(HangSo.VaiTro.TenHienThi, "Key", "Value", vaiTro);
        ViewBag.MaHienTai = HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan) ?? 0;

        return View(await ds.OrderBy(t => t.VaiTro).ThenBy(t => t.HoTen).ToListAsync());
    }

    [PhanQuyen(HangSo.VaiTro.Admin)]
    [HttpGet]
    public IActionResult Create()
    {
        NapVaiTro(HangSo.VaiTro.KhachHang);
        return View(new TaiKhoan());
    }

    [PhanQuyen(HangSo.VaiTro.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TenDangNhap,MatKhau,HoTen,Email,VaiTro,TrangThai")] TaiKhoan model)
    {
        model.TenDangNhap = (model.TenDangNhap ?? "").Trim();

        if (await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == model.TenDangNhap))
            ModelState.AddModelError(nameof(TaiKhoan.TenDangNhap), "Tên đăng nhập đã tồn tại.");

        if (!HangSo.VaiTro.TenHienThi.ContainsKey(model.VaiTro ?? ""))
            ModelState.AddModelError(nameof(TaiKhoan.VaiTro), "Vai trò không hợp lệ.");

        if (!ModelState.IsValid)
        {
            NapVaiTro(model.VaiTro);
            return View(model);
        }

        model.MatKhau = PasswordHasher.Hash(model.MatKhau);
        _context.TaiKhoan.Add(model);
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã thêm tài khoản " + model.TenDangNhap + ".";
        return RedirectToAction(nameof(Index));
    }

    [PhanQuyen(HangSo.VaiTro.Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tk = await _context.TaiKhoan.FindAsync(id);
        if (tk == null) return NotFound();

        tk.MatKhau = string.Empty;   // không hiển thị mật khẩu cũ
        NapVaiTro(tk.VaiTro);
        return View(tk);
    }

    [PhanQuyen(HangSo.VaiTro.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("MaTaiKhoan,MatKhau,HoTen,Email,VaiTro,TrangThai")] TaiKhoan model)
    {
        if (id != model.MaTaiKhoan) return NotFound();
        var tk = await _context.TaiKhoan.FindAsync(id);
        if (tk == null) return NotFound();

        // Không cho sửa tên đăng nhập; mật khẩu để trống nghĩa là giữ nguyên.
        ModelState.Remove(nameof(TaiKhoan.TenDangNhap));
        bool doiMatKhau = !string.IsNullOrWhiteSpace(model.MatKhau);
        if (!doiMatKhau) ModelState.Remove(nameof(TaiKhoan.MatKhau));

        if (!HangSo.VaiTro.TenHienThi.ContainsKey(model.VaiTro ?? ""))
            ModelState.AddModelError(nameof(TaiKhoan.VaiTro), "Vai trò không hợp lệ.");

        int maHienTai = HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan) ?? 0;
        if (id == maHienTai && (model.VaiTro != tk.VaiTro || !model.TrangThai))
            ModelState.AddModelError("", "Không thể đổi vai trò hoặc khóa chính tài khoản đang đăng nhập.");

        if (!ModelState.IsValid)
        {
            model.TenDangNhap = tk.TenDangNhap;
            model.MatKhau = string.Empty;
            NapVaiTro(model.VaiTro);
            return View(model);
        }

        tk.HoTen = model.HoTen;
        tk.Email = model.Email;
        tk.VaiTro = model.VaiTro;
        tk.TrangThai = model.TrangThai;
        if (doiMatKhau) tk.MatKhau = PasswordHasher.Hash(model.MatKhau);

        await _context.SaveChangesAsync();
        TempData["ThongBao"] = "Đã cập nhật tài khoản " + tk.TenDangNhap + ".";
        return RedirectToAction(nameof(Index));
    }

    // Khóa / mở khóa tài khoản
    [PhanQuyen(HangSo.VaiTro.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var tk = await _context.TaiKhoan.FindAsync(id);
        if (tk == null) return NotFound();

        if (id == HttpContext.Session.GetInt32(HangSo.SessionKey.MaTaiKhoan))
        {
            TempData["Loi"] = "Không thể khóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        tk.TrangThai = !tk.TrangThai;
        await _context.SaveChangesAsync();
        TempData["ThongBao"] = (tk.TrangThai ? "Đã mở khóa " : "Đã khóa ") + tk.TenDangNhap + ".";
        return RedirectToAction(nameof(Index));
    }

    private void NapVaiTro(string? chon)
    {
        ViewBag.DsVaiTro = new SelectList(HangSo.VaiTro.TenHienThi, "Key", "Value", chon);
    }
}