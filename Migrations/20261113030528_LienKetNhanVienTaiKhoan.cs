using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Migrations
{
    /// <inheritdoc />
    public partial class LienKetNhanVienTaiKhoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaTaiKhoan",
                table: "NhanVien",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_MaTaiKhoan",
                table: "NhanVien",
                column: "MaTaiKhoan",
                unique: true,
                filter: "[MaTaiKhoan] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_NhanVien_TaiKhoan_MaTaiKhoan",
                table: "NhanVien",
                column: "MaTaiKhoan",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NhanVien_TaiKhoan_MaTaiKhoan",
                table: "NhanVien");

            migrationBuilder.DropIndex(
                name: "IX_NhanVien_MaTaiKhoan",
                table: "NhanVien");

            migrationBuilder.DropColumn(
                name: "MaTaiKhoan",
                table: "NhanVien");
        }
    }
}
