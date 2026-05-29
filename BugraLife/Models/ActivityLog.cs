using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class ActivityLog
    {
        [Key]
        public int activitylog_id { get; set; }

        public int activitydefinition_id { get; set; }

        [ForeignKey("activitydefinition_id")]
        public virtual ActivityDefinition? ActivityDefinition { get; set; }

        public DateTime log_date { get; set; } // Hangi gün yapıldı?
    }
}
