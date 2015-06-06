using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprintReportGenerator.Models
{
    public class TaskModel
    {
        public TaskType TaskType { get; set; }
        public string Description { get; set; }
        public int Percentage { get; set; }
    }
}
