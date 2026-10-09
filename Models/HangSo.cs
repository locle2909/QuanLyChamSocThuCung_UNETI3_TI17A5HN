// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Phần chung nhóm trưởng: hằng số vai trò, khóa Session, trạng thái nghiệp vụ.
namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

public static class HangSo
{
    public static class VaiTro
    {
        public const string Admin = "Admin";
        public const string NhanVien = "NhanVien";
        public const string KhachHang = "KhachHang";

        public static readonly IReadOnlyDictionary<string, string> TenHienThi = new Dictionary<string, string>
        {
            [Admin] = "Admin/Quản lý",
            [NhanVien] = "Nhân viên",
            [KhachHang] = "Khách hàng"
        };

        public static string Ten(string? vaiTro) =>
            vaiTro != null && TenHienThi.TryGetValue(vaiTro, out var ten) ? ten : (vaiTro ?? "");
    }

    public static class SessionKey
    {
        public const string MaTaiKhoan = "MaTaiKhoan";
        public const string HoTen = "HoTen";
        public const string VaiTro = "VaiTro";
    }

    public static class NhomDichVu
    {
        public static readonly string[] DanhSach =
            { "Vệ sinh - Làm đẹp", "Sức khỏe", "Huấn luyện", "Lưu trú" };
    }

    // Theo mục 7.2, 25.4 của đề
    public static class TrangThaiDichVu
    {
        public const string DangMoDangKy = "Đang mở đăng ký";
        public const string DaDu = "Đã đủ";
        public const string DangPhucVu = "Đang phục vụ";
        public const string TamNgung = "Tạm ngừng";
        public const string NgungPhucVu = "Ngừng phục vụ";
    }

    // Theo mục 7.4
    public static class TrangThaiLichHen
    {
        public const string ChoXacNhan = "Chờ xác nhận";
        public const string DaXacNhan = "Đã xác nhận";
        public const string DaHuy = "Đã hủy";
        public const string DaHoanThanh = "Đã hoàn thành";
    }

    // Theo mục 8.3
    public static class TrangThaiCaDichVu
    {
        public const string DaLenLich = "Đã lên lịch";
        public const string DaHoanThanh = "Đã hoàn thành";
        public const string DaHuy = "Đã hủy";
    }

    // Theo mục 8.6
    public static class TrangThaiKetQua
    {
        public const string ChoThucHien = "Chờ thực hiện";
        public const string DangThucHien = "Đang thực hiện";
        public const string HoanThanh = "Hoàn thành";
    }
}
