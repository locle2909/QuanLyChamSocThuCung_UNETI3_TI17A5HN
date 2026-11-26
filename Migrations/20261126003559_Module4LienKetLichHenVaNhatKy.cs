using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Migrations
{
    /// <inheritdoc />
    public partial class Module4LienKetLichHenVaNhatKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChuNoiBo",
                table: "KetQuaDichVu",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaNguoiCapNhat",
                table: "KetQuaDichVu",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaLichHen",
                table: "CaDichVu",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NhatKyKetQuaDichVu",
                columns: table => new
                {
                    MaNhatKy = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKetQuaDichVu = table.Column<int>(type: "int", nullable: false),
                    MaNguoiSua = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyKetQuaDichVu", x => x.MaNhatKy);
                    table.ForeignKey(
                        name: "FK_NhatKyKetQuaDichVu_KetQuaDichVu_MaKetQuaDichVu",
                        column: x => x.MaKetQuaDichVu,
                        principalTable: "KetQuaDichVu",
                        principalColumn: "MaKetQuaDichVu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaDichVu_MaLichHen",
                table: "CaDichVu",
                column: "MaLichHen",
                unique: true,
                filter: "[MaLichHen] IS NOT NULL AND [TrangThai] <> N'Đã hủy'");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyKetQuaDichVu_MaKetQuaDichVu",
                table: "NhatKyKetQuaDichVu",
                column: "MaKetQuaDichVu");

            migrationBuilder.AddForeignKey(
                name: "FK_CaDichVu_LichHen_MaLichHen",
                table: "CaDichVu",
                column: "MaLichHen",
                principalTable: "LichHen",
                principalColumn: "MaLichHen",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaDichVu_LichHen_MaLichHen",
                table: "CaDichVu");

            migrationBuilder.DropTable(
                name: "NhatKyKetQuaDichVu");

            migrationBuilder.DropIndex(
                name: "IX_CaDichVu_MaLichHen",
                table: "CaDichVu");

            migrationBuilder.DropColumn(
                name: "GhiChuNoiBo",
                table: "KetQuaDichVu");

            migrationBuilder.DropColumn(
                name: "MaNguoiCapNhat",
                table: "KetQuaDichVu");

            migrationBuilder.DropColumn(
                name: "MaLichHen",
                table: "CaDichVu");
        }
    }
}
