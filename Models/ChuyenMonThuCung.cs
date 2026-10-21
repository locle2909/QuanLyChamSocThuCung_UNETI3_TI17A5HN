using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class ChuyenMonThuCung
    {
        [Key]
        public int MaChuyenMon { get; set; }

        [Required]
        public int MaThuCung { get; set; }

        [Required]
        public int MaLoaiDichVu { get; set; }

        [Required(ErrorMessage = "Giống/loài chuyên môn bắt buộc nhập")]
        [StringLength(80)]
        public string GiongLoaiChuyenMon { get; set; } = "";

        [Range(0, 100, ErrorMessage = "Tuổi môn phải >= 0")]
        public int TuoiMon { get; set; }

        [StringLength(500)]
        public string? GhiChu { get; set; }

        [Required]
        [StringLength(30)]
        public string TrangThai { get; set; } = "Hiệu lực";

        [ForeignKey(nameof(MaThuCung))]
        public ThuCung? ThuCung { get; set; }

        [ForeignKey(nameof(MaLoaiDichVu))]
        public LoaiDichVu? LoaiDichVu { get; set; }
    }
}
