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

        public double X { get; set; }
        public double Y { get; set; }


        public Point TransformedPosition(ParameterSet parameters)
        {
            var xCoord = (int)(((X - parameters.MinX) / parameters.RangeX) * (parameters.ImageWidth - parameters.StarWidth * 2)) + parameters.StarWidth;
            var yCoord = (int)(((Y - parameters.MinY) / parameters.RangeY) * (parameters.ImageHeight - parameters.StarWidth * 2)) + parameters.StarWidth;
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

            if (Owner == -1)
            {
                graphic.FillEllipse(new SolidBrush(parameters.Colors[-1]), rect);
            }
            switch (Owner/8)
            {
                case 0:
                    graphic.DrawEllipse(pen, rect);
                    break;
                case 1:
                    graphic.DrawRectangle(pen, rect);
                    break;
                case 2:
                    var hexagon = BuildHexagon(parameters);
                    graphic.DrawPath(pen, hexagon);
                    break;
                default:
                    throw new InvalidOperationException("Renderer was only designed for 24 players");
            }
        }

        private GraphicsPath BuildHexagon(ParameterSet parameters)
        {
            var hexagon = new GraphicsPath();
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
            translateMatrix.Translate(position.X, position.Y);
            translateMatrix.Scale(.1f, .1f);
            hexagon.Transform(translateMatrix);
            return hexagon;
        }


    }
}