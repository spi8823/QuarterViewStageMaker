using System;

namespace QuarterViewing
{
    public class Point
    {
        public static int RhombusHorizontalWidthInPixels = 32;
        public static int RhombusVerticalWidthInPixels = 16;
        public static int HeightToPixels = 16;

        public double X = 0;
        public double Y = 0;
        public double Z = 0;

        public readonly bool IsAbsolute = true;

        public int RawX { get { return (int)X; } }
        public int RawY { get { return (int)Y; } }
        public int RawZ { get { return (int)Z; } }

        public double Length
        {
            get
            {
                return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z, 2));
            }
        }

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

            x += RhombusHorizontalWidthInPixels / 2 * X;
            x += -RhombusHorizontalWidthInPixels / 2 * Y;

            y += RhombusVerticalWidthInPixels / 2 * X;
            y += RhombusVerticalWidthInPixels / 2 * Y;
            y += HeightToPixels * Z;

            //zが大きくなるほど手前に描画される（隠れなくなる）
            z -= (int)X + (int)Y;
            z += Z / 100;
            z -= (X % 1.0 + Y % 1.0) / 10000;

            return new Point(x, y, z, false);
        }

        public Point ToAbsolutePoint()
        {
            double x = Y / RhombusVerticalWidthInPixels - X / RhombusHorizontalWidthInPixels;
            double y = Y / RhombusVerticalWidthInPixels + X / RhombusHorizontalWidthInPixels;
            double z = 0;

            return new Point(x, y, z, true);
        }
        #endregion
    }
}
