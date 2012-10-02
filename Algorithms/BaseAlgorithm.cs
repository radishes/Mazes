using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Mazes.Algorithms
{
    public abstract class BaseAlgorithm
    {
        protected Func<Point, bool> PeekFunc;
        protected Func<Rect, bool> NukeFunc;
        public static string name;
        public static List<string> variants;
        public BaseAlgorithm(Func<Point, bool> PeekFunc, Func<Rect, bool> NukeFunc)
        {
            this.PeekFunc = PeekFunc;
            this.NukeFunc = NukeFunc;
        }

        public abstract MazeState Step(MazeData md);
    }
}
