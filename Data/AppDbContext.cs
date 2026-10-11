
using Microsoft.EntityFrameworkCore;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<NhanVien> NhanVien { get; set; }
    public DbSet<ChuyenMonNhanVien> ChuyenMonNhanVien { get; set; }
    public DbSet<PhanCongNhanVien> PhanCongNhanVien { get; set; }
    public DbSet<CaDichVu> CaDichVu { get; set; }
    public DbSet<KetQuaDichVu> KetQuaDichVu { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var fk in modelBuilder.Model
                     .GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<ChuyenMonNhanVien>()
            .HasIndex(x => new { x.MaNhanVien, x.TenChuyenMon })
            .IsUnique();
    }
}