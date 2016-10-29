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
    /// FigureSelectButton.xaml の相互作用ロジック
    /// </summary>
    public partial class FigureSelectButton : UserControl
    {
        public static readonly Size Size = new Size(36, 36);
        public Figure Figure { get; private set; }
        public event EventHandler Click;
        public bool Selected;

        public FigureSelectButton(Figure figure, EventHandler clickEvent)
        {
            InitializeComponent();
            Figure = figure;
            RenderSize = Size;
            FigureImage.Source = Figure.Image.Clone();
            Click += clickEvent;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
