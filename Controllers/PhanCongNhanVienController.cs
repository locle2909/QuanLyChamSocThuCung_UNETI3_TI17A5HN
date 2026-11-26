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
        public async Task<IActionResult> Index(string? tuKhoa, string? trangThai, DateTime? tuNgay, DateTime? denNgay)
        {
            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangThai = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
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
                var maNhanVien = await (
                    from nv in _context.NhanVien
                    join tk in _context.TaiKhoan on nv.MaTaiKhoan equals (int?)tk.MaTaiKhoan
                    where tk.MaTaiKhoan == maTk
                          && tk.TrangThai
                          && tk.VaiTro == HangSo.VaiTro.NhanVien
                          && nv.TrangThai
                    select (int?)nv.MaNhanVien).FirstOrDefaultAsync();

                ViewBag.ChuaLienKet = maNhanVien == null;
                truyVan = truyVan.Where(x => x.MaNhanVien == maNhanVien);
            }

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                var kw = tuKhoa.Trim();
                var coMa = int.TryParse(kw, out var ma);
                truyVan = truyVan.Where(x =>
                    x.HoTenNhanVien.Contains(kw)
                    || (coMa && (x.MaNhanVien == ma || x.MaCaDichVu == ma || x.MaPhanCong == ma)));
            }

            if (!string.IsNullOrWhiteSpace(trangThai))
                truyVan = truyVan.Where(x => x.TrangThai == trangThai);
            if (tuNgay.HasValue)
                truyVan = truyVan.Where(x => x.ThoiGianBatDau >= tuNgay.Value.Date);
            if (denNgay.HasValue)
                truyVan = truyVan.Where(x => x.ThoiGianBatDau < denNgay.Value.Date.AddDays(1));

            return View(await truyVan.OrderByDescending(x => x.ThoiGianBatDau).ToListAsync());
        }

        // 2. Hiển thị giao diện tạo phân công
        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> Create()
        {
            await NapDanhSachLuaChon();

            return View(new PhanCongNhanVien());
        }

        // 3. Lấy nhân viên phù hợp với ca được chọn:
        //    đúng chuyên môn + đang hoạt động + không trùng lịch.
        //    Trả về { lyDo, danhSach } để giao diện báo đúng nguyên nhân khi rỗng.
        [HttpGet]
        [PhanQuyen(HangSo.VaiTro.Admin)]
        public async Task<IActionResult> NhanVienTheoCa(int maCaDichVu)
        {
            var ca = await _context.CaDichVu.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaCaDichVu == maCaDichVu);

            if (ca == null)
                return KetQuaNhanVien("Ca dịch vụ không tồn tại.");

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
                return KetQuaNhanVien(
                    "Ca này đang ở trạng thái \"" + ca.TrangThai +
                    "\", chỉ phân công được cho ca \"" +
                    HangSo.TrangThaiCaDichVu.DaLenLich + "\".");

            // Không cho phân công ca đã đến giờ bắt đầu hoặc đã quá giờ.
            if (ca.ThoiGianBatDau <= DateTime.Now)
                return KetQuaNhanVien(
                    "Ca đã đến giờ bắt đầu hoặc đã quá thời gian, không thể phân công.");

            // Mỗi ca chỉ được có một nhân viên phân công còn hiệu lực.
            if (await _context.PhanCongNhanVien.AnyAsync(pc =>
                    pc.MaCaDichVu == maCaDichVu
                    && pc.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc))
                return KetQuaNhanVien(
                    "Ca này đã có nhân viên được phân công. Không thể phân công thêm.");

            var dichVu = await _context.DichVus.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaDichVu == ca.MaDichVu);
            if (dichVu == null)
                return KetQuaNhanVien("Không tìm thấy dịch vụ của ca này (Dịch vụ " + ca.MaDichVu + ").");

            var nhom = dichVu.NhomDichVu;

            // Thú cưng của ca phải có thông tin chăm sóc hiệu lực đúng nhóm dịch vụ
            if (!await ThuCungDungNhomAsync(ca.MaThuCung, nhom))
                return KetQuaNhanVien(
                    "Thú cưng " + ca.MaThuCung + " chưa có thông tin chăm sóc đang hiệu lực " +
                    "thuộc nhóm \"" + nhom + "\". Hãy thêm ở mục \"Thông tin chăm sóc\".");

            // Nhân viên đang hoạt động và có chuyên môn đúng nhóm
            var nhanVienDungNhom = await LayNhanVienDungChuyenMonAsync(nhom);
            if (nhanVienDungNhom.Count == 0)
                return KetQuaNhanVien(
                    "Chưa có nhân viên đang hoạt động nào có chuyên môn \"" + nhom +
                    "\". Hãy thêm chuyên môn ở mục \"Nhân viên\".");

            // Loại những nhân viên đã có phân công giao thời gian với ca này
            var maNhanViens = nhanVienDungNhom.Select(n => n.MaNhanVien).ToList();

            var maBiTrung = await (
                from pcCu in _context.PhanCongNhanVien
                join caCu in _context.CaDichVu on pcCu.MaCaDichVu equals caCu.MaCaDichVu
                where maNhanViens.Contains(pcCu.MaNhanVien)
                      && pcCu.MaCaDichVu != ca.MaCaDichVu
                      && pcCu.TrangThai != HangSo.TrangThaiPhanCong.DaHuy
                      && pcCu.TrangThai != HangSo.TrangThaiPhanCong.HoanThanh
                      && caCu.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy
                      && caCu.TrangThai != HangSo.TrangThaiCaDichVu.DaHoanThanh
                      && caCu.ThoiGianBatDau < ca.ThoiGianKetThuc
                      && caCu.ThoiGianKetThuc > ca.ThoiGianBatDau
                select pcCu.MaNhanVien
            ).Distinct().ToListAsync();

            var danhSach = nhanVienDungNhom
                .Where(n => !maBiTrung.Contains(n.MaNhanVien))
                .OrderBy(n => n.HoTen)
                .Select(n => new { n.MaNhanVien, n.HoTen })
                .ToList();

            if (danhSach.Count == 0)
                return KetQuaNhanVien(
                    "Có " + nhanVienDungNhom.Count + " nhân viên đúng chuyên môn \"" + nhom +
                    "\" nhưng tất cả đều đã có ca trùng thời gian " +
                    ca.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm") + " - " +
                    ca.ThoiGianKetThuc.ToString("HH:mm") + ".");

            return Json(new { lyDo = (string?)null, danhSach });
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

            // Nhân viên phải đang hoạt động và đã liên kết tài khoản nhân viên đang hoạt động.
            var nhanVien = await NhanVienHopLe()
                .FirstOrDefaultAsync(x => x.MaNhanVien == pc.MaNhanVien);

            if (nhanVien == null && pc.MaNhanVien > 0)
            {
                ModelState.AddModelError(
                    nameof(pc.MaNhanVien),
                    "Nhân viên không tồn tại, đã ngừng hoạt động hoặc chưa liên kết tài khoản đăng nhập.");
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

                // Không cho phân công ca đã đến giờ bắt đầu hoặc đã quá giờ.
                if (ca.ThoiGianBatDau <= DateTime.Now)
                {
                    ModelState.AddModelError(
                        nameof(pc.MaCaDichVu),
                        "Ca đã đến giờ bắt đầu hoặc đã quá thời gian, không thể phân công.");
                }

                // Mỗi ca chỉ được có một nhân viên phân công còn hiệu lực.
                var caDaCoNhanVien = await _context.PhanCongNhanVien.AnyAsync(x =>
                    x.MaCaDichVu == pc.MaCaDichVu
                    && x.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc);

                if (caDaCoNhanVien)
                {
                    ModelState.AddModelError(
                        nameof(pc.MaCaDichVu),
                        "Ca này đã có nhân viên được phân công. Không thể phân công thêm.");
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

        // Nhân viên đủ điều kiện nhận phân công: hồ sơ đang hoạt động và đã liên kết
        // với tài khoản đăng nhập (vai trò Nhân viên, đang hoạt động)
        private IQueryable<NhanVien> NhanVienHopLe()
        {
            return from nv in _context.NhanVien
                   join tk in _context.TaiKhoan on nv.MaTaiKhoan equals (int?)tk.MaTaiKhoan
                   where nv.TrangThai
                         && tk.TrangThai
                         && tk.VaiTro == HangSo.VaiTro.NhanVien
                   select nv;
        }

        // 8.2: nhân viên có chuyên môn đúng nhóm dịch vụ, và thú cưng của ca có thông tin chăm sóc
        // đang hiệu lực đúng loại dịch vụ. Trả về null nếu hợp lệ, ngược lại trả về thông báo lỗi.
        private async Task<string?> KiemTraChuyenMon(CaDichVu ca, int maNhanVien)
        {
            var dichVu = await _context.DichVus.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaDichVu == ca.MaDichVu);
            if (dichVu == null)
                return "Không tìm thấy dịch vụ của ca này.";

            var nhom = dichVu.NhomDichVu;

            var nhanVienDung = (await LayNhanVienDungChuyenMonAsync(nhom))
                .Any(n => n.MaNhanVien == maNhanVien);
            if (!nhanVienDung)
                return "Nhân viên không có chuyên môn phù hợp với dịch vụ này.";

            if (!await ThuCungDungNhomAsync(ca.MaThuCung, nhom))
                return "Thú cưng chưa có thông tin chăm sóc đang hiệu lực đúng loại dịch vụ này.";

            return null;
        }

        // Chuẩn hóa chuỗi nhóm dịch vụ để so khớp: bỏ khoảng trắng thừa,
        // đồng nhất các loại dấu gạch, không phân biệt hoa/thường.
        private static string ChuanHoaNhom(string? chuoi)
        {
            if (string.IsNullOrWhiteSpace(chuoi)) return string.Empty;

            var s = chuoi
                .Replace('\u2013', '-')   // en dash
                .Replace('\u2014', '-')   // em dash
                .Replace('\u2212', '-');  // minus
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s*-\s*", "-");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return s.Trim().ToLowerInvariant();
        }

        // Nhân viên đang hoạt động có chuyên môn thuộc nhóm dịch vụ.
        private async Task<List<NhanVien>> LayNhanVienDungChuyenMonAsync(string nhom)
        {
            var nhomChuan = ChuanHoaNhom(nhom);

            var nhanViens = await NhanVienHopLe().AsNoTracking().ToListAsync();

            var chuyenMons = await _context.ChuyenMonNhanVien.AsNoTracking()
                .Select(cm => new { cm.MaNhanVien, cm.TenChuyenMon })
                .ToListAsync();

            var maDung = chuyenMons
                .Where(cm => ChuanHoaNhom(cm.TenChuyenMon) == nhomChuan)
                .Select(cm => cm.MaNhanVien)
                .ToHashSet();

            return nhanViens.Where(nv => maDung.Contains(nv.MaNhanVien)).ToList();
        }

        // Thú cưng có thông tin chăm sóc hiệu lực, loại dịch vụ hoạt động, đúng nhóm.
        private async Task<bool> ThuCungDungNhomAsync(int maThuCung, string nhom)
        {
            var nhomChuan = ChuanHoaNhom(nhom);

            var cacNhom = await (
                from cm in _context.ChuyenMonThuCungs.AsNoTracking()
                join l in _context.LoaiDichVu.AsNoTracking() on cm.MaLoaiDichVu equals l.MaLoaiDichVu
                where cm.MaThuCung == maThuCung
                      && cm.TrangThai == HangSo.TrangThaiChuyenMon.HieuLuc
                      && l.TrangThai
                select l.NhomDichVu).ToListAsync();

            return cacNhom.Any(n => ChuanHoaNhom(n) == nhomChuan);
        }

        private JsonResult KetQuaNhanVien(string lyDo) =>
            Json(new { lyDo, danhSach = new List<object>() });

        // 6. Nạp danh sách lựa chọn cho giao diện
        private async Task NapDanhSachLuaChon()
        {
            ViewBag.NhanViens = await NhanVienHopLe()
                .OrderBy(x => x.HoTen)
                .ToListAsync();

            var hienTai = DateTime.Now;

            // Chỉ hiển thị ca sắp diễn ra, chưa hủy/hoàn thành
            // và chưa có nhân viên được phân công còn hiệu lực.
            ViewBag.CaDichVus = await _context.CaDichVu
                .Where(x =>
                    x.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich
                    && x.ThoiGianBatDau > hienTai
                    && !_context.PhanCongNhanVien.Any(pc =>
                        pc.MaCaDichVu == x.MaCaDichVu
                        && pc.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc))
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