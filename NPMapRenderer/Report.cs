using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NPMapRenderer
{
    public class Player
    {
        [JsonProperty("uid")] 
        public int Id { get; set; }
        public string Alias { get; set; }
        public Dictionary<string, Tech> Tech { get; set; } = new Dictionary<string, Tech>(); 
    }

    public class Tech
    {
        public double Value { get; set; }
        public int Level { get; set; }
    }
    public class Report
    {
        public int Tick { get; set; }
        public int Turn => Tick/6; // TODO: Setting?
        [JsonProperty("player_uid")]
        public int Player { get; set; }
        public Dictionary<int, Star> Stars { get; set; } = new Dictionary<int, Star>();
        public Dictionary<int, Player> Players { get; set; } = new Dictionary<int, Player>();

        /// <summary>
        /// Collapse several reports into 
        /// </summary>
        /// <param name="reports"></param>
        /// <returns></returns>
        public static Report Merge(IEnumerable<Report> reports)
        {
            var aggregateReport = new Report { Tick = -1, Player = -1};
            foreach (var report in reports)
            {
                if (aggregateReport.Tick == -1)
                    aggregateReport.Tick = report.Tick;
                else if (aggregateReport.Tick != report.Tick)
                {
                    Console.WriteLine($"Warning: One of the reports labeled for turn {aggregateReport.Tick} is really for turn {report.Tick}.");
                    continue;
                }
                // Add the current player
                // TODO: Conflict checking
                foreach (var player in report.Players)
                {
                    if (player.Key == report.Player &&
                        aggregateReport.Players.ContainsKey(player.Key))
                    {
                        aggregateReport.Players.Remove(player.Key);
                    }
                    aggregateReport.Players.Add(player.Key, player.Value);
                }

                // Star info
                foreach (var star in report.Stars)
                {
                    if (!aggregateReport.Stars.ContainsKey(star.Key))
                        aggregateReport.Stars.Add(star.Key, star.Value);
                    else
                    {
                        var existing = aggregateReport.Stars[star.Key];
                        if (existing.Name != star.Value.Name ||
                            existing.Owner != star.Value.Owner ||
                            Math.Abs(existing.X - star.Value.X) > 0.0001 ||
                            Math.Abs(existing.Y - star.Value.Y) > 0.0001)
                        {
                            Console.WriteLine($"Warning: Two reports are reporting conflicting states for star the star {existing.Name} on turn {aggregateReport.Turn}");
                            continue;
                        }
                    }
                }
            }
            return aggregateReport;
        }
    }
}
