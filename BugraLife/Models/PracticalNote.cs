using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class PracticalNote
    {
        [Key]
        public int practicalnote_id { get; set; }

        [Required]
        public string practicalnote_title { get; set; } // Başlık

        public string practicalnote_description { get; set; } // Açıklama / Kod / Çözüm

        public DateTime created_at { get; set; } = DateTime.Now;
    }
}