using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class KetQuaDichVu
    {
        [Key]
        public int MaKetQuaDichVu { get; set; }

        public int MaCaDichVu { get; set; }

        public int MaKhachHang { get; set; }

        public string TrangThaiKetQuaDichVu { get; set; } = "Chờ thực hiện";

        public DateTime ThoiGianGhiNhan { get; set; } = DateTime.Now;

        public string? GhiChu { get; set; }
    }
}
