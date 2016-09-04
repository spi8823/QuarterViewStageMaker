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
using Reactive.Bindings;

namespace QuarterViewStageMaker
{
    /// <summary>
    /// BlockSettingPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class BlockSettingPanel : UserControl
    {
        public ReactiveProperty<Block> Block;

        public BlockSettingPanel(Block block)
        {
            InitializeComponent();

            Block = new ReactiveProperty<Block>(block);
            DataContext = Block.Value;

            ZLabel.Content = Block.Value.Position.Z.ToString();
            BlockImage.Source = Block.Value.Image.Source;
        }
    }
}
