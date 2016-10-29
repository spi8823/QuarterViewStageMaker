using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
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
    public class Project
    {
        public readonly string Name;
        public readonly string ProjectFolder;
        public string StageFolder { get { return ProjectFolder + "\\" + "Stage"; } }
        public string MaptipFolder { get { return ProjectFolder + "\\" + "Maptip"; } }
        public string FigureFolder { get { return ProjectFolder + "\\" + "MapObject"; } }
        public string ProjectSettingFileName { get { return ProjectFolder + "\\Setting.json"; } }
        public List<Maptip> Maptips = new List<Maptip>();
        public List<Figure> Figures = new List<Figure>();
        public List<Stage> Stages = new List<Stage>();
        public ProjectSetting Setting { get; private set; }

        public Project(string projectFolder)
        {
            Name = projectFolder;

            ProjectFolder = projectFolder;

            foreach(var folder in new[] { ProjectFolder, StageFolder, MaptipFolder, FigureFolder})
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }

            Setting = ProjectSetting.Load(ProjectSettingFileName);
            LoadMaptips();
            LoadFigures();
            LoadStages();
        }

        public void SaveSetting()
        {
            ProjectSetting.Save(Setting, ProjectSettingFileName);
        }

        public void LoadStages()
        {
            Stages = new List<Stage>();
            foreach (var filename in Directory.GetFiles(StageFolder, "*.stage"))
            {
                var stage = Stage.Load(this, filename);
                Stages.Add(stage);
            }
        }

        public void LoadMaptips()
        {
            Maptips = new List<Maptip>();
            foreach(var filename in Directory.GetFiles(MaptipFolder, "*.png"))
            {
                var maptip = new Maptip(filename);
                if (Setting.MaptipHeights.ContainsKey(maptip.Name))
                    maptip.SetHeight(Setting.MaptipHeights[maptip.Name]);
                Maptips.Add(maptip);
            }
        }

        public void LoadFigures()
        {
            Figures = new List<Figure>();
            foreach(var filename in Directory.GetFiles(FigureFolder, "*.png"))
            {
                var figure = new Figure(filename);
                if (Setting.FigureTags.ContainsKey(figure.Name))
                    figure.SetTag(Setting.FigureTags[figure.Name]);
                Figures.Add(figure);
            }
        }

        public Stage CreateStage(string name, int width, int depth)
        {
            int id = Stages.Count;
            while (Stages.Any(s => s.ID == id))
                id++;
            var stage = new Stage(this, name, width, depth, id);
            Stages.Add(stage);
            Stage.Save(stage);

            return stage;
        }

        public void ImportStage(string filename)
        {

        }

        public void SaveMaptipSetting(Maptip maptip, string name, double height)
        {
            if (Setting.MaptipHeights.ContainsKey(maptip.Name))
                Setting.MaptipHeights.Remove(maptip.Name);

            Setting.MaptipHeights.Add(name, height);

            maptip.SetName(name);
            maptip.SetHeight(height);

            ProjectSetting.Save(Setting, ProjectSettingFileName);
        }

        public void SaveFigureSetting(Figure figure, string name, string tag)
        {
            if (Setting.FigureTags.ContainsKey(figure.Name))
                Setting.FigureTags.Remove(figure.Name);

            Setting.FigureTags.Add(name, tag);

            figure.SetName(name);
            figure.SetTag(tag);

            ProjectSetting.Save(Setting, ProjectSettingFileName);
        }

        public Maptip GetMaptip(string Name)
        {
            foreach(var maptip in Maptips)
            {
                if (maptip.Name == Name)
                    return maptip;
            }

            return null;
        }

        public Figure GetFigure(string name)
        {
            foreach(var figure in Figures)
            {
                if (figure.Name == name)
                    return figure;
            }

            return null;
        }

        public void ImportMaptip(string sourceFileName)
        {
            if (Path.GetExtension(sourceFileName) != ".png")
                return;

            var fileName = MaptipFolder + "\\" + Path.GetFileName(sourceFileName);
            File.Copy(sourceFileName, fileName, true);

            Maptips.Add(new Maptip(fileName));
        }

        public void ImportFigure(string sourceFileName)
        {
            if (Path.GetExtension(sourceFileName) != ".png")
                return;

            var filename = FigureFolder + "\\" + Path.GetFileName(sourceFileName);
            File.Copy(sourceFileName, filename, true);

            Figures.Add(new Figure(filename));
         }

        public void ImportSlicedMaptip(string sourceFileName)
        {
            var imageList = new List<CroppedBitmap>();

            var sourceImage = new BitmapImage(new Uri(sourceFileName));

            for (var i = 0; i < sourceImage.PixelWidth / Maptip.RhombusHorizontalWidthInPixels; i++)
            {
                for (var j = 0; j < sourceImage.PixelHeight / (Maptip.RhombusVerticalWidthInPixels + Maptip.HeightInPixels); j++)
                {
                    var croppedImage = new CroppedBitmap(sourceImage, 
                        new System.Windows.Int32Rect(
                            i * Maptip.RhombusHorizontalWidthInPixels, 
                            j * (Maptip.RhombusVerticalWidthInPixels + Maptip.HeightInPixels), 
                            Maptip.RhombusHorizontalWidthInPixels, 
                            Maptip.RhombusVerticalWidthInPixels + Maptip.HeightInPixels));
                    imageList.Add(croppedImage);
                }
            }

            var maptips = new List<Maptip>();
            for (var i = 0; i < imageList.Count; i++)
            {
                var fileName = MaptipFolder + "\\" + Path.GetFileNameWithoutExtension(sourceFileName) + "_" + i.ToString() + ".png";

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imageList[i]));

                using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(stream);
                }

                maptips.Add(new Maptip(fileName));
            }

            Maptips.AddRange(maptips);
        }

    }

    [JsonObject()]
    public class ProjectSetting
    {
        [JsonProperty("MaptipHeights")]
        public Dictionary<string, double> MaptipHeights { get; set; }

        [JsonProperty("FigureTags")]
        public Dictionary<string, string> FigureTags { get; set; }

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; }

        public ProjectSetting()
        {
            MaptipHeights = new Dictionary<string, double>();
            FigureTags = new Dictionary<string, string>();
            Tags = new List<string>();
        }

        public static ProjectSetting Load(string filename)
        {
            string json = "";
            try
            {
                using (StreamReader reader = new StreamReader(filename, Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(json))
                return new ProjectSetting();

            try
            {
                return JsonConvert.DeserializeObject<ProjectSetting>(json);
            }
            catch
            {
                return new ProjectSetting();
            }
        }

        public static void Save(ProjectSetting setting, string filename)
        {
            string json = JsonConvert.SerializeObject(setting);

            try
            {
                using (StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
                    writer.Write(json);
            }
            catch { }
        }
    }
}
