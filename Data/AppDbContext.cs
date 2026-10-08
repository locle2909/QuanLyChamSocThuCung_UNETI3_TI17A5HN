// Họ và tên: ........
// Mã sinh viên: ........
// Nội dung thực hiện: Phần chung nhóm trưởng: DbContext khung, mỗi SV chỉ sửa vùng của mình.
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ===== SV1: TaiKhoan, LoaiDichVu =====
    // public DbSet<TaiKhoan> TaiKhoan { get; set; }
    // public DbSet<LoaiDichVu> LoaiDichVu { get; set; }

    // ===== SV2: ThuCung, ChuyenMonThuCung =====
    // public DbSet<ThuCung> ThuCung { get; set; }
    // public DbSet<ChuyenMonThuCung> ChuyenMonThuCung { get; set; }

    // ===== SV3: KhachHang, DichVu, LichHen =====
    // public DbSet<KhachHang> KhachHang { get; set; }
    // public DbSet<DichVu> DichVu { get; set; }
    // public DbSet<LichHen> LichHen { get; set; }

    // ===== SV4: PhanCongNhanVien, CaDichVu, KetQuaDichVu =====
    // public DbSet<PhanCongNhanVien> PhanCongNhanVien { get; set; }
    // public DbSet<CaDichVu> CaDichVu { get; set; }
    // public DbSet<KetQuaDichVu> KetQuaDichVu { get; set; }

    // ===== SV5: ThanhToan =====
    // public DbSet<ThanhToan> ThanhToan { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mọi khóa ngoại dùng Restrict để không xóa dây chuyền làm mất lịch sử.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        // ----- SV1 -----
        // modelBuilder.Entity<TaiKhoan>().HasIndex(t => t.TenDangNhap).IsUnique();
        // modelBuilder.Entity<LoaiDichVu>().HasIndex(l => l.TenLoaiDichVu).IsUnique();

        // ----- SV2 -----

        // ----- SV3 -----

        // ----- SV4 -----

        // ----- SV5 -----
    }
}
