using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

public static class HoanThanhCaHelper
{
    // Nếu mọi kết quả của ca đều đã được khách hàng xác nhận thì chuyển ca, các phân công
    // còn hiệu lực và lịch hẹn tương ứng sang "hoàn thành".
    // Hàm chỉ thay đổi dữ liệu trong context, người gọi tự gọi SaveChangesAsync.
    // Trả về true nếu ca được chuyển sang hoàn thành.
    public static async Task<bool> HoanThanhCaNeuDuDieuKien(AppDbContext db, int maCaDichVu)
    {
        var ca = await db.CaDichVu.FirstOrDefaultAsync(c => c.MaCaDichVu == maCaDichVu);
        if (ca == null || ca.TrangThai != HangSo.TrangThaiCaDichVu.DaLenLich)
            return false;

        var ketQuas = await db.KetQuaDichVu
            .Where(k => k.MaCaDichVu == maCaDichVu)
            .ToListAsync();

        if (ketQuas.Count == 0)
            return false;

        bool conChuaXacNhan = ketQuas.Any(k =>
            k.TrangThaiKetQuaDichVu != HangSo.TrangThaiKetQua.HoanThanh
            || k.ThoiGianKhachXacNhanHoanThanh == null);
        if (conChuaXacNhan)
            return false;

        ca.TrangThai = HangSo.TrangThaiCaDichVu.DaHoanThanh;

        var phanCongs = await db.PhanCongNhanVien
            .Where(p => p.MaCaDichVu == maCaDichVu
                        && p.TrangThai == HangSo.TrangThaiPhanCong.HieuLuc)
            .ToListAsync();
        foreach (var pc in phanCongs)
            pc.TrangThai = HangSo.TrangThaiPhanCong.HoanThanh;

        if (ca.MaLichHen != null)
        {
            var lichHen = await db.LichHen
                .FirstOrDefaultAsync(l => l.MaLichHen == ca.MaLichHen.Value);
            if (lichHen != null && lichHen.TrangThai == HangSo.TrangThaiLichHen.DaXacNhan)
                lichHen.TrangThai = HangSo.TrangThaiLichHen.DaHoanThanh;
        }

        return true;
    }
}
