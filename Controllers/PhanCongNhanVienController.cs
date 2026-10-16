
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class PhanCongNhanVienController : Controller
    {
        private readonly AppDbContext _context;

        public PhanCongNhanVienController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var danhSach = await (
                from pc in _context.PhanCongNhanVien
                join nv in _context.NhanVien
                    on pc.MaNhanVien equals nv.MaNhanVien
                join ca in _context.CaDichVu
                    on pc.MaCaDichVu equals ca.MaCaDichVu
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
                }).ToListAsync();

            return View(danhSach);
        }

        public async Task<IActionResult> Create()
        {
            await NapDanhSachLuaChon();
            return View(new PhanCongNhanVien());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhanCongNhanVien pc)
        {
            // Các trường này được lấy từ ca dịch vụ, không nhập độc lập.
            ModelState.Remove(nameof(pc.MaCaDichVu));
            ModelState.Remove(nameof(pc.NgayPhanCong));
            ModelState.Remove(nameof(pc.TrangThai));

            var nhanVien = await _context.NhanVien
                .FirstOrDefaultAsync(x =>
                    x.MaNhanVien == pc.MaNhanVien && x.TrangThai);

            if (nhanVien == null)
                ModelState.AddModelError(
                    nameof(pc.MaNhanVien),
                    "Nhân viên không tồn tại hoặc đã ngừng hoạt động.");

            var ca = await _context.CaDichVu
                .FirstOrDefaultAsync(x => x.MaCaDichVu == pc.MaCaDichVu);

            if (ca == null)
            {
                ModelState.AddModelError(
                    nameof(pc.MaCaDichVu), "Ca dịch vụ không tồn tại.");
            }
            else if (ca.TrangThai == "Đã hủy" ||
                     ca.TrangThai == "Hoàn thành")
            {
                ModelState.AddModelError(
                    nameof(pc.MaCaDichVu),
                    "Không thể phân công vào ca đã hủy hoặc hoàn thành.");
            }

            if (nhanVien != null && ca != null &&
                ca.ThoiGianKetThuc > ca.ThoiGianBatDau)
            {
                // Hai khoảng thời gian giao nhau khi:
                // Ca cũ bắt đầu trước khi ca mới kết thúc
                // và ca cũ kết thúc sau khi ca mới bắt đầu.
                var biTrungLich =
                    await (from pcCu in _context.PhanCongNhanVien
                           join caCu in _context.CaDichVu
                               on pcCu.MaCaDichVu equals caCu.MaCaDichVu
                           where pcCu.MaNhanVien == pc.MaNhanVien
                                 && pcCu.TrangThai != "Đã hủy"
                                 && caCu.TrangThai != "Đã hủy"
                                 && caCu.TrangThai != "Hoàn thành"
                                 && caCu.ThoiGianBatDau < ca.ThoiGianKetThuc
                                 && caCu.ThoiGianKetThuc > ca.ThoiGianBatDau
                           select pcCu.MaPhanCong).AnyAsync();

                if (biTrungLich)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Nhân viên đã có ca bị trùng thời gian. Hãy chọn nhân viên hoặc ca khác.");
                }
            }

            if (!ModelState.IsValid)
            {
                await NapDanhSachLuaChon();
                return View(pc);
            }

            pc.MaCaDichVu = ca!.MaCaDichVu;
            pc.NgayPhanCong = DateTime.Now;
            pc.TrangThai = "Đã phân công";

            _context.PhanCongNhanVien.Add(pc);
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Phân công nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id)
        {
            var pc = await _context.PhanCongNhanVien.FindAsync(id);

            if (pc == null)
                return NotFound();

            if (pc.TrangThai == "Hoàn thành")
            {
                TempData["ThongBao"] =
                    "Không thể hủy phân công đã hoàn thành.";
                return RedirectToAction(nameof(Index));
            }

            pc.TrangThai = "Đã hủy";
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã hủy phân công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task NapDanhSachLuaChon()
        {
            ViewBag.NhanViens = await _context.NhanVien
                .Where(x => x.TrangThai)
                .OrderBy(x => x.HoTen)
                .ToListAsync();

            ViewBag.CaDichVus = await _context.CaDichVu
                .Where(x => x.TrangThai != "Đã hủy"
                         && x.TrangThai != "Hoàn thành")
                .OrderBy(x => x.ThoiGianBatDau)
                .ToListAsync();
        }
    }

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