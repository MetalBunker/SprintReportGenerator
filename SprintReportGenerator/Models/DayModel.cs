using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprintReportGenerator.Models
{
    public class DayModel
    {
        public List<TaskModel> Tasks { get; set; }
        public DateTime Date { get; set; }
        public int? SprintNumber { get; set; }
    }
}
