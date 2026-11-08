// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Entity lưu yêu cầu đặt lại mật khẩu (mã đã băm, hạn dùng, dùng một lần).
using System.ComponentModel.DataAnnotations;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

public class YeuCauDatLaiMatKhau
{
    [Key]
    public int MaYeuCau { get; set; }

    public int MaTaiKhoan { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }

    // SHA-256 của mã gửi trong email, 64 ký tự hex. Không lưu mã gốc.
    [Required, StringLength(64)]
    public string MaHash { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.Now;

    public DateTime HetHan { get; set; }

    public bool DaDung { get; set; }
}