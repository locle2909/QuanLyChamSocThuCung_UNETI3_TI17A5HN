// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Entity ThanhToan (SV5): mỗi dòng là một lần thanh toán chi phí của một lịch hẹn.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

public class ThanhToan
{
    [Key]
    public int MaThanhToan { get; set; }

    [Display(Name = "Lịch hẹn")]
    public int MaLichHen { get; set; }

    [Display(Name = "Ngày thanh toán")]
    public DateTime NgayThanhToan { get; set; } = DateTime.Now;

    [Range(0.01, 1000000000, ErrorMessage = "Số tiền phải lớn hơn 0")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Số tiền")]
    public decimal SoTien { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức")]
    [StringLength(50)]
    [Display(Name = "Phương thức")]
    public string PhuongThuc { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Người ghi nhận")]
    public string? NguoiGhiNhan { get; set; }

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }
}
