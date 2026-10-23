// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Entity LoaiDichVu.
using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

public class LoaiDichVu
{
    [Key]
    public int MaLoaiDichVu { get; set; }

    [Required(ErrorMessage = "Tên loại dịch vụ là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên loại dịch vụ tối đa 100 ký tự")]
    [Display(Name = "Tên loại dịch vụ")]
    public string TenLoaiDichVu { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Required(ErrorMessage = "Nhóm dịch vụ là bắt buộc")]
    [StringLength(50)]
    [Display(Name = "Nhóm dịch vụ")]
    public string NhomDichVu { get; set; } = string.Empty;

    [Display(Name = "Hoạt động")]
    public bool TrangThai { get; set; } = true;

    // Bỏ comment khi nhánh SV2, SV3 đã gộp:
    // public ICollection<DichVu> DichVus { get; set; } = new List<DichVu>();
    // public ICollection<ChuyenMonThuCung> ChuyenMons { get; set; } = new List<ChuyenMonThuCung>();
}