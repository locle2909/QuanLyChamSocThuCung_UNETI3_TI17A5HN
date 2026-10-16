
using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class ChuyenMonNhanVien
    {
        [Key]
        public int MaChuyenMon { get; set; }

        public int MaNhanVien { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chuyên môn")]
        [StringLength(100)]
        public string TenChuyenMon { get; set; } = string.Empty;

        public string? GhiChu { get; set; }
    }
}