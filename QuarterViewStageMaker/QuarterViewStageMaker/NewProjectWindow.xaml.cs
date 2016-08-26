using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// NewProjectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewProjectWindow : Window
    {
        public ReactiveProperty<string> ProjectName { get; set; } = new ReactiveProperty<string>("NewProject");
        public ReactiveProperty<string> RootFolder { get; set; } = new ReactiveProperty<string>(App.ProjectsFolder);

        public NewProjectWindow()
        {
            InitializeComponent();

            ProjectNameBox.DataContext = this;
            RootFolderBox.DataContext = this;
            ProjectNameLabel.DataContext = this;
    }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }
    }
}
