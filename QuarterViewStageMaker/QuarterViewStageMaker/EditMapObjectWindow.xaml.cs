using Microsoft.Win32;
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

namespace QuarterViewStageMaker
{
    /// <summary>
    /// EditMapObjectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class EditMapObjectWindow : Window
    {
        Project Project;
        Stage Stage;
        Figure SelectedFigure;
        string StageBufferJson;

        public EditMapObjectWindow(Stage stage)
        {
            InitializeComponent();
            Stage = stage;
            Project = stage.Project;
            StageBufferJson = Stage.Serialize(stage);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MapObjectEditCanvas.MapObjectEdited += MapObjectEditCanvas_MapObjectEdited;
            MapObjectEditCanvas.SetStage(Stage);
            ShowFigureList();
        }

        /// <summary>
        /// マップチップの一覧を表示する
        /// </summary>
        public void ShowFigureList()
        {
            FigureListCanvas.Children.Clear();
            FigureListCanvas.Height = Math.Max(FigureListViewer.Height, (Project.Maptips.Count / 6 + 1) * MaptipSelectButton.Size.Height);

            for (var i = 0; i < Project.Figures.Count; i++)
            {
                var button = new FigureSelectButton(Project.Figures[i], FigureSelected);
                FigureListCanvas.Children.Add(button);

                Canvas.SetLeft(button, (i % 6) * MaptipSelectButton.Size.Width);
                Canvas.SetTop(button, (i / 6) * MaptipSelectButton.Size.Height);
            }

            FigureListCanvas.UpdateLayout();
        }

        public void FigureSelected(object sender, EventArgs e)
        {
            var button = sender as FigureSelectButton;
            var figure = button.Figure;
            foreach (FigureSelectButton b in FigureListCanvas.Children)
                b.UnSelect();
            button.Select();
            SelectedFigure = figure;
            MapObjectEditCanvas.SelectedFigure = figure;
            ShowFigureData(figure);
        }

        private void ShowFigureData(Figure figure)
        {
            if (figure == null)
            {
                SelectedFigureImageNameBox.Text = "";
                SelectedFigureImageTagBox.Text = "";
                return;
            }
            SelectedFigureImage.Source = figure.Image.Clone();
            SelectedFigureImageNameBox.Text = figure.Name;
            SelectedFigureImageTagBox.Text = figure.DefaultTag;
        }

        private void ImportFigures(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "pngファイル(*.png)|*.png",
                CheckFileExists = true,
                Multiselect = true,
                ReadOnlyChecked = true,
                Title = "",
                ShowReadOnly = false,
            };

            if (ofd.ShowDialog(this) ?? false)
            {
                var filenames = ofd.FileNames;
                foreach (var fileName in filenames)
                {
                    Project.ImportFigure(fileName);
                }

                ShowFigureList();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MapObjectEditCanvas.Canvas.Children.Clear();
            foreach(var mapObject in Stage.MapObjects)
            {
                mapObject.Refresh();
            }
        }

        private void FigureSetting_Changed(object sender, TextChangedEventArgs e)
        {
            SaveFigureSettingButton.IsEnabled = true;
        }

        private void DeleteFigureButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFigure == null)
                return;

            var result = MessageBox.Show("本当に削除しますか？", "確認", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
                return;

            MapObjectEditCanvas.DeleteMapObject(SelectedFigure);
            MapObjectEditCanvas.SelectedFigure = null;
            Project.Figures.Remove(SelectedFigure);
            SelectedFigure.DeleteFile();
            SelectedFigure = null;
            ShowFigureList();
            ShowFigureData(null);
        }

        private void SaveFigureSettingButton_Click(object sender, RoutedEventArgs e)
        {
            var nowStage = Stage.Deserialize(Project, StageBufferJson);

            Project.SaveFigureSetting(SelectedFigure, SelectedFigureImageNameBox.Text, SelectedFigureImageTagBox.Text);
            SaveFigureSettingButton.IsEnabled = false;

            Stage.Save(nowStage);
            foreach (var stage in Project.Stages)
            {
                if (stage != Stage)
                    Stage.Save(stage);
            }
        }

        private void MapObjectEditCanvas_MapObjectEdited(object sender, EventArgs e)
        {
            var m = e as MapObjectEditCanvas.MapObjectEditedEventArgs;

            MapObjectSettingPanel.ShowDatas(MapObjectEditCanvas.SelectedMapObject);
            SaveStageButton.IsEnabled = true;
            RedoButton.IsEnabled = m.CanRedo;
            UndoButton.IsEnabled = m.CanUndo;
        }

        private void Undo(object sender, RoutedEventArgs e)
        {
            if (Stage == null)
                return;

            Stage = MapObjectEditCanvas.Undo() ?? Stage;
            StageBufferJson = Stage.Serialize(Stage);
            SaveStageButton.IsEnabled = true;
        }

        private void Redo(object sender, RoutedEventArgs e)
        {
            if (Stage == null)
                return;

            Stage = MapObjectEditCanvas.Redo() ?? Stage;
            StageBufferJson = Stage.Serialize(Stage);
            SaveStageButton.IsEnabled = true;
        }

        private void SaveStage(object sender, RoutedEventArgs e)
        {
            if (Stage == null)
                return;

            SaveStageButton.IsEnabled = false;
            Stage.Save(Stage);
            StageBufferJson = Stage.Serialize(Stage);
        }

        private void ReverseCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (Stage == null)
                return;

            MapObjectEditCanvas.SetReverse(ReverseCheckBox.IsChecked.Value);
        }

        private void MagnificationDecideButton_Click(object sender, RoutedEventArgs e)
        {
            if (Stage == null)
                return;

            MapObjectEditCanvas.SetMagnification(MagnificationUpDown.Value ?? 1);
        }

        private void MapObjectSettingPanel_MapObjectSettingChanged(object sender, MapObjectSettingPanel.MapObjectSettingChangedEventArgs e)
        {
            MapObjectEditCanvas.SetPosition(e.MapObject.Image, e.MapObject.Position);
            MapObjectEditCanvas.Edited();
        }
    }
}
