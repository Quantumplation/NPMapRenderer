using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json;
using Svg;
using Svg.Pathing;
using Svg.Transforms;

namespace NPMapRenderer
{
    public class Star
    {
        [JsonProperty("n")]
        public string Name { get; set; }
        [JsonProperty("puid")]
        public int Owner { get; set; }

        /// <summary>
        /// Is the star visible? this is to indicate a star that used to be visible, and no longer is.
        /// </summary>
        [JsonIgnore]
        public bool StillVisible { get; set; } = true;

        public double X { get; set; }
        public double Y { get; set; }


        public Point TransformedPosition(ParameterSet parameters)
        {
            var xCoord = (int)(((X - parameters.MinX) / parameters.RangeX) * parameters.ImageWidth);
            var yCoord = (int)(((Y - parameters.MinY) / parameters.RangeY) * parameters.ImageHeight);
            return new Point(xCoord, yCoord);
        }

        // See: http://www.rdwarf.com/lerickson/hex/
        public void Draw(SvgDocument svgDocument, ParameterSet parameters)
        {
            var position = TransformedPosition(parameters);

            var color = new SvgColourServer(parameters.Colors[Owner%8]);
            var outlineColor = new SvgColourServer(parameters.Colors[-1]);

            var stroke = StillVisible ? color : outlineColor;
            // This hatch pattern should have been generated earlier
            var fill = StillVisible ? SvgPaintServer.None : new SvgDeferredPaintServer(svgDocument, $"#hatch{Owner}");

            if (Owner == -1)
            {
                svgDocument.Children.Add(new SvgCircle
                {
                    CenterX = position.X,
                    CenterY = position.Y,
                    Radius = parameters.HalfStarWidth,
                    Fill = new SvgColourServer(parameters.Colors[-1])
                });
            }
            else switch (Owner/8)
            {
                case 0:
                {
                    svgDocument.Children.Add(new SvgCircle
                    {
                        CenterX = position.X,
                        CenterY = position.Y,
                        Radius = parameters.HalfStarWidth,
                        Stroke = stroke,
                        StrokeWidth = parameters.StarStroke,
                        Fill = fill
                    });
                } break;
                case 1:
                {
                    svgDocument.Children.Add(new SvgRectangle
                    {
                        X = position.X - parameters.HalfStarWidth,
                        Y = position.Y - parameters.HalfStarWidth,
                        Width = parameters.StarWidth,
                        Height = parameters.StarWidth,
                        Stroke = stroke,
                        StrokeWidth = parameters.StarStroke,
                        Fill = fill
                    });
                } break;
                case 2:
                {
                    var hexagon = BuildHexagon(parameters);
                    hexagon.Stroke = stroke;
                    hexagon.StrokeWidth = parameters.StarStroke;
                    hexagon.Fill = fill;
                    svgDocument.Children.Add(hexagon);
                } break;
                default:
                    throw new InvalidOperationException("Renderer was only designed for 24 players");
            }
        }

        private SvgPath BuildHexagon(ParameterSet parameters)
        {
            var hexagon = new SvgPath();
            var hexBLength = parameters.HalfStarWidth * 1;
            var hexALength = (Math.Sin(Math.PI / 6) * hexBLength);
            var hexCLength = 2 * hexALength;
            var points = new[]
            {
                new PointF(0f, (float)(hexALength + hexCLength)),
                new PointF(0f, (float)hexALength), 
                new PointF(hexBLength, 0f),
                new PointF(2f * hexBLength, (float)hexALength),
                new PointF(2f * hexBLength, (float)(hexALength + hexCLength)),
                new PointF(hexBLength, (float)(2f * hexCLength)),
            };
            
            hexagon.PathData.Add(new SvgMoveToSegment(points[0]));
            for (var i = 0; i < 5; i++)
            {
                hexagon.PathData.Add(new SvgLineSegment(points[i], points[i+1]));
            }
            hexagon.PathData.Add(new SvgClosePathSegment());
            
            var position = TransformedPosition(parameters);
            hexagon.Transforms.Add(new SvgTranslate(position.X - parameters.HalfStarWidth, position.Y - parameters.HalfStarWidth));
            return hexagon;
        }
    }
}