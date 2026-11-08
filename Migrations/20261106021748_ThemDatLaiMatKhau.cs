   using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemDatLaiMatKhau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YeuCauDatLaiMatKhau",
                columns: table => new
                {
                    MaYeuCau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    MaHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaDung = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauDatLaiMatKhau", x => x.MaYeuCau);
                    table.ForeignKey(
                        name: "FK_YeuCauDatLaiMatKhau_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDatLaiMatKhau_MaHash",
                table: "YeuCauDatLaiMatKhau",
                column: "MaHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDatLaiMatKhau_MaTaiKhoan",
                table: "YeuCauDatLaiMatKhau",
                column: "MaTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YeuCauDatLaiMatKhau");
        }
    }
}
