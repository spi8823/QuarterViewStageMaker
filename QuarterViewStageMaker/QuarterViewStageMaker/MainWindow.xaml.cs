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
        public string StageBufferJson = "";
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
                SelectTagComboBox.ItemsSource = Project.Setting.Tags;
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

        private void CreateStage(object sender, EventArgs e)
        {
            if(StageCanvas.Updated)
            {
                var r = MessageBox.Show("ステージが編集されています。\n変更を保存しますか？", "確認", MessageBoxButton.YesNoCancel);
                if(r == MessageBoxResult.Yes)
                {
                    SaveStage(sender, e);
                }
                else if(r == MessageBoxResult.No)
                {
                    RevertStage();
                }
                else if(r == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

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
                StageSelectComboBox.ItemsSource = Project.Stages;
                StageSelectComboBox.UpdateLayout();
                StageSelectComboBox.SelectedIndex = StageSelectComboBox.Items.Count - 1;
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
            if(maptip == null)
            {
                SelectedMaptipImage.Source = null;
                SelectedMaptipImageNameBox.Text = "";
                SelectedMaptipImageHeightBox.Text = "";
                SaveMaptipSettingButton.IsEnabled = false;
                DeleteMaptipButton.IsEnabled = false;
                return;
            }
            SelectedMaptipImage.Source = maptip.Image;
            SelectedMaptipImageNameBox.Text = maptip.Name;
            SelectedMaptipImageHeightBox.Text = maptip.Height.ToString("0.0");
            DeleteMaptipButton.IsEnabled = true;
        }
        
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public void SaveStage(object sender, EventArgs e)
        {
            if (Stage == null)
                return;

            Stage.Save(Stage);
            StageBufferJson = Stage.Serialize(Stage);
        }

        private void StageSetting_Changed(object sender, EventArgs e)
        {
            SaveStageSettingButton.IsEnabled = true;
        }

        private void StageSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StageCanvas.Updated)
            {
                var result = MessageBox.Show("ステージが編集されています。\n変更を保存しますか？", "確認", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    Stage.Save(Stage);
                    SelectStage(StageSelectComboBox.SelectedItem as Stage);
                }
                else if (result == MessageBoxResult.No)
                {
                    RevertStage();
                    SelectStage(StageSelectComboBox.SelectedItem as Stage);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            else
            {
                SelectStage(StageSelectComboBox.SelectedItem as Stage);
            }
        }

        public void RevertStage()
        {
            var stage = Stage.Deserialize(Project, StageBufferJson);
            Project.Stages[Project.Stages.IndexOf(Stage)] = stage;
            Stage = stage;
            StageCanvas.Stage = Stage;
            StageCanvas.StageReverted();
        }

        private void SelectStage(Stage stage)
        {
            Stage = stage;
            StageBufferJson = Stage.Serialize(Stage);
            StageNameBox.Text = Stage.Name;
            StageWidthUpDown.Value = Stage.Width;
            StageDepthUpDown.Value = Stage.Depth;
            StageDiscriptionBox.Text = Stage.Discription;
            SaveStageSettingButton.IsEnabled = false;

            StageCanvas.SetStage(Stage);
        }

        private void SaveStageSettingButton_Click(object sender, EventArgs e)
        {
            Stage.Name = StageNameBox.Text;
            Stage.SetSize(StageWidthUpDown.Value ?? Stage.Width, StageDepthUpDown.Value ?? Stage.Depth);
            SaveStageSettingButton.IsEnabled = false;
            StageCanvas.StageEdited();
            StageCanvas.DrawStage();
        }

        private void Undo(object sender, EventArgs e)
        {
            if (Stage != null)
                StageCanvas.Undo();
        }

        private void Redo(object sender, EventArgs e)
        {
            if (Stage != null)
                StageCanvas.Redo();
        }

        private void DeleteMaptipButton_Click(object sender, EventArgs e)
        {
            if (SelectedMaptip == null)
                return;

            var result = MessageBox.Show("本当に削除しますか？", "確認", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
                return;

            Project.Maptips.Remove(SelectedMaptip);
            File.Delete(SelectedMaptip.ImageFileName);
            SelectedMaptip = null;
            ShowMaptipList();
            ShowMaptipData(null);
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            var tag = AddTagTextBox.Text;
            if(Project.Setting.Tags.Contains(tag))
            {
                MessageBox.Show("このタグはすでに登録されています。");
            }
            else
            {
                Project.Setting.Tags.Add(tag);
                Project.SaveSetting();
            }
        }

        private void SetSquaresTagButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.SetTags((string)SelectTagComboBox.SelectedItem);
        }

        private void DeleteAllSquaresButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.DeleteAllSelectedSquares();
        }

        private void SmoothSquaresButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.Smooth();
        }

        private void AddOneStepButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.AddOnStep();
        }

        private void DeleteOneStepButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.DeleteOneStep();
        }

        private void StageCanvas_Edited(object sender, StageCanvas.StageEditedEventArgs e)
        {
            UndoButton.IsEnabled = e.CanUndo;
            RedoButton.IsEnabled = e.CanRedo;
        }
    }
}
