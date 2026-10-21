// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Dữ liệu mẫu cho TaiKhoan và LoaiDichVu (SV1).
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data.Seed;

public static class Module1Seed
{
    public static void Seed(AppDbContext db)
    {
        // Chỉ thêm khi bảng còn trống, chạy nhiều lần không bị trùng.
        if (!db.TaiKhoan.Any())
        {
            db.TaiKhoan.AddRange(
                new TaiKhoan
                {
                    TenDangNhap = "admin",
                    MatKhau = PasswordHasher.Hash("Admin@123"),
                    HoTen = "Quản trị viên",
                    Email = "admin@petcare.vn",
                    VaiTro = HangSo.VaiTro.Admin,
                    TrangThai = true
                },
                new TaiKhoan
                {
                    TenDangNhap = "nhanvien1",
                    MatKhau = PasswordHasher.Hash("NhanVien@123"),
                    HoTen = "Nhân viên mẫu",
                    Email = "nhanvien1@petcare.vn",
                    VaiTro = HangSo.VaiTro.NhanVien,
                    TrangThai = true
                },
                new TaiKhoan
                {
                    TenDangNhap = "khachhang1",
                    MatKhau = PasswordHasher.Hash("KhachHang@123"),
                    HoTen = "Khách hàng mẫu",
                    Email = "khachhang1@petcare.vn",
                    VaiTro = HangSo.VaiTro.KhachHang,
                    TrangThai = true
                });
            db.SaveChanges();
        }

        if (!db.LoaiDichVu.Any())
        {
            db.LoaiDichVu.AddRange(
                new LoaiDichVu { TenLoaiDichVu = "Tắm gội", NhomDichVu = "Vệ sinh - Làm đẹp", MoTa = "Tắm, sấy, chải lông" },
                new LoaiDichVu { TenLoaiDichVu = "Cắt tỉa lông", NhomDichVu = "Vệ sinh - Làm đẹp", MoTa = "Cắt tỉa, tạo kiểu lông" },
                new LoaiDichVu { TenLoaiDichVu = "Khám tổng quát", NhomDichVu = "Sức khỏe", MoTa = "Khám sức khỏe định kỳ" },
                new LoaiDichVu { TenLoaiDichVu = "Tiêm phòng", NhomDichVu = "Sức khỏe", MoTa = "Tiêm vắc xin theo lịch" },
                new LoaiDichVu { TenLoaiDichVu = "Huấn luyện cơ bản", NhomDichVu = "Huấn luyện", MoTa = "Dạy các lệnh cơ bản" },
                new LoaiDichVu { TenLoaiDichVu = "Gửi thú cưng theo ngày", NhomDichVu = "Lưu trú", MoTa = "Trông giữ thú cưng theo ngày" });
            db.SaveChanges();
        }
    }
}