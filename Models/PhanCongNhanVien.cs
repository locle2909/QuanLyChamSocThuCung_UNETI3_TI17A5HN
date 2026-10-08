using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class PhanCongNhanVien
    {
        [Key]
        public int MaPhanCong { get; set; }

        public int MaDichVu { get; set; }

        public int MaThuCung { get; set; }

        public DateTime NgayPhanCong { get; set; }

        public string TrangThai { get; set; } = "Đã phân công";

        public string? GhiChu { get; set; }
    }
}