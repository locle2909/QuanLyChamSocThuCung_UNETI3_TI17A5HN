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
using System.Security.Cryptography;
using System.Text;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers;

public class TaiKhoanController : Controller
{
    private readonly AppDbContext _context;
    private readonly EmailService _email;

    public TaiKhoanController(AppDbContext context, EmailService email)
    {
        _context = context;
        _email = email;
    }

    // ================= ĐĂNG NHẬP / ĐĂNG XUẤT (không yêu cầu quyền) =================

    [HttpGet]
    public IActionResult DangNhap()
    {
        if (HttpContext.Session.GetString(HangSo.SessionKey.VaiTro) != null)
            return RedirectToAction("Index", "Home");

        return View(new DangNhapViewModel());
    }
    // Xử lý đăng nhập
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

        // Lưu phiên 
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

    //Thêm , kiểm tra trùng tên đăng nhập
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
    // ================= QUÊN MẬT KHẨU QUA EMAIL (không yêu cầu quyền) =================

    [HttpGet]
    public IActionResult QuenMatKhau() => View(new QuenMatKhauViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuenMatKhau(QuenMatKhauViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string email = model.Email.Trim();
        var ds = await _context.TaiKhoan.Where(t => t.Email == email && t.TrangThai).ToListAsync();

        // Chỉ xử lý khi có đúng một tài khoản dùng email này.
        // Dù đúng hay sai đều trả lời giống nhau để không lộ email nào có tài khoản.
        if (ds.Count == 1)
        {
            var tk = ds[0];

            // Chống gửi dồn dập: mỗi tài khoản tối đa 1 yêu cầu mỗi phút
            bool vuaGui = await _context.YeuCauDatLaiMatKhau
                .AnyAsync(y => y.MaTaiKhoan == tk.MaTaiKhoan && y.NgayTao > DateTime.Now.AddMinutes(-1));

            if (!vuaGui)
            {
                // Vô hiệu các yêu cầu cũ chưa dùng của tài khoản này
                var cu = await _context.YeuCauDatLaiMatKhau
                    .Where(y => y.MaTaiKhoan == tk.MaTaiKhoan && !y.DaDung).ToListAsync();
                foreach (var y in cu) y.DaDung = true;

                string token = TaoToken();
                _context.YeuCauDatLaiMatKhau.Add(new YeuCauDatLaiMatKhau
                {
                    MaTaiKhoan = tk.MaTaiKhoan,
                    MaHash = BamToken(token),
                    NgayTao = DateTime.Now,
                    HetHan = DateTime.Now.AddMinutes(15)
                });
                await _context.SaveChangesAsync();

                string lienKet = Url.Action(nameof(DatMatKhauMoi), "TaiKhoan", new { token }, Request.Scheme)!;
                string html = "<p>Xin chào " + System.Net.WebUtility.HtmlEncode(tk.HoTen) + ",</p>"
                    + "<p>Bạn vừa yêu cầu đặt lại mật khẩu tài khoản PetCare. Bấm vào liên kết dưới đây "
                    + "(có hiệu lực trong 15 phút, chỉ dùng được một lần):</p>"
                    + "<p><a href=\"" + lienKet + "\">Đặt lại mật khẩu</a></p>"
                    + "<p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>";
                await _email.GuiAsync(tk.Email!, "PetCare - Đặt lại mật khẩu", html, lienKet);
            }
        }

        TempData["ThongBao"] = "Nếu email này thuộc một tài khoản đang hoạt động, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu. Liên kết có hiệu lực 15 phút.";
        return RedirectToAction(nameof(QuenMatKhau));
    }

    [HttpGet]
    public async Task<IActionResult> DatMatKhauMoi(string? token)
    {
        if (string.IsNullOrEmpty(token) || await TimYeuCauHopLe(token) == null)
            return View("LienKetKhongHopLe");

        return View(new DatMatKhauMoiViewModel { Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DatMatKhauMoi(DatMatKhauMoiViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var yc = await TimYeuCauHopLe(model.Token);
        if (yc == null) return View("LienKetKhongHopLe");

        var tk = await _context.TaiKhoan.FindAsync(yc.MaTaiKhoan);
        if (tk == null || !tk.TrangThai) return View("LienKetKhongHopLe");

        tk.MatKhau = PasswordHasher.Hash(model.MatKhauMoi);

        // Mã này và mọi mã khác chưa dùng của tài khoản đều hết hiệu lực
        var conLai = await _context.YeuCauDatLaiMatKhau
            .Where(y => y.MaTaiKhoan == tk.MaTaiKhoan && !y.DaDung).ToListAsync();
        foreach (var y in conLai) y.DaDung = true;

        await _context.SaveChangesAsync();

        TempData["ThongBao"] = "Đã đặt lại mật khẩu. Vui lòng đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(DangNhap));
    }

    // Mã hợp lệ: đúng bản băm, chưa dùng, chưa hết hạn
    private async Task<YeuCauDatLaiMatKhau?> TimYeuCauHopLe(string token)
    {
        string hash = BamToken(token);
        return await _context.YeuCauDatLaiMatKhau
            .FirstOrDefaultAsync(y => y.MaHash == hash && !y.DaDung && y.HetHan > DateTime.Now);
    }

    // 32 byte ngẫu nhiên, mã hóa Base64 an toàn cho URL
    private static string TaoToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    // Chỉ lưu SHA-256 của mã trong database, không lưu mã gốc
    private static string BamToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private void NapVaiTro(string? chon)
    {
        ViewBag.DsVaiTro = new SelectList(HangSo.VaiTro.TenHienThi, "Key", "Value", chon);
    }
}