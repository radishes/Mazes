// maze module by radishes

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

using Mazes;

namespace MazesPlugin.Commands
{

    public class MazeCommand : WECommand
    {

        private Maze maze;

        private int tunnelWidth = 3;
        private int wallWidth = 3;
        private int totalWidth;
        private int algorithm = 0;
        private int parameter = 0;

        private Point up, down, left, right;
        private List<Point> dirs;

        private Random rng;


        public MazeCommand(int x, int y, int x2, int y2, int plr, int tunnelWidth, int wallWidth, int algorithm, int parameter)
            : base(x, y, x2, y2, plr)
        {
            this.tunnelWidth = tunnelWidth;
            this.wallWidth = wallWidth;
            this.algorithm = algorithm;
            this.parameter = parameter;

            this.totalWidth = this.wallWidth + this.tunnelWidth;
            this.up = new Point(0, -this.totalWidth);
            this.down = new Point(0, this.totalWidth);
            this.left = new Point(-this.totalWidth, 0);
            this.right = new Point(this.totalWidth, 0);
            this.dirs = new List<Point>(new Point[] { this.up, this.right, this.down, this.left });

            this.rng = new Random();
        }

        public override void Execute()
        {
            // assumes x,y and x2,y2 have been normalized so that x,y is top-left
            Tools.PrepareUndo(x, y, x2, y2, plr);

            switch (algorithm)
            {
                case 0:
                    TShock.Players[plr].SendMessage(String.Format("0: Recursive Backtracker maze creation initiated!"), Color.Green);
                    this.maze = new Mazes.Maze(new Mazes.Point(x, y), new Mazes.Point(x2 - x, y2 - y), new Mazes.Point(TShock.Players[plr].TileX, TShock.Players[plr].TileY),
                        tunnelWidth, wallWidth, algorithm, 0, this.PeekBlock, this.NukeTiles);
                    //RecursiveBacktracker();
                    break;
                case 1:
                    // TShock.Players[plr].SendMessage(String.Format("1: Growing Tree maze creation initiated!"), Color.Green);
                    //  GrowingTree();
                    break;
                default:
                    TShock.Players[plr].SendMessage(String.Format("Invalid algorithm specified."), Color.Red);
                    break;
            }
            MazeState ms = new MazeState();
            while (ms.status < 8)
            {
                ms = maze.Step();
            }



            TShock.Players[plr].SendMessage(String.Format("Maze creation complete."), Color.LimeGreen);
            ResetSection();

        } //Execute()



        public bool PeekBlock(Mazes.Point p)
        {
            return Main.tile[p.X, p.Y].active;
        }


        public bool NukeTiles(Mazes.Rect r)
        {
            for (int i = r.p.X; i < r.p.X + r.offset.X; i++)
            {
                for (int j = r.p.Y; j < r.p.Y + r.offset.Y; j++)
                {
                    Main.tile[i, j].active = false;
                    Main.tile[i, j].type = 0;
                    //Main.tile[i, j].lava = false;
                    //Main.tile[i, j].liquid = 0;
                    //Main.tile[i, j].wall = 0;
                    //Main.tile[i, j].wire = false;
                }
            }
            return true;
        }

        public void DrawWire(Mazes.Point p)
        {
            Main.tile[p.X, p.Y].wire = true;
        }

        private bool TestPointForTunnel(Mazes.Point p)
        { // check that the point is within the selected area, and is active (has a tile on it)
            Mazes.Rect r = new Mazes.Rect(x, y, x2 - (this.totalWidth * 2), y2 - (this.totalWidth * 2));
            return (r.PointInRect(p) && Main.tile[p.X, p.Y].active);
        }
    }

}

