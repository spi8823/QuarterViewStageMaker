using System;
using System.Windows.Controls;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace QuarterViewStageMaker
{
    [JsonObject("Block")]
    public class Block
    {
        [JsonIgnore()]
        public readonly Square Square;

        [JsonIgnore()]
        public Maptip Maptip;

        [JsonProperty("Name")]
        public string Name { get { return Maptip.Name; } }

        [JsonProperty("Position")]
        public Point Position;

        [JsonIgnore()]
        private Image _Image;

        public Block(Square square, Point position, Maptip maptip)
        {
            Square = square;
            Position = position;

            Maptip = maptip ?? null;

            _Image = new Image();
            _Image.Source = Maptip?.Image ?? new System.Windows.Media.Imaging.BitmapImage();
        }
    }
}
