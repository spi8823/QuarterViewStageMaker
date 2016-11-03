using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace QuarterViewStageMaker
{
    [JsonObject()]
    public class MapObject
    {
        [JsonProperty("Name")]
        public string Name { get { return Figure.Name; } }
        [JsonProperty("Position")]
        public Point Position { get; set; }
        [JsonIgnore()]
        public Figure Figure;
        [JsonProperty("Tag")]
        public string Tag { get; set; }
        [JsonProperty("Discription")]
        public string Discription;
        [JsonProperty("Parameters")]
        public Dictionary<string, string> Parameters;

        [JsonIgnore()]
        public Image Image;

        public MapObject(Figure figure, Point position)
        {
            Figure = figure;
            Position = position;
            Tag = figure.DefaultTag;
            Discription = "";
            Parameters = new Dictionary<string, string>();

            Image = new Image();
            Image.Source = Figure?.Image?.Clone() ?? new System.Windows.Media.Imaging.BitmapImage();
            Image.Tag = this;
        }

        public void Refresh()
        {
            Image = new Image();
            Image.Source = Figure?.Image.Clone() ?? new System.Windows.Media.Imaging.BitmapImage();
            Image.Tag = this;
        }
    }
}
