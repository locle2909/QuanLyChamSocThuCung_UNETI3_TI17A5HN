
using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class NhanVien
    {
        [Key]
        public int MaNhanVien { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        public bool TrangThai { get; set; } = true;

        [Display(Name = "Tài khoản đăng nhập")]
        public int? MaTaiKhoan { get; set; }
    }
}