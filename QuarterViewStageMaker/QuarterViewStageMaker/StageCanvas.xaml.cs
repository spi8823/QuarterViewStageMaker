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
    /// StageCanvas.xaml の相互作用ロジック
    /// </summary>
    public partial class StageCanvas : UserControl
    {
        public static readonly Size CanvasMargin = new Size(100, 200);

        public Size DefaultSize;
        public Stage Stage = null;
        public Maptip SelectedMaptip = null;

        private List<Square> _SelectedSquares = new List<Square>();
        private List<Image> _ProvisionalImages = new List<Image>();
        private List<Line> _AimLines = new List<Line>();
        private List<Line> _SelectedSquaresAimLines = new List<Line>();

        private Stack<string> _UndoStack = new Stack<string>();
        private Stack<string> _RedoStack = new Stack<string>();
        private string _NowStageJson = "";

        public StageCanvas()
        {
            InitializeComponent();
        }

        private void StageCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            DefaultSize = new Size(ActualWidth, ActualHeight);
        }

        public void SetStage(Stage stage)
        {
            Stage = stage;
            StageEditted();
            DrawStage();
        }

        public void DrawStage()
        {
            if (Stage == null)
                return;

            Cursor = Cursors.Wait;

            Width = Math.Max(DefaultSize.Width, Math.Max(Stage.Width, Stage.Depth) * Maptip.RhombusHorizontalWidthInPixels + CanvasMargin.Width); 
            Height = Math.Max(DefaultSize.Height, (Stage.Width + Stage.Depth) * Maptip.RhombusVerticalWidthInPixels / 2 + CanvasMargin.Height);

            Canvas.Width = Width;
            Canvas.Height = Height;

            Canvas.Children.Clear();
            foreach (var image in _ProvisionalImages)
                Canvas.Children.Add(image);

            for (var i = 0; i <= Stage.Width; i++)
            {
                var start = new Point(i, 0).ToCanvasPosition(Canvas);
                var end = new Point(i, Stage.Depth).ToCanvasPosition(Canvas);

                var line = new Line();
                line.X1 = start.X;
                line.Y1 = start.Y;
                line.X2 = end.X;
                line.Y2 = end.Y;
                line.StrokeThickness = 1;
                line.Stroke = Brushes.Green;
                line.MouseDown += Canvas_MouseDown;
                line.MouseMove += Canvas_MouseMove;
                line.MouseUp += Canvas_MouseUp;
                Canvas.Children.Add(line);
            }

            for (var i = 0; i <= Stage.Depth; i++)
            {
                var start = new Point(0, i).ToCanvasPosition(Canvas);
                var end = new Point(Stage.Width, i).ToCanvasPosition(Canvas);

                var line = new Line();
                line.X1 = start.X;
                line.Y1 = start.Y;
                line.X2 = end.X;
                line.Y2 = end.Y;
                line.StrokeThickness = 1;
                line.Stroke = Brushes.Green;
                line.MouseDown += Canvas_MouseDown;
                line.MouseMove += Canvas_MouseMove;
                line.MouseUp += Canvas_MouseUp;
                Canvas.Children.Add(line);
            }

            foreach (var square in Stage.Squares)
            {
                foreach (var block in square.Blocks)
                {
                    AddMaptip(block.Maptip, block.Position);
                }
            }

            Cursor = Cursors.Arrow;
        }

        public void AddMaptip(Maptip maptip, Point position)
        {
            var image = new Image();
            image.Source = maptip.Image;
            Canvas.Children.Add(image);

            var drawPoint = position.ToCanvasPosition(Canvas);
            Canvas.SetLeft(image, drawPoint.X - maptip.ImageWidth / 2);
            Canvas.SetBottom(image, Canvas.Height - drawPoint.Y);
            Panel.SetZIndex(image, drawPoint.RawZ);
        }

        private Point _DragStartPoint = null;
        private Point _PreviousDragPoint = null;
        private DragMode _DragMode = DragMode.None;

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Stage == null)
                return;

            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed)
            {
                _DragMode = DragMode.None;
                UnDrawProvisionalBlocks();
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down)
                    _DragMode = DragMode.PartialSelect;
                else if ((Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) == KeyStates.Down)
                    _DragMode = DragMode.RegionSelect;
                else
                {
                    _DragMode = DragMode.Write;
                    UnselectSquares();
                    DrawAimLines(new List<Square>());
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                _DragMode = DragMode.Delete;
                UnselectSquares();
                DrawAimLines(new List<Square>());
            }
            var pos = e.GetPosition(this as IInputElement);
            _DragStartPoint = new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Stage == null)
                return;

            var pos = e.GetPosition(this as IInputElement);
            var nowPoint = new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas);

            if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
                _DragMode = DragMode.None;

            if (_DragMode == DragMode.None)
            {
                if (Stage.DoesContainsPoint(nowPoint))
                    DrawAimLines(new List<Square> { Stage.Squares[nowPoint.RawX, nowPoint.RawY] });
                else
                    DrawAimLines(new List<Square>());
                return;
            }

            if(_PreviousDragPoint != null && !(nowPoint.RawX == _PreviousDragPoint.RawX && nowPoint.RawY == _PreviousDragPoint.RawY))
            {
                if (_DragMode == DragMode.Write)
                {
                    UnDrawProvisionalBlocks();
                    DrawProvisionalBlocks(_DragStartPoint, nowPoint);
                }
            }

            if (_DragMode != DragMode.Write)
            {
                var squares = new List<Square>();
                for (var i = Math.Max(0, Math.Min(_DragStartPoint.RawX, nowPoint.RawX)); i <= Math.Min(Stage.Width - 1, Math.Max(_DragStartPoint.RawX, nowPoint.RawX)); i++)
                {
                    for (var j = Math.Max(0, Math.Min(_DragStartPoint.RawY, nowPoint.RawY)); j <= Math.Min(Stage.Depth - 1, Math.Max(_DragStartPoint.RawY, nowPoint.RawY)); j++)
                    {
                        squares.Add(Stage.Squares[i, j]);
                    }
                }
                DrawAimLines(squares);
            }

            _PreviousDragPoint = nowPoint;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Stage == null)
                return;

            var pos = e.GetPosition(this as IInputElement);
            var nowPoint = new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas);

            if (_DragMode == DragMode.None)
                return;

            if (_DragMode == DragMode.Write)
            {
                AddBlocks(_DragStartPoint, nowPoint);
                StageEditted();
                UnselectSquares();
            }
            else if (_DragMode == DragMode.Delete)
            {
                DeleteBlocks(_DragStartPoint, nowPoint);
                StageEditted();
                UnselectSquares();
            }
            else if(_DragMode == DragMode.PartialSelect)
            {
                SelectSquares(_DragStartPoint, nowPoint, true);
            }
            else if(_DragMode == DragMode.RegionSelect)
            {
                SelectSquares(_DragStartPoint, nowPoint);
            }

            if (Stage.DoesContainsPoint(nowPoint))
                DrawAimLines(new List<Square> { Stage.Squares[nowPoint.RawX, nowPoint.RawY] });
            else
                DrawAimLines(new List<Square>());
            _DragMode = DragMode.None;
            UnDrawProvisionalBlocks();
        }

        private void AddBlocks(Point start, Point end)
        {
            if (Stage == null || SelectedMaptip == null)
                return;

            for (var i = Math.Min(start.RawX, end.RawX); i <= Math.Max(start.RawX, end.RawX); i++)
            {
                if (i < 0 || Stage.Width <= i)
                    continue;
                for (var j = Math.Min(start.RawY, end.RawY); j <= Math.Max(start.RawY, end.RawY); j++)
                {
                    if (j < 0 || Stage.Depth <= j)
                        continue;

                    AddMaptip(SelectedMaptip, new Point(i, j, Stage.Squares[i, j].Height));
                    Stage.Squares[i, j].AddBlock(SelectedMaptip);
                }
            }
        }

        private void DeleteBlocks(Point start, Point end)
        {
            if (Stage == null)
                return;

            for (var i = Math.Min(start.RawX, end.RawX); i <= Math.Max(start.RawX, end.RawX); i++)
            {
                if (i < 0 || Stage.Width <= i)
                    continue;
                for (var j = Math.Min(start.RawY, end.RawY); j <= Math.Max(start.RawY, end.RawY); j++)
                {
                    if (j < 0 || Stage.Depth <= j)
                        continue;

                    var square = Stage.Squares[i, j];
                    if (square.Blocks.Count != 0)
                        square.Blocks.RemoveAt(square.Blocks.Count - 1);
                }
            }

            DrawStage();
        }

        private void UnDrawProvisionalBlocks()
        {
            foreach (var image in _ProvisionalImages)
                Canvas.Children.Remove(image);

            _ProvisionalImages = new List<Image>();
        }

        private void DrawProvisionalBlocks(Point start, Point end)
        {
            if (Stage == null || SelectedMaptip == null)
                return;

            for(var i = Math.Min(start.RawX, end.RawX);i <= Math.Max(start.RawX, end.RawX);i++)
            {
                if (i < 0 || Stage.Width <= i)
                    continue;
                for(var j = Math.Min(start.RawY, end.RawY);j <= Math.Max(start.RawY, end.RawY);j++)
                {
                    if (j < 0 || Stage.Depth <= j)
                        continue;

                    var drawPoint = new Point(i, j, Stage.Squares[i, j].Height).ToCanvasPosition(Canvas);
                    var image = new Image();
                    image.Source = SelectedMaptip.Image;
                    Canvas.Children.Add(image);
                    image.Visibility = Visibility.Visible;
                    Canvas.SetLeft(image, drawPoint.X - SelectedMaptip.ImageWidth / 2);
                    Canvas.SetBottom(image, RenderSize.Height - drawPoint.Y);
                    Panel.SetZIndex(image, drawPoint.RawZ);

                    _ProvisionalImages.Add(image);
                }
            }
        }

        public void DrawAimLines(List<Square> squares)
        {
            foreach(var line in _AimLines)
            {
                Canvas.Children.Remove(line);
            }

            _AimLines = new List<Line>();
            foreach(var square in squares)
            {
                var lines = new Line[4];
                var startPoints = new Point[4]
                {
                    Point.UnitZ * square.Height + square.Position,
                    Point.UnitZ * square.Height + square.Position + Point.UnitX,
                    Point.UnitZ * square.Height + square.Position + Point.UnitX + Point.UnitY,
                    Point.UnitZ * square.Height + square.Position + Point.UnitY
                };
                var endPoints = new Point[4]
                {
                    Point.UnitZ * square.Height + square.Position + Point.UnitX,
                    Point.UnitZ * square.Height + square.Position + Point.UnitX + Point.UnitY,
                    Point.UnitZ * square.Height + square.Position + Point.UnitY,
                    Point.UnitZ * square.Height + square.Position
                };

                for(var i = 0;i < 4;i++)
                {
                    lines[i] = new Line();
                    var line = lines[i];
                    var start = startPoints[i].ToCanvasPosition(Canvas);
                    var end = endPoints[i].ToCanvasPosition(Canvas);
                    line.X1 = start.RawX;
                    line.Y1 = start.RawY;
                    line.X2 = end.RawX;
                    line.Y2 = end.RawY;
                    line.StrokeThickness = 1;
                    line.Stroke = Brushes.Blue;
                    line.MouseDown += Canvas_MouseDown;
                    line.MouseMove += Canvas_MouseMove;
                    line.MouseUp += Canvas_MouseUp;
                    Canvas.Children.Add(line);
                    Panel.SetZIndex(line, 1000);
                    _AimLines.Add(line);
                }
            }
        }

        public void DrawSelectedSquaresAimLine()
        {
            foreach (var line in _SelectedSquaresAimLines)
                Canvas.Children.Remove(line);

            _SelectedSquaresAimLines = new List<Line>();

            foreach (var square in _SelectedSquares)
            {
                var lines = new Line[4];
                var startPoints = new Point[4]
                {
                    Point.UnitZ * square.Height + square.Position,
                    Point.UnitZ * square.Height + square.Position + Point.UnitX,
                    Point.UnitZ * square.Height + square.Position + Point.UnitX + Point.UnitY,
                    Point.UnitZ * square.Height + square.Position + Point.UnitY
                };
                var endPoints = new Point[4]
                {
                    Point.UnitZ * square.Height + square.Position + Point.UnitX,
                    Point.UnitZ * square.Height + square.Position + Point.UnitX + Point.UnitY,
                    Point.UnitZ * square.Height + square.Position + Point.UnitY,
                    Point.UnitZ * square.Height + square.Position
                };

                for (var i = 0; i < 4; i++)
                {
                    lines[i] = new Line();
                    var line = lines[i];
                    var start = startPoints[i].ToCanvasPosition(Canvas);
                    var end = endPoints[i].ToCanvasPosition(Canvas);
                    line.X1 = start.RawX;
                    line.Y1 = start.RawY;
                    line.X2 = end.RawX;
                    line.Y2 = end.RawY;
                    line.StrokeThickness = 1;
                    line.Stroke = Brushes.Red;
                    line.MouseDown += Canvas_MouseDown;
                    line.MouseMove += Canvas_MouseMove;
                    line.MouseUp += Canvas_MouseUp;
                    Canvas.Children.Add(line);
                    Panel.SetZIndex(line, 1000);
                    _SelectedSquaresAimLines.Add(line);
                }
            }
        }

        public void SelectSquares(Point start, Point end, bool partial = false)
        {
            if (!partial)
                _SelectedSquares = new List<Square>();

            for (var i = Math.Max(0, Math.Min(start.RawX, end.RawX)); i <= Math.Min(Stage.Width - 1, Math.Max(start.RawX, end.RawX)); i++)
            {
                for (var j = Math.Max(0, Math.Min(start.RawY, end.RawY)); j <= Math.Min(Stage.Depth - 1, Math.Max(start.RawY, end.RawY)); j++)
                {
                    var square = Stage.Squares[i, j];

                    if (partial && _SelectedSquares.Contains(square))
                        _SelectedSquares.Remove(square);
                    else
                        _SelectedSquares.Add(square);
                }
            }

            DrawSelectedSquaresAimLine();
        }
    
        public void UnselectSquares()
        {
            _SelectedSquares = new List<Square>();

            DrawSelectedSquaresAimLine();
        }

        public void StageEditted()
        {
            if(_NowStageJson != "")
            {
                _UndoStack.Push(_NowStageJson);
            }
            _NowStageJson = Stage.Serialize(Stage);

            _RedoStack = new Stack<string>();
        }

        public void Undo()
        {
            if (_UndoStack.Count == 0)
                return;
            _RedoStack.Push(_NowStageJson);
            string json = _UndoStack.Pop();
            Stage = Stage.Deserialize(Stage.Project, json);
            _NowStageJson = json;

            int b = 0;
            foreach(var square in Stage.Squares)
            {
                square.Blocks.ForEach(block => b++);
            }
            Console.WriteLine("Blocks : " + b);
            DrawStage();
        }

        public void Redo()
        {
            if (_RedoStack.Count == 0)
                return;
            string json = _RedoStack.Pop();
            Stage = Stage.Deserialize(Stage.Project, json);
            _NowStageJson = json;
            _UndoStack.Push(_NowStageJson);

            DrawStage();
        }

        public enum DragMode
        {
            None, PartialSelect, RegionSelect, Write, Delete, 
        }
    }
}
