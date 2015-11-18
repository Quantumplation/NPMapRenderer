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

        public float X { get; set; }
        public float Y { get; set; }

        public PointF WorldPosition
        {
            get { return new PointF(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        // See: http://www.rdwarf.com/lerickson/hex/
        public void Draw(SvgDocument svgDocument, ParameterSet parameters)
        {
            var position = parameters.ToScreenPosition(WorldPosition);

            var color = new SvgColourServer(parameters.Colors[Owner%8]);
            var strokeDash = new SvgUnitCollection
            {
                StillVisible ? SvgUnit.None : new SvgUnit(parameters.StarStroke),
                StillVisible ? SvgUnit.None : new SvgUnit(parameters.StarStroke * 2f)
            };

            svgDocument.Children.Add(new SvgCircle
            {
                CenterX = position.X,
                CenterY = position.Y,
                Radius = parameters.HalfStarWidth,
                Fill = new SvgColourServer(parameters.Colors[-1])
            });

            if (Owner == -1)
                return;

            switch (Owner/8)
            {
                case 0:
                {
                    svgDocument.Children.Add(new SvgCircle
                    {
                        CenterX = position.X,
                        CenterY = position.Y,
                        Radius = parameters.HalfStarOwnerWidth,
                        Stroke = color,
                        StrokeWidth = parameters.StarStroke,
                        StrokeDashArray = strokeDash,
                        StrokeLineCap = SvgStrokeLineCap.Round,
                        Fill = SvgPaintServer.None
                    });
                } break;
                case 1:
                {
                    svgDocument.Children.Add(new SvgRectangle
                    {
                        X = position.X - parameters.HalfStarOwnerWidth,
                        Y = position.Y - parameters.HalfStarOwnerWidth,
                        Width = parameters.StarOwnerWidth,
                        Height = parameters.StarOwnerWidth,
                        Stroke = color,
                        StrokeWidth = parameters.StarStroke,
                        StrokeDashArray = strokeDash,
                        StrokeLineCap = SvgStrokeLineCap.Round,
                        Fill = SvgPaintServer.None
                    });
                } break;
                case 2:
                {
                    var hexagon = BuildHexagon(parameters);
                    hexagon.Stroke = color;
                    hexagon.StrokeWidth = parameters.StarStroke;
                    hexagon.StrokeDashArray = strokeDash;
                    hexagon.StrokeLineCap = SvgStrokeLineCap.Round;
                    hexagon.Fill = SvgPaintServer.None;
                    svgDocument.Children.Add(hexagon);
                } break;
                default:
                    throw new InvalidOperationException("Renderer was only designed for 24 players");
            }
        }

        private SvgPath BuildHexagon(ParameterSet parameters)
        {
            var hexagon = new SvgPath();
            var hexBLength = parameters.HalfStarOwnerWidth;
            var hexCLength = hexBLength / (float)Math.Sin(Math.PI / 3.0);
            var hexALength = hexCLength / 2f;
            var points = new[]
            {
                new PointF(0f, hexALength + hexCLength),
                new PointF(0f, hexALength), 
                new PointF(hexBLength, 0f),
                new PointF(2f * hexBLength, hexALength),
                new PointF(2f * hexBLength, hexALength + hexCLength),
                new PointF(hexBLength, 2f * hexCLength),
            };
            
            hexagon.PathData.Add(new SvgMoveToSegment(points[0]));
            for (var i = 0; i < 5; i++)
            {
                hexagon.PathData.Add(new SvgLineSegment(points[i], points[i+1]));
            }
            hexagon.PathData.Add(new SvgClosePathSegment());
            
            var position = parameters.ToScreenPosition(WorldPosition);
            hexagon.Transforms.Add(new SvgTranslate(position.X - parameters.HalfStarOwnerWidth,
                position.Y - parameters.HalfStarOwnerWidth - hexALength / 4f));
            return hexagon;
        }
    }
}