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

    public class SolveCommand : WECommand
    {
        private Solver solver;

        public SolveCommand(int x, int y, int x2, int y2, int plr)
            : base(x, y, x2, y2, plr)
        {

        }

        public override void Execute()
        {
            // assumes x,y and x2,y2 have been normalized so that x,y is top-left
            Tools.PrepareUndo(x, y, x2, y2, plr);

            this.solver = new Solver(new Mazes.Point(x,y-1), new Mazes.Point(x2,y2-1), this.PeekBlock);
            while (solver.state < 8)
            {
                solver.SolveStep();
            }

            foreach (Mazes.Point sp in solver.solution)
            {
                DrawWire(sp);
            }
            

            TShock.Players[plr].SendMessage(String.Format("Solve complete."), Color.LimeGreen);
            ResetSection();

        } //Execute()



        public bool PeekBlock(Mazes.Point p)
        {
            try
            {
                return Main.tile[p.X, p.Y].active;
            }
            catch
            {
                TShock.Players[plr].SendMessage(String.Format("Solve is having a bad problem!! Server crash may be imminent!"), Color.Red);
                return false;
            }
        }

        public void DrawWire(Mazes.Point p)
        {
            Main.tile[p.X, p.Y].wire = true;
        }

    }
    
}

