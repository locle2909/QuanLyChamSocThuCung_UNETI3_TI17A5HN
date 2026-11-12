using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data.Seed;

public static class Module3Seed
{
    public static void Seed(AppDbContext db)
    {
        var services = new[]
        {
            new DichVu
            {
                TenDichVu = "Tắm gội cơ bản",
                MoTa = "Tắm, sấy và chải lông cho thú cưng.",
                Gia = 150_000,
                NhomDichVu = "Vệ sinh - Làm đẹp",
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy
            },
            new DichVu
            {
                TenDichVu = "Cắt tỉa lông",
                MoTa = "Cắt tỉa và vệ sinh lông theo nhu cầu.",
                Gia = 250_000,
                NhomDichVu = "Vệ sinh - Làm đẹp",
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy
            },
            new DichVu
            {
                TenDichVu = "Khám tổng quát",
                MoTa = "Kiểm tra sức khỏe tổng quát cho thú cưng.",
                Gia = 200_000,
                NhomDichVu = "Sức khỏe",
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy
            },
            new DichVu
            {
                TenDichVu = "Tiêm phòng cơ bản",
                MoTa = "Tư vấn và tiêm phòng theo lịch phù hợp.",
                Gia = 300_000,
                NhomDichVu = "Sức khỏe",
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy
            },
            new DichVu
            {
                TenDichVu = "Gửi thú cưng theo ngày",
                MoTa = "Chăm sóc và trông giữ thú cưng trong ngày.",
                Gia = 180_000,
                NhomDichVu = "Lưu trú",
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy
            }
        };

        foreach (var service in services)
        {
            if (!db.DichVus.Any(item => item.TenDichVu == service.TenDichVu))
                db.DichVus.Add(service);
        }
        db.SaveChanges();

        var serviceIds = db.DichVus
            .Where(item => services.Select(service => service.TenDichVu).Contains(item.TenDichVu))
            .Select(item => new { item.TenDichVu, item.MaDichVu })
            .ToList()
            .GroupBy(item => item.TenDichVu)
            .ToDictionary(group => group.Key, group => group.First().MaDichVu);

        var packages = new[]
        {
            new GoiDichVu
            {
                TenGoi = "Chăm sóc và làm đẹp",
                MoTa = "Kết hợp tắm gội và cắt tỉa lông, giúp thú cưng sạch sẽ và gọn gàng.",
                GiaGoi = 400_000,
                GiaKhuyenMai = 360_000,
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy,
                ChiTietGoiDichVus =
                [
                    new ChiTietGoiDichVu { MaDichVu = serviceIds["Tắm gội cơ bản"], SoLuong = 1 },
                    new ChiTietGoiDichVu { MaDichVu = serviceIds["Cắt tỉa lông"], SoLuong = 1 }
                ]
            },
            new GoiDichVu
            {
                TenGoi = "Sức khỏe định kỳ",
                MoTa = "Gồm khám tổng quát và tiêm phòng cơ bản cho thú cưng.",
                GiaGoi = 500_000,
                GiaKhuyenMai = 450_000,
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy,
                ChiTietGoiDichVus =
                [
                    new ChiTietGoiDichVu { MaDichVu = serviceIds["Khám tổng quát"], SoLuong = 1 },
                    new ChiTietGoiDichVu { MaDichVu = serviceIds["Tiêm phòng cơ bản"], SoLuong = 1 }
                ]
            },
            new GoiDichVu
            {
                TenGoi = "Nghỉ dưỡng 3 ngày",
                MoTa = "Trông giữ thú cưng 3 ngày, kèm một lần tắm gội.",
                GiaGoi = 690_000,
                GiaKhuyenMai = 620_000,
                TrangThai = HangSo.TrangThaiDichVu.DangMoDangKy,
                ChiTietGoiDichVus =
                [
                    new ChiTietGoiDichVu { MaDichVu = serviceIds["Gửi thú cưng theo ngày"], SoLuong = 3 },
                    new ChiTietGoiDichVu { MaDichVu = serviceIds["Tắm gội cơ bản"], SoLuong = 1 }
                ]
            }
        };

        foreach (var package in packages)
        {
            if (!db.GoiDichVus.Any(item => item.TenGoi == package.TenGoi))
                db.GoiDichVus.Add(package);
        }
        db.SaveChanges();
    }
}
