using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemXacNhanHoanThanhDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianDeXuatHoanThanh",
                table: "KetQuaDichVu",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianKhachXacNhanHoanThanh",
                table: "KetQuaDichVu",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThoiGianDeXuatHoanThanh",
                table: "KetQuaDichVu");

            migrationBuilder.DropColumn(
                name: "ThoiGianKhachXacNhanHoanThanh",
                table: "KetQuaDichVu");
        }
    }
}
