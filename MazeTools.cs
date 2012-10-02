using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Mazes
{

    public class Point : IEquatable<Point>
    {
        public int X, Y;

        public Point()
        {
        }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public void Move(Point vector)
        {
            this.X += vector.X;
            this.Y += vector.Y;
        }


        public bool Equals(Point other)
        {
            bool b = (this.X == other.X && this.Y == other.Y);
            return b;
        }

        public bool Equality(Point left, Point right)
        {
            return right.Equals(left);
        }

        public bool Inequality(Point left, Point right)
        {
            return !Equality(right, left);
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public System.Drawing.Point GetSysPoint()
        {
            return new System.Drawing.Point(this.X, this.Y);
        }

        public Point MovePointDir(int dir)
        {
            switch (dir)
            {
                case 0:
                    return new Point(this.X, this.Y - 1);
                case 1:
                    return new Point(this.X+1, this.Y);
                case 2:
                    return new Point(this.X, this.Y + 1);
                case 3:
                    return new Point(this.X-1, this.Y);
                default:
                    return this;
            }
        }

        public List<Point> GetSurroundingPoints()
        {
            List<Point> points = new List<Point>();
            for (int d = 0; d < 4; d++)
            {
                points.Add(this.MovePointDir(d));
            }
            return points;
        }

    }

    public struct Rect
    {
        public Point p;
        public Point offset;

        public Rect(Point p, Point offset)
        {
            this.p = p;
            this.offset = offset;
        }
        public Rect(int x, int y, int w, int h)
        {
            this.p = new Point(x,y);
            this.offset = new Point(w,h);
        }

        public Point BottomRight()
        {
            return new Point(p.X + offset.X, p.Y + offset.Y);
        }

        public bool PointInRect(Point p)
        {
            if (p.X >= this.p.X &&
                p.Y >= this.p.Y &&
                p.X < this.p.X + this.offset.X &&
                p.Y < this.p.Y + this.offset.Y )
                return true;
            else
                return false;
        }

        public bool insideOf(Rect r)
        { // is this rect completely inside of rect r?
            if (r.PointInRect(this.p) && r.PointInRect(this.BottomRight()))
                return true;
            else
                return false;
        }
    }

    public static class MazeTools
    {

        public static bool PointInRect(int x, int y, int x2, int y2, Point p)
        {
            if (p.X >= x &&
                p.Y >= y &&
                p.X < x2 &&
                p.Y < y2)
                return true;
            else
                return false;
        }


        public static bool ArePointsEqual(Point p1, Point p2)
        {
            return ((p1.X == p2.X) && (p1.Y == p2.Y));
        }



        public static Point MoveCell(Point currentCell, Point moveOffset)
        {
            return new Point(currentCell.X + moveOffset.X, currentCell.Y + moveOffset.Y);
        }


        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }


        public static int PointToDir(Point p)
        {
            if (p.Y < 0)
                return 0;
            if (p.X > 0)
                return 1;
            if (p.Y > 0)
                return 2;
            if (p.X < 0)
                return 3;

            return -1;
        }

        public static Point DirToPoint(int dir, int moveValue=1)
        {
            Point p = new Point(0,0);
            switch (dir)
            {
                case 0:
                    p.Y -= moveValue;
                    break;
                case 1:
                    p.X += moveValue;
                    break;
                case 2:
                    p.Y += moveValue;
                    break;
                default:
                    p.X -= moveValue;
                    break;
            }
            return p;
        }

        public static Point MovePoint(Point p, Point offset)
        {
            return new Point(p.X + offset.X, p.Y + offset.Y);
        }

        public static int TurnRight(int dir)
        {
            if (dir < 3)
                return dir + 1;
            else
                return 0;
        }
        public static int TurnLeft(int dir)
        {
            if (dir > 0)
                return dir - 1;
            else
                return 3;
        }



    }


}
