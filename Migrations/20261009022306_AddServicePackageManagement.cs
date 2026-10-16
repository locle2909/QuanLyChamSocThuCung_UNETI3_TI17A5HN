using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePackageManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DichVus",
                columns: table => new
                {
                    MaDichVu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDichVu = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Gia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NhomDichVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVus", x => x.MaDichVu);
                });

            migrationBuilder.CreateTable(
                name: "GoiDichVus",
                columns: table => new
                {
                    MaGoi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenGoi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GiaGoi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GiaKhuyenMai = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiDichVus", x => x.MaGoi);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietGoiDichVus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGoi = table.Column<int>(type: "int", nullable: false),
                    MaDichVu = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietGoiDichVus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiDichVus_DichVus_MaDichVu",
                        column: x => x.MaDichVu,
                        principalTable: "DichVus",
                        principalColumn: "MaDichVu",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietGoiDichVus_GoiDichVus_MaGoi",
                        column: x => x.MaGoi,
                        principalTable: "GoiDichVus",
                        principalColumn: "MaGoi",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiDichVus_MaDichVu",
                table: "ChiTietGoiDichVus",
                column: "MaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGoiDichVus_MaGoi_MaDichVu",
                table: "ChiTietGoiDichVus",
                columns: new[] { "MaGoi", "MaDichVu" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietGoiDichVus");

            migrationBuilder.DropTable(
                name: "DichVus");

            migrationBuilder.DropTable(
                name: "GoiDichVus");
        }
    }
}
