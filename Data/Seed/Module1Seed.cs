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
        // Thêm từng bản ghi theo tên: chạy nhiều lần không bị trùng.
        ThemTaiKhoan(db, "admin", "Admin@123", "Quản trị viên", "admin@petcare.vn", HangSo.VaiTro.Admin);
        ThemTaiKhoan(db, "quanly", "Admin@123", "Quản lý trung tâm", "quanly@petcare.vn", HangSo.VaiTro.Admin);

        for (int i = 1; i <= 5; i++)
            ThemTaiKhoan(db, "nhanvien" + i, "NhanVien@123", "Nhân viên " + i,
                         $"nhanvien{i}@petcare.vn", HangSo.VaiTro.NhanVien);

        for (int i = 1; i <= 30; i++)
            ThemTaiKhoan(db, "khachhang" + i, "KhachHang@123", "Khách hàng " + i,
                         $"khachhang{i}@petcare.vn", HangSo.VaiTro.KhachHang);
        db.SaveChanges();

        ThemLoai(db, "Tắm gội", "Vệ sinh - Làm đẹp", "Tắm, sấy, chải lông");
        ThemLoai(db, "Cắt tỉa lông", "Vệ sinh - Làm đẹp", "Cắt tỉa, tạo kiểu lông");
        ThemLoai(db, "Spa thú cưng", "Vệ sinh - Làm đẹp", "Massage, dưỡng lông, chăm sóc da");
        ThemLoai(db, "Khám tổng quát", "Sức khỏe", "Khám sức khỏe định kỳ");
        ThemLoai(db, "Tiêm phòng", "Sức khỏe", "Tiêm vắc xin theo lịch");
        ThemLoai(db, "Chăm sóc răng miệng", "Sức khỏe", "Lấy cao răng, kiểm tra răng miệng");
        ThemLoai(db, "Huấn luyện cơ bản", "Huấn luyện", "Dạy các lệnh cơ bản");
        ThemLoai(db, "Huấn luyện nâng cao", "Huấn luyện", "Rèn kỷ luật và kỹ năng nâng cao");
        ThemLoai(db, "Gửi thú cưng theo ngày", "Lưu trú", "Trông giữ thú cưng theo ngày");
        ThemLoai(db, "Lưu trú dài ngày", "Lưu trú", "Trông giữ thú cưng nhiều ngày");
        db.SaveChanges();
    }

    private static void ThemTaiKhoan(AppDbContext db, string ten, string matKhau, string hoTen, string email, string vaiTro)
    {
        if (db.TaiKhoan.Any(t => t.TenDangNhap == ten)) return;
        db.TaiKhoan.Add(new TaiKhoan
        {
            TenDangNhap = ten,
            MatKhau = PasswordHasher.Hash(matKhau),
            HoTen = hoTen,
            Email = email,
            VaiTro = vaiTro,
            TrangThai = true
        });
    }

    private static void ThemLoai(AppDbContext db, string ten, string nhom, string moTa)
    {
        if (db.LoaiDichVu.Any(l => l.TenLoaiDichVu == ten)) return;
        db.LoaiDichVu.Add(new LoaiDichVu { TenLoaiDichVu = ten, NhomDichVu = nhom, MoTa = moTa, TrangThai = true });
    }
}