using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuarterViewStageMaker
{
    /// <summary>
    /// MaptipSelectButton.xaml の相互作用ロジック
    /// </summary>
    public partial class MaptipSelectButton : UserControl
    {
        public static Size Size { get { return new Size(Maptip.RhombusHorizontalWidthInPixels + 4, Maptip.RhombusVerticalWidthInPixels + Maptip.HeightInPixels + 4); } }
        public Maptip Maptip { get; private set; }
        public event EventHandler Click;
        public bool Selected { get; private set; }

        public MaptipSelectButton(Maptip maptip, EventHandler clickEvent)
        {
            InitializeComponent();

            Maptip = maptip;
            RenderSize = new Size(Maptip.Image.PixelWidth, Maptip.Image.PixelHeight);

            MaptipImage.Source = Maptip.Image;

            Click += clickEvent;
        }

        public void Button_Click(object sender, EventArgs e)
        {
            Click(this, e);
        }

        public void Select()
        {
            if (Selected)
                return;

            Selected = true;
            Button.BorderBrush = Brushes.Red;
        }

        public void UnSelect()
        {
            if (!Selected)
                return;

            Selected = false;
            Button.BorderBrush = Brushes.White;
        }
    }
}
