using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class LoaiDichVu
    {
        [Key]
        public int MaLoaiDichVu { get; set; }

        [Required(ErrorMessage = "Tên loại dịch vụ bắt buộc nhập")]
        [StringLength(100)]
        public string TenLoaiDichVu { get; set; } = "";

        public bool TrangThai { get; set; } = true;

        public ICollection<ChuyenMonThuCung> ChuyenMons { get; set; } = new List<ChuyenMonThuCung>();
    }
}
