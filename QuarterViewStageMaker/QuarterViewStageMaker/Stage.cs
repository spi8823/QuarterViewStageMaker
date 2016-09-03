using System;
using System.IO;
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
    [JsonObject("Stage")]
    public class Stage
    {
        [JsonIgnore()]
        public Project Project;

        [JsonProperty("ID")]
        public int ID { get; private set; } = 0;

        [JsonProperty("Name")]
        public string Name { get; set; } = "";

        [JsonProperty("Discription")]
        public string Discription { get; private set; } = "";

        [JsonProperty("Width")]
        public int Width { get; private set; }
        [JsonProperty("Depth")]
        public int Depth { get; private set; }

        [JsonProperty("Squares")]
        public Square[,] Squares { get; private set; } = null;

        [JsonIgnore()]
        public string StageFileName { get { return Project.StageFolder + "\\" + Name + ".stage"; } }

        public Stage(Project project, string name, int width, int depth, int id)
        {
            Project = project;

            Name = name;
            Width = width;
            Depth = depth;

            Squares = new Square[Width, Depth];
            for (var i = 0; i < Width; i++)
                for (var j = 0; j < Depth; j++)
                    Squares[i, j] = new Square(this, new Point(i, j), "");

            ID = id;
        }

        public void SetSize(int width, int depth)
        {
            var newSquares = new Square[width, depth];

            for(var i = 0;i < Math.Min(Width, width);i++)
            {
                for(var j = 0;j < Math.Min(Depth, depth);j++)
                {
                    newSquares[i, j] = Squares[i, j];
                }
            }

            for(var i = 0;i < width;i++)
            {
                for(var j = 0;j < depth;j++)
                {
                    if(newSquares[i, j] == null)
                    {
                        var square = new Square(this, new Point(i, j), "");
                        newSquares[i, j] = square;
                    }
                }
            }

            Squares = newSquares;

            Width = width;
            Depth = depth;
        }

        public bool DoesContainsPoint(Point point)
        {
            if (point.X < 0 || Width <= point.X)
                return false;
            if (point.Y < 0 || Depth <= point.Y)
                return false;

            return true;
        }

        public static string Serialize(Stage stage)
        {
            string json = JsonConvert.SerializeObject(stage, Formatting.Indented);

            return json;
        }

        public static void Save(Stage stage)
        {
            string json = Serialize(stage);

            using (StreamWriter writer = new StreamWriter(stage.StageFileName, false, Encoding.UTF8))
            {
                writer.Write(json);
            }
        }

        public static Stage Deserialize(Project project, string json)
        {
            dynamic stageData = JsonConvert.DeserializeObject(json);
            var stage = new Stage(project, (string)stageData.Name.Value, (int)stageData.Width.Value, (int)stageData.Depth.Value, (int)stageData.ID.Value);
            foreach (dynamic squareLine in stageData.Squares)
                foreach (dynamic squareData in squareLine)
                {
                    dynamic position = squareData.Position;
                    var square = new Square(stage, new Point((double)position.X.Value, (double)position.Y.Value, (double)position.Z.Value), (string)squareData.Discription.Value);
                    square.SetTag((string)squareData.Tag.Value);
                    foreach (dynamic blockData in squareData.Blocks)
                    {
                        position = blockData.Position;
                        var block = new Block(square, new Point((double)position.X.Value, (double)position.Y.Value, (double)position.Z.Value), project.GetMaptip((string)blockData.Name.Value));
                        square.Blocks.Add(block);
                    }

                    stage.Squares[square.Position.RawX, square.Position.RawY] = square;
                }

            return stage;
        }

        public static Stage Load(Project project, string filename)
        {
            var json = "";
            using (StreamReader reader = new StreamReader(filename, Encoding.UTF8))
            {
                json = reader.ReadToEnd();
            }

            var stage = Deserialize(project, json);
            return stage;
        }
    }
}
