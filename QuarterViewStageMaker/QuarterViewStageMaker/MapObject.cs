using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuarterViewStageMaker
{
    public class MapObject
    {
        public Point Position;
        public Figure Figure;
        public Point Size;
        public Point Center;
        public string Tag;
        public string Discription;
        public Dictionary<string, double> Parameters;

        public Image Image;

        public MapObject(Figure figure, Point position)
        {
            Figure = figure;
            Position = position;
            Size = Figure.DefaultSize;
            Center = Figure.DefaultCenter;
            Tag = figure.DefaultTag;
            Parameters = new Dictionary<string, double>();

            Image = new Image();
            Image.Source = Figure?.Image.Clone() ?? new System.Windows.Media.Imaging.BitmapImage();
            Image.Tag = this;
        }
    }
}
