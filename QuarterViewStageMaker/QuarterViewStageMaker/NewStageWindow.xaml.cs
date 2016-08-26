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
using System.Windows.Shapes;
using Reactive.Bindings;

namespace QuarterViewStageMaker
{
    /// <summary>
    /// NewStageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewStageWindow : Window
    {
        public ReactiveProperty<string> StageName { get; private set; } = new ReactiveProperty<string>("NewStage");
        public ReactiveProperty<int> StageWidth { get; private set; } = new ReactiveProperty<int>(16);
        public ReactiveProperty<int> StageHeight { get; private set; } = new ReactiveProperty<int>(16);

        public NewStageWindow()
        {
            InitializeComponent();

            StageNameBox.DataContext = this;
            StageWidthBox.DataContext = this;
            StageHeightBox.DataContext = this;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
