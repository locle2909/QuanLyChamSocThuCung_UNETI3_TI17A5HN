using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class GoiDichVu : IValidatableObject
    {
        [Key]
        public int MaGoi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên gói dịch vụ")]
        [StringLength(150)]
        [Display(Name = "Tên gói")]
        public string TenGoi { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        [StringLength(1000)]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá gói")]
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá gói không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá gói")]
        public decimal GiaGoi { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá khuyến mãi không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá khuyến mãi")]
        public decimal? GiaKhuyenMai { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = HangSo.TrangThaiDichVu.DangMoDangKy;

        // Quan hệ 1-N đến bảng trung gian
        public virtual ICollection<ChiTietGoiDichVu> ChiTietGoiDichVus { get; set; } = new List<ChiTietGoiDichVu>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (GiaKhuyenMai.HasValue && GiaKhuyenMai.Value >= GiaGoi)
                yield return new ValidationResult("Giá khuyến mãi phải nhỏ hơn giá gói", new[] { nameof(GiaKhuyenMai) });

            if (!new[]
                {
                    HangSo.TrangThaiDichVu.DangMoDangKy,
                    HangSo.TrangThaiDichVu.DaDu,
                    HangSo.TrangThaiDichVu.DangPhucVu,
                    HangSo.TrangThaiDichVu.TamNgung,
                    HangSo.TrangThaiDichVu.NgungPhucVu
                }.Contains(TrangThai))
                yield return new ValidationResult("Trạng thái gói dịch vụ không hợp lệ", new[] { nameof(TrangThai) });
        }
    }
}