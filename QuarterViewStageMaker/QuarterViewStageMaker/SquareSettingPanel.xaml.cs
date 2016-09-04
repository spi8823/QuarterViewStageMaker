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
    /// SquareSettingPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class SquareSettingPanel : UserControl
    {
        public ReactiveProperty<Square> Square { get; private set; }

        public SquareSettingPanel(Square square)
        {
            InitializeComponent();

            Square = new ReactiveProperty<Square>(square);
            DataContext = Square.Value;

            SelectTagComboBox.ItemsSource = Square.Value.Stage.Project.Setting.Tags;
            SelectTagComboBox.Text = Square.Value.Tag;
            XLabel.Content = Square.Value.Position.RawX.ToString();
            YLabel.Content = Square.Value.Position.RawY.ToString();

            AddBlockSettingPanels();

            Square.PropertyChanged += Square_PropertyChanged;
        }

        private void AddBlockSettingPanels()
        {
            BlockInformationListPanel.Children.Clear();

            foreach (var block in Square.Value.Blocks)
            {
                var panel = new BlockSettingPanel(block);
                BlockInformationListPanel.Children.Add(panel);
            }
        }

        public void Square_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            AddBlockSettingPanels();
            SelectTagComboBox.ItemsSource = null;
            SelectTagComboBox.ItemsSource = Square.Value.Stage.Project.Setting.Tags;
            SelectTagComboBox.Text = Square.Value.Tag;
        }

        private void SelectTagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Square.Value.SetTag(SelectTagComboBox.SelectedItem as string);
        }
    }
}
