using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.ViewModels
{
    public class ChuyenMonIndexViewModel
    {
        public List<ChuyenMonThuCung> Items { get; set; } = new();
        public string? Keyword { get; set; }
        public string? TrangThai { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}
