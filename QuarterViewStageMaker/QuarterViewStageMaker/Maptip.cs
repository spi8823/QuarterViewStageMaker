using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace QuarterViewStageMaker
{
    public class Maptip
    {
        public static int RhombusHorizontalWidthInPixels = 32;
        public static int RhombusVerticalWidthInPixels = 16;
        public static int HeightInPixels = 16;

        public string ImageFileName { get; private set; }
        public string Name { get; private set; }
        public BitmapImage Image { get; private set; }
        public double Height { get; private set; }
        public readonly int ImageWidth;
        public readonly int ImageHeight;

        public Maptip(string fileName)
        {
            ImageFileName = fileName;
            Name = Path.GetFileNameWithoutExtension(ImageFileName);

            Image = new BitmapImage(new Uri(ImageFileName)).Clone();
            Height = 1;
            ImageWidth = Image.PixelWidth;
            ImageHeight = Image.PixelHeight;
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

        public void SetHeight(double height)
        {
            Height = height;
        }

        public void Permeate()
        {
        }
    }
}
