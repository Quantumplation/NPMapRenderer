using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Svg;
using Svg.Transforms;

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
                StarWidth = 5,

                Colors = new Dictionary<int, Color>
                {
                    [-1] = Color.White,
                    [0] = Color.FromArgb(4, 51, 255),
                    [1] = Color.FromArgb(0, 160, 223),
                    [2] = Color.FromArgb(55, 187, 0),
                    [3] = Color.FromArgb(255, 190, 14),
                    [4] = Color.FromArgb(255, 98, 0),
                    [5] = Color.FromArgb(193, 26, 0),
                    [6] = Color.FromArgb(193, 46, 191),
                    [7] = Color.FromArgb(97, 39, 196)
                }
            };

            Report last = null;
            Console.WriteLine($"MinX: {parameters.MinX}, MinY: {parameters.MinY}, MaxX: {parameters.MaxX}, MaxY: {parameters.MaxY}");
            foreach (var turnReport in game.OrderBy(x => x.Turn))
            {
                // Fold forward the invisible stars
                turnReport.CarryForward(last);
                last = turnReport;

                var starSvg = new SvgDocument
                {
                    Width = parameters.ImageWidth,
                    Height = parameters.ImageHeight,
                };
                var defs = new SvgDefinitionList();
                starSvg.Children.Add(defs);

                // Render the background
                turnReport.DrawBackground(starSvg, parameters);

                // Render the player vision circles
                turnReport.DrawVision(starSvg, defs, parameters);

                // Render all the stars
                foreach (var star in turnReport.Stars.Values)
                {
                    star.Draw(starSvg, parameters);
                }

                foreach (var fleet in turnReport.Fleets.Values)
                {
                    var start = parameters.ToScreenPosition(fleet.WorldPosition);
                    starSvg.Children.Add(new SvgCircle
                    {
                        CenterX = start.X,
                        CenterY = start.Y,
                        Fill = new SvgColourServer(parameters.Colors[fleet.Owner%8]),
                        Radius = parameters.HalfStarWidth * 0.85f
                    });
                    foreach (var order in fleet.Orders)
                    {
                        if (turnReport.Stars.ContainsKey(order.DestinationStar))
                        {
                            var end = parameters.ToScreenPosition(turnReport.Stars[order.DestinationStar].WorldPosition);

                            starSvg.Children.Add(new SvgLine
                            {
                                StartX = start.X,
                                StartY = start.Y,
                                EndX = end.X,
                                EndY = end.Y,
                                Stroke = new SvgColourServer(parameters.Colors[fleet.Owner%8])
                            });
                            start = end;
                        }
                        else
                        {
                            var end = parameters.ToScreenPosition(fleet.WorldLPosition);
                            var dir = new Point() { X = (end.X - start.X) * 10, Y = (end.Y - start.Y) * 10 };
                            var strokeDash = new SvgUnitCollection
                            {
                                new SvgUnit(parameters.StarStroke/2),
                                new SvgUnit(parameters.StarStroke)
                            };
                            starSvg.Children.Add(new SvgLine
                            {
                                StartX = start.X,
                                StartY = start.Y,
                                EndX = start.X - dir.X,
                                EndY = start.Y - dir.Y,
                                Stroke = new SvgColourServer(parameters.Colors[fleet.Owner % 8]),
                                StrokeWidth = parameters.StarStroke / 2,
                                StrokeDashArray = strokeDash,
                                StrokeLineCap = SvgStrokeLineCap.Round

                            });
                            break;
                        }
                    }
                }
                
                starSvg.Write(Path.Combine(directory, $"map_{turnReport.Turn}.svg"));
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
            catch(Exception ex)
            {
                Console.WriteLine($"Warning: File {filename} could not be parsed, skipping.  Exception: {ex.Message}.");
            }
            return state;
        }
    }
}
