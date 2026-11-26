using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    // Lịch sử sửa kết quả dịch vụ (ai sửa, lúc nào, sửa gì)
    public class NhatKyKetQuaDichVu
    {
        [Key]
        public int MaNhatKy { get; set; }

        public int MaKetQuaDichVu { get; set; }

        public int MaNguoiSua { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [StringLength(1000)]
        public string NoiDung { get; set; } = string.Empty;
    }
}
