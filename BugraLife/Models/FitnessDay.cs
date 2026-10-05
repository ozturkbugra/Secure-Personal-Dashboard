using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    // Antrenman günü/split (ör. "1. Gün - Göğüs"). İçinde sıralı gruplar bulunur.
    public class FitnessDay
    {
        [Key]
        public int fitnessday_id { get; set; }

        [Required]
        public string fitnessday_name { get; set; }

        public int fitnessday_order { get; set; }

        public bool fitnessday_active { get; set; } = true;

        public virtual ICollection<FitnessGroup> Groups { get; set; } = new List<FitnessGroup>();
    }
}
