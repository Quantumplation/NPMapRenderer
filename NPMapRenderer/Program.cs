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

            Report last = null;

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
