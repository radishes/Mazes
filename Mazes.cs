using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using Mazes.Algorithms;

namespace Mazes
{
    public struct MazeState
    {
        public int status;
        public string message;

        public MazeState(int status, string message)
        {
            /*  statuses
             * -1: Error
             *  0: NULL
             *  1: Maze initiated
             *  2: Sucessful step, didn't tunnel
             *  4: Successful step, tunneled
             *  8: Complete
             */
            this.status = status;
            this.message = message;
        }


    }

    public class MazeData
    {
        // data representing the maze itself
        public Point coords; // top left corner of the entire maze area
        public Point size; // size of the maze in units (pixels, blocks, etc)
        public Point startCoords; // point at which to start maze creation
        public int tunnelWidth; // same units as size
        public int wallWidth; // same units as size
        public int totalWidth; // tunnelWidth + wallWidth

        public Random rng;
        public Point up, down, left, right;
        public List<Point> dirs;
        public List<Point> tunnelling; // the tunnelling path, in order of execution
        public Point lastCell;

        public int variant;

        public MazeData(Point coords, Point size, Point startCoords, int tunnelWidth,
                        int wallWidth, int variant=0)
        {
            this.coords = coords;
            this.size = size;
            this.startCoords = startCoords;
            this.tunnelWidth = tunnelWidth;
            this.wallWidth = wallWidth;
            this.totalWidth = tunnelWidth + wallWidth;
            this.tunnelling = new List<Point>();
            this.variant = variant;

            this.up = new Point(0, -this.totalWidth);
            this.down = new Point(0, this.totalWidth);
            this.left = new Point(-this.totalWidth, 0);
            this.right = new Point(this.totalWidth, 0);
            this.dirs = new List<Point>(new Point[] { this.up, this.right, this.down, this.left });
            this.rng = new Random();


        }
       
        public Point tunnelWidthP()
            {
                return new Point(tunnelWidth, tunnelWidth);
            }

        public bool TestPointInMaze(Point p)
        { // check that the point is within the maze area
            return (MazeTools.PointInRect(this.coords.X, this.coords.Y, this.coords.X + this.size.X, this.coords.Y + this.size.Y, p));
        }

        public bool TestCellInMaze(Point p)
        {
            Rect r = new Rect(this.coords.X, this.coords.Y, this.coords.X + this.size.X, this.coords.Y + this.size.Y);
            Rect cell = new Rect(p.X, p.Y, this.tunnelWidth, this.tunnelWidth);
            return cell.insideOf(r);
        }

        public Rect MakeOffset(Point p, int dir)
        {
            Point p2 = new Point(p.X, p.Y); // amount to adjust cell by to create "doorway" between cells
            Point offset = new Point();
            if (dir == 0)
            {
                offset.X = this.tunnelWidth;
                offset.Y = this.totalWidth;
            }
            if (dir == 1)
            {
                p2.X -= this.wallWidth;
                offset.X = this.totalWidth;
                offset.Y = this.tunnelWidth;
            }
            if (dir == 2)
            {
                p2.Y -= this.wallWidth;
                offset.X = this.tunnelWidth;
                offset.Y = this.totalWidth;
            }
            if (dir == 3)
            {
                offset.X = this.totalWidth;
                offset.Y = this.tunnelWidth;
            }
            return new Rect(p2, offset);
        }

    }


    public class Maze
    {
        public MazeData mazeData;
        public BaseAlgorithm mazeAlgorithm;

        public static Dictionary<string, BaseAlgorithm> allAlgorithms = new Dictionary<string, BaseAlgorithm>();        

        public Maze(Point coords, Point size, Point startCoords, int tunnelWidth, int wallWidth, int algorithmID, int variant, Func<Point, bool> PeekFunc, Func<Rect, bool> NukeFunc)
        {
            mazeData = new MazeData(coords, size, startCoords, tunnelWidth, wallWidth, variant);

            if (allAlgorithms.Count == 0)
                allAlgorithms.Add(GrowingTree.name, new GrowingTree(PeekFunc, NukeFunc));

            switch (algorithmID)
            {
                default:
                    this.mazeAlgorithm = new GrowingTree(PeekFunc, NukeFunc);
                    break;
            }
        }

        public MazeState Step()
        {
            return this.mazeAlgorithm.Step(this.mazeData);
        }

    }



    public class ExploredPoint
    {
        public Mazes.Point p;
        public ExploredPoint parent;
        public int generation;

        public ExploredPoint(Point p, ExploredPoint parent, int generation)
        {
            this.p = p;
            this.parent = parent;
            this.generation = generation;
        }
        
