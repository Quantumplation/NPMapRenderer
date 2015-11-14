using System.Collections.Generic;
using System.Drawing;

namespace NPMapRenderer
{
    public class ParameterSet
    {
        public int MinX { get; set; }
        public int MinY { get; set; }

        public int MaxX { get; set; }
        public int MaxY { get; set; }

        public int RangeX => MaxX - MinX;
        public int RangeY => MaxY - MinY;

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public int StarWidth { get; set; }
        public int HalfStarWidth => StarWidth/2;
        public int StarStroke => StarWidth/10;

        public Dictionary<int, Color> Colors { get; set; } 
    }
}