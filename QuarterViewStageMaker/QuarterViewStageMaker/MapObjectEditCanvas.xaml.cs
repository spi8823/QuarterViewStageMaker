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
    /// MapObjectEditCanvas.xaml の相互作用ロジック
    /// </summary>
    public partial class MapObjectEditCanvas : UserControl
    {
        public static readonly RoutedEvent MapObjectEditedEvent = EventManager.RegisterRoutedEvent("MapObjectEdited", RoutingStrategy.Bubble, typeof(EventHandler<MapObjectEditedEventArgs>), typeof(StageCanvas));
        public event RoutedEventHandler MapObjectEdited
        {
            add { AddHandler(MapObjectEditedEvent, value); }
            remove { RemoveHandler(MapObjectEditedEvent, value); }
        }

        public class MapObjectEditedEventArgs : RoutedEventArgs
        {
            public bool CanUndo;
            public bool CanRedo;

            public MapObjectEditedEventArgs(RoutedEvent routedEvent, object source, bool canUndo, bool canRedo)
                : base(routedEvent, source)
            {
                CanUndo = canUndo;
                CanRedo = canRedo;
            }
        }

        public static readonly Size CanvasMargin = new Size(64, 200);

        public Size DefaultSize;
        public Stage Stage;
        public Figure SelectedFigure;
        public bool IsReversed { get; private set; } = false;
        public double Magnification { get; private set; } = 1;
        public MapObject SelectedMapObject = null;

        private List<Image> _BlockImages = new List<Image>();
        private List<Image> _MapObjectImages = new List<Image>();
        private Image _ProvisionalImage;
        private List<Line> _AimLines = new List<Line>();
        private DragMode _DragMode;

        private Stack<string> _UndoStack = new Stack<string>();
        private Stack<string> _RedoStack = new Stack<string>();
        private string _NowStageJson = "";

        public MapObjectEditCanvas()
        {
            InitializeComponent();
        }

        private void MapObjectEditCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            DefaultSize = new Size(ActualWidth, ActualHeight);
        }

        public void SetStage(Stage stage)
        {
            Stage = stage;
            Edited();
            DrawStage();
            DrawMapObjects();

            _UndoStack = new Stack<string>();
            _RedoStack = new Stack<string>();
        }

        public void Edited()
        {
            RaiseEvent(new MapObjectEditedEventArgs(MapObjectEditedEvent, this, _UndoStack.Count != 0, _RedoStack.Count != 0));
        }

        public void DrawStage()
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
                    AddMaptip(block);
                }
            }

            Cursor = Cursors.Arrow;
        }

        private void AddMaptip(Block block)
        {
            var image = block.Image;
            Canvas.Children.Add(image);

            SetSize(block, Magnification);
            SetPosition(block.Image, block.Position);
        }

        private void AddMapObject(MapObject mapObject)
        {
            var image = mapObject.Image;
            Canvas.Children.Add(image);
            _MapObjectImages.Add(image);

            SetSize(mapObject, Magnification);
            SetPosition(mapObject.Image, mapObject.Position);

            image.MouseDown -= MapObject_MouseDown;
            image.MouseDown += MapObject_MouseDown;
        }

        private void MapObject_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            var mapObject = image?.Tag as MapObject;

            if (mapObject == null)
                return;

            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed)
            {
                SelectMapObject(null);
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                SelectMapObject(((Image)sender).Tag as MapObject);
                _DragMode = DragMode.Move;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                DeleteMapObject(mapObject);
                _DragMode = DragMode.None;
            }
            var mousePosition = e.GetPosition(Canvas);
            _PreviousDragPoint = new Point(mousePosition.X, mousePosition.Y).ToAbsolutePointFromCanvasPosition(Canvas, Magnification);

            e.Handled = true;

            Edited();
        }

        private void DeleteMapObject(MapObject mapObject)
        {
            Canvas.Children.Remove(mapObject.Image);
            Stage.MapObjects.Remove(mapObject);
            SelectMapObject(null);
        }

        private void SetSize(MapObject mapObject, double magnification)
        {
            var image = mapObject.Image;
            image.Width = mapObject.Figure.ImageWidth * Magnification;
            image.Height = mapObject.Figure.ImageHeight * Magnification;
        }

        private void SetSize(Block block, double magnification)
        {
            var image = block.Image;
            image.Width = block.Maptip.ImageWidth * Magnification;
            image.Height = block.Maptip.ImageHeight * Magnification;
        }

        private void SetPosition(Image image, Point quarterviewPosition)
        {
            var drawPoint = (GetRealPoint(quarterviewPosition, true)).ToCanvasPosition(Canvas, Magnification);
            Canvas.SetLeft(image, drawPoint.X - image.Width / 2);
            Canvas.SetBottom(image, Canvas.Height - drawPoint.Y);
            Panel.SetZIndex(image, drawPoint.RawZ);
         }

        private void SelectMapObject(MapObject mapObject)
        {
            SelectedMapObject = mapObject;
            if(SelectedMapObject == null)
            {
                Console.WriteLine("Yeah");
            }
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

        public void DrawMapObjects()
        {
            _MapObjectImages = new List<Image>();

            foreach(var mapObject in Stage.MapObjects)
            {
                AddMapObject(mapObject);
            }
        }

        Point _PreviousDragPoint = null;
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Stage == null)
                return;

            if(e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed)
            {
                _DragMode = DragMode.None;
                return;
            }

            var pos = e.GetPosition(this as IInputElement);
            var nowPoint = GetRealPoint(new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas, Magnification), false);

            _DragMode = DragMode.Write;

            _PreviousDragPoint = nowPoint;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (Stage == null)
                return;

            var pos = e.GetPosition(this as IInputElement);
            var nowPoint = GetRealPoint(new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas, Magnification), false);

            if (_DragMode != DragMode.Move)
            {
                if (Stage.DoesContainsPoint(nowPoint))
                {
                    var square = Stage.Squares[nowPoint.RawX, nowPoint.RawY];
                    DrawAimLines(new List<Square>() { square });
                }
            }
            else
            {
                if(Stage.DoesContainsPoint(SelectedMapObject.Position))
                {
                    var square = Stage.Squares[SelectedMapObject.Position.RawX, SelectedMapObject.Position.RawY];
                    DrawAimLines(new List<Square>() { square });
                }
            }

            if(_DragMode == DragMode.Write)
            {

            }
            else if(_DragMode == DragMode.Move)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var mousePosition = e.GetPosition(Canvas);
                    var position = nowPoint - _PreviousDragPoint;
                    SelectedMapObject.Position += position;
                    SetPosition(SelectedMapObject.Image, SelectedMapObject.Position);

                    Edited();
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {

                }
            }

            _PreviousDragPoint = nowPoint;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Stage == null)
                return;

            var pos = e.GetPosition(this as IInputElement);
            var nowPoint = GetRealPoint(new Point(pos.X, pos.Y).ToAbsolutePointFromCanvasPosition(Canvas, Magnification), false);
            if (_DragMode == DragMode.Write)
            {
                if (Stage.DoesContainsPoint(nowPoint))
                {
                    var mo = Stage.AddMapObject(SelectedFigure, new Point(nowPoint.RawX, nowPoint.RawY, Stage.Squares[nowPoint.RawX, nowPoint.RawY].Height));
                    if (mo != null)
                    {
                        AddMapObject(mo);
                        SelectedMapObject = mo;
                        Edited();
                    }
                }
            }

            _DragMode = DragMode.None;
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

        public enum DragMode
        {
            Write, Delete, Select, Move, None
        }
    }
}
