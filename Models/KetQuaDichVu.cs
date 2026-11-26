
using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class KetQuaDichVu
    {
        [Key]
        public int MaKetQuaDichVu { get; set; }

        public int MaCaDichVu { get; set; }

        public int MaKhachHang { get; set; }

        // Chờ thực hiện, Đang thực hiện,
        // Chờ khách xác nhận, Hoàn thành
        public string TrangThaiKetQuaDichVu { get; set; }
            = "Chờ thực hiện";

        public DateTime ThoiGianGhiNhan { get; set; }
            = DateTime.Now;

        public string? GhiChu { get; set; }

        // Chỉ Admin và nhân viên được xem
        public string? GhiChuNoiBo { get; set; }

        public int? MaNguoiCapNhat { get; set; }

        // Thời điểm nhân viên đề xuất hoàn thành
        public DateTime? ThoiGianDeXuatHoanThanh { get; set; }

        // Thời điểm khách hàng xác nhận hoàn thành
        public DateTime? ThoiGianKhachXacNhanHoanThanh { get; set; }
    }
}
