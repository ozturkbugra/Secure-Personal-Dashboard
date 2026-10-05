using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    // Tekil fitness hareketi: ad + açıklama (set/ağırlık notları buraya) + foto.
    public class FitnessExercise
    {
        [Key]
        public int fitnessexercise_id { get; set; }

        [Required]
        public string fitnessexercise_name { get; set; }

        public string? fitnessexercise_description { get; set; }

        // wwwroot'a göre göreli yol (ör. /fitness/abc.jpg)
        public string? fitnessexercise_image { get; set; }

        public DateTime fitnessexercise_created { get; set; } = DateTime.Now;
    }
}
