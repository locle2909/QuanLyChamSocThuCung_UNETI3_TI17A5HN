
using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class CaDichVu
    {
        [Key]
        public int MaCaDichVu { get; set; }

        public int MaDichVu { get; set; }

        public int MaThuCung { get; set; }

        public DateTime ThoiGianBatDau { get; set; }

        public DateTime ThoiGianKetThuc { get; set; }

        public string? PhongHocHoacHinhThuc { get; set; }

        public string? GhiChu { get; set; }

        public string TrangThai { get; set; } = "Đã lên lịch";
    }
}

