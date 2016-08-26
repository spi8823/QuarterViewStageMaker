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
using System.IO;
using Microsoft.Win32;

namespace QuarterViewStageMaker
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public Project Project = null;
        public Stage Stage = null;
        public Maptip SelectedMaptip = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var projectFolder = Properties.Settings.Default.ProjectFolder;
            if(!string.IsNullOrWhiteSpace(projectFolder) && Directory.Exists(projectFolder))
            {
                Project = new Project(projectFolder);
                ShowMaptipList();
            }

            if (Project == null)
                NewProject(this, null);

            if (Project == null)
                Close();
            else
            {
                Properties.Settings.Default.ProjectFolder = Project.ProjectFolder;
                Properties.Settings.Default.Save();
                StageSelectComboBox.ItemsSource = Project.Stages;
            }
        }

        private void NewProject(object sender, EventArgs e)
        {
            var window = new NewProjectWindow();
            var result = window.ShowDialog();

            if(result ?? false)
            {
                var project = new Project(window.RootFolder.Value + "\\" + window.ProjectName.Value);
                Project = project;
            }
        }

        private void OpenProject(object sender, EventArgs e)
        {
        }

        private void ImportMaptips(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "pngファイル(*.png)|*.png",
                CheckFileExists = true,
                Multiselect= true,
                ReadOnlyChecked = true,
                Title = "",
                ShowReadOnly = false,
            };

            if(ofd.ShowDialog(this) ?? false)
            {
                var filenames = ofd.FileNames;
                foreach(var fileName in filenames)
                {
                    Project.ImportMaptip(fileName);
                }

                ShowMaptipList();
            }
        }

        private void ImportSlicedMaptip(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "pngファイル(*.png)|*.png",
            };

            if(ofd.ShowDialog(this) ?? false)
            {
                var filenames = ofd.FileNames;
                foreach(var fileName in filenames)
                {
                    Project.ImportSlicedMaptip(fileName);
                }
            }

            ShowMaptipList();
        }

        private void NewStage(object sender, EventArgs e)
        {
            var window = new NewStageWindow();

            var result = window.ShowDialog();

            if(result ?? false)
            {
                var stageName = window.StageName.Value;
                if(Project.Stages.Exists(s => s.Name == stageName))
                {
                    int i = 0;
                    while(Project.Stages.Exists(s => s.Name == stageName + "_" + i.ToString()))
                    {
                        i++;
                    }
                    stageName = stageName + "_" + i.ToString();
                }
                var stage = Project.CreateStage(stageName, window.StageWidth.Value, window.StageHeight.Value);
                SelectStage(stage);
            }
        }

        public void ShowMaptipList()
        {
            MaptipListCanvas.Children.Clear();
            MaptipListCanvas.Height = Math.Max(MaptipListViewer.Height, (Project.Maptips.Count / 6 + 1) * MaptipSelectButton.Size.Height);

            for(var i = 0;i < Project.Maptips.Count;i++)
            {
                var button = new MaptipSelectButton(Project.Maptips[i], MaptipSelected);
                MaptipListCanvas.Children.Add(button);

                Canvas.SetLeft(button, (i % 6) * MaptipSelectButton.Size.Width);
                Canvas.SetTop(button, (i / 6) * MaptipSelectButton.Size.Height);
                Console.WriteLine(button.RenderSize);
            }
        }

        public void MaptipSelected(object sender, EventArgs e)
        {
            var button = sender as MaptipSelectButton;
            if (button == null)
                return;

            foreach(MaptipSelectButton item in MaptipListCanvas.Children)
            {
                if (item != button)
                    item.UnSelect();
            }

            button.Select();
            SelectedMaptip = button.Maptip;
            ShowMaptipData(button.Maptip);
            SaveMaptipSettingButton.IsEnabled = false;
            StageCanvas.SelectedMaptip = SelectedMaptip;
        }

        private void MaptipSetting_Changed(object sender, EventArgs e)
        {
            SaveMaptipSettingButton.IsEnabled = true;
        }

        private void SaveMaptipSettingButton_Click(object sender, RoutedEventArgs e)
        {
            Project.SaveMaptipSetting(SelectedMaptip, SelectedMaptipImageNameBox.Text, double.Parse(SelectedMaptipImageHeightBox.Text));
            SaveMaptipSettingButton.IsEnabled = false;
        }

        public void ShowMaptipData(Maptip maptip)
        {
            SelectedMaptipImage.Source = maptip.Image;
            SelectedMaptipImageNameBox.Text = maptip.Name;
            SelectedMaptipImageHeightBox.Text = maptip.Height.ToString("0.0");
        }
        
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public void SaveStage(object sender, EventArgs e)
        {
            Stage.Save(Stage);
        }

        private void BrowseStageButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveStageSettingButton_Click(object sender, EventArgs e)
        {

        }

        private void StageSetting_Changed(object sender, EventArgs e)
        {
            SaveStageSettingButton.IsEnabled = true;
        }

        private void StageSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectStage(StageSelectComboBox.SelectedItem as Stage);
        }

        private void SelectStage(Stage stage)
        {
            Stage = stage;
            StageNameBox.Text = Stage.Name;
            StageWidthUpDown.Value = Stage.Width;
            StageDepthUpDown.Value = Stage.Depth;
            StageDiscriptionBox.Text = Stage.Discription;
            SaveStageSettingButton.IsEnabled = false;

            StageCanvas.SetStage(Stage);
        }

        private void SaveStageSettingButton_Click(object sender, RoutedEventArgs e)
        {
            Stage.Name = StageNameBox.Text;
            Stage.SetSize(StageWidthUpDown.Value ?? Stage.Width, StageDepthUpDown.Value ?? Stage.Depth);
            SaveStageSettingButton.IsEnabled = false;

            StageCanvas.DrawStage();
        }

        private void PermeateMaptipButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedMaptip.Permeate();
            ShowMaptipData(SelectedMaptip);
        }

        private void Undo(object sender, RoutedEventArgs e)
        {
            if (Stage != null)
                StageCanvas.Undo();
        }

        private void Redo(object sender, RoutedEventArgs e)
        {
            if (Stage != null)
                StageCanvas.Redo();
        }
    }
}
