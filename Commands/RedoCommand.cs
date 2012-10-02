﻿using System;
using TShockAPI;

namespace MazesPlugin.Commands
{
	public class RedoCommand : WECommand
	{
		private int steps;

		public RedoCommand(int plr, int steps)
			: base(0, 0, 0, 0, plr)
		{
			this.steps = steps;
		}

		public override void Execute()
		{
			int i = 0;
			for (; MazesPlugin.Players[plr].redoLevel != -1 && i < steps; i++)
			{
				Tools.Redo(plr);
			}
			TShock.Players[plr].SendMessage(String.Format("Redid last {0}action{1}.",
				i == 1 ? "" : i + " ", i == 1 ? "" : "s"), Color.Green);
		}
	}
}