        public bool Equals(ExploredPoint other)
        {
            bool b = (this.p == other.p);
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

    }

    public class Solver
    {
        Func<Point, bool> PeekFunc;
        int currentGen;
        public int state;
        public List<ExploredPoint> exploredPoints;
        public List<Point> solution;

        Point start;
        Point end;
       // int indexOfEndFinder = -1;

        //public LinkedList<SolveUnit> solvers;
        public List<ExploredPoint> alivePoints;

        public Solver(Point start, Point end, Func<Point, bool> PeekFunc)
        {
            //this.solvers = new LinkedList<SolveUnit>();
            this.alivePoints = new List<ExploredPoint>();
            this.start = start;
            this.end = end;
            this.PeekFunc = PeekFunc;
            this.currentGen = 0;
            this.state = 0;

            exploredPoints = new List<ExploredPoint>();

            if (this.PeekFunc(start) != this.PeekFunc(end))
            {
                state = -4;
             //   MessageBoxError("The beginning and end points are not on the same colored surface, so there is no valid path between them. Check your parameters and try again. Use the left and right mouse buttons to set the start and end point on the same colored path.");
                return;
            }
        }

        public HashSet<Point> GetExploredPoints(int gensBack = 2)
        {
            HashSet<Point> points = new HashSet<Point>();
            //HashSet<Point> points = exploredPoints.Aggregate((points, next) => points.Add(next.p));
            //points.Zip<Point, Point> ;
            int oldestGen = currentGen - gensBack;
            for (int i = exploredPoints.Count-1; i >= 0; i--)
            //foreach (ExploredPoint ep in exploredPoints)
            {
                if (exploredPoints[i].generation < oldestGen)
                    break;
                points.Add(exploredPoints[i].p);
            }
            return points;
        }

        public void SolveStep()
        { // runs once per "frame" of maze solving
            if (this.state < 0)
                return; //error condition

            if (this.alivePoints.Count <= 0)
            { // no solve in progress already, initialize solver
                this.state = 1;
                this.exploredPoints.Add(new ExploredPoint(start, null, 0));
                this.currentGen = 1;
                for (int d = 0; d < 4; d++)
                {
                    alivePoints.Add(new ExploredPoint(MazeTools.MovePoint(start, MazeTools.DirToPoint(d)), exploredPoints.First(), currentGen));
                    this.exploredPoints.Add(new ExploredPoint(MazeTools.MovePoint(start, MazeTools.DirToPoint(d)), exploredPoints.First(), currentGen));
                }
            }

            List<ExploredPoint> nextGenPoints = new List<ExploredPoint>();
            this.currentGen++;

Stopwatch stopwatch = Stopwatch.StartNew();
            HashSet<Point> ep = GetExploredPoints();
stopwatch.Stop();
Debug.WriteLine(stopwatch.ElapsedTicks);

            foreach (ExploredPoint node in alivePoints)
            {

                if (MazeTools.ArePointsEqual(node.p, this.end))
                {
                    this.state = 4;
                }

                if (this.state >= 4)
                    break; // found solution
                List<Point> pNeighbors = node.p.GetSurroundingPoints();
                foreach (Point neighbor in pNeighbors)
                {
                    if (MazeTools.ArePointsEqual(node.parent.p, neighbor))
                        continue;
//Stopwatch stopwatch = Stopwatch.StartNew();
                    if (ep.Contains(neighbor))
                        continue;
//stopwatch.Stop();
//Debug.WriteLine(stopwatch.ElapsedTicks);
                    if (!PeekFunc(neighbor))
                    {
                        // maintaining three separate lists with similar information. this can be fixed up I bet.
                        nextGenPoints.Add(new ExploredPoint(neighbor, node, this.currentGen));
                        this.exploredPoints.Add(new ExploredPoint(neighbor, node, this.currentGen));
                        ep.Add(neighbor); // ugh
                    }


                }                    
                
            } // end foreach node in alivePoints

            if (this.state == 4)
            { // solution found; now trace back
                if (solution == null)
                {
                    solution = new List<Point>();
                    ExploredPoint tracer = exploredPoints.Last();
                    while (tracer != exploredPoints.First())
                    {
                        solution.Add(new Point(tracer.p.X, tracer.p.Y));
                        tracer = tracer.parent;
                    }
                    this.state = 12;
                }
            }

            alivePoints = nextGenPoints;

            if (alivePoints.Count <= 0)
            {
                this.state = 8;
            }
            
        }
        
    }
}

