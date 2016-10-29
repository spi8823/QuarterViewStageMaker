using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;

namespace QuarterViewStageMaker
{
    public class Figure
    {
        public string ImageFileName;
        public BitmapImage Image;
        public string Name;
        public Point DefaultSize;
        public Point DefaultCenter;
        public readonly int ImageWidth;
        public readonly int ImageHeight;
        public string DefaultTag;

        public Figure(string filename)
        {
            ImageFileName = filename;
            Name = Path.GetFileNameWithoutExtension(ImageFileName);

            Image = new BitmapImage(new Uri(ImageFileName));

            ImageWidth = Image.PixelWidth;
            ImageHeight = Image.PixelHeight;

            var width = ImageWidth / (double)Maptip.RhombusHorizontalWidthInPixels;
            var height = ImageHeight / (double)Maptip.RhombusVerticalWidthInPixels;
            DefaultSize = new Point(width, width, height);
            DefaultCenter = new Point(width / 2, width / 2, height / 2);
        }

        public void SetName(string name)
        {
            string filename = Path.GetDirectoryName(ImageFileName) + "\\" + name + ".png";
            if (File.Exists(filename))
                return;

            File.Copy(ImageFileName, filename);
            File.Delete(ImageFileName);
            ImageFileName = filename;
            Name = name;
        }

        public void SetTag(string tag)
        {
            DefaultTag = tag;
        }

        public void DeleteFile()
        {
            Image = null;
            File.Delete(ImageFileName);
        }
    }
}
