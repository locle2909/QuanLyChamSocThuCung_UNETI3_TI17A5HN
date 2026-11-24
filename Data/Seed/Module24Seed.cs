// Dữ liệu mẫu cho Thú cưng, Thông tin chăm sóc (SV2) và Nhân viên, Chuyên môn nhân viên (SV4).
// Chạy lại nhiều lần không bị trùng. Cần Module1Seed và Module3Seed chạy trước.
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data.Seed;

public static class Module24Seed
{
    public static void Seed(AppDbContext db)
    {
        // ----- Nhân viên: gắn với tài khoản nhanvien1..5, mỗi người một nhóm chuyên môn -----
        var chuyenMonNhanVien = new[]
        {
            ("nhanvien1", "Vệ sinh - Làm đẹp"),
            ("nhanvien2", "Vệ sinh - Làm đẹp"),
            ("nhanvien3", "Sức khỏe"),
            ("nhanvien4", "Huấn luyện"),
            ("nhanvien5", "Lưu trú"),
        };

        foreach (var (tenDangNhap, nhom) in chuyenMonNhanVien)
        {
            var tk = db.TaiKhoan.FirstOrDefault(t => t.TenDangNhap == tenDangNhap);
            if (tk == null) continue;

            var nv = db.NhanVien.FirstOrDefault(n => n.MaTaiKhoan == tk.MaTaiKhoan);
            if (nv == null)
            {
                nv = new NhanVien
                {
                    HoTen = tk.HoTen,
                    Email = tk.Email,
                    SoDienThoai = "09000000" + tenDangNhap[^1] + "0",
                    TrangThai = true,
                    MaTaiKhoan = tk.MaTaiKhoan
                };
                db.NhanVien.Add(nv);
                db.SaveChanges();
            }

            if (!db.ChuyenMonNhanVien.Any(c => c.MaNhanVien == nv.MaNhanVien && c.TenChuyenMon == nhom))
            {
                db.ChuyenMonNhanVien.Add(new ChuyenMonNhanVien { MaNhanVien = nv.MaNhanVien, TenChuyenMon = nhom });
                db.SaveChanges();
            }
        }

        // ----- Thú cưng: mỗi khách hàng 1..5 một thú cưng, có thông tin chăm sóc cho 3 nhóm dịch vụ -----
        var giongLoai = new[] { "Chó Poodle", "Mèo Anh lông ngắn", "Chó Corgi", "Mèo Ba Tư", "Chó Golden" };
        var loaiMau = new[] { "Tắm gội", "Khám tổng quát", "Gửi thú cưng theo ngày" };

        for (int i = 1; i <= 5; i++)
        {
            var tk = db.TaiKhoan.FirstOrDefault(t => t.TenDangNhap == "khachhang" + i);
            if (tk == null) continue;

            var chuSoHuu = tk.MaTaiKhoan;
            var ten = "Thú cưng " + i;

            var tc = db.ThuCungs.FirstOrDefault(t => t.MaTaiKhoan == chuSoHuu && t.HoTen == ten);
            if (tc == null)
            {
                tc = new ThuCung
                {
                    MaTaiKhoan = chuSoHuu,
                    HoTen = ten,
                    NgaySinh = DateTime.Today.AddYears(-2),
                    GioiTinh = i % 2 == 0 ? "Cái" : "Đực",
                    SoDienThoai = "0911000" + i.ToString("D3"),
                    Email = tk.Email ?? $"khachhang{i}@petcare.vn",
                    DiaChi = "Hà Nội",
                    GiongLoai = giongLoai[i - 1],
                    Tuoi = 2,
                    TrangThai = "Hoạt động"
                };
                db.ThuCungs.Add(tc);
                db.SaveChanges();
            }

            foreach (var tenLoai in loaiMau)
            {
                var loai = db.LoaiDichVu.FirstOrDefault(l => l.TenLoaiDichVu == tenLoai);
                if (loai == null) continue;

                if (db.ChuyenMonThuCungs.Any(c => c.MaThuCung == tc.MaThuCung && c.MaLoaiDichVu == loai.MaLoaiDichVu))
                    continue;

                db.ChuyenMonThuCungs.Add(new ChuyenMonThuCung
                {
                    MaThuCung = tc.MaThuCung,
                    MaLoaiDichVu = loai.MaLoaiDichVu,
                    GiongLoaiChuyenMon = tc.GiongLoai,
                    TuoiMon = tc.Tuoi,
                    TrangThai = HangSo.TrangThaiChuyenMon.HieuLuc
                });
            }
            db.SaveChanges();
        }
    }
}