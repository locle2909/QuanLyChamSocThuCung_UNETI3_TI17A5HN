using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models
{
    public class ThuCung
    {
        [Key]
        public int MaThuCung { get; set; }

        [Required(ErrorMessage = "Mã tài khoản bắt buộc nhập")]
        [StringLength(50)]
        public string MaTaiKhoan { get; set; } = "";

        [Required(ErrorMessage = "Họ tên thú cưng bắt buộc nhập")]
        [StringLength(100)]
        public string HoTen { get; set; } = "";

        [DataType(DataType.Date)]
        [CustomValidation(typeof(ThuCung), nameof(ValidateNgaySinh))]
        public DateTime NgaySinh { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [StringLength(20)]
        public string GioiTinh { get; set; } = "Đực";

        [Required(ErrorMessage = "Số điện thoại bắt buộc nhập")]
        [RegularExpression(@"^(0\d{9}|\+84\d{9})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string SoDienThoai { get; set; } = "";

        [Required(ErrorMessage = "Email bắt buộc nhập")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [StringLength(250)]
        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Giống/loài bắt buộc nhập")]
        [StringLength(80)]
        public string GiongLoai { get; set; } = "";

        [Range(0, 100, ErrorMessage = "Tuổi phải >= 0")]
        public int Tuoi { get; set; }

        [StringLength(500)]
        public string? GhiChuSucKhoe { get; set; }

        [Required(ErrorMessage = "Trạng thái bắt buộc chọn")]
        [StringLength(30)]
        public string TrangThai { get; set; } = "Hoạt động";

        public ICollection<ChuyenMonThuCung> ChuyenMons { get; set; } = new List<ChuyenMonThuCung>();

        public static ValidationResult? ValidateNgaySinh(DateTime value, ValidationContext context)
        {
            if (value.Date > DateTime.Today)
                return new ValidationResult("Ngày sinh phải nhỏ hơn hoặc bằng ngày hiện tại");
            return ValidationResult.Success;
        }
    }
}
