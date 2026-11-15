using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

public static class KiemTraLichHelper
{
    // Quy định của nhóm (có thể chỉnh): ca thường nằm trong 07:00 - 21:00 cùng một ngày
    public const int GioMoCua = 7;
    public const int GioDongCua = 21;
    // Dịch vụ lưu trú được kéo dài nhiều ngày
    public const string NhomLuuTru = "Lưu trú";
    public const int SoNgayLuuTruToiDa = 30;

    // Các hình thức không phải phòng vật lý thì không kiểm tra trùng phòng
    private static readonly string[] HinhThucKhongCanPhong = { "tại nhà", "trực tuyến", "online" };

    // Hai khoảng [bd1, kt1) và [bd2, kt2) trùng khi bd1 < kt2 && bd2 < kt1
    public static async Task<List<(string Truong, string ThongBao)>> KiemTraCa(
        AppDbContext db, CaDichVu ca, int boQuaMaCa = 0)
    {
        var loi = new List<(string Truong, string ThongBao)>();
        var bd = ca.ThoiGianBatDau;
        var kt = ca.ThoiGianKetThuc;
        var maThuCung = ca.MaThuCung;
        var maDichVu = ca.MaDichVu;

        if (kt <= bd)
        {
            loi.Add((nameof(CaDichVu.ThoiGianKetThuc), "Thời gian kết thúc phải sau thời gian bắt đầu."));
            return loi;
        }

        var thuCung = await db.ThuCungs.AsNoTracking().FirstOrDefaultAsync(x => x.MaThuCung == maThuCung);
        if (thuCung == null)
            loi.Add((nameof(CaDichVu.MaThuCung), "Thú cưng không tồn tại."));
        else if (thuCung.TrangThai != "Hoạt động")
            loi.Add((nameof(CaDichVu.MaThuCung), "Thú cưng đã ngừng hoạt động."));

        var dichVu = await db.DichVus.AsNoTracking().FirstOrDefaultAsync(x => x.MaDichVu == maDichVu);
        if (dichVu == null)
        {
            loi.Add((nameof(CaDichVu.MaDichVu), "Dịch vụ không tồn tại."));
        }
        else
        {
            if (dichVu.TrangThai == HangSo.TrangThaiDichVu.TamNgung
                || dichVu.TrangThai == HangSo.TrangThaiDichVu.NgungPhucVu)
                loi.Add((nameof(CaDichVu.MaDichVu), "Dịch vụ đang tạm ngừng hoặc đã ngừng phục vụ."));

            // Khoảng thời gian hợp lệ của dịch vụ
            if (dichVu.NhomDichVu == NhomLuuTru)
            {
                if ((kt - bd).TotalDays > SoNgayLuuTruToiDa)
                    loi.Add((nameof(CaDichVu.ThoiGianKetThuc), $"Dịch vụ lưu trú tối đa {SoNgayLuuTruToiDa} ngày."));
            }
            else if (bd.Date != kt.Date)
            {
                loi.Add((nameof(CaDichVu.ThoiGianKetThuc), "Ca phải bắt đầu và kết thúc trong cùng một ngày."));
            }
            else if (bd.TimeOfDay < TimeSpan.FromHours(GioMoCua) || kt.TimeOfDay > TimeSpan.FromHours(GioDongCua))
            {
                loi.Add((nameof(CaDichVu.ThoiGianBatDau), $"Ca phải nằm trong giờ làm việc {GioMoCua}:00 - {GioDongCua}:00."));
            }
        }

        // Các ca chưa hủy có khoảng thời gian giao với ca đang kiểm tra
        var trung = db.CaDichVu.AsNoTracking().Where(c =>
            c.MaCaDichVu != boQuaMaCa
            && c.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy
            && c.ThoiGianBatDau < kt
            && bd < c.ThoiGianKetThuc);

        if (await trung.AnyAsync(c => c.MaThuCung == maThuCung))
            loi.Add((nameof(CaDichVu.MaThuCung), "Thú cưng đã có ca dịch vụ khác trùng thời gian."));

        if (await trung.AnyAsync(c => c.MaDichVu == maDichVu))
            loi.Add((nameof(CaDichVu.MaDichVu), "Dịch vụ đã có ca khác trùng thời gian."));

        var phong = ca.PhongHocHoacHinhThuc?.Trim().ToLower();
        if (!string.IsNullOrEmpty(phong) && !HinhThucKhongCanPhong.Contains(phong))
        {
            if (await trung.AnyAsync(c => c.PhongHocHoacHinhThuc != null
                                          && c.PhongHocHoacHinhThuc.Trim().ToLower() == phong))
                loi.Add((nameof(CaDichVu.PhongHocHoacHinhThuc), "Phòng này đã có ca khác trùng thời gian."));
        }

        // 8.5: khi đổi lịch ca, khách hàng đã có kết quả trong ca không được trùng với ca khác của họ
        if (boQuaMaCa > 0)
        {
            var khachTrongCa = await db.KetQuaDichVu
                .Where(k => k.MaCaDichVu == boQuaMaCa)
                .Select(k => k.MaKhachHang)
                .ToListAsync();

            foreach (var maKhach in khachTrongCa)
            {
                if (await KhachBiTrungCa(db, maKhach, bd, kt, boQuaMaCa))
                {
                    loi.Add((string.Empty, $"Khách hàng mã {maKhach} đã có ca dịch vụ khác trùng khoảng thời gian mới."));
                    break;
                }
            }
        }

        return loi;
    }

    // 8.5: khách đã được đưa vào một ca khác (chưa hủy) có khoảng thời gian giao nhau
    public static Task<bool> KhachBiTrungCa(AppDbContext db, int maKhachHang,
        DateTime bd, DateTime kt, int boQuaMaCa = 0)
    {
        return (from kq in db.KetQuaDichVu
                join c in db.CaDichVu on kq.MaCaDichVu equals c.MaCaDichVu
                where kq.MaKhachHang == maKhachHang
                      && c.MaCaDichVu != boQuaMaCa
                      && c.TrangThai != HangSo.TrangThaiCaDichVu.DaHuy
                      && c.ThoiGianBatDau < kt
                      && bd < c.ThoiGianKetThuc
                select c.MaCaDichVu).AnyAsync();
    }

    // 8.5: khách có hai lịch hẹn ĐÃ XÁC NHẬN trùng khoảng thời gian
    public static Task<bool> KhachBiTrungLichHen(AppDbContext db, int maTaiKhoan,
        DateTime bd, DateTime kt, int boQuaMaLichHen = 0)
    {
        return db.LichHen.AnyAsync(l =>
            l.MaTaiKhoan == maTaiKhoan
            && l.MaLichHen != boQuaMaLichHen
            && l.TrangThai == HangSo.TrangThaiLichHen.DaXacNhan
            && l.ThoiGianBatDau < kt
            && bd < l.ThoiGianKetThuc);
    }
}