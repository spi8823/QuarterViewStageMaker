using System;
using Newtonsoft.Json;

/*
The MIT License (MIT)

Copyright (c) 2007 James Newton-King

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace QuarterViewStageMaker
{
    [JsonObject("Point")]
    public class Point
    {
        public static readonly Point UnitX = new Point(1, 0);
        public static readonly Point UnitY = new Point(0, 1);
        public static readonly Point UnitZ = new Point(0, 0, 1);

        [JsonProperty("X")]
        public double X = 0;
        [JsonProperty("Y")]
        public double Y = 0;
        [JsonProperty("Z")]
        public double Z = 0;

        [JsonIgnore()]
        public readonly bool IsAbsolute = true;

        [JsonIgnore()]
        public int RawX { get { return (int)X; } }
        [JsonIgnore()]
        public int RawY { get { return (int)Y; } }
        [JsonIgnore()]
        public int RawZ { get { return (int)Z; } }

        [JsonIgnore()]
        public double Length
        {
            get
            {
                return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));
            }
        }

        [JsonIgnore()]
        public Point Normal
        {
            get
            {
                return this / Length;
            }
        }

        public Point(double x, double y, double z = 0, bool isAbsolute = true)
        {
            X = x;
            Y = y;
            Z = z;

            IsAbsolute = isAbsolute;
        }

        public Point(Point point)
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;

            IsAbsolute = point.IsAbsolute;
        }

        #region 演算子
        public static Point operator*(double a, Point p)
        {
            return new Point(a * p.X, a * p.Y, a * p.Z, p.IsAbsolute);
        }

        public static Point operator*(Point p, double a)
        {
            return a * p;
        }

        public static Point operator/(Point p, double a)
        {
            if (a == 0)
                return new Point(0, 0, 0);

            return p * (1 / a);
        }

        public static Point operator+(Point p, Point q)
        {
            if (p.IsAbsolute != q.IsAbsolute)
                q = q.GetConvertedPoint();

            return new Point(p.X + q.X, p.Y + q.Y, p.Z + q.Z, p.IsAbsolute);
        }

        public static Point operator-(Point p, Point q)
        {
            return p + (-1 * q);
        }

        public static Point operator-(Point p)
        {
            return -1 * p;
        }
        #endregion

        #region 変換
        public Point GetConvertedPoint()
        {
            if (IsAbsolute)
                return ToRelativePoint();
            else
                return ToAbsolutePoint();
        }

        public Point ToRelativePoint()
        {
            if (!IsAbsolute)
                return new Point(this);

            double x = 0;
            double y = 0;
            double z = 0;

            x += Maptip.RhombusHorizontalWidthInPixels / 2 * X;
            x += -Maptip.RhombusHorizontalWidthInPixels / 2 * Y;

            y += Maptip.RhombusVerticalWidthInPixels / 2 * X;
            y += Maptip.RhombusVerticalWidthInPixels / 2 * Y;
            y += Maptip.HeightInPixels * Z;

            //zが大きくなるほど手前に描画される（隠れなくなる）
            z -= (int)X + (int)Y;
            z += Z / 100;
            z -= (X % 1.0 + Y % 1.0) / 10000;
            z += 2000;
            z *= 10000;

            return new Point(x, y, z, false);
        }

        public Point ToAbsolutePoint()
        {
            if (IsAbsolute)
                return new Point(this);

            double x = Y / Maptip.RhombusVerticalWidthInPixels + X / Maptip.RhombusHorizontalWidthInPixels;
            double y = Y / Maptip.RhombusVerticalWidthInPixels - X / Maptip.RhombusHorizontalWidthInPixels;
            double z = 0;

            return new Point(x, y, z, true);
        }

        public Point ToCanvasPosition(System.Windows.Controls.Canvas canvas, double magnification)
        {
            var relative = ToRelativePoint();
            var x = relative.X * magnification + canvas.Width / 2;
            var y = canvas.Height - StageCanvas.CanvasMargin.Height / 2 - relative.Y * magnification;
            return new Point(x, y, relative.Z + 100, false);
        }

        public Point ToAbsolutePointFromCanvasPosition(System.Windows.Controls.Canvas canvas, double magnification)
        {
            var x = X - canvas.Width / 2;
            var y = canvas.Height - StageCanvas.CanvasMargin.Height / 2 - Y;
            return new Point(x / magnification, y / magnification, 0, false).ToAbsolutePoint();
        }
        #endregion
    }
}
