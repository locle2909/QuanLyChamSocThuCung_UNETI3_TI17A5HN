
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    [PhanQuyen(HangSo.VaiTro.Admin, HangSo.VaiTro.NhanVien)]
    public class PhanCongNhanVienController : Controller
    {
        private readonly AppDbContext _context;

        public PhanCongNhanVienController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Hiển thị danh sách phân công
        // 1. Hiển thị danh sách phân công
        public async Task<IActionResult> Index()
        {
            var truyVan =
                from pc in _context.PhanCongNhanVien
                join nv in _context.NhanVien on pc.MaNhanVien equals nv.MaNhanVien
                join ca in _context.CaDichVu on pc.MaCaDichVu equals ca.MaCaDichVu
                orderby ca.ThoiGianBatDau descending
                select new PhanCongHienThi
                {
                    MaPhanCong = pc.MaPhanCong,
                    MaNhanVien = nv.MaNhanVien,
                    HoTenNhanVien = nv.HoTen,
                    MaCaDichVu = ca.MaCaDichVu,
                    ThoiGianBatDau = ca.ThoiGianBatDau,
                    ThoiGianKetThuc = ca.ThoiGianKetThuc,
                    TrangThai = pc.TrangThai,
                    GhiChu = pc.GhiChu
                };

            // Nhân viên chỉ xem phân công của chính mình
            if (!HttpContext.LaAdmin())
            {
                var maTk = HttpContext.LayMaTaiKhoan();
                var maNhanVien = await _context.NhanVien
                    .Where(n => n.MaTaiKhoan == maTk && n.TrangThai)
                    .Select(n => (int?)n.MaNhanVien)
                    .FirstOrDefaultAsync();

                ViewBag.ChuaLienKet = maNhanVien == null;
                truyVan = truyVan.Where(x => x.MaNhanVien == maNhanVien);
            }

            return View(await truyVan.ToListAsync());
        }

        // 2. Hiển thị giao diện tạo phân công
        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Create()
        {
            await NapDanhSachLuaChon();

            return View(new PhanCongNhanVien());
        }

        // 3. Lấy nhân viên có chuyên môn phù hợp với ca được chọn
        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> NhanVienTheoCa(int maCaDichVu)
        {
            var ca = await _context.CaDichVu.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaCaDichVu == maCaDichVu);

            if (ca == null || ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return Json(new List<object>());

            var dichVu = await _context.DichVus.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaDichVu == ca.MaDichVu);
            if (dichVu == null)
                return Json(new List<object>());

            var nhom = dichVu.NhomDichVu.Trim();
            var maThuCung = ca.MaThuCung;

            // Thú cưng của ca phải có thông tin chăm sóc hiệu lực đúng loại dịch vụ
            bool thuCungDung = await (
                from cm in _context.ChuyenMonThuCungs
                join l in _context.LoaiDichVu on cm.MaLoaiDichVu equals l.MaLoaiDichVu
                where cm.MaThuCung == maThuCung
                      && cm.TrangThai == HangSo.TrangThaiChuyenMon.HieuLuc
                      && l.TrangThai
                      && l.NhomDichVu == nhom
                select cm.MaChuyenMon).AnyAsync();
            if (!thuCungDung)
                return Json(new List<object>());

            var danhSach = await _context.NhanVien
                .Where(nv => nv.TrangThai &&
                             _context.ChuyenMonNhanVien.Any(cm =>
                                 cm.MaNhanVien == nv.MaNhanVien &&
                                 cm.TenChuyenMon.Trim() == nhom))
                .OrderBy(nv => nv.HoTen)
                .Select(nv => new { nv.MaNhanVien, nv.HoTen })
                .ToListAsync();

            return Json(danhSach);
        }

        // 4. Xử lý tạo phân công
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Create(PhanCongNhanVien pc)
        {
            // Hai trường này do hệ thống tự thiết lập.
            ModelState.Remove(nameof(pc.NgayPhanCong));
            ModelState.Remove(nameof(pc.TrangThai));

            // Kiểm tra mã nhân viên.
            if (pc.MaNhanVien <= 0)
            {
                ModelState.AddModelError(
                    nameof(pc.MaNhanVien),
                    "Vui lòng chọn nhân viên.");
            }

            // Kiểm tra mã ca.
            if (pc.MaCaDichVu <= 0)
            {
                ModelState.AddModelError(
                    nameof(pc.MaCaDichVu),
                    "Vui lòng chọn ca dịch vụ.");
            }

            // Kiểm tra nhân viên đang hoạt động.
            var nhanVien = await _context.NhanVien
                .FirstOrDefaultAsync(x =>
                    x.MaNhanVien == pc.MaNhanVien &&
                    x.TrangThai);

            if (nhanVien == null && pc.MaNhanVien > 0)
            {
                ModelState.AddModelError(
                    nameof(pc.MaNhanVien),
                    "Nhân viên không tồn tại hoặc đã ngừng hoạt động.");
            }

            // Kiểm tra ca dịch vụ.
            var ca = await _context.CaDichVu
                .FirstOrDefaultAsync(x =>
                    x.MaCaDichVu == pc.MaCaDichVu);

            if (ca == null && pc.MaCaDichVu > 0)
            {
                ModelState.AddModelError(
                    nameof(pc.MaCaDichVu),
                    "Ca dịch vụ không tồn tại.");
            }

            if (ca != null)
            {
                // Không phân công vào ca đã hủy hoặc hoàn thành.
                if (ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHuy ||
                    ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh)
                {
                    ModelState.AddModelError(
                        nameof(pc.MaCaDichVu),
                        "Không thể phân công vào ca đã hủy hoặc hoàn thành.");
                }

                // Kiểm tra thời gian ca.
                if (ca.ThoiGianKetThuc <= ca.ThoiGianBatDau)
                {
                    ModelState.AddModelError(
                        nameof(pc.MaCaDichVu),
                        "Thời gian kết thúc phải sau thời gian bắt đầu.");
                }
            }

            // Kiểm tra chuyên môn phù hợp với nhóm dịch vụ.
            // 8.2: kiểm tra chuyên môn nhân viên và thông tin chăm sóc của thú cưng
            if (nhanVien != null && ca != null)
            {
                var loiChuyenMon = await KiemTraChuyenMon(ca, nhanVien.MaNhanVien);
                if (loiChuyenMon != null)
                    ModelState.AddModelError(nameof(pc.MaNhanVien), loiChuyenMon);
            }

            // Kiểm tra nhân viên có bị trùng lịch hay không.
            if (nhanVien != null &&
                ca != null &&
                ca.ThoiGianKetThuc > ca.ThoiGianBatDau &&
                ca.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy &&
                ca.TrangThai != HangSo.TrangThaiCaDichVu.DaHoanThanh)
            {
                var biTrungLich = await (
                    from pcCu in _context.PhanCongNhanVien
                    join caCu in _context.CaDichVu
                        on pcCu.MaCaDichVu equals caCu.MaCaDichVu
                    where pcCu.MaNhanVien == pc.MaNhanVien

                          // Bỏ qua phân công đã hủy.
                          && pcCu.TrangThai !=
                             HangSo.TrangThaiPhanCong.DaHuy

                          // Bỏ qua phân công đã hoàn thành.
                          && pcCu.TrangThai !=
                             HangSo.TrangThaiPhanCong.HoanThanh

                          // Bỏ qua ca đã hủy hoặc hoàn thành.
                          && caCu.TrangThai !=
                             HangSo.TrangThaiCaDichVu.DaHuy

                          && caCu.TrangThai !=
                             HangSo.TrangThaiCaDichVu.DaHoanThanh

                          // Hai khoảng thời gian giao nhau.
                          && caCu.ThoiGianBatDau < ca.ThoiGianKetThuc
                          && caCu.ThoiGianKetThuc > ca.ThoiGianBatDau

                    select pcCu.MaPhanCong
                ).AnyAsync();

                if (biTrungLich)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Nhân viên đã có ca bị trùng thời gian. " +
                        "Hãy chọn nhân viên hoặc ca khác.");
                }
            }

            // Nếu dữ liệu không hợp lệ, quay lại giao diện.
            if (!ModelState.IsValid)
            {
                await NapDanhSachLuaChon();
                return View(pc);
            }

            // Thiết lập dữ liệu phân công.
            pc.NgayPhanCong = DateTime.Now;
            pc.TrangThai = HangSo.TrangThaiPhanCong.HieuLuc;

            _context.PhanCongNhanVien.Add(pc);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Phân công nhân viên thành công.";

            return RedirectToAction(nameof(Index));
        }

        // 5. Hủy phân công
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Huy(int id)
        {
            var pc = await _context.PhanCongNhanVien
                .FirstOrDefaultAsync(x => x.MaPhanCong == id);

            if (pc == null)
                return NotFound();

            if (pc.TrangThai == HangSo.TrangThaiPhanCong.DaHuy)
            {
                TempData["ThongBao"] = "Phân công này đã được hủy.";
                return RedirectToAction(nameof(Index));
            }

            if (pc.TrangThai == HangSo.TrangThaiPhanCong.HoanThanh)
            {
                TempData["ThongBao"] =
                    "Không thể hủy phân công đã hoàn thành.";

                return RedirectToAction(nameof(Index));
            }

            var ca = await _context.CaDichVu
                .FirstOrDefaultAsync(x =>
                    x.MaCaDichVu == pc.MaCaDichVu);

            if (ca != null &&
                ca.TrangThai == HangSo.TrangThaiCaDichVu.DaHoanThanh)
            {
                TempData["ThongBao"] =
                    "Không thể hủy phân công của ca đã hoàn thành.";

                return RedirectToAction(nameof(Index));
            }

            pc.TrangThai = HangSo.TrangThaiPhanCong.DaHuy;

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã hủy phân công nhân viên.";

            return RedirectToAction(nameof(Index));
        }

        // 8.2: nhân viên có chuyên môn đúng nhóm dịch vụ, và thú cưng của ca có thông tin chăm sóc
        // đang hiệu lực đúng loại dịch vụ. Trả về null nếu hợp lệ, ngược lại trả về thông báo lỗi.
        private async Task<string?> KiemTraChuyenMon(CaDichVu ca, int maNhanVien)
        {
            var dichVu = await _context.DichVus.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaDichVu == ca.MaDichVu);
            if (dichVu == null)
                return "Không tìm thấy dịch vụ của ca này.";

            var nhom = dichVu.NhomDichVu.Trim();

            bool nhanVienDung = await _context.ChuyenMonNhanVien.AnyAsync(x =>
                x.MaNhanVien == maNhanVien && x.TenChuyenMon.Trim() == nhom);
            if (!nhanVienDung)
                return "Nhân viên không có chuyên môn phù hợp với dịch vụ này.";

            var maThuCung = ca.MaThuCung;
            bool thuCungDung = await (
                from cm in _context.ChuyenMonThuCungs
                join l in _context.LoaiDichVu on cm.MaLoaiDichVu equals l.MaLoaiDichVu
                where cm.MaThuCung == maThuCung
                      && cm.TrangThai == HangSo.TrangThaiChuyenMon.HieuLuc
                      && l.TrangThai
                      && l.NhomDichVu == nhom
                select cm.MaChuyenMon).AnyAsync();
            if (!thuCungDung)
                return "Thú cưng chưa có thông tin chăm sóc đang hiệu lực đúng loại dịch vụ này.";

            return null;
        }

        // 6. Nạp danh sách lựa chọn cho giao diện
        private async Task NapDanhSachLuaChon()
        {
            ViewBag.NhanViens = await _context.NhanVien
                .Where(x => x.TrangThai)
                .OrderBy(x => x.HoTen)
                .ToListAsync();

            ViewBag.CaDichVus = await _context.CaDichVu
                .Where(x =>
                    x.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy &&
                    x.TrangThai != HangSo.TrangThaiCaDichVu.DaHoanThanh)
                .OrderBy(x => x.ThoiGianBatDau)
                .ToListAsync();
        }
    }

    // Model dùng để hiển thị danh sách phân công
    public class PhanCongHienThi
    {
        public int MaPhanCong { get; set; }

        public int MaNhanVien { get; set; }

        public string HoTenNhanVien { get; set; } = string.Empty;

        public int MaCaDichVu { get; set; }

        public DateTime ThoiGianBatDau { get; set; }

        public DateTime ThoiGianKetThuc { get; set; }

        public string TrangThai { get; set; } = string.Empty;

        public string? GhiChu { get; set; }
    }
}
