namespace Katalog.Models
{
    public class Layanan
    {
        public int Id { get; set; }

        public string Nama { get; set; } = string.Empty;

        public string Deskripsi { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        public string? BackgroundColor { get; set; }
    }
}
