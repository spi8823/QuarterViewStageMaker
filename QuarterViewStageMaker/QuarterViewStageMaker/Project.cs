using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace QuarterViewStageMaker
{
    public class Project
    {
        public readonly string Name;
        public readonly string ProjectFolder;
        public string StageFolder { get { return ProjectFolder + "\\" + "Stage"; } }
        public string MaptipFolder { get { return ProjectFolder + "\\" + "Maptip"; } }
        public string MaptipHeightsFileName { get { return MaptipFolder + "\\MaptipHeights.json"; } }
        public string ProjectSettingFileName { get { return ProjectFolder + "\\Setting.json"; } }
        public List<Maptip> Maptips = new List<Maptip>();
        public List<Stage> Stages = new List<Stage>();
        public ProjectSetting Setting { get; private set; }

        public Project(string projectFolder)
        {
            Name = projectFolder;

            ProjectFolder = projectFolder;
            if (!Directory.Exists(ProjectFolder))
                Directory.CreateDirectory(ProjectFolder);

            if (!Directory.Exists(StageFolder))
                Directory.CreateDirectory(StageFolder);
            if (!Directory.Exists(MaptipFolder))
                Directory.CreateDirectory(MaptipFolder);

            Setting = ProjectSetting.Load(ProjectSettingFileName);
            LoadMaptips();
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

            try
            {
                using (StreamWriter writer = new StreamWriter(MaptipHeightsFileName, false, Encoding.UTF8))
                {
                    var json = JsonConvert.SerializeObject(Setting.MaptipHeights, Formatting.Indented);
                    writer.Write(json);
                }
            }
            catch { }

            maptip.SetName(name);
            maptip.SetHeight(height);

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

        public void ImportMaptip(string sourceFileName)
        {
            if (Path.GetExtension(sourceFileName) != ".png")
                return;

            var fileName = MaptipFolder + "\\" + Path.GetFileName(sourceFileName);
            File.Copy(sourceFileName, fileName, true);

            Maptips.Add(new Maptip(fileName));
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
        }

    }

    [JsonObject()]
    public class ProjectSetting
    {
        [JsonProperty("MaptipHeights")]
        public Dictionary<string, double> MaptipHeights { get; set; }

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; }

        public ProjectSetting()
        {
            MaptipHeights = new Dictionary<string, double>();
            Tags = new List<string>();
        }

        public ProjectSetting(Dictionary<string, double> maptipheights, List<string> tags)
        {
            MaptipHeights = maptipheights;
            Tags = tags;
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
