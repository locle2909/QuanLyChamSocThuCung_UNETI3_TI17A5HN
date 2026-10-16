using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class ChiTietGoiDichVu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaGoi { get; set; }

        [ForeignKey("MaGoi")]
        public virtual GoiDichVu? GoiDichVu { get; set; }

        [Required]
        public int MaDichVu { get; set; }

        [ForeignKey("MaDichVu")]
        public virtual DichVu? DichVu { get; set; }

        [Display(Name = "Số lượng")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; } = 1;
    }
}
//Số lượng của dịch vụ đó trong gói (mặc định là 1 lần).
// Quan hệ N-N giữa GoiDichVu và DichVu thông qua ChiTietGoiDichVu