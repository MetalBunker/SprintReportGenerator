using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SprintReportGenerator.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                System.Console.WriteLine("SprintReportGenerator\n\nUsage: SprintReportGenerator.Console <taskFile> <Developer Name>");
                return;
            }

            try
            {
                var fileName = args[0];
                var devName = args[1];

                var parser = new Parser();
                var sprints = parser.Parse(File.ReadAllText(fileName), GetLimitDate());

                System.Console.WriteLine(JsonConvert.SerializeObject(sprints, Formatting.Indented));

                var report = parser.GeneratePercentagesReport(sprints, devName);

                var tempFileName = Path.GetTempFileName() + ".txt";
                File.WriteAllText(tempFileName, report);
                Process.Start(tempFileName);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("ERROR >>>>>>>>>>>");
                System.Console.WriteLine(ex.Message);
                Thread.Sleep(10 * 1000);
            }
        }

        // TODO: Pretty useless for now, maybe we could pass this as a command line parameter
        private static DateTime GetLimitDate()
        {
            return new DateTime(2015, 02, 23); // Day of start of the first sprint in which we started to use the app
            var limitDate = DateTime.Now.Date.AddDays(-20);

            while (limitDate.DayOfWeek != DayOfWeek.Monday)
            {
                limitDate = limitDate.AddDays(1);
            }

            return limitDate;
        }
    }
}
