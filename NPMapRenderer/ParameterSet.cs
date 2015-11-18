using System.Collections.Generic;
using System.Drawing;

namespace NPMapRenderer
{
    public class ParameterSet
    {
        public float MinX { get; set; }
        public float MinY { get; set; }

        public float MaxX { get; set; }
        public float MaxY { get; set; }

        public float RangeX => MaxX - MinX;
        public float RangeY => MaxY - MinY;
        public float AspectRatio => RangeX/RangeY;

        public float Scale { get; set; }

        public int ImageWidth => (int)(Scale*AspectRatio);
        public int ImageHeight => (int)(Scale);

        public float StarWidth { get; set; }
        public float HalfStarWidth => StarWidth/2f;
        public float StarStroke => StarWidth/3f;
        public float StarOwnerWidth => StarWidth*2f;
        public float HalfStarOwnerWidth => StarOwnerWidth/2f;

        public Dictionary<int, Color> Colors { get; set; }


        public Point ToScreenPosition(PointF world)
        {
            var xCoord = (int)(((world.X - MinX) / RangeX) * ImageWidth);
            var yCoord = (int)(((world.Y - MinY) / RangeY) * ImageHeight);
            return new Point(xCoord, yCoord);
        }

        public PointF ToWorldPosition(Point screen)
        {
            var worldX = ((double)screen.X / ImageWidth) * RangeX + MinX;
            var worldY = ((double)screen.Y / ImageHeight) * RangeY + MinY;
            return new PointF((float)worldX, (float)worldY);
        }

        public float ToScreenDistance(float worldDistance)
        {
            return (worldDistance/RangeX)*ImageWidth;
        }

        public float ToWorldDistance(float screenDistance)
        {
            return (screenDistance/ImageWidth)*RangeX;
        }
    }
}