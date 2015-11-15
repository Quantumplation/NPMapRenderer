using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json;

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
        public void Draw(Graphics graphic, ParameterSet parameters)
        {
            var position = TransformedPosition(parameters);
            var rect = new Rectangle(
                position.X - parameters.HalfStarWidth,
                position.Y - parameters.HalfStarWidth,
                parameters.StarWidth, parameters.StarWidth);

            var pen = new Pen(parameters.Colors[Owner%8], parameters.StarStroke);
            var hatched = new HatchBrush(HatchStyle.DarkDownwardDiagonal, parameters.Colors[Owner%8]);
            var outline = new Pen(parameters.Colors[-1]);

            if (Owner == -1)
            {
                graphic.FillEllipse(new SolidBrush(parameters.Colors[-1]), rect);
            }
            else switch (Owner/8)
            {
                case 0:
                {
                    if (StillVisible)
                    {
                        graphic.DrawEllipse(pen, rect);
                    }
                    else
                    {
                        graphic.FillEllipse(hatched, rect);
                        graphic.DrawRectangle(outline, rect);
                    }
                } break;
                case 1:
                {
                    if (StillVisible)
                    {
                        graphic.DrawRectangle(pen, rect);
                    }
                    else
                    {
                        graphic.FillRectangle(hatched, rect);
                        graphic.DrawRectangle(outline, rect);
                    }
                } break;
                case 2:
                {
                    var hexagon = BuildHexagon(parameters, true);
                    if (StillVisible)
                    {
                        graphic.DrawPath(pen, hexagon);
                    }
                    else
                    {
                        graphic.FillPath(hatched, hexagon);
                        graphic.DrawPath(outline, hexagon);
                    }
                } break;
                default:
                    throw new InvalidOperationException("Renderer was only designed for 24 players");
            }
        }

        private GraphicsPath BuildHexagon(ParameterSet parameters, bool filled)
        {
            var hexagon = new GraphicsPath();
            hexagon.FillMode = filled ? FillMode.Winding : FillMode.Alternate;
            var hexBLength = parameters.HalfStarWidth * 10;
            var hexALength = (Math.Sin(Math.PI / 6) * hexBLength);
            var hexCLength = 2 * hexALength;
            var points = new[]
            {
                new Point(0, (int)(hexALength + hexCLength)),
                new Point(0, (int)hexALength),
                new Point(hexBLength, 0),
                new Point(2 * hexBLength, (int)hexALength),
                new Point(2 * hexBLength, (int)(hexALength + hexCLength)),
                new Point(hexBLength, (int)(2 * hexCLength)),
            };
            hexagon.AddLines(points);
            hexagon.CloseFigure();
            var translateMatrix = new Matrix();
            var position = TransformedPosition(parameters);
            translateMatrix.Translate(position.X - parameters.HalfStarWidth, position.Y - parameters.HalfStarWidth);
            translateMatrix.Scale(.1f, .1f);
            hexagon.Transform(translateMatrix);
            return hexagon;
        }


    }
}