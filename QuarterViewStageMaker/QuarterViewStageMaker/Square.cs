using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

/*
The MIT License (MIT)

Copyright (c) 2007 James Newton-King

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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

        [JsonProperty("Tag")]
        public string Tag { get; private set; } = "";

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

        public void SetTag(string tag)
        {
            Tag = tag;
        }
    }
}
