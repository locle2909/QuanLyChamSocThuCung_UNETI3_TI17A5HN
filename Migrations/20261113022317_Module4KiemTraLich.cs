using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Migrations
{
    /// <inheritdoc />
    public partial class Module4KiemTraLich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_KetQuaDichVu_MaCaDichVu_MaKhachHang",
                table: "KetQuaDichVu",
                columns: new[] { "MaCaDichVu", "MaKhachHang" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KetQuaDichVu_MaCaDichVu_MaKhachHang",
                table: "KetQuaDichVu");
        }
    }
}
