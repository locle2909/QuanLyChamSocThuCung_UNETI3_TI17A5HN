// Họ và tên: Vũ Duy Cường
// Mã sinh viên: 23103100305
// Nội dung thực hiện: Entity ThanhToan (thanh toán chi phí dịch vụ theo lịch hẹn).
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        [Display(Name = "Lịch hẹn")]
        public int MaLichHen { get; set; }

        [Display(Name = "Ngày thanh toán")]
        [DataType(DataType.DateTime)]
        public DateTime NgayThanhToan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
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

        [ForeignKey(nameof(MaLichHen))]
        public virtual LichHen? LichHen { get; set; }
    }
}
