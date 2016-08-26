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
            Maptip = maptip;

            _Image = new Image();
            _Image.Source = Maptip.Image;
        }

        public void Draw(Canvas canvas)
        {
            if (!canvas.Children.Contains(_Image))
                canvas.Children.Add(_Image);

            var relativePoint = Position.ToRelativePoint();
            _Image.Margin = new System.Windows.Thickness(
                relativePoint.X + 50 - _Image.RenderSize.Width / 2, 
                canvas.Height - relativePoint.Y - _Image.RenderSize.Height + 50, 
                canvas.Width - relativePoint.X - _Image.RenderSize.Width / 2, 
                relativePoint.Y + 50);
        }
    }
}
