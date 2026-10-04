using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels
{
    public class ThuCungIndexViewModel
    {
        public List<ThuCung> Items { get; set; } = new();
        public string? Keyword { get; set; }
        public int? MaLoaiDichVu { get; set; }
        public string? GiongLoai { get; set; }
        public int? Tuoi { get; set; }
        public string? TrangThai { get; set; }
        public string Sort { get; set; } = "name_asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public List<LoaiDichVu> LoaiDichVus { get; set; } = new();
        public List<string> GiongLoais { get; set; } = new();
    }
}
