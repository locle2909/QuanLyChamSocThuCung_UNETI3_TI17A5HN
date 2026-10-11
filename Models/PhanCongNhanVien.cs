using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class PhanCongNhanVien
    {
        [Key]
        public int MaPhanCong { get; set; }

        public int MaNhanVien { get; set; }

        public int MaCaDichVu { get; set; }

        public DateTime NgayPhanCong { get; set; } = DateTime.Now;

        public string TrangThai { get; set; } = "Đã phân công";

        public string? GhiChu { get; set; }
    }
}