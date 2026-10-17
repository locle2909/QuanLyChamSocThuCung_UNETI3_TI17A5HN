// Họ và tên: Vũ Duy Cường
// Mã sinh viên: 23103100305
// Nội dung thực hiện: Hằng số trạng thái dùng chung và hàm tính chi phí dịch vụ
// (số tiền còn lại, trạng thái chi phí) cho Module 5.
using System.Globalization;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers
{
    // >>> Nếu nhóm đặt chuỗi trạng thái khác thì CHỈ SỬA Ở ĐÂY <<<
    public static class VaiTroConst
    {
        public const string Admin = "Admin";
        public const string NhanVien = "Nhân viên";
        public const string KhachHang = "Khách hàng";
    }

    public static class TrangThaiLichHen
    {
        public const string ChoXacNhan = "Chờ xác nhận";
        public const string DaXacNhan = "Đã xác nhận";
        public const string DaHuy = "Đã hủy";
        public const string DaHoanThanh = "Đã hoàn thành";
    }

    public static class TrangThaiDichVu
    {
        public const string DangMoDangKy = "Đang mở đăng ký";
        public const string DangPhucVu = "Đang phục vụ";
        public const string DaDu = "Đã đủ";
    }

    public static class TrangThaiCa
    {
        public const string DaLenLich = "Đã lên lịch";
        public const string DaHoanThanh = "Đã hoàn thành";
        public const string DaHuy = "Đã hủy";
    }

    public static class TrangThaiKetQua
    {
        public const string ChoThucHien = "Chờ thực hiện";
        public const string DangThucHien = "Đang thực hiện";
        public const string HoanThanh = "Hoàn thành";
    }

    public static class TrangThaiHoatDong
    {
        public const string DangHoatDong = "Đang hoạt động";
    }

    public static class TrangThaiChiPhi
    {
        public const string ChuaThanhToan = "Chưa thanh toán";
        public const string ThanhToanMotPhan = "Thanh toán một phần";
        public const string DaThanhToan = "Đã thanh toán";
    }

    public static class PhuongThucThanhToan
    {
        public static readonly string[] DanhSach = { "Tiền mặt", "Chuyển khoản", "Thẻ" };
    }

    public static class ChiPhiHelper
    {
        // SoTienConLai = DonGia - TongSoTienDaThanhToan
        public static decimal TinhConLai(decimal donGia, decimal tongDaThanhToan)
            => Math.Max(donGia - tongDaThanhToan, 0m);

        // Chưa thanh toán: tổng = 0 | Một phần: 0 < tổng < đơn giá | Đã thanh toán: tổng = đơn giá
        public static string LayTrangThai(decimal tongDaThanhToan, decimal donGia)
        {
            if (tongDaThanhToan <= 0) return TrangThaiChiPhi.ChuaThanhToan;
            if (tongDaThanhToan >= donGia) return TrangThaiChiPhi.DaThanhToan;
            return TrangThaiChiPhi.ThanhToanMotPhan;
        }

        public static string LayCssBadge(string trangThai) => trangThai switch
        {
            TrangThaiChiPhi.DaThanhToan => "bg-success",
            TrangThaiChiPhi.ThanhToanMotPhan => "bg-warning text-dark",
            _ => "bg-danger"
        };

        public static string ToVnd(this decimal v)
            => v.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
    }
}
