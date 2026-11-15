using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    [PhanQuyen(HangSo.VaiTro.Admin, HangSo.VaiTro.NhanVien)]
    public class CaDichVuController : Controller
    {
        private readonly AppDbContext _context;

        public CaDichVuController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            ViewBag.DsTenDichVu = await _context.DichVus.AsNoTracking()
                .ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
            ViewBag.DsTenThuCung = await _context.ThuCungs.AsNoTracking()
                .ToDictionaryAsync(x => x.MaThuCung, x => x.HoTen);

            var truyVan = _context.CaDichVu.AsNoTracking();

            if (!HttpContext.LaAdmin())
            {
                var maTk = HttpContext.LayMaTaiKhoan();
                var caCuaToi = _context.CaCuaNhanVien(maTk);
                truyVan = truyVan.Where(c => caCuaToi.Contains(c.MaCaDichVu));
                ViewBag.ChuaLienKet = !await _context.NhanVien.AnyAsync(n => n.MaTaiKhoan == maTk && n.TrangThai);
            }

            var danhSach = await truyVan.OrderByDescending(x => x.ThoiGianBatDau).ToListAsync();
            return View(danhSach);
        }

        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Create()
        {
            await NapLuaChon();
            var batDau = DateTime.Today.AddDays(1).AddHours(KiemTraLichHelper.GioMoCua);
            return View(new CaDichVu
            {
                ThoiGianBatDau = batDau,
                ThoiGianKetThuc = batDau.AddHours(1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Create(
            [Bind("MaDichVu,MaThuCung,ThoiGianBatDau,ThoiGianKetThuc,PhongHocHoacHinhThuc,GhiChu")] CaDichVu ca)
        {
            ca.PhongHocHoacHinhThuc = string.IsNullOrWhiteSpace(ca.PhongHocHoacHinhThuc)
                ? null : ca.PhongHocHoacHinhThuc.Trim();

            if (ca.ThoiGianBatDau <= DateTime.Now)
                ModelState.AddModelError(nameof(ca.ThoiGianBatDau), "Không thể tạo ca trong quá khứ.");

            foreach (var (truong, thongBao) in await KiemTraLichHelper.KiemTraCa(_context, ca))
                ModelState.AddModelError(truong, thongBao);

            if (!ModelState.IsValid)
            {
                await NapLuaChon();
                return View(ca);
            }

            ca.TrangThai = HangSo.TrangThaiCaDichVu.DaLenLich;
            _context.CaDichVu.Add(ca);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Tạo ca dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Edit(int id)
        {
            var ca = await _context.CaDichVu.AsNoTracking().FirstOrDefaultAsync(x => x.MaCaDichVu == id);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ sửa được ca đang ở trạng thái 'Đã lên lịch'.");

            await NapTenHienThi(ca);
            return View(ca);
        }

        // Chỉ cho đổi giờ, phòng, ghi chú. Dịch vụ và thú cưng của ca không đổi được.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Edit(int id,
            [Bind("MaCaDichVu,ThoiGianBatDau,ThoiGianKetThuc,PhongHocHoacHinhThuc,GhiChu")] CaDichVu model)
        {
            if (id != model.MaCaDichVu) return NotFound();

            var ca = await _context.CaDichVu.FindAsync(id);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ sửa được ca đang ở trạng thái 'Đã lên lịch'.");

            model.MaDichVu = ca.MaDichVu;
            model.MaThuCung = ca.MaThuCung;
            model.TrangThai = ca.TrangThai;
            model.PhongHocHoacHinhThuc = string.IsNullOrWhiteSpace(model.PhongHocHoacHinhThuc)
                ? null : model.PhongHocHoacHinhThuc.Trim();

            bool doiGio = model.ThoiGianBatDau != ca.ThoiGianBatDau
                          || model.ThoiGianKetThuc != ca.ThoiGianKetThuc;
            bool doiPhong = model.PhongHocHoacHinhThuc != ca.PhongHocHoacHinhThuc;

            if (doiGio)
            {
                if (model.ThoiGianBatDau <= DateTime.Now)
                    ModelState.AddModelError(nameof(model.ThoiGianBatDau), "Không thể dời ca về quá khứ.");

                bool coPhanCong = await _context.PhanCongNhanVien.AnyAsync(x =>
                    x.MaCaDichVu == id && x.TrangThai != HangSo.TrangThaiPhanCong.DaHuy);
                if (coPhanCong)
                    ModelState.AddModelError(string.Empty,
                        "Ca đã có nhân viên được phân công. Hãy hủy phân công trước khi đổi thời gian.");
            }

            if (doiGio || doiPhong)
            {
                foreach (var (truong, thongBao) in await KiemTraLichHelper.KiemTraCa(_context, model, boQuaMaCa: id))
                    ModelState.AddModelError(truong, thongBao);
            }

            if (!ModelState.IsValid)
            {
                await NapTenHienThi(ca);
                return View(model);
            }

            ca.ThoiGianBatDau = model.ThoiGianBatDau;
            ca.ThoiGianKetThuc = model.ThoiGianKetThuc;
            ca.PhongHocHoacHinhThuc = model.PhongHocHoacHinhThuc;
            ca.GhiChu = model.GhiChu;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Cập nhật ca dịch vụ thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Huy(int id)
        {
            var ca = await _context.CaDichVu.FindAsync(id);
            if (ca == null) return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ hủy được ca đang ở trạng thái 'Đã lên lịch'.");

            ca.TrangThai = HangSo.TrangThaiCaDichVu.DaHuy;

            // Phân công còn hiệu lực của ca này cũng bị hủy
            var phanCongs = await _context.PhanCongNhanVien
                .Where(x => x.MaCaDichVu == id && x.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc)
                .ToListAsync();
            foreach (var pc in phanCongs)
                pc.TrangThai = HangSo.TrangThaiPhanCong.DaHuy;

            await _context.SaveChangesAsync();
            TempData["ThongBao"] = "Đã hủy ca dịch vụ.";
            return RedirectToAction(nameof(Index));
        }

        // 8.8: hoàn thành ca khi tất cả kết quả của ca đều "Hoàn thành"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoanThanh(int id)
        {
            var ca = await _context.CaDichVu.FindAsync(id);
            if (ca == null) return NotFound();

            if (!HttpContext.LaAdmin() && !await _context.CaCuaNhanVien(HttpContext.LayMaTaiKhoan()).AnyAsync(c => c == id))
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Loi("Chỉ hoàn thành được ca đang ở trạng thái 'Đã lên lịch'.");

            if (ca.ThoiGianBatDau > DateTime.Now)
                return Loi("Ca chưa bắt đầu nên chưa thể hoàn thành.");

            var trangThais = await _context.KetQuaDichVu
                .Where(x => x.MaCaDichVu == id)
                .Select(x => x.TrangThaiKetQuaDichVu)
                .ToListAsync();

            if (trangThais.Count == 0)
                return Loi("Ca chưa có kết quả dịch vụ nào để hoàn thành.");

            int chuaXong = trangThais.Count(t => t != HangSo.TrangThaiKetQua.HoanThanh);
            if (chuaXong > 0)
                return Loi($"Còn {chuaXong} kết quả chưa ở trạng thái 'Hoàn thành'.");

            ca.TrangThai = HangSo.TrangThaiCaDichVu.DaHoanThanh;

            var phanCongs = await _context.PhanCongNhanVien
                .Where(x => x.MaCaDichVu == id && x.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc)
                .ToListAsync();
            foreach (var pc in phanCongs)
                pc.TrangThai = HangSo.TrangThaiPhanCong.HoanThanh;

            await _context.SaveChangesAsync();
            TempData["ThongBao"] = "Ca dịch vụ đã hoàn thành.";
            return RedirectToAction(nameof(Index));
        }

        private IActionResult Loi(string thongBao)
        {
            TempData["Loi"] = thongBao;
            return RedirectToAction(nameof(Index));
        }

        private async Task NapLuaChon()
        {
            ViewBag.DichVus = await _context.DichVus.AsNoTracking()
                .Where(x => x.TrangThai != HangSo.TrangThaiDichVu.TamNgung
                         && x.TrangThai != HangSo.TrangThaiDichVu.NgungPhucVu)
                .OrderBy(x => x.TenDichVu)
                .ToListAsync();

            ViewBag.ThuCungs = await _context.ThuCungs.AsNoTracking()
                .Where(x => x.TrangThai == "Hoạt động")
                .OrderBy(x => x.HoTen)
                .ToListAsync();
        }

        private async Task NapTenHienThi(CaDichVu ca)
        {
            ViewBag.TenDichVu = await _context.DichVus.AsNoTracking()
                .Where(x => x.MaDichVu == ca.MaDichVu).Select(x => x.TenDichVu).FirstOrDefaultAsync()
                ?? ("Dịch vụ #" + ca.MaDichVu);
            ViewBag.TenThuCung = await _context.ThuCungs.AsNoTracking()
                .Where(x => x.MaThuCung == ca.MaThuCung).Select(x => x.HoTen).FirstOrDefaultAsync()
                ?? ("Thú cưng #" + ca.MaThuCung);
        }
    }
}