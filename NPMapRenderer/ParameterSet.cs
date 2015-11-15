using System.Collections.Generic;
using System.Drawing;

namespace NPMapRenderer
{
    public class ParameterSet
    {
        public double MinX { get; set; }
        public double MinY { get; set; }

        public double MaxX { get; set; }
        public double MaxY { get; set; }

        public double RangeX => MaxX - MinX;
        public double RangeY => MaxY - MinY;
        public double AspectRatio => RangeX/RangeY;

        public double Scale { get; set; }

        public int ImageWidth => (int)(Scale*AspectRatio);
        public int ImageHeight => (int)(Scale);

        public int StarWidth { get; set; }
        public int HalfStarWidth => StarWidth/2;
        public int StarStroke => StarWidth/10;

        public Dictionary<int, Color> Colors { get; set; } 
    }
}