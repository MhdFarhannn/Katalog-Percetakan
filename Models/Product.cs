namespace Katalog.Models
{
    public class Product
    {
        public int Id { get; set; }

        public int IdKategoriProduct { get; set; }

        public int IdStatusProduct { get; set; }

        public string Nama { get; set; } = string.Empty;

        public string? Deskripsi { get; set; }

        public string? ImagePath { get; set; }

        public decimal Harga { get; set; }

        public decimal Diskon { get; set; }
        

        public string? BackgroundColor { get; set; }

        public KategoryProduct? KategoryProduct { get; set; }

        public StatusProduct? StatusProduct { get; set; }
    }
}