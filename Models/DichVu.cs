using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class DichVu : IValidatableObject
    {
        [Key]
        public int MaDichVu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
        [StringLength(150)]
        [Display(Name = "Tên dịch vụ")]
        public string TenDichVu { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        [StringLength(1000)]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá dịch vụ")]
        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá dịch vụ không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá")]
        public decimal Gia { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhóm dịch vụ")]
        [StringLength(100)]
        [Display(Name = "Nhóm dịch vụ")]
        public string NhomDichVu { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = HangSo.TrangThaiDichVu.DangMoDangKy;

        // Quan hệ 1-N (Một dịch vụ có thể xuất hiện trong nhiều chi tiết gói)
        public virtual ICollection<ChiTietGoiDichVu> ChiTietGoiDichVus { get; set; } = new List<ChiTietGoiDichVu>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!HangSo.NhomDichVu.DanhSach.Contains(NhomDichVu))
                yield return new ValidationResult("Nhóm dịch vụ không hợp lệ", new[] { nameof(NhomDichVu) });

            if (!new[]
                {
                    HangSo.TrangThaiDichVu.DangMoDangKy,
                    HangSo.TrangThaiDichVu.DaDu,
                    HangSo.TrangThaiDichVu.DangPhucVu,
                    HangSo.TrangThaiDichVu.TamNgung,
                    HangSo.TrangThaiDichVu.NgungPhucVu
                }.Contains(TrangThai))
                yield return new ValidationResult("Trạng thái dịch vụ không hợp lệ", new[] { nameof(TrangThai) });
        }
    }
}