using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuarterViewStageMaker
{
    [JsonObject("Square")]
    public class Square
    {
        [JsonIgnore()]
        public readonly Stage Stage;

        [JsonProperty("Position")]
        public readonly Point Position;

        [JsonProperty("Height")]
        public double Height { get { return Blocks.Sum(block => block.Maptip.Height); } }

        [JsonProperty("Blocks")]
        public List<Block> Blocks { get; private set; }

        [JsonProperty("Discription")]
        public string Discription { get; private set; }

        public Square(Stage stage, Point position, string discription)
        {
            Stage = stage;
            Position = position;
            Blocks = new List<Block>();
            Discription = discription;
        }

        public void AddBlock(Maptip maptip)
        {
            var block = new Block(this, new Point(Position.X, Position.Y, Height), maptip);
            Blocks.Add(block);
        }

        public void SetDiscription(string discription)
        {
            Discription = discription;
        }

        public void Draw(System.Windows.Controls.Canvas canvas)
        {
            foreach(var block in Blocks)
            {
                block.Draw(canvas);
            }
        }
    }
}
