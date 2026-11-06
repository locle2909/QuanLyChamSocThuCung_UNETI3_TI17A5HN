using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

public class LichHenFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thú cưng.")]
    [Display(Name = "Thú cưng")]
    public int MaThuCung { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ.")]
    [Display(Name = "Dịch vụ")]
    public int MaDichVu { get; set; }

    [Display(Name = "Thời gian bắt đầu")]
    public DateTime ThoiGianBatDau { get; set; }

    [Display(Name = "Thời gian kết thúc")]
    public DateTime ThoiGianKetThuc { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }
}
