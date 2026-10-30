namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

public class LichHenIndexItemViewModel
{
    public int MaLichHen { get; set; }
    public string TenKhachHang { get; set; } = string.Empty;
    public string TenThuCung { get; set; } = string.Empty;
    public string TenDichVu { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
    public bool CoTheHuy { get; set; }
    public bool CoTheXacNhan { get; set; }
    public bool CoTheHoanThanh { get; set; }
}
