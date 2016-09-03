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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (StageCanvas.Updated)
            {
                var result = MessageBox.Show("ステージが編集されています。\n変更を保存しますか？", "確認", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    Stage.Save(Stage);
                }
                else if (result == MessageBoxResult.No)
                {
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
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

        /// <summary>
        /// マップチップ素材ファイルをプロジェクトのMaptipsフォルダにコピーして使えるようにする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// マップチップがいっぱい並んでる素材を32×32の画像に分割して保存後、それぞれインポートする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// ステージを新規作成する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        
        /// <summary>
        /// ステージを保存する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SaveStage(object sender, EventArgs e)
        {
            if (Stage == null)
                return;

            Stage.Save(Stage);
            StageBufferJson = Stage.Serialize(Stage);
        }

        /// <summary>
        /// ステージを保存時の状態に戻す
        /// </summary>
        public void RevertStage()
        {
            var stage = Stage.Deserialize(Project, StageBufferJson);
            Project.Stages[Project.Stages.IndexOf(Stage)] = stage;
            Stage = stage;
            StageCanvas.Stage = Stage;
            StageCanvas.StageReverted();
        }

        /// <summary>
        /// ステージを選択
        /// </summary>
        /// <param name="stage"></param>
        private void SelectStage(Stage stage)
        {
            if (stage == null)
                return;

            Stage = stage;
            StageBufferJson = Stage.Serialize(Stage);
            StageNameBox.Text = Stage.Name;
            StageWidthUpDown.Value = Stage.Width;
            StageDepthUpDown.Value = Stage.Depth;
            StageDiscriptionBox.Text = Stage.Discription;
            SaveStageSettingButton.IsEnabled = false;

            StageCanvas.SetStage(Stage);
        }

        #region Maptip
        /// <summary>
        /// マップチップの一覧を表示する
        /// </summary>
        public void ShowMaptipList()
        {
            MaptipListCanvas.Children.Clear();
            MaptipListCanvas.Height = Math.Max(MaptipListViewer.Height, (Project.Maptips.Count / 6 + 1) * MaptipSelectButton.Size.Height);

            for (var i = 0; i < Project.Maptips.Count; i++)
            {
                var button = new MaptipSelectButton(Project.Maptips[i], MaptipSelected);
                MaptipListCanvas.Children.Add(button);

                Canvas.SetLeft(button, (i % 6) * MaptipSelectButton.Size.Width);
                Canvas.SetTop(button, (i / 6) * MaptipSelectButton.Size.Height);
            }

            MaptipListCanvas.UpdateLayout();
        }

        /// <summary>
        /// マップチップが選択されたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MaptipSelected(object sender, EventArgs e)
        {
            var button = sender as MaptipSelectButton;
            if (button == null)
                return;

            foreach (MaptipSelectButton item in MaptipListCanvas.Children)
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

        /// <summary>
        /// マップチップの情報を表示する
        /// </summary>
        /// <param name="maptip"></param>
        public void ShowMaptipData(Maptip maptip)
        {
            if (maptip == null)
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

        /// <summary>
        /// マップチップの情報が変更された
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaptipSetting_Changed(object sender, EventArgs e)
        {
            SaveMaptipSettingButton.IsEnabled = true;
        }

        /// <summary>
        /// マップチップの情報を保存するボタンのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveMaptipSettingButton_Click(object sender, RoutedEventArgs e)
        {
            Project.SaveMaptipSetting(SelectedMaptip, SelectedMaptipImageNameBox.Text, double.Parse(SelectedMaptipImageHeightBox.Text));
            SaveMaptipSettingButton.IsEnabled = false;
        }

        /// <summary>
        /// マップチップを削除するボタンが押されたイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteMaptipButton_Click(object sender, EventArgs e)
        {
            if (SelectedMaptip == null)
                return;

            var result = MessageBox.Show("本当に削除しますか？", "確認", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
                return;

            Project.Maptips.Remove(SelectedMaptip);
            SelectedMaptip.DeleteFile();
            SelectedMaptip = null;
            ShowMaptipList();
            ShowMaptipData(null);
        }
        #endregion

        #region StageSetting
        /// <summary>
        /// ステージの情報が編集されたイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StageSetting_Changed(object sender, EventArgs e)
        {
            SaveStageSettingButton.IsEnabled = true;
        }

        /// <summary>
        /// ステージの情報の編集を保存するイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveStageSettingButton_Click(object sender, EventArgs e)
        {
            File.Delete(Stage.StageFileName);
            var stage = Stage.Deserialize(Project, StageBufferJson);
            stage.Name = StageNameBox.Text;
            Stage.SetSize(StageWidthUpDown.Value ?? Stage.Width, StageDepthUpDown.Value ?? Stage.Depth);
            Stage.Save(stage);

            Stage.Name = StageNameBox.Text;
            Stage.SetSize(StageWidthUpDown.Value ?? Stage.Width, StageDepthUpDown.Value ?? Stage.Depth);
            StageSelectComboBox.ItemsSource = null;
            StageSelectComboBox.ItemsSource = Project.Stages;
            StageSelectComboBox.UpdateLayout();
            SaveStageSettingButton.IsEnabled = false;
            //StageCanvas.StageEdited();
            StageCanvas.DrawStage();
        }

        /// <summary>
        /// ステージ選択のコンボボックスの選択が変更されたイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        #endregion

        #region ステージ編集用
        /// <summary>
        /// アンドゥする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Undo(object sender, EventArgs e)
        {
            if (Stage != null)
                StageCanvas.Undo();
        }

        /// <summary>
        /// リドゥする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Redo(object sender, EventArgs e)
        {
            if (Stage != null)
                StageCanvas.Redo();
        }

        /// <summary>
        /// 使用できるタグを追加する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// 選択中のマスにタグをつける
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetSquaresTagButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.SetTags((string)SelectTagComboBox.SelectedItem);
        }

        /// <summary>
        /// 選択中のマス上にあるブロックをすべて削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteAllSquaresButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.DeleteAllSelectedSquares();
        }

        /// <summary>
        /// 選択中のマスの高さを揃える
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SmoothSquaresButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.Smooth();
        }

        /// <summary>
        /// 選択中のマスに一段ずつブロックを追加する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddOneStepButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.AddOneStep();
        }

        /// <summary>
        /// 選択中のマスから一段ずつブロックを削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteOneStepButton_Click(object sender, RoutedEventArgs e)
        {
            StageCanvas.DeleteOneStep();
        }

        /// <summary>
        /// ステージが編集されたときに実行される
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StageCanvas_Edited(object sender, StageCanvas.StageEditedEventArgs e)
        {
            UndoButton.IsEnabled = e.CanUndo;
            RedoButton.IsEnabled = e.CanRedo;
        }
        #endregion

        /// <summary>
        /// ライセンス用のウィンドウを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowLicenseWindow(object sender, RoutedEventArgs e)
        {
            var window = new LicenseWindow();
            window.ShowDialog();
        }
    }
}
