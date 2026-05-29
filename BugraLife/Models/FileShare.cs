using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class FileShared
    {
        [Key]
        public int Id { get; set; }

        public string Token { get; set; } // Linkin sonundaki rastgele kod
        public string FilePath { get; set; } // Dosyanın gerçek yolu
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true; // İptal edildi mi?
    }
}