// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Hàm tính chi phí dịch vụ (số tiền còn lại, trạng thái chi phí) dùng cho SV5.
using System.Globalization;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

public static class ChiPhiHelper
{
    // SoTienConLai = DonGia - TongDaThanhToan (không âm)
    public static decimal TinhConLai(decimal donGia, decimal tongDaThanhToan)
        => Math.Max(donGia - tongDaThanhToan, 0m);

    // Chưa thanh toán: tổng = 0 | Một phần: 0 < tổng < đơn giá | Đã thanh toán: tổng >= đơn giá
    public static string LayTrangThai(decimal tongDaThanhToan, decimal donGia)
    {
        if (tongDaThanhToan <= 0) return HangSo.TrangThaiChiPhi.ChuaThanhToan;
        if (tongDaThanhToan >= donGia) return HangSo.TrangThaiChiPhi.DaThanhToan;
        return HangSo.TrangThaiChiPhi.ThanhToanMotPhan;
    }

    public static string LayCssBadge(string trangThai) => trangThai switch
    {
        HangSo.TrangThaiChiPhi.DaThanhToan => "bg-success",
        HangSo.TrangThaiChiPhi.ThanhToanMotPhan => "bg-warning text-dark",
        _ => "bg-danger"
    };

    public static string ToVnd(this decimal v)
        => v.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
}
