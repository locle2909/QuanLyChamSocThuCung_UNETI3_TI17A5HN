// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: ViewModel Dashboard và các báo cáo thống kê (SV5).
namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels;

public class DashboardViewModel
{
    public DateTime TuNgay { get; set; }
    public DateTime DenNgay { get; set; }

    public int TongLoaiDichVu { get; set; }
    public int TongThuCungHoatDong { get; set; }
    public int TongKhachHang { get; set; }
    public int TongDichVu { get; set; }
    public int TongGoiDichVu { get; set; }
    public int DichVuDangMoDangKy { get; set; }
    public int DichVuDangPhucVu { get; set; }
    public int DichVuDaDu { get; set; }
    public int LichHenChoXacNhan { get; set; }
    public int CaDichVuSapToi { get; set; }
    public decimal TongThuTrongKy { get; set; }
    public int LichHenConThieuChiPhi { get; set; }
}

public class ThongKeItem
{
    public string Ten { get; set; } = string.Empty;
    public int SoLuong { get; set; }
}

public class ThongKeTienThang
{
    public int Thang { get; set; }
    public decimal SoTien { get; set; }
}

public class BuoiPhucVuRow
{
    public string TenThuCung { get; set; } = string.Empty;
    public int Thang { get; set; }
    public int SoBuoi { get; set; }
}

public class DichVuThongKeRow
{
    public string TenDichVu { get; set; } = string.Empty;
    public int SoLichHieuLuc { get; set; }
    public decimal PhaiThu { get; set; }
    public decimal DaThu { get; set; }
    public decimal ConPhaiThu => Math.Max(PhaiThu - DaThu, 0m);
    public int TongKetQua { get; set; }
    public int SoBuoiCoMat { get; set; }
    // TyLeChuyenCan = SoBuoiCoMat / TongKetQua * 100
    public double TyLeChuyenCan => TongKetQua == 0 ? 0 : SoBuoiCoMat * 100.0 / TongKetQua;
}

public class ThongKeViewModel
{
    public int Nam { get; set; }
    public List<ThongKeItem> DichVuTheoLoai { get; set; } = new();
    public List<ThongKeItem> KhachHangTheoDichVu { get; set; } = new();
    public List<ThongKeItem> LichHenTheoTrangThai { get; set; } = new();
    public List<ThongKeItem> LoaiDichVuNhieuDangKy { get; set; } = new();
    public List<ThongKeItem> ThuCungDatNhieu { get; set; } = new();
    public List<BuoiPhucVuRow> BuoiPhucVuTheoThang { get; set; } = new();
    public List<ThongKeTienThang> DoanhThuTheoThang { get; set; } = new();
    public List<DichVuThongKeRow> ChiTietDichVu { get; set; } = new();
}

public class ChuyenCanRow
{
    public string TenDichVu { get; set; } = string.Empty;
    public int TongBuoi { get; set; }
    public int SoBuoiCoMat { get; set; }
    public double TyLe => TongBuoi == 0 ? 0 : SoBuoiCoMat * 100.0 / TongBuoi;
}
