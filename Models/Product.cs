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

        public string? BackgroundColor { get; set; }
    }
}
