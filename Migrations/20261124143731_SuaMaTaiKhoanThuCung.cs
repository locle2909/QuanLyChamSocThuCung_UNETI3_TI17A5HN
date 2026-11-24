using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Migrations
{
    /// <inheritdoc />
    public partial class SuaMaTaiKhoanThuCung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaTaiKhoan",
                table: "ThuCungs",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_ThuCungs_MaTaiKhoan",
                table: "ThuCungs",
                column: "MaTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongNhanVien_MaCaDichVu",
                table: "PhanCongNhanVien",
                column: "MaCaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongNhanVien_MaNhanVien",
                table: "PhanCongNhanVien",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_LichHen_MaDichVu",
                table: "LichHen",
                column: "MaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_LichHen_MaTaiKhoan",
                table: "LichHen",
                column: "MaTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_LichHen_MaThuCung",
                table: "LichHen",
                column: "MaThuCung");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaDichVu_MaKhachHang",
                table: "KetQuaDichVu",
                column: "MaKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_CaDichVu_MaDichVu",
                table: "CaDichVu",
                column: "MaDichVu");

            migrationBuilder.CreateIndex(
                name: "IX_CaDichVu_MaThuCung",
                table: "CaDichVu",
                column: "MaThuCung");

            migrationBuilder.AddForeignKey(
                name: "FK_CaDichVu_DichVus_MaDichVu",
                table: "CaDichVu",
                column: "MaDichVu",
                principalTable: "DichVus",
                principalColumn: "MaDichVu",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CaDichVu_ThuCungs_MaThuCung",
                table: "CaDichVu",
                column: "MaThuCung",
                principalTable: "ThuCungs",
                principalColumn: "MaThuCung",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChuyenMonNhanVien_NhanVien_MaNhanVien",
                table: "ChuyenMonNhanVien",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KetQuaDichVu_CaDichVu_MaCaDichVu",
                table: "KetQuaDichVu",
                column: "MaCaDichVu",
                principalTable: "CaDichVu",
                principalColumn: "MaCaDichVu",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KetQuaDichVu_TaiKhoan_MaKhachHang",
                table: "KetQuaDichVu",
                column: "MaKhachHang",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHen_DichVus_MaDichVu",
                table: "LichHen",
                column: "MaDichVu",
                principalTable: "DichVus",
                principalColumn: "MaDichVu",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHen_TaiKhoan_MaTaiKhoan",
                table: "LichHen",
                column: "MaTaiKhoan",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichHen_ThuCungs_MaThuCung",
                table: "LichHen",
                column: "MaThuCung",
                principalTable: "ThuCungs",
                principalColumn: "MaThuCung",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhanCongNhanVien_CaDichVu_MaCaDichVu",
                table: "PhanCongNhanVien",
                column: "MaCaDichVu",
                principalTable: "CaDichVu",
                principalColumn: "MaCaDichVu",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PhanCongNhanVien_NhanVien_MaNhanVien",
                table: "PhanCongNhanVien",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThuCungs_TaiKhoan_MaTaiKhoan",
                table: "ThuCungs",
                column: "MaTaiKhoan",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaDichVu_DichVus_MaDichVu",
                table: "CaDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_CaDichVu_ThuCungs_MaThuCung",
                table: "CaDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_ChuyenMonNhanVien_NhanVien_MaNhanVien",
                table: "ChuyenMonNhanVien");

            migrationBuilder.DropForeignKey(
                name: "FK_KetQuaDichVu_CaDichVu_MaCaDichVu",
                table: "KetQuaDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_KetQuaDichVu_TaiKhoan_MaKhachHang",
                table: "KetQuaDichVu");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHen_DichVus_MaDichVu",
                table: "LichHen");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHen_TaiKhoan_MaTaiKhoan",
                table: "LichHen");

            migrationBuilder.DropForeignKey(
                name: "FK_LichHen_ThuCungs_MaThuCung",
                table: "LichHen");

            migrationBuilder.DropForeignKey(
                name: "FK_PhanCongNhanVien_CaDichVu_MaCaDichVu",
                table: "PhanCongNhanVien");

            migrationBuilder.DropForeignKey(
                name: "FK_PhanCongNhanVien_NhanVien_MaNhanVien",
                table: "PhanCongNhanVien");

            migrationBuilder.DropForeignKey(
                name: "FK_ThuCungs_TaiKhoan_MaTaiKhoan",
                table: "ThuCungs");

            migrationBuilder.DropIndex(
                name: "IX_ThuCungs_MaTaiKhoan",
                table: "ThuCungs");

            migrationBuilder.DropIndex(
                name: "IX_PhanCongNhanVien_MaCaDichVu",
                table: "PhanCongNhanVien");

            migrationBuilder.DropIndex(
                name: "IX_PhanCongNhanVien_MaNhanVien",
                table: "PhanCongNhanVien");

            migrationBuilder.DropIndex(
                name: "IX_LichHen_MaDichVu",
                table: "LichHen");

            migrationBuilder.DropIndex(
                name: "IX_LichHen_MaTaiKhoan",
                table: "LichHen");

            migrationBuilder.DropIndex(
                name: "IX_LichHen_MaThuCung",
                table: "LichHen");

            migrationBuilder.DropIndex(
                name: "IX_KetQuaDichVu_MaKhachHang",
                table: "KetQuaDichVu");

            migrationBuilder.DropIndex(
                name: "IX_CaDichVu_MaDichVu",
                table: "CaDichVu");

            migrationBuilder.DropIndex(
                name: "IX_CaDichVu_MaThuCung",
                table: "CaDichVu");

            migrationBuilder.AlterColumn<string>(
                name: "MaTaiKhoan",
                table: "ThuCungs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
