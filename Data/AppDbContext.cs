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
    public DbSet<TaiKhoan> TaiKhoan { get; set; }
    public DbSet<LoaiDichVu> LoaiDichVu { get; set; }

    // ===== SV2: ThuCung, ChuyenMonThuCung =====
    public DbSet<ThuCung> ThuCungs { get; set; }
    public DbSet<ChuyenMonThuCung> ChuyenMonThuCungs { get; set; }

    // ===== SV3: KhachHang, DichVu, GoiDichVu, LichHen =====
    // public DbSet<KhachHang> KhachHang { get; set; }
    // public DbSet<LichHen> LichHen { get; set; }
    public DbSet<DichVu> DichVus { get; set; }
    public DbSet<GoiDichVu> GoiDichVus { get; set; }
    public DbSet<ChiTietGoiDichVu> ChiTietGoiDichVus { get; set; }

    // ===== SV4: NhanVien, ChuyenMonNhanVien, PhanCongNhanVien, CaDichVu, KetQuaDichVu =====
    public DbSet<NhanVien> NhanVien { get; set; }
    public DbSet<ChuyenMonNhanVien> ChuyenMonNhanVien { get; set; }
    public DbSet<PhanCongNhanVien> PhanCongNhanVien { get; set; }
    public DbSet<CaDichVu> CaDichVu { get; set; }
    public DbSet<KetQuaDichVu> KetQuaDichVu { get; set; }

    // ===== SV5: ThanhToan =====
    // public DbSet<ThanhToan> ThanhToan { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Mọi khóa ngoại dùng Restrict để không xóa dây chuyền làm mất lịch sử.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;

        // ----- SV1 -----
        modelBuilder.Entity<TaiKhoan>().HasIndex(t => t.TenDangNhap).IsUnique();
        modelBuilder.Entity<LoaiDichVu>().HasIndex(l => l.TenLoaiDichVu).IsUnique();

        // ----- SV2 -----
        modelBuilder.Entity<ThuCung>().HasIndex(x => x.HoTen);
        modelBuilder.Entity<ThuCung>().HasIndex(x => x.Email);
        modelBuilder.Entity<ThuCung>().HasIndex(x => x.SoDienThoai);

        modelBuilder.Entity<ChuyenMonThuCung>()
            .HasOne(x => x.ThuCung)
            .WithMany(x => x.ChuyenMons)
            .HasForeignKey(x => x.MaThuCung)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChuyenMonThuCung>()
            .HasOne(x => x.LoaiDichVu)
            .WithMany(x => x.ChuyenMons)
            .HasForeignKey(x => x.MaLoaiDichVu)
            .OnDelete(DeleteBehavior.Restrict);

        // ----- SV3 -----
        modelBuilder.Entity<ChiTietGoiDichVu>()
            .HasIndex(ct => new { ct.MaGoi, ct.MaDichVu })
            .IsUnique();

        // ----- SV4 -----
        modelBuilder.Entity<ChuyenMonNhanVien>()
            .HasIndex(x => new { x.MaNhanVien, x.TenChuyenMon })
            .IsUnique();

        // ----- SV5 -----
    }
}