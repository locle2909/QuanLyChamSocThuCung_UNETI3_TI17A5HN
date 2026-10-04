using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ThuCung> ThuCungs => Set<ThuCung>();
        public DbSet<LoaiDichVu> LoaiDichVus => Set<LoaiDichVu>();
        public DbSet<ChuyenMonThuCung> ChuyenMonThuCungs => Set<ChuyenMonThuCung>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<LoaiDichVu>().HasData(
                new LoaiDichVu { MaLoaiDichVu = 1, TenLoaiDichVu = "Tắm vệ sinh", TrangThai = true },
                new LoaiDichVu { MaLoaiDichVu = 2, TenLoaiDichVu = "Cắt tỉa lông", TrangThai = true },
                new LoaiDichVu { MaLoaiDichVu = 3, TenLoaiDichVu = "Khám sức khỏe", TrangThai = true },
                new LoaiDichVu { MaLoaiDichVu = 4, TenLoaiDichVu = "Tiêm phòng", TrangThai = true },
                new LoaiDichVu { MaLoaiDichVu = 5, TenLoaiDichVu = "Trông giữ", TrangThai = true },
                new LoaiDichVu { MaLoaiDichVu = 6, TenLoaiDichVu = "Spa thú cưng", TrangThai = true }
            );
        }
    }
}
