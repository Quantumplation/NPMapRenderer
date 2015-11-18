﻿using System;
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

                var starSvg = new SvgDocument
                {
                    Width = parameters.ImageWidth,
                    Height = parameters.ImageHeight,
                };
                var defs = new SvgDefinitionList();
                starSvg.Children.Add(defs);

                // This rectangle acts as the background
                starSvg.Children.Add(new SvgRectangle
                {
                    Width = parameters.ImageWidth,
                    Height = parameters.ImageHeight,
                    Fill = new SvgColourServer(Color.Black)
                });

                // Render the player vision circles
                foreach (var player in turnReport.Players)
                {
                    //var playerVision = new Bitmap(parameters.ImageWidth, parameters.ImageHeight);
                    //var playerVisionGraphic = Graphics.FromImage(playerVision);
                    //// One pass to render the colored boundary
                    //var pen = new Pen(parameters.Colors[player.Key % 8]);
                    //var dashed = new Pen(parameters.Colors[player.Key%8]);
                    //dashed.DashStyle = DashStyle.Dot;
                    //var clear = new SolidBrush(Color.Black);
                    //int scanningWidth = (int)((player.Value.Tech["scanning"].Value / parameters.RangeX) * parameters.ImageWidth);
                    //int scanningHeight = (int)((player.Value.Tech["scanning"].Value / parameters.RangeY) * parameters.ImageHeight);
                    //foreach (var star in turnReport.Stars.Values.Where(x => x.Owner == player.Key))
                    //{
                    //    var pos = star.TransformedPosition(parameters);
                    //    var rect = new Rectangle(
                    //        (pos.X - scanningWidth),
                    //        (pos.Y - scanningHeight),
                    //        (scanningWidth*2), (scanningHeight*2));
                    //    playerVisionGraphic.DrawEllipse( star.StillVisible ? pen : dashed, rect);
                    //}
                    //// And then to clear out the center
                    //foreach (var star in turnReport.Stars.Values.Where(x => x.Owner == player.Key))
                    //{
                    //    var pos = star.TransformedPosition(parameters);
                    //    var rect = new Rectangle(
                    //        (pos.X - scanningWidth + parameters.StarStroke),
                    //        (pos.Y - scanningHeight + parameters.StarStroke),
                    //        (scanningWidth - parameters.StarStroke)*2, (scanningHeight - parameters.StarStroke)*2);
                    //    playerVisionGraphic.FillEllipse(clear, rect);


                    // We render a rectangle as large as the image for the player,
                    // then mask it to make the scanning range
                    var playerVision = new SvgRectangle
                    {
                        Width = parameters.ImageWidth,
                        Height = parameters.ImageHeight,
                        Fill = new SvgColourServer(parameters.Colors[player.Key%8])
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
                    
                    int scanningRadius = (int)((player.Value.Tech["scanning"].Value / parameters.RangeX) * parameters.ImageWidth);
                    foreach (var star in turnReport.Stars.Values.Where(x => x.Owner == player.Key))
                    {
                        if (star.StillVisible)
                        {
                            var pos = star.TransformedPosition(parameters);
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
                    }
                    starSvg.Children.Add(playerVision);
                }

                // Render all the stars
                foreach (var star in turnReport.Stars.Values)
                {
                    star.Draw(starSvg, parameters);
                }
                
                starSvg.Write(Path.Combine(directory, $"map_{turnReport.Turn}.svg"));

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
