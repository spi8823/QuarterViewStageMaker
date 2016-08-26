using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuarterViewStageMaker
{
    [JsonObject("Stage")]
    public class Stage
    {
        [JsonIgnore()]
        public Project Project;

        [JsonProperty("Name")]
        public string Name { get; set; } = "";

        [JsonProperty("Width")]
        public int Width { get; private set; }
        [JsonProperty("Depth")]
        public int Depth { get; private set; }

        [JsonProperty("Squares")]
        public Square[,] Squares { get; private set; } = null;

        [JsonProperty("Discription")]
        public string Discription { get; private set; } = "";

        public Stage(Project project, string name, int width = 16, int depth = 16)
        {
            Project = project;

            Name = name;
            Width = width;
            Depth = depth;

            Squares = new Square[Width, Depth];
            for (var i = 0; i < Width; i++)
                for (var j = 0; j < Depth; j++)
                    Squares[i, j] = new Square(this, new Point(i, j), "");
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

            var filename = stage.Project.StageFolder + "\\" + stage.Name + ".stage";
            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                writer.Write(json);
            }
        }

        public static Stage Deserialize(Project project, string json)
        {
            dynamic stageData = JsonConvert.DeserializeObject(json);
            var stage = new Stage(project, (string)stageData.Name.Value, (int)stageData.Width.Value, (int)stageData.Depth.Value);
            foreach (dynamic squareLine in stageData.Squares)
                foreach (dynamic squareData in squareLine)
                {
                    dynamic position = squareData.Position;
                    var square = new Square(stage, new Point((double)position.X.Value, (double)position.Y.Value, (double)position.Z.Value), (string)squareData.Discription.Value);
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
