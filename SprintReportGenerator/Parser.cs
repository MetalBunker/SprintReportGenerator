using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SprintReportGenerator.Models;

namespace SprintReportGenerator
{
    public class Parser
    {
        private const int _sprintDaysCount = 10;

        public List<SprintModel> Parse(string notes, DateTime limitDate)
        {
            var days = new List<DayModel>();
            var currentLineNumber = 0;

            var lines = notes.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            DayModel currentDay = null;

            foreach (var line in lines)
            {
                try
                {
                    var trimmedLine = line.Trim();
                    var day = ParseDayModel(trimmedLine);

                    if (day == null)
                    {
                        var task = ParseTaskModel(trimmedLine);
                        if (task != null)
                        {
                            if (currentDay == null) throw new Exception("Task with no day found!");

                            currentDay.Tasks.Add(task);
                        }
                    }
                    else
                    {
                        currentDay = day;
                        days.Add(currentDay);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(currentLineNumber + ": " + ex.Message, ex);
                }

                currentLineNumber++;
            }

            days = days.OrderBy(day => day.Date).Where(day => day.Date >= limitDate).ToList();

            if(!days[0].SprintNumber.HasValue) throw new Exception("First day doesn't have a sprint!");

            var sprints = new List<SprintModel>();
            SprintModel currentSprint = null;

            foreach (var day in days)
            {
                if (day.SprintNumber.HasValue)
                {
                    currentSprint = new SprintModel()
                    {
                        Days = new List<DayModel>() {day},
                        SprintNumber = day.SprintNumber.Value
                    };
                    sprints.Add(currentSprint);
                }
                else
                {
                    day.SprintNumber = currentSprint.SprintNumber;
                    currentSprint.Days.Add(day);
                }
            }

            return sprints;
        }

        public string GeneratePercentagesReport(List<SprintModel> sprints, string name)
        {
            const float percentagePerDay = 100f / _sprintDaysCount;
            var reportBuilder = new StringBuilder();
            
            foreach (var sprint in sprints)
            {
                if (sprint.Days.Count < _sprintDaysCount)
                {
                    reportBuilder.AppendFormat("SPRINT {0} ignored because it has only {1} day(s).", sprint.SprintNumber,
                        sprint.Days.Count);
                    reportBuilder.AppendLine();
                    continue;
                }
                
                ValidateSprint(sprint);

                AutocompleteDaysPercentage(sprint);

                var tasks = sprint.Days.SelectMany(day => day.Tasks);

                tasks.GroupBy(task => new {task.TaskType, task.Description},
                        (key, group) => 
                            new { key.TaskType, key.Description, Percentage = group.Sum(task => task.Percentage) / percentagePerDay })
                    .OrderBy(row => row.TaskType).ThenBy(row => row.Description).ToList()
                    .ForEach(row =>
                        reportBuilder.AppendFormat("{0}\t{1}\t{2}%\t{3}\t{4}{5}", sprint.SprintNumber, name, 
                        row.Percentage.ToString().Replace('.', ','), row.TaskType, row.Description, Environment.NewLine)
                    );

                reportBuilder.AppendLine();
            }

            return reportBuilder.ToString();
        }

        private void AutocompleteDaysPercentage(SprintModel sprint)
        {
            foreach (var day in sprint.Days)
            {
                var percentage = day.Tasks.Sum(task => task.Percentage);
                if (percentage == 100) continue;

                var tasksWithoutPercentage = day.Tasks.Where(task => task.Percentage == 0).ToList();
                var percentageToAssign = (100 - percentage)/tasksWithoutPercentage.Count();

                tasksWithoutPercentage.ForEach(task => task.Percentage = percentageToAssign);

                ValidateDay(day);
            }
        }

        private void ValidateDay(DayModel day)
        {
            if (day.Tasks.Sum(task => task.Percentage) != 100)
            {
                // I guess this could never happen, but tasks could get some real decimal percentages
                throw new Exception(string.Format("SPRINT {0}, Day {1}: Autocomplete couldn't get 100% for this day.",
                    day.SprintNumber, day.Date));
            }
        }

        private void ValidateSprint(SprintModel sprint)
        {
            // Sprints must have _sprintDaysCount days
            if (sprint.Days.Count > _sprintDaysCount)
            {
                throw new Exception(string.Format("SPRINT {0}: has {1} days. Sprints must have {2} days!",
                       sprint.SprintNumber, sprint.Days.Count, _sprintDaysCount));
            }

            // Each day must appear only once
            var invalidDate = sprint.Days.GroupBy(day => day.Date).FirstOrDefault(group => group.Count() > 1);
            if (invalidDate != null)
            {
                throw new Exception(string.Format("SPRINT {0}: Day {1} appears more than once.",
                       sprint.SprintNumber, invalidDate.Key));
            }

            // Each day must have at least 1 task
            var invalidDay = sprint.Days.FirstOrDefault(day => day.Tasks.Count == 0);
            if (invalidDay != null)
            {
                throw new Exception(string.Format("SPRINT {0}, Day {1}: must have at least 1 task.",
                    sprint.SprintNumber, invalidDay.Date));
            }

            // Sum of all tasks of a particular day must be 100% at most
            invalidDay = sprint.Days.FirstOrDefault(day => day.Tasks.Sum(task => task.Percentage) > 100);
            if (invalidDay != null)
            {
                throw new Exception(string.Format("SPRINT {0}, Day {1}: Sum of all tasks must be 100% at most.",
                    sprint.SprintNumber, invalidDay.Date));
            }

            // When a day is already at 100%, all the tasks should have some percentage
            invalidDay = sprint.Days.FirstOrDefault(day => day.Tasks.Sum(task => task.Percentage) == 100);
            if (invalidDay != null && invalidDay.Tasks.Any(task => task.Percentage == 0))
            {
                throw new Exception(string.Format("SPRINT {0}, Day {1}: Each task must have some percentage assigned when day is already at 100%.",
                    sprint.SprintNumber, invalidDay.Date));
            }

            // When all the tasks have a percentage assigned, day should be at 100%
            invalidDay = sprint.Days.FirstOrDefault(day =>
                day.Tasks.Sum(task => task.Percentage) < 100 && day.Tasks.All(task => task.Percentage != 0));
            if (invalidDay != null)
            {
                throw new Exception(string.Format("SPRINT {0}, Day {1}: Sum of all task must be 100% when all the tasks have percentages assigned.",
                    sprint.SprintNumber, invalidDay.Date));
            }
        }

        /// <summary>
        /// Parses a line into a TaskModel.
        /// Possible tasks metadata formats:
        /// [This is an example feature]
        /// [B:This is an example bug]
        /// [O:This in an example task that took my 70%:70]
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private TaskModel ParseTaskModel(string line)
        {
            var metaData = GetMetaData(line);
            if (metaData == null || metaData.Length == 0) return null;

            if (metaData.Length < 1 || metaData.Length > 3) throw new Exception("Invalid metadata, expected between 1 and 3 parameters");

            var task = new TaskModel();

            switch (metaData.Length)
            {
                case 1:
                {
                    // We assume we got [Description]
                    task.TaskType = TaskType.Feature;
                    task.Description = metaData[0];
                    break;
                }
                case 2:
                {
                    if (metaData[0].Length == 1)
                    { // We assume we got [TaskType:Description]
                        task.TaskType = (TaskType) metaData[0].ToUpper()[0];
                        task.Description = metaData[1];
                    }
                    else
                    { // We assume we got [Description:%]
                        task.TaskType = TaskType.Feature;
                        task.Description = metaData[0];
                        task.Percentage = int.Parse(metaData[1]);
                    }
                    break;
                }
                case 3:
                {
                    // We assume we got [TaskType:Description:%]
                    task.TaskType = (TaskType) metaData[0].ToUpper()[0];
                    task.Description = metaData[1];
                    task.Percentage = int.Parse(metaData[2]);
                    break;
                }
            }

            return task;
        }

        private DayModel ParseDayModel(string line)
        {
            DateTime date;
            if (line.Length < 10 || !DateTime.TryParse(line.Substring(0, 10), out date)) return null;

            var day = new DayModel()
            {
                Date = date,
                Tasks = new List<TaskModel>()
            };

            var metaData = GetMetaData(line);

            if (metaData == null || metaData.Length == 0) return day;

            switch (metaData[0].ToUpper())
            {
                case "SPRINT":
                    if(metaData.Length == 1) throw new Exception("Missing sprint number");
                    day.SprintNumber = int.Parse(metaData[1]);
                    break;
                case "DAYOFF":
                    day.Tasks.Add(new TaskModel()
                    {
                        Description = "Day off" + (metaData.Length > 1 ? " - " + metaData[1] : string.Empty),
                        Percentage = 100,
                        TaskType = TaskType.Leave
                    });
                    break;
            }

            return day;
        }

        private string[] GetMetaData(string line)
        {
            var startIndex = line.IndexOf('[');
            if (startIndex < 0) return null;
            
            var finishIndex = line.IndexOf(']');
            if(finishIndex <= 0) throw new Exception("Missing closing ]");

            return line.Substring(startIndex + 1, finishIndex - startIndex - 1).Split(':');
        }
    }
}
