using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class ActivityDefinition
    {
        [Key]
        public int activitydefinition_id { get; set; }

        [Required]
        public string name { get; set; } // Aktivite Adı

        public string color { get; set; } = "#3788d8"; // Takvimde gözükecek renk

        public bool is_active { get; set; } = true;
    }
}
