using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NPMapRenderer
{
    /// <summary>
    ///  This program renders the state of a game of Neptunes Pride over time, if all the players use
    /// https://github.com/malorisdead/NeptunesPrideStateDownloader to download their state during the game
    /// 
    /// Performs rudimentary conflict detection.  Work In Progress.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            var directory = ConfigurationManager.AppSettings["downloadDirectory"];

            if (directory == null)
            {
                Console.WriteLine("Please remember to configure your settings.");
                return;
            }

            var files = Directory.GetFiles(directory, $"gamestate_*_*.json");
            
            // Read out all the files, and collapse them into one game state.
            var game = 
                (from file in files
                let text = File.ReadAllText(file)
                let report = TryParseState(file, text)?.Report
                where report != null
                group report by report.Turn into turn
                select Report.Merge(turn)).ToList();

            // Feel free to tweak to your liking
            var parameters = new ParameterSet
            {
                MinX = game.Min(x => x.Stars.Min(y => y.Value.X)) - 1,
                MinY = game.Min(x => x.Stars.Min(y => y.Value.Y)) - 1,
                MaxX = game.Max(x => x.Stars.Max(y => y.Value.X)) + 1,
                MaxY = game.Max(x => x.Stars.Max(y => y.Value.Y)) + 1,

                Scale = 1500,
                StarWidth = 10,

                Colors = new Dictionary<int, Color>
                {
                    [-1] = Color.White,
                    [0] = Color.Blue,
                    [1] = Color.Cyan,
                    [2] = Color.Green,
                    [3] = Color.Gold,
                    [4] = Color.DarkOrange,
                    [5] = Color.Red,
                    [6] = Color.DeepPink,
                    [7] = Color.Purple
                }
            };

            Report last = null;
            Console.WriteLine($"MinX: {parameters.MinX}, MinY: {parameters.MinY}, MaxX: {parameters.MaxX}, MaxY: {parameters.MaxY}");
            foreach (var turnReport in game.OrderBy(x => x.Turn))
            {
                // Fold forward the invisible stars
                if (last != null)
                {
                    foreach (var star in last.Stars)
                    {
                        if (!turnReport.Stars.ContainsKey(star.Key))
                        {
                            turnReport.Stars.Add(star.Key, new Star
                            {
                                Name = star.Value.Name,
                                Owner = star.Value.Owner,
                                StillVisible = false,
                                X = star.Value.X,
                                Y = star.Value.Y
                            });
                        }
                    }
                }


                var starLayer = new Bitmap(parameters.ImageWidth, parameters.ImageHeight);
                starLayer.MakeTransparent(Color.Black);
                var starGraphic = Graphics.FromImage(starLayer);

                // Render the player vision circles
                foreach (var player in turnReport.Players)
                {
                    var playerVision = new Bitmap(parameters.ImageWidth, parameters.ImageHeight);
                    var playerVisionGraphic = Graphics.FromImage(playerVision);
                    // One pass to render the colored boundary
                    var pen = new Pen(parameters.Colors[player.Key % 8]);
                    var dashed = new Pen(parameters.Colors[player.Key%8]);
                    dashed.DashStyle = DashStyle.Dot;
                    var clear = new SolidBrush(Color.Black);
                    int scanningWidth = (int)((player.Value.Tech["scanning"].Value / parameters.RangeX) * parameters.ImageWidth);
                    int scanningHeight = (int)((player.Value.Tech["scanning"].Value / parameters.RangeY) * parameters.ImageHeight);
                    foreach (var star in turnReport.Stars.Values.Where(x => x.Owner == player.Key))
                    {
                        var pos = star.TransformedPosition(parameters);
                        var rect = new Rectangle(
                            (pos.X - scanningWidth),
                            (pos.Y - scanningHeight),
                            (scanningWidth*2), (scanningHeight*2));
                        playerVisionGraphic.DrawEllipse( star.StillVisible ? pen : dashed, rect);
                    }
                    // And then to clear out the center
                    foreach (var star in turnReport.Stars.Values.Where(x => x.Owner == player.Key))
                    {
                        var pos = star.TransformedPosition(parameters);
                        var rect = new Rectangle(
                            (pos.X - scanningWidth + parameters.StarStroke),
                            (pos.Y - scanningHeight + parameters.StarStroke),
                            (scanningWidth - parameters.StarStroke)*2, (scanningHeight - parameters.StarStroke)*2);
                        playerVisionGraphic.FillEllipse(clear, rect);
                    }
                    playerVision.MakeTransparent(Color.Black);
                    starGraphic.DrawImage(playerVision, new Rectangle(0, 0, starLayer.Width, starLayer.Height), new Rectangle(0, 0, playerVision.Width, playerVision.Height), GraphicsUnit.Pixel);
                    playerVision.Dispose();
                }

                // Render all the stars
                foreach (var star in turnReport.Stars.Values)
                {
                    star.Draw(starGraphic, parameters);
                }

                starLayer.Save(Path.Combine(directory, $"map_{turnReport.Turn}.png"));
                starLayer.Dispose();
                last = turnReport;
            }
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static State TryParseState(string filename, string text)
        {
            State state = null;
            try
            {
                state = JsonConvert.DeserializeObject<State>(text);
            }
            catch
            {
                Console.WriteLine($"Warning: File {filename} could not be parsed, skipping.");
            }
            return state;
        }
    }
}
