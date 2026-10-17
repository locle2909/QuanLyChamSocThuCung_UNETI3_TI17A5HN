// Họ và tên: Vũ Duy Cường
// Mã sinh viên: 23103100305
// Nội dung thực hiện: ViewModel cho danh sách chi phí, ghi nhận thanh toán và hóa đơn.
using System.ComponentModel.DataAnnotations;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels
{
    public class ChiPhiLichHenViewModel
    {
        public int MaLichHen { get; set; }
        public int MaKhachHang { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public DateTime NgayDat { get; set; }
        public string TrangThaiLichHen { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public decimal TongDaThanhToan { get; set; }

        public decimal SoTienConLai => ChiPhiHelper.TinhConLai(DonGia, TongDaThanhToan);
        public string TrangThaiChiPhi => ChiPhiHelper.LayTrangThai(TongDaThanhToan, DonGia);
    }

    public class ThanhToanIndexViewModel
    {
        public List<ChiPhiLichHenViewModel> Items { get; set; } = new();
        public string? TuKhoa { get; set; }
        public string? TrangThaiChiPhi { get; set; }
        public string? SapXep { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public bool HasPrevious => PageIndex > 1;
        public bool HasNext => PageIndex < TotalPages;
    }

    public class ThanhToanCreateViewModel
    {
        public int MaLichHen { get; set; }

        // Chỉ để hiển thị (không do người dùng nhập)
        public string TenKhachHang { get; set; } = string.Empty;
        public string TenDichVu { get; set; } = string.Empty;
        public string TrangThaiLichHen { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public decimal TongDaThanhToan { get; set; }
        public decimal SoTienConLai => ChiPhiHelper.TinhConLai(DonGia, TongDaThanhToan);

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "Số tiền phải lớn hơn 0")]
        [Display(Name = "Số tiền thanh toán")]
        public decimal? SoTien { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức")]
        public string PhuongThuc { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }
    }

    public class HoaDonViewModel
    {
        public ChiPhiLichHenViewModel ThongTin { get; set; } = new();
        public List<ThanhToan> LichSu { get; set; } = new();
        public bool LaAdmin { get; set; }
    }
}
