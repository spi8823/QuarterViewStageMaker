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
        public static readonly Size CanvasMargin = new Size(64, 200);

        #region Event
        public static readonly RoutedEvent EditedEvent = EventManager.RegisterRoutedEvent("Edited", RoutingStrategy.Bubble, typeof(EventHandler<StageEditedEventArgs>), typeof(StageCanvas));
        public static readonly RoutedEvent SquareSelectedEvent = EventManager.RegisterRoutedEvent("SquareSelected", RoutingStrategy.Bubble, typeof(EventHandler<SquareSelectedEventArgs>), typeof(StageCanvas));
        public static readonly RoutedEvent SelectedMaptipChangedEvent = EventManager.RegisterRoutedEvent("SelectedMaptipChanged", RoutingStrategy.Bubble, typeof(EventHandler<SelectedMaptipChangedEventArgs>), typeof(StageCanvas));

        public event EventHandler Edited
        {
            add { AddHandler(EditedEvent, value); }
            remove { RemoveHandler(EditedEvent, value); }
        }

        public event EventHandler SquareSelected
        {
            add { AddHandler(SquareSelectedEvent, value); }
            remove { RemoveHandler(SquareSelectedEvent, value); }
        }

        public event EventHandler SelectedMaptipChanged
        {
            add { AddHandler(SelectedMaptipChangedEvent, value); }
            remove { RemoveHandler(SelectedMaptipChangedEvent, value); }
        }

        public class StageEditedEventArgs : RoutedEventArgs
        {
            public bool CanUndo;
            public bool CanRedo;

            public StageEditedEventArgs(RoutedEvent routedEvent, object source, bool canUndo, bool canRedo)
                : base(routedEvent, source)
            {
                CanUndo = canUndo;
                CanRedo = canRedo;
            }
        }

        public class SquareSelectedEventArgs : RoutedEventArgs
        {
            public List<Square> SelectedSquares;

            public SquareSelectedEventArgs(RoutedEvent routedEvent, object source, List<Square> squares)
                : base(routedEvent, source)
            {
                SelectedSquares = squares;
            }
        }

        public class SelectedMaptipChangedEventArgs : RoutedEventArgs
        {
            public Maptip SelectedMaptip;

            public SelectedMaptipChangedEventArgs(RoutedEvent routedEvent, object source, Maptip maptip)
                : base(routedEvent, source)
            {
                SelectedMaptip = maptip;
            }
        }
        #endregion

        public Size DefaultSize;
        public Stage Stage = null;
        public Maptip SelectedMaptip = null;
        public bool IsReversed { get; private set; } = false;
        public double Magnification { get; private set; } = 1;

        private List<Square> _SelectedSquares = new List<Square>();
        private Image[,] _ProvisionalImages;
        private List<Line> _AimLines = new List<Line>();
        private List<Line> _SelectedSquaresAimLines = new List<Line>();
        private List<Block> _CopiedBlocks = new List<Block>();

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
            StageEdited();
            DrawStage();

            _UndoStack = new Stack<string>();
            _RedoStack = new Stack<string>();
        }

        public void DrawStage(bool redraw = false)
        {
            if (Stage == null)
                return;

            Cursor = Cursors.Wait;

            Width = Math.Max(DefaultSize.Width, Math.Max(Stage.Width, Stage.Depth) * Maptip.RhombusHorizontalWidthInPixels * Magnification + CanvasMargin.Width);
            Height = Math.Max(DefaultSize.Height, (Stage.Width + Stage.Depth) * Maptip.RhombusVerticalWidthInPixels / 2 * Magnification + CanvasMargin.Height);

            Canvas.Width = Width;
            Canvas.Height = Height;

            Canvas.Children.Clear();

            for (var i = 0; i <= Stage.Width; i++)
            {
                var start = new Point(i, 0).ToCanvasPosition(Canvas, Magnification);
                var end = new Point(i, Stage.Depth).ToCanvasPosition(Canvas, Magnification);

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
                var start = new Point(0, i).ToCanvasPosition(Canvas, Magnification);
                var end = new Point(Stage.Width, i).ToCanvasPosition(Canvas, Magnification);

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
                    AddMaptip(block, true);
                }
            }

            Cursor = Cursors.Arrow;
        }

        public void AddMaptip(Block block, bool redraw = false)
        {
            var image = block.Image;
            if (image.Parent != null)
                (image.Parent as Canvas).Children.Remove(image);
            Canvas.Children.Add(image);

            if (!redraw && block.IsImageInitialized)
                return;

            image.Width = block.Maptip.ImageWidth * Magnification;
            image.Height = block.Maptip.ImageHeight * Magnification;
            var drawPoint = (GetRealPoint(block.Position, true)).ToCanvasPosition(Canvas, Magnification);
            Canvas.SetLeft(image, drawPoint.X - image.Width / 2);
            Canvas.SetBottom(image, Canvas.Height - drawPoint.Y);
            Panel.SetZIndex(image, drawPoint.RawZ);

            block.IsImageInitialized = true;
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
                DrawAimLines(new List<Square>());
                return;
            }

            var pos = e.GetPosition(this as IInputElement);
            _DragStartPoint = GetRealPoint(new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas, Magnification), false);

            if (e.ChangedButton == MouseButton.Left)
            {
                if ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down)
                    _DragMode = DragMode.PartialSelect;
                else if ((Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) == KeyStates.Down)
                    _DragMode = DragMode.RegionSelect;
                else if ((Keyboard.GetKeyStates(Key.LeftAlt) & KeyStates.Down) == KeyStates.Down)
                    _DragMode = DragMode.Paste;
                else
                {
                    _DragMode = DragMode.Write;
                    _ProvisionalImages = new Image[Stage.Width, Stage.Depth];
                    DrawProvisionalBlocks(_DragStartPoint, _DragStartPoint);
                    UnselectSquares();
                    DrawAimLines(new List<Square>());
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if ((Keyboard.GetKeyStates(Key.LeftAlt) & KeyStates.Down) == KeyStates.Down)
                    _DragMode = DragMode.Spuit;
                else
                {
                    _DragMode = DragMode.Delete;
                    UnselectSquares();
                    DrawAimLines(new List<Square>());
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Stage == null)
                return;

            var pos = e.GetPosition(this as IInputElement);
            var nowPoint = GetRealPoint(new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas, Magnification), false);

            if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
                _DragMode = DragMode.None;

            if (_DragMode == DragMode.None || _DragMode == DragMode.Spuit || _DragMode == DragMode.Paste)
            {
                if (Stage.DoesContainsPoint(nowPoint))
                    DrawAimLines(new List<Square> { Stage.Squares[nowPoint.RawX, nowPoint.RawY] });
                else
                    DrawAimLines(new List<Square>());
                return;
            }
            else if (_DragMode != DragMode.Write)
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
            else if (_PreviousDragPoint != null && !(nowPoint.RawX == _PreviousDragPoint.RawX && nowPoint.RawY == _PreviousDragPoint.RawY))
            {
                DrawProvisionalBlocks(_DragStartPoint, nowPoint);
            }

            _PreviousDragPoint = nowPoint;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Stage == null)
                return;

            var pos = e.GetPosition(this as IInputElement);
            var nowPoint = GetRealPoint(new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas, Magnification), false);

            if (_DragMode == DragMode.None)
                return;

            if (_DragMode == DragMode.Write)
            {
                AddBlocks(_DragStartPoint, nowPoint);
                UnselectSquares();
            }
            else if (_DragMode == DragMode.Delete)
            {
                DeleteBlocks(_DragStartPoint, nowPoint);
                UnselectSquares();
            }
            else if (_DragMode == DragMode.PartialSelect)
            {
                SelectSquares(_DragStartPoint, nowPoint, true);
            }
            else if (_DragMode == DragMode.RegionSelect)
            {
                SelectSquares(_DragStartPoint, nowPoint);
            }
            else if (_DragMode == DragMode.Spuit)
            {
                if (Stage.DoesContainsPoint(nowPoint))
                {
                    var square = Stage.Squares[nowPoint.RawX, nowPoint.RawY];
                    RaiseEvent(new SelectedMaptipChangedEventArgs(SelectedMaptipChangedEvent, this, square.Blocks.Last().Maptip));
                }
            }
            else if (_DragMode == DragMode.Paste)
            {
                PasteBlocks(nowPoint);
            }

            if (Stage.DoesContainsPoint(nowPoint))
                DrawAimLines(new List<Square> { Stage.Squares[nowPoint.RawX, nowPoint.RawY] });
            else
                DrawAimLines(new List<Square>());

            _DragMode = DragMode.None;
            UnDrawProvisionalBlocks();
        }

        /// <summary>
        /// ステージにブロックを追加する
        /// 範囲は引数で指定された点から点への矩形部分
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
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

                    var block = Stage.Squares[i, j].AddBlock(SelectedMaptip);
                    AddMaptip(block);
                }
            }

            StageEdited();
        }

        public void InsertBlocks(int index)
        {
            if (Stage == null || SelectedMaptip == null)
                return;

            foreach (var square in _SelectedSquares)
            {
                var block = square.InsertBlock(SelectedMaptip, index);
            }
            DrawStage();
            StageEdited();
        }

        public void CopyBlocks(int index, int height)
        {
            Cursor = Cursors.Wait;

            _CopiedBlocks = new List<Block>();

            var minPoint = new Point(double.MaxValue, double.MaxValue);
            foreach (var square in _SelectedSquares)
            {
                if (square.Position.X < minPoint.X)
                    minPoint.X = square.Position.X;

                if (square.Position.Y < minPoint.Y)
                    minPoint.Y = square.Position.Y;
            }

            foreach (var square in _SelectedSquares)
            {
                for (var i = index; i < Math.Min(index + height, square.Blocks.Count); i++)
                {
                    var block = new Block(null, square.Blocks[i].Position - minPoint, square.Blocks[i].Maptip);
                    _CopiedBlocks.Add(block);
                }
            }

            Cursor = Cursors.Arrow;
        }

        public void PasteBlocks(int index)
        {
            Cursor = Cursors.Wait;

            var minPoint = new Point(double.MaxValue, double.MaxValue);
            foreach (var square in _SelectedSquares)
            {
                if (square.Position.X < minPoint.X)
                    minPoint.X = square.Position.X;

                if (square.Position.Y < minPoint.Y)
                    minPoint.Y = square.Position.Y;
            }

            foreach (var square in _SelectedSquares)
            {
                var position = square.Position - minPoint;
                var blocks = _CopiedBlocks.FindAll(block => block.Position.X == position.X && block.Position.Y == position.Y);
                blocks.Sort((a, b) => (int)((a.Position.Z - b.Position.Z) * 10));

                for (var i = 0; i < blocks.Count; i++)
                {
                    square.InsertBlock(blocks[i].Maptip, index + i);
                }
            }

            DrawStage();
            StageEdited();

            Cursor = Cursors.Arrow;
        }

        public void PasteBlocks(Point point)
        {
            Cursor = Cursors.Wait;

            var blocksBuff = new List<Block>(_CopiedBlocks);

            while (0 < blocksBuff.Count)
            {
                var blocks = new List<Block>() { blocksBuff[0] };
                blocksBuff.RemoveAt(0);

                if (blocksBuff.Count != 0)
                {
                    for (var i = blocks.Count - 1; -1 < i; i--)
                    {
                        if (blocksBuff[i].Position.X == blocks[0].Position.X && blocksBuff[i].Position.Y == blocks[0].Position.Y)
                        {
                            blocks.Add(blocksBuff[i]);
                            blocksBuff.RemoveAt(i);
                        }
                    }
                }

                if (!Stage.DoesContainsPoint(blocks[0].Position + point))
                    continue;

                var square = Stage.Squares[blocks[0].Position.RawX + point.RawX, blocks[0].Position.RawY + point.RawY];
                blocks.Sort((a, b) => (int)((a.Position.Z - b.Position.Z) * 10));

                var c = square.Blocks.Count;
                for (var i = 0; i < blocks.Count; i++)
                {
                    square.InsertBlock(blocks[i].Maptip, c + i);
                }
            }

            DrawStage();
            StageEdited();

            Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// 指定された点から点への矩形部分のブロックを一段削除する
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void DeleteBlocks(Point start, Point end)
        {
            if (Stage == null)
                return;

            var flag = false;

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
                    {
                        var block = square.Blocks[square.Blocks.Count - 1];
                        Canvas.Children.Remove(block.Image);
                        square.Blocks.Remove(block);

                        flag = true;
                    }
                }
            }

            if (flag)
                StageEdited();

            //DrawStage();
        }

        /// <summary>
        /// 選択中のマスのブロックをすべて削除する
        /// </summary>
        public void DeleteAllSelectedSquares()
        {
            if (Stage == null || SelectedMaptip == null || _SelectedSquares == null)
                return;

            var flag = false;
            foreach (var square in _SelectedSquares)
            {
                if (square.Blocks.Count != 0)
                {
                    foreach (var block in square.Blocks)
                        Canvas.Children.Remove(block.Image);
                    square.Blocks.Clear();
                    flag = true;
                }
            }

            if (flag)
            {
                StageEdited();
                //DrawStage();
                DrawSelectedSquaresAimLine();
            }
        }

        /// <summary>
        /// 選択中のマスに一段ずつブロックを追加する
        /// </summary>
        public void AddOneStep()
        {
            if (Stage == null || SelectedMaptip == null || _SelectedSquares == null)
                return;

            var flag = false;
            foreach (var square in _SelectedSquares)
            {
                var block = square.AddBlock(SelectedMaptip);
                AddMaptip(block);
                flag = true;
            }

            if (flag)
            {
                StageEdited();
                //DrawStage();
                DrawSelectedSquaresAimLine();
            }
        }

        /// <summary>
        /// 選択中のマスから一段ずつブロックを削除する
        /// </summary>
        public void DeleteOneStep()
        {
            if (Stage == null || _SelectedSquares == null)
                return;

            var flag = false;
            foreach (var square in _SelectedSquares)
            {
                if (square.Blocks.Count != 0)
                {
                    var block = square.Blocks[square.Blocks.Count - 1];
                    Canvas.Children.Remove(block.Image);
                    square.Blocks.Remove(block);
                    flag = true;
                }
            }

            if (flag)
            {
                StageEdited();
                //DrawStage();
                DrawSelectedSquaresAimLine();
            }
        }

        /// <summary>
        /// 選択中のマスの高さを揃える（低いほうに）
        /// </summary>
        public void Smooth()
        {
            if (Stage == null || SelectedMaptip == null || _SelectedSquares == null)
                return;

            var flag = false;

            var height = _SelectedSquares.Min(square => square.Height);

            foreach (var square in _SelectedSquares)
            {
                while (height < square.Height)
                {
                    var block = square.Blocks[square.Blocks.Count - 1];
                    Canvas.Children.Remove(block.Image);
                    square.Blocks.Remove(block);
                    flag = true;
                }
            }

            if (flag)
            {
                StageEdited();
                //DrawStage();
                DrawSelectedSquaresAimLine();
            }
        }

        /// <summary>
        /// 追加するブロックを仮表示する
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        private void DrawProvisionalBlocks(Point start, Point end)
        {
            if (Stage == null || SelectedMaptip == null)
                return;

            var minx = Math.Min(start.RawX, end.RawX);
            var maxx = Math.Max(start.RawX, end.RawX);
            var miny = Math.Min(start.RawY, end.RawY);
            var maxy = Math.Max(start.RawY, end.RawY);

            for(var i = 0;i < Stage.Width;i++)
            {
                for(var j = 0;j < Stage.Depth;j++)
                {
                    if(i < minx || maxx < i || j < miny || maxy < j)
                    {
                        if(_ProvisionalImages[i, j] != null)
                        {
                            Canvas.Children.Remove(_ProvisionalImages[i, j]);
                            _ProvisionalImages[i, j] = null;
                        }
                    }
                }
            }

            for (var i = minx; i <= maxx; i++)
            {
                if (i < 0 || Stage.Width <= i)
                    continue;
                for (var j = miny; j <= maxy; j++)
                {
                    if (j < 0 || Stage.Depth <= j)
                        continue;

                    if (_ProvisionalImages[i, j] != null)
                        continue;

                    var drawPoint = new Point(i, j, Stage.Squares[i, j].Height).ToCanvasPosition(Canvas, Magnification);
                    var image = new Image();
                    image.Source = SelectedMaptip.Image;
                    image.Width = SelectedMaptip.ImageWidth * Magnification;
                    image.Height = SelectedMaptip.ImageHeight * Magnification;
                    Canvas.Children.Add(image);
                    image.Visibility = Visibility.Visible;
                    Canvas.SetLeft(image, drawPoint.X - image.Width / 2);
                    Canvas.SetBottom(image, RenderSize.Height - drawPoint.Y);
                    Panel.SetZIndex(image, drawPoint.RawZ);

                    _ProvisionalImages[i, j] = image;
                }
            }
        }

        /// <summary>
        /// 仮表示中のブロックを削除する
        /// </summary>
        private void UnDrawProvisionalBlocks()
        {
            if (_ProvisionalImages == null)
                return;

            foreach (var image in _ProvisionalImages)
            {
                if (image != null)
                    Canvas.Children.Remove(image);
            }
            _ProvisionalImages = new Image[Stage.Width, Stage.Depth];
        }

        /// <summary>
        /// 操作の対象となるマスに目印を表示
        /// </summary>
        /// <param name="squares"></param>
        public void DrawAimLines(List<Square> squares)
        {
            foreach (var line in _AimLines)
            {
                Canvas.Children.Remove(line);
            }

            _AimLines = new List<Line>();
            foreach (var square in squares)
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
                    var start = GetRealPoint(startPoints[i], false).ToCanvasPosition(Canvas, Magnification);
                    var end = GetRealPoint(endPoints[i], false).ToCanvasPosition(Canvas, Magnification);
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
                    Panel.SetZIndex(line, int.MaxValue);
                    _AimLines.Add(line);
                }
            }
        }

        /// <summary>
        /// 選択中のマスに目印を表示
        /// </summary>
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
                    var start = GetRealPoint(startPoints[i], false).ToCanvasPosition(Canvas, Magnification);
                    var end = GetRealPoint(endPoints[i], false).ToCanvasPosition(Canvas, Magnification);
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

        /// <summary>
        /// マスを選択する
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="partial">部分選択するかどうか</param>
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

            RaiseEvent(new SquareSelectedEventArgs(SquareSelectedEvent, this, _SelectedSquares));
        }

        /// <summary>
        /// 選択を解除する
        /// </summary>
        public void UnselectSquares()
        {
            _SelectedSquares = new List<Square>();

            DrawSelectedSquaresAimLine();

            RaiseEvent(new SquareSelectedEventArgs(SquareSelectedEvent, this, _SelectedSquares));
        }

        /// <summary>
        /// 選択中のマスにタグをつける
        /// </summary>
        /// <param name="tag"></param>
        public void SetTags(string tag)
        {
            foreach (var square in _SelectedSquares)
            {
                square.SetTag(tag);
            }
        }

        /// <summary>
        /// ステージが変更されたときに実行する
        /// アンドゥとかリドゥのための処理
        /// </summary>
        public void StageEdited()
        {
            if (_NowStageJson != "")
            {
                _UndoStack.Push(_NowStageJson);
            }
            _NowStageJson = Stage.Serialize(Stage);

            _RedoStack = new Stack<string>();

            RaiseEvent(new StageEditedEventArgs(EditedEvent, this, _UndoStack.Count != 0, _RedoStack.Count != 0));
        }

        /// <summary>
        /// ステージが保存された状態に戻されたときに実行する処理
        /// </summary>
        public void StageReverted()
        {
            if (_NowStageJson != "")
            {
                _UndoStack.Push(_NowStageJson);
            }
            _NowStageJson = Stage.Serialize(Stage);
            _RedoStack = new Stack<string>();

            RaiseEvent(new StageEditedEventArgs(EditedEvent, this, _UndoStack.Count != 0, _RedoStack.Count != 0));
        }

        /// <summary>
        /// アンドゥする
        /// </summary>
        public Stage Undo()
        {
            if (_UndoStack.Count == 0)
                return null;
            _RedoStack.Push(_NowStageJson);
            string json = _UndoStack.Pop();
            Stage = Stage.Deserialize(Stage.Project, json);
            _NowStageJson = json;

            int b = 0;
            foreach (var square in Stage.Squares)
            {
                square.Blocks.ForEach(block => b++);
            }
            DrawStage();

            RaiseEvent(new StageEditedEventArgs(EditedEvent, this, !(_UndoStack.Count == 0), !(_RedoStack.Count == 0)));
            return Stage;
        }

        /// <summary>
        /// リドゥする
        /// </summary>
        public Stage Redo()
        {
            if (_RedoStack.Count == 0)
                return null;
            string json = _RedoStack.Pop();
            Stage = Stage.Deserialize(Stage.Project, json);
            _NowStageJson = json;
            _UndoStack.Push(_NowStageJson);

            DrawStage();

            RaiseEvent(new StageEditedEventArgs(EditedEvent, this, !(_UndoStack.Count == 0), !(_RedoStack.Count == 0)));

            return Stage;
        }

        public void SetReverse(bool reverse)
        {
            IsReversed = reverse;

            if (Stage == null)
                return;
            DrawStage(true);
            DrawSelectedSquaresAimLine();
        }

        public void SetMagnification(double magnification)
        {
            Magnification = magnification;

            if (Stage == null)
                return;
            DrawStage(true);
            DrawSelectedSquaresAimLine();
        }

        public Point GetRealPoint(Point point, bool isRaw)
        {
            if (IsReversed)
                if (isRaw)
                    return new Point(Stage.Width - point.X - 1, Stage.Depth - point.Y - 1, point.Z);
                else
                    return new Point(Stage.Width - point.X, Stage.Depth - point.Y, point.Z);
            else
                return point;
        }

        public enum DragMode
        {
            None, PartialSelect, RegionSelect, Write, Delete, Spuit, Paste
        }
    }
}
