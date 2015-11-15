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

            // Feel free to tweak to your liking
            var parameters = new ParameterSet
            {
                MinX = -5,
                MinY = -5,
                MaxX = 5,
                MaxY = 5,

                ImageWidth = 1000,
                ImageHeight = 1000,

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
            
            // Read out all the files, and collapse them into one game state.
            var game = 
                from file in files
                let text = File.ReadAllText(file)
                let report = TryParseState(file, text)?.Report
                where report != null
                group report by report.Turn into turn
                select Report.Merge(turn);

            foreach (var turnReport in game)
            {
                var starLayer = new Bitmap(parameters.ImageWidth, parameters.ImageWidth);
                starLayer.MakeTransparent(Color.Black);
                var starGraphic = Graphics.FromImage(starLayer);

                // Render the player vision circles
                foreach (var player in turnReport.Players)
                {
                    var playerVision = new Bitmap(parameters.ImageWidth, parameters.ImageHeight);
                    var playerVisionGraphic = Graphics.FromImage(playerVision);
                    // One pass to render the colored boundary
                    var brush = new SolidBrush(parameters.Colors[player.Key % 8]);
                    var clear = new SolidBrush(Color.Black);
                    int scanningRadius = (int)((player.Value.Tech["scanning"].Value / parameters.RangeX) * parameters.ImageWidth);
                    foreach (var star in turnReport.Stars.Values.Where(x => x.Owner == player.Key))
                    {
                        var pos = star.TransformedPosition(parameters);
                        var rect = new Rectangle(
                            pos.X - scanningRadius,
                            pos.Y - scanningRadius,
                            scanningRadius * 2, scanningRadius * 2);
                        playerVisionGraphic.FillEllipse(brush, rect);
                    }
                    foreach (var star in turnReport.Stars.Values.Where(x => x.Owner == player.Key))
                    {
                        var pos = star.TransformedPosition(parameters);
                        var rect = new Rectangle(
                            pos.X - scanningRadius + parameters.StarStroke,
                            pos.Y - scanningRadius + parameters.StarStroke,
                            (scanningRadius - parameters.StarStroke) * 2, (scanningRadius - parameters.StarStroke) * 2);
                        playerVisionGraphic.FillEllipse(clear, rect);
                    }
                    playerVision.MakeTransparent(Color.Black);
                    starGraphic.DrawImage(playerVision, Point.Empty);
                }

                // Render all the stars
                foreach (var star in turnReport.Stars.Values)
                {
                    star.Draw(starGraphic, parameters);
                }

                starLayer.Save(Path.Combine(directory, $"map_{turnReport.Turn}.png"));
            }
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
