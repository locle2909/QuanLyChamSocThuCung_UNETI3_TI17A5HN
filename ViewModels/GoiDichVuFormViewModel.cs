using System.ComponentModel.DataAnnotations;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

public class GoiDichVuFormViewModel : IValidatableObject
{
    public int MaGoi { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên gói dịch vụ")]
    [StringLength(150)]
    [Display(Name = "Tên gói")]
    public string TenGoi { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá gói không hợp lệ")]
    [Display(Name = "Giá gói")]
    public decimal GiaGoi { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá khuyến mãi không hợp lệ")]
    [Display(Name = "Giá khuyến mãi")]
    public decimal? GiaKhuyenMai { get; set; }

    [Required]
    [Display(Name = "Trạng thái")]
    public string TrangThai { get; set; } = HangSo.TrangThaiDichVu.DangMoDangKy;

    public List<GoiDichVuServiceItemViewModel> DichVus { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GiaKhuyenMai.HasValue && GiaKhuyenMai.Value >= GiaGoi)
            yield return new ValidationResult("Giá khuyến mãi phải nhỏ hơn giá gói", [nameof(GiaKhuyenMai)]);

        if (!new[]
            {
                HangSo.TrangThaiDichVu.DangMoDangKy,
                HangSo.TrangThaiDichVu.DaDu,
                HangSo.TrangThaiDichVu.DangPhucVu,
                HangSo.TrangThaiDichVu.TamNgung,
                HangSo.TrangThaiDichVu.NgungPhucVu
            }.Contains(TrangThai))
            yield return new ValidationResult("Trạng thái gói dịch vụ không hợp lệ", [nameof(TrangThai)]);

        if (!DichVus.Any(dichVu => dichVu.Chon))
            yield return new ValidationResult("Vui lòng chọn ít nhất một dịch vụ cho gói", [nameof(DichVus)]);
    }
}

public class GoiDichVuServiceItemViewModel
{
    public int MaDichVu { get; set; }
    public string TenDichVu { get; set; } = string.Empty;
    public decimal Gia { get; set; }
    public bool Chon { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int SoLuong { get; set; } = 1;
}