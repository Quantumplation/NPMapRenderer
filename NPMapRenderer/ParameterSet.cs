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

        public float StarWidth { get; set; }
        public float HalfStarWidth => StarWidth/2f;
        public float StarStroke => StarWidth/3f;
        public float StarOwnerWidth => StarWidth*2f;
        public float HalfStarOwnerWidth => StarOwnerWidth/2f;

        public Dictionary<int, Color> Colors { get; set; } 
    }
}