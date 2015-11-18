using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Svg;
using Svg.Transforms;

namespace NPMapRenderer
{
    public class Report
    {
        public int Tick { get; set; }
        public int Turn => Tick/6; // TODO: Setting?
        [JsonProperty("player_uid")]
        public int Player { get; set; }
        public Dictionary<int, Star> Stars { get; set; } = new Dictionary<int, Star>();
        public Dictionary<int, Player> Players { get; set; } = new Dictionary<int, Player>();
        public Dictionary<int, Fleet> Fleets { get; set; } = new Dictionary<int, Fleet>();

        public void CarryForward(Report last)
        {
            if (last == null) return;
            // TODO fleets
            foreach (var star in last.Stars)
            {
                if (!Stars.ContainsKey(star.Key))
                {
                    Stars.Add(star.Key, new Star
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

        public void DrawBackground(SvgDocument canvas, ParameterSet parameters)
        {
            // This rectangle acts as the background
            canvas.Children.Add(new SvgRectangle
            {
                Width = parameters.ImageWidth,
                Height = parameters.ImageHeight,
                Fill = new SvgColourServer(Color.Black)
            });
        }

        public void DrawVision(SvgDocument canvas, SvgDefinitionList defs, ParameterSet parameters)
        {
            foreach (var player in Players)
            {
                // We render a rectangle as large as the image for the player,
                // then mask it to make the scanning range
                var playerVision = new SvgRectangle
                {
                    Width = parameters.ImageWidth,
                    Height = parameters.ImageHeight,
                    Fill = new SvgColourServer(parameters.Colors[player.Key % 8])
                };
                var playerVisionMask = new SvgUnknownElement("mask")
                {
                    ID = $"mask{player.Key}"
                };

                // We generate a hatch pattern for each player in case it needs
                // to be used when rendering stars
                // http://www.colabrativ.com/scalable-vector-graphics-svg-pattern-examples/
                var hatchPattern = new SvgPatternServer
                {
                    ID = $"hatch{player.Key}",
                    X = 0,
                    Y = 0,
                    Width = parameters.StarStroke * 2,
                    Height = parameters.StarStroke * 2,
                    PatternUnits = SvgCoordinateUnits.UserSpaceOnUse,
                    PatternTransform = new SvgTransformCollection { new SvgRotate(315) }
                };
                hatchPattern.Children.Add(new SvgRectangle
                {
                    X = 0,
                    Y = 0,
                    Width = parameters.StarStroke,
                    Height = parameters.StarStroke * 2,
                    Fill = playerVision.Fill
                });
                defs.Children.Add(hatchPattern);

                // The masks are put into groups to assure they render
                // in the proper order
                var lowerMask = new SvgGroup();
                var upperMask = new SvgGroup();
                playerVisionMask.Children.Add(lowerMask);
                playerVisionMask.Children.Add(upperMask);
                playerVision.CustomAttributes["mask"] = $"url(#{playerVisionMask.ID})";
                defs.Children.Add(playerVisionMask);

                var scanningRadius = parameters.ToScreenDistance((float)player.Value.Tech["scanning"].Value);
                foreach (var star in Stars.Values.Where(x => x.Owner == player.Key))
                {
                    var pos = parameters.ToScreenPosition(star.WorldPosition);
                    // First mask: a filled, outer circle
                    lowerMask.Children.Add(new SvgCircle
                    {
                        CenterX = pos.X,
                        CenterY = pos.Y,
                        Radius = scanningRadius,
                        Fill = new SvgColourServer(Color.White)
                    });

                    // Second mask: an empty, inner circle
                    upperMask.Children.Add(new SvgCircle
                    {
                        CenterX = pos.X,
                        CenterY = pos.Y,
                        Radius = scanningRadius - parameters.StarStroke,
                        Fill = new SvgColourServer(Color.Black)
                    });
                }
                canvas.Children.Add(playerVision);
            }
        }

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
                    if(!aggregateReport.Players.ContainsKey(player.Key))
                        aggregateReport.Players.Add(player.Key, player.Value);
                }

                // Fleets
                // TODO: Conflict checking
                foreach (var fleet in report.Fleets)
                {
                    if (!aggregateReport.Fleets.ContainsKey(fleet.Key))
                        aggregateReport.Fleets.Add(fleet.Key, fleet.Value);
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
