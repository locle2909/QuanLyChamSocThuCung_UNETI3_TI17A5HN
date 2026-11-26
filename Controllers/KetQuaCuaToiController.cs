using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Filters;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    // Khách hàng xem kết quả của chính mình, xác nhận hoàn thành hoặc yêu cầu làm lại
    [PhanQuyen(HangSo.VaiTro.KhachHang)]
    public class KetQuaCuaToiController : Controller
    {
        private readonly AppDbContext _context;

        public KetQuaCuaToiController(AppDbContext context)
        {
            _context = context;
        }

        // Tài khoản khách hàng bị khóa hoặc đổi vai trò thì mất quyền ngay
        private Task<bool> TaiKhoanHopLe(int maTk)
        {
            return _context.TaiKhoan.AnyAsync(t =>
                t.MaTaiKhoan == maTk
                && t.TrangThai
                && t.VaiTro == HangSo.VaiTro.KhachHang);
        }

        private static string GhiThem(string? cu, string them)
        {
            return string.IsNullOrWhiteSpace(cu) ? them : cu + "\n" + them;
        }

        // ===== Danh sách kết quả của khách hàng =====

        public async Task<IActionResult> Index()
        {
            var maTk = HttpContext.LayMaTaiKhoan();

            if (!await TaiKhoanHopLe(maTk))
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");

            var rows = await (
                from kq in _context.KetQuaDichVu.AsNoTracking()
                join ca in _context.CaDichVu.AsNoTracking() on kq.MaCaDichVu equals ca.MaCaDichVu
                where kq.MaKhachHang == maTk
                orderby ca.ThoiGianBatDau descending
                select new { kq, ca }).ToListAsync();

            var tenDv = await _context.DichVus.AsNoTracking()
                .ToDictionaryAsync(x => x.MaDichVu, x => x.TenDichVu);
            var tenTc = await _context.ThuCungs.AsNoTracking()
                .ToDictionaryAsync(x => x.MaThuCung, x => x.HoTen);

            // Chỉ trả về ghi chú dành cho khách, không có ghi chú nội bộ
            var ds = rows.Select(x => new KetQuaHienThi
            {
                MaKetQuaDichVu = x.kq.MaKetQuaDichVu,
                MaCaDichVu = x.ca.MaCaDichVu,
                MaKhachHang = x.kq.MaKhachHang,
                TenDichVu = tenDv.GetValueOrDefault(x.ca.MaDichVu, "#" + x.ca.MaDichVu),
                TenThuCung = tenTc.GetValueOrDefault(x.ca.MaThuCung, "#" + x.ca.MaThuCung),
                ThoiGianBatDau = x.ca.ThoiGianBatDau,
                TrangThaiCa = x.ca.TrangThai,
                TrangThai = x.kq.TrangThaiKetQuaDichVu,
                ThoiGianGhiNhan = x.kq.ThoiGianGhiNhan,
                GhiChu = x.kq.GhiChu,
                ThoiGianDeXuatHoanThanh = x.kq.ThoiGianDeXuatHoanThanh,
                ThoiGianKhachXacNhanHoanThanh = x.kq.ThoiGianKhachXacNhanHoanThanh,
                ChoKhachXacNhan = x.ca.TrangThai == HangSo.TrangThaiCaDichVu.DaLenLich
                                  && x.kq.TrangThaiKetQuaDichVu == HangSo.TrangThaiKetQua.ChoKhachXacNhan
            }).ToList();

            return View(ds);
        }

        // ===== Khách hàng xác nhận hoàn thành =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XacNhanHoanThanh(int id)
        {
            var maTk = HttpContext.LayMaTaiKhoan();

            if (!await TaiKhoanHopLe(maTk))
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");

            // Chỉ lấy kết quả thuộc khách hàng đang đăng nhập
            var kq = await _context.KetQuaDichVu
                .FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id && x.MaKhachHang == maTk);
            if (kq == null)
                return NotFound();

            var ca = await _context.CaDichVu.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);
            if (ca == null)
                return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
            {
                TempData["Loi"] = "Ca dịch vụ đã hủy hoặc đã hoàn thành, không thể xác nhận.";
                return RedirectToAction(nameof(Index));
            }

            if (kq.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.ChoKhachXacNhan
                || kq.ThoiGianDeXuatHoanThanh == null)
            {
                TempData["Loi"] = "Kết quả này chưa được nhân viên đề xuất hoàn thành.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bayGio = DateTime.Now;

                kq.TrangThaiKetQuaDichVu = HangSo.TrangThaiKetQua.HoanThanh;
                kq.ThoiGianKhachXacNhanHoanThanh = bayGio;
                kq.ThoiGianGhiNhan = bayGio;
                kq.MaNguoiCapNhat = maTk;

                _context.NhatKyKetQuaDichVu.Add(new NhatKyKetQuaDichVu
                {
                    MaKetQuaDichVu = kq.MaKetQuaDichVu,
                    MaNguoiSua = maTk,
                    ThoiGian = bayGio,
                    NoiDung = "Khách hàng xác nhận hoàn thành dịch vụ."
                });

                await _context.SaveChangesAsync();

                // Chỉ hoàn thành ca (và lịch hẹn) khi mọi kết quả của ca đã được khách xác nhận
                bool caHoanThanh = await HoanThanhCaHelper.HoanThanhCaNeuDuDieuKien(_context, ca.MaCaDichVu);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["ThongBao"] = caHoanThanh
                    ? "Bạn đã xác nhận hoàn thành. Ca dịch vụ đã chính thức hoàn thành."
                    : "Bạn đã xác nhận hoàn thành kết quả dịch vụ.";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Loi"] = "Có lỗi xảy ra khi xác nhận. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== Khách hàng chưa hài lòng, yêu cầu làm lại =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeuCauLamLai(int id, string? lyDo)
        {
            var maTk = HttpContext.LayMaTaiKhoan();

            if (!await TaiKhoanHopLe(maTk))
                return RedirectToAction("KhongCoQuyen", "TaiKhoan");

            var kq = await _context.KetQuaDichVu
                .FirstOrDefaultAsync(x => x.MaKetQuaDichVu == id && x.MaKhachHang == maTk);
            if (kq == null)
                return NotFound();

            var ca = await _context.CaDichVu.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaCaDichVu == kq.MaCaDichVu);
            if (ca == null)
                return NotFound();

            if (ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
            {
                TempData["Loi"] = "Ca dịch vụ đã hủy hoặc đã hoàn thành.";
                return RedirectToAction(nameof(Index));
            }

            if (kq.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.ChoKhachXacNhan)
            {
                TempData["Loi"] = "Kết quả này không ở trạng thái chờ bạn xác nhận.";
                return RedirectToAction(nameof(Index));
            }

            lyDo = lyDo?.Trim();
            if (string.IsNullOrEmpty(lyDo))
            {
                TempData["Loi"] = "Vui lòng cho biết lý do bạn chưa hài lòng.";
                return RedirectToAction(nameof(Index));
            }

            if (lyDo.Length > 500)
            {
                TempData["Loi"] = "Lý do tối đa 500 ký tự.";
                return RedirectToAction(nameof(Index));
            }

            var bayGio = DateTime.Now;

            // Trả về cho nhân viên làm lại; nhân viên thấy lý do trong ghi chú nội bộ
            kq.TrangThaiKetQuaDichVu = HangSo.TrangThaiKetQua.DangThucHien;
            kq.ThoiGianDeXuatHoanThanh = null;
            kq.ThoiGianGhiNhan = bayGio;
            kq.MaNguoiCapNhat = maTk;
            kq.GhiChuNoiBo = GhiThem(kq.GhiChuNoiBo,
                $"[{bayGio:dd/MM/yyyy HH:mm}] Khách hàng yêu cầu làm lại: {lyDo}");

            _context.NhatKyKetQuaDichVu.Add(new NhatKyKetQuaDichVu
            {
                MaKetQuaDichVu = kq.MaKetQuaDichVu,
                MaNguoiSua = maTk,
                ThoiGian = bayGio,
                NoiDung = "Khách hàng chưa hài lòng, yêu cầu làm lại. Lý do: " + lyDo
            });

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã gửi yêu cầu làm lại cho nhân viên.";
            return RedirectToAction(nameof(Index));
        }
    }
}