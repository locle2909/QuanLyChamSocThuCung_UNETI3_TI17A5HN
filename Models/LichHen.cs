using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

public class LichHen
{
    [Key]
    public int MaLichHen { get; set; }

    public int MaTaiKhoan { get; set; }

    public int MaThuCung { get; set; }

    public int MaDichVu { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    [Required]
    [StringLength(30)]
    public string TrangThai { get; set; } = HangSo.TrangThaiLichHen.ChoXacNhan;

    [StringLength(500)]
    public string? GhiChu { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;
}
