// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: ViewModel danh sách chi phí, ghi nhận thanh toán và hóa đơn (SV5).
using System.ComponentModel.DataAnnotations;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

public class ChiPhiLichHenViewModel
{
    public int MaLichHen { get; set; }
    public int MaTaiKhoan { get; set; }
    public string TenKhachHang { get; set; } = string.Empty;
    public int MaDichVu { get; set; }
    public string TenDichVu { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; }
    public string TrangThaiLichHen { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    public decimal TongDaThanhToan { get; set; }

    public decimal SoTienConLai => ChiPhiHelper.TinhConLai(DonGia, TongDaThanhToan);
    public string TrangThaiChiPhi => ChiPhiHelper.LayTrangThai(TongDaThanhToan, DonGia);
}

public class ThanhToanIndexViewModel
{
    public List<ChiPhiLichHenViewModel> Items { get; set; } = new();
    public bool LaAdmin { get; set; }
    public string? TuKhoa { get; set; }
    public string? TrangThaiChiPhi { get; set; }
    public string? SapXep { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public decimal TongConLai { get; set; }
    public int TotalPages => Math.Max((int)Math.Ceiling(TotalItems / (double)PageSize), 1);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;
}

public class ThanhToanCreateViewModel
{
    public int MaLichHen { get; set; }

    // Chỉ để hiển thị, luôn được nạp lại từ CSDL (không tin dữ liệu gửi lên)
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
