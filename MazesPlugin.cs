// Based on a module from MarioE's WorldEdit plugin

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Hooks;
using Terraria;
using TShockAPI;
using MazesPlugin.Commands;

namespace MazesPlugin
{
	[APIVersion(1, 12)]
	public class MazesPlugin : TerrariaPlugin
	{

		public static List<byte> InvalidTiles = new List<byte>();
		public static PlayerInfo[] Players = new PlayerInfo[256];
		public static List<Func<int, int, int, bool>> Selections = new List<Func<int, int, int, bool>>();
		public static List<string> SelectionNames = new List<string>();
		public static Dictionary<string, byte> TileNames = new Dictionary<string, byte>();
		public static Dictionary<string, byte> WallNames = new Dictionary<string, byte>();

		public override string Author
		{
			get { return "radishes, MarioE"; }
		}
		private BlockingCollection<WECommand> CommandQueue = new BlockingCollection<WECommand>();
		private Thread CommandQueueThread;
		public override string Description
		{
			get { return "Adds commands for the generation and solving of mazes."; }
		}
		public override string Name
		{
			get { return "Mazes"; }
		}
		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}

		public MazesPlugin(Main game)
			: base(game)
		{
			for (int i = 0; i < 256; i++)
			{
				Players[i] = new PlayerInfo();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GameHooks.Initialize -= OnInitialize;
				NetHooks.GetData -= OnGetData;
				ServerHooks.Leave -= OnLeave;
			}
		}
		public override void Initialize()
		{
			GameHooks.Initialize += OnInitialize;
			NetHooks.GetData += OnGetData;
			ServerHooks.Leave += OnLeave;
		}

		void OnGetData(GetDataEventArgs e)
		{
			if (!e.Handled && e.MsgID == PacketTypes.Tile)
			{
				PlayerInfo info = Players[e.Msg.whoAmI];
				if (info.pt != 0)
				{
					int X = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 1);
					int Y = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 5);
					if (X >= 0 && Y >= 0 && X < Main.maxTilesX && Y < Main.maxTilesY)
					{
						if (info.pt == 1)
						{
							info.x = X;
							info.y = Y;
							TShock.Players[e.Msg.whoAmI].SendMessage(String.Format("Set point 1.", X, Y), Color.Yellow);
						}
						else
						{
							info.x2 = X;
							info.y2 = Y;
							TShock.Players[e.Msg.whoAmI].SendMessage(String.Format("Set point 2.", X, Y), Color.Yellow);
						}
						info.pt = 0;
						e.Handled = true;
						TShock.Players[e.Msg.whoAmI].SendTileSquare(X, Y, 3);
					}
				}
			}
		}
		void OnInitialize()
		{
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Maze, "/maze"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Solve, "/solve"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", ClearHistory, "/mclearhistory"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Contract, "/mcontract"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Expand, "/mexpand"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Inset, "/minset"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Outset, "/moutset"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", PointCmd, "/mpoint"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Redo, "/mredo"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Undo, "/mundo"));
            TShockAPI.Commands.ChatCommands.Add(new Command("mazes", Size, "/msize"));


			#region Invalid Tiles
			InvalidTiles.Add(33);
			InvalidTiles.Add(49);
			InvalidTiles.Add(78);
			#endregion
			#region Selections
			Selections.Add((i, j, plr) => ((i + j) & 1) == 0);
			Selections.Add((i, j, plr) => ((i + j) & 1) == 1);
			Selections.Add((i, j, plr) =>
			{
				PlayerInfo info = Players[plr];

				int X = Math.Min(info.x, info.x2);
				int Y = Math.Min(info.y, info.y2);
				int X2 = Math.Max(info.x, info.x2);
				int Y2 = Math.Max(info.y, info.y2);

				Vector2 center = new Vector2((float)(X2 - X) / 2, (float)(Y2 - Y) / 2);
				float major = Math.Max(center.X, center.Y);
				float minor = Math.Min(center.X, center.Y);
				if (center.Y > center.X)
				{
					float temp = major;
					major = minor;
					minor = temp;
				}
				return (i - center.X - X) * (i - center.X - X) / (major * major) + (j - center.Y - Y) * (j - center.Y - Y) / (minor * minor) <= 1;
			});
			Selections.Add((i, j, plr) => true);
			Selections.Add((i, j, plr) =>
			{
				return i == Players[plr].x || i == Players[plr].x2 || j == Players[plr].y || j == Players[plr].y2;
			});
			SelectionNames.Add("altcheckers");
			SelectionNames.Add("checkers");
			SelectionNames.Add("ellipse");
			SelectionNames.Add("normal");
			SelectionNames.Add("outline");
			#endregion
			#region Tile Names
			TileNames.Add("dirt", 0);
			TileNames.Add("stone", 1);
			TileNames.Add("grass", 2);
			TileNames.Add("iron", 6);
			TileNames.Add("copper", 7);
			TileNames.Add("gold", 8);
			TileNames.Add("silver", 9);
			TileNames.Add("platform", 19);
			TileNames.Add("demonite", 22);
			TileNames.Add("corrupt grass", 23);
			TileNames.Add("ebonstone", 25);
			TileNames.Add("wood", 30);
			TileNames.Add("meteorite", 37);
			TileNames.Add("gray brick", 38);
			TileNames.Add("red brick", 39);
			TileNames.Add("clay", 40);
			TileNames.Add("blue brick", 41);
			TileNames.Add("green brick", 43);
			TileNames.Add("pink brick", 44);
			TileNames.Add("gold brick", 45);
			TileNames.Add("silver brick", 46);
			TileNames.Add("copper brick", 47);
			TileNames.Add("spike", 48);
			TileNames.Add("cobweb", 51);
			TileNames.Add("sand", 53);
			TileNames.Add("glass", 54);
			TileNames.Add("obsidian", 56);
			TileNames.Add("ash", 57);
			TileNames.Add("hellstone", 58);
			TileNames.Add("mud", 59);
			TileNames.Add("jungle grass", 60);
			TileNames.Add("sapphire", 63);
			TileNames.Add("ruby", 64);
			TileNames.Add("emerald", 65);
			TileNames.Add("topaz", 66);
			TileNames.Add("amethyst", 67);
			TileNames.Add("diamond", 68);
			TileNames.Add("mushroom grass", 70);
			TileNames.Add("obsidian brick", 75);
			TileNames.Add("hellstone brick", 76);
			TileNames.Add("cobalt", 107);
			TileNames.Add("mythril", 108);
			TileNames.Add("hallowed grass", 109);
			TileNames.Add("adamantite", 111);
			TileNames.Add("ebonsand", 112);
			TileNames.Add("pearlsand", 116);
			TileNames.Add("pearlstone", 117);
			TileNames.Add("pearlstone brick", 118);
			TileNames.Add("iridescent brick", 119);
			TileNames.Add("mudstone block", 120);
			TileNames.Add("cobalt brick", 121);
			TileNames.Add("mythril brick", 122);
			TileNames.Add("silt", 123);
			TileNames.Add("wooden beam", 124);
			TileNames.Add("ice", 127);
			TileNames.Add("active stone", 130);
			TileNames.Add("inactive stone", 131);
			TileNames.Add("demonite brick", 140);
			TileNames.Add("candy cane", 145);
			TileNames.Add("green candy cane", 146);
			TileNames.Add("snow", 147);
			TileNames.Add("snow brick", 148);
			// These are not actually correct, but are for ease of usage.
			TileNames.Add("air", 149);
			TileNames.Add("lava", 150);
			TileNames.Add("water", 151);
			TileNames.Add("wire", 152);
			#endregion
			#region Wall Names
			WallNames.Add("air", 0);
			WallNames.Add("stone", 1);
			WallNames.Add("ebonstone", 3);
			WallNames.Add("wood", 4);
			WallNames.Add("gray brick", 5);
			WallNames.Add("red brick", 6);
			WallNames.Add("gold brick", 10);
			WallNames.Add("silver brick", 11);
			WallNames.Add("copper brick", 12);
			WallNames.Add("hellstone brick", 13);
			WallNames.Add("mud", 15);
			WallNames.Add("dirt", 16);
			WallNames.Add("blue brick", 17);
			WallNames.Add("green brick", 18);
			WallNames.Add("pink brick", 19);
			WallNames.Add("obsidian brick", 20);
			WallNames.Add("glass", 21);
			WallNames.Add("pearlstone brick", 22);
			WallNames.Add("iridescent brick", 23);
			WallNames.Add("mudstone brick", 24);
			WallNames.Add("cobalt brick", 25);
			WallNames.Add("mythril brick", 26);
			WallNames.Add("planked", 27);
			WallNames.Add("pearlstone", 28);
			WallNames.Add("candy cane", 29);
			WallNames.Add("green candy cane", 30);
			WallNames.Add("snow brick", 31);
			#endregion
			CommandQueueThread = new Thread(QueueCallback);
			CommandQueueThread.Name = "Mazes Callback";
			CommandQueueThread.Start();
			Directory.CreateDirectory("mazes");
		}
		void OnLeave(int plr)
		{
			File.Delete(Path.Combine("mazes", String.Format("clipboard-{0}.dat", plr)));
			foreach (string fileName in Directory.EnumerateFiles("mazes", String.Format("??do-{0}-*.dat", plr)))
			{
				File.Delete(fileName);
			}
			Players[plr] = new PlayerInfo();
		}

		void QueueCallback(object t)
		{
			while (!Netplay.disconnect)
			{
				WECommand command = CommandQueue.Take();
				command.Position();
				command.Execute();
			}
		}

		void ClearClipboard(CommandArgs e)
		{
			File.Delete(Path.Combine("mazes", String.Format("clipboard-{0}.dat", e.Player.Index)));
			e.Player.SendMessage("Cleared clipboard.", Color.Green);
		}
		void ClearHistory(CommandArgs e)
		{
            foreach (string fileName in Directory.EnumerateFiles("mazes", "??do-" + e.Player.Index + "-*.dat"))
			{
				File.Delete(fileName);
			}
			e.Player.SendMessage("Cleared history.", Color.Green);
		}
		void Contract(CommandArgs e)
		{
			if (e.Parameters.Count != 2)
			{
				e.Player.SendMessage("Invalid syntax! Proper syntax: //contract <amount> <direction>", Color.Red);
				return;
			}
			PlayerInfo info = Players[e.Player.Index];
			if (info.x == -1 || info.y == -1 || info.x2 == -1 || info.y2 == -1)
			{
				e.Player.SendMessage("Invalid selection.", Color.Red);
				return;
			}

			int amount;
			if (!int.TryParse(e.Parameters[0], out amount) || amount < 0)
			{
				e.Player.SendMessage("Invalid contraction amount.", Color.Red);
				return;
			}
			switch (e.Parameters[1].ToLower())
			{
				case "d":
				case "down":
					if (info.y < info.y2)
					{
						info.y += amount;
					}
					else
					{
						info.y2 += amount;
					}
					e.Player.SendMessage(String.Format("Contracted selection down {0}.", amount), Color.Green);
					break;

				case "l":
				case "left":
					if (info.x < info.x2)
					{
						info.x2 -= amount;
					}
					else
					{
						info.x -= amount;
					}
					e.Player.SendMessage(String.Format("Contracted selection left {0}.", amount), Color.Green);
					break;

				case "r":
				case "right":
					if (info.x < info.x2)
					{
						info.x += amount;
					}
					else
					{
						info.x2 += amount;
					}
					e.Player.SendMessage(String.Format("Contracted selection right {0}.", amount), Color.Green);
					break;

				case "u":
				case "up":
					if (info.y < info.y2)
					{
						info.y2 -= amount;
					}
					else
					{
						info.y -= amount;
					}
					e.Player.SendMessage(String.Format("Contracted selection up {0}.", amount), Color.Green);
					break;

				default:
					e.Player.SendMessage("Invalid direction.", Color.Red);
					break;
			}
		}
		void Expand(CommandArgs e)
		{
			if (e.Parameters.Count != 2)
			{
				e.Player.SendMessage("Invalid syntax! Proper syntax: //expand <amount> <direction>", Color.Red);
				return;
			}
			PlayerInfo info = Players[e.Player.Index];
			if (info.x == -1 || info.y == -1 || info.x2 == -1 || info.y2 == -1)
			{
				e.Player.SendMessage("Invalid selection.", Color.Red);
				return;
			}

			int amount;
			if (!int.TryParse(e.Parameters[0], out amount) || amount < 0)
			{
				e.Player.SendMessage("Invalid expansion amount.", Color.Red);
				return;
			}
			switch (e.Parameters[1].ToLower())
			{
				case "d":
				case "down":
					if (info.y < info.y2)
					{
						info.y2 += amount;
					}
					else
					{
						info.y += amount;
					}
					e.Player.SendMessage(String.Format("Expanded selection down {0}.", amount), Color.Green);
					break;

				case "l":
				case "left":
					if (info.x < info.x2)
					{
						info.x -= amount;
					}
					else
					{
						info.x2 -= amount;
					}
					e.Player.SendMessage(String.Format("Expanded selection left {0}.", amount), Color.Green);
					break;

				case "r":
				case "right":
					if (info.x < info.x2)
					{
						info.x2 += amount;
					}
					else
					{
						info.x += amount;
					}
					e.Player.SendMessage(String.Format("Expanded selection right {0}.", amount), Color.Green);
					break;

				case "u":
				case "up":
					if (info.y < info.y2)
					{
						info.y -= amount;
					}
					else
					{
						info.y2 -= amount;
					}
					e.Player.SendMessage(String.Format("Expanded selection up {0}.", amount), Color.Green);
					break;

				default:
					e.Player.SendMessage("Invalid direction.", Color.Red);
					break;
			}
		}
		void Inset(CommandArgs e)
		{
			if (e.Parameters.Count != 1)
			{
				e.Player.SendMessage("Invalid syntax! Proper syntax: //inset <amount>", Color.Red);
				return;
			}
			PlayerInfo info = Players[e.Player.Index];
			if (info.x == -1 || info.y == -1 || info.x2 == -1 || info.y2 == -1)
			{
				e.Player.SendMessage("Invalid selection.", Color.Red);
				return;
			}

			int amount;
			if (!int.TryParse(e.Parameters[0], out amount) || amount < 0)
			{
				e.Player.SendMessage("Invalid inset amount.", Color.Red);
				return;
			}
			if (info.x < info.x2)
			{
				info.x += amount;
				info.x2 -= amount;
			}
			else
			{
				info.x -= amount;
				info.x2 += amount;
			}
			if (info.y < info.y2)
			{
				info.y += amount;
				info.y2 -= amount;
			}
			else
			{
				info.y -= amount;
				info.y2 += amount;
			}
			e.Player.SendMessage(String.Format("Inset selection by {0}.", amount));
		}
        void Maze(CommandArgs e)
        {
            if (e.Parameters.Count < 1 || e.Parameters.Count > 4)
            {
                e.Player.SendMessage("Invalid syntax! Proper syntax: //maze <tunnel_width> <wall_width>", Color.Red);
                return;
            }

            // check that points are sent
            PlayerInfo info = Players[e.Player.Index];
            if (info.x == -1 || info.y == -1 || info.x2 == -1 || info.y2 == -1)
            {
                e.Player.SendMessage("Invalid selection. First select an area using //point", Color.Red);
                return;
            }

            int x = Math.Min(info.x, info.x2);
            int y = Math.Min(info.y, info.y2);
            int x2 = Math.Max(info.x, info.x2);
            int y2 = Math.Max(info.y, info.y2);

            int tunnelWidth = 3;
            int wallWidth = 3;
            int algorithm = 0;
            int parameter = 0;
            if (!int.TryParse(e.Parameters[0], out tunnelWidth) || tunnelWidth <= 0)
            {
                e.Player.SendMessage("Invalid tunnel width!", Color.Red);
                return;
            }
            if (!int.TryParse(e.Parameters[1], out wallWidth) || wallWidth <= 0)
            {
                e.Player.SendMessage("Invalid tunnel width!", Color.Red);
                return;
            }
            try
            {
                if (!int.TryParse(e.Parameters[2], out algorithm) || algorithm < 0)
                {
                    algorithm = 0; // default to the first alogrithm if unspecified or not understood
                }
            }
            catch { algorithm = 0; }
            try
            {
                if (!int.TryParse(e.Parameters[3], out parameter) || parameter < 0)
                {
                    parameter = 0; // default
                }
            }
            catch { parameter = 0; }


            CommandQueue.Add(new MazeCommand(x, y, x2, y2, e.Player.Index, tunnelWidth, wallWidth, algorithm, parameter));

        }
        void Solve(CommandArgs e)
        {
            if (e.Parameters.Count > 0)
            {
                e.Player.SendMessage("Invalid syntax! Proper syntax: //solve", Color.Red);
                return;
            }

            // check that points are sent
            PlayerInfo info = Players[e.Player.Index];
            if (info.x == -1 || info.y == -1 || info.x2 == -1 || info.y2 == -1)
            {
                e.Player.SendMessage("Invalid selection. First select an area using //point", Color.Red);
                return;
            }

            int x = Math.Min(info.x, info.x2);
            int y = Math.Min(info.y, info.y2);
            int x2 = Math.Max(info.x, info.x2);
            int y2 = Math.Max(info.y, info.y2);



            CommandQueue.Add(new SolveCommand(x, y, x2, y2, e.Player.Index));

        }
		void Outset(CommandArgs e)
		{
			if (e.Parameters.Count != 1)
			{
				e.Player.SendMessage("Invalid syntax! Proper syntax: //outset <amount>", Color.Red);
				return;
			}
			PlayerInfo info = Players[e.Player.Index];
			if (info.x == -1 || info.y == -1 || info.x2 == -1 || info.y2 == -1)
			{
				e.Player.SendMessage("Invalid selection.", Color.Red);
				return;
			}

			int amount;
			if (!int.TryParse(e.Parameters[0], out amount) || amount < 0)
			{
				e.Player.SendMessage("Invalid outset amount.", Color.Red);
				return;
			}
			if (info.x < info.x2)
			{
				info.x -= amount;
				info.x2 += amount;
			}
			else
			{
				info.x += amount;
				info.x2 -= amount;
			}
			if (info.y < info.y2)
			{
				info.y -= amount;
				info.y2 += amount;
			}
			else
			{
				info.y += amount;
				info.y2 -= amount;
			}
			e.Player.SendMessage(String.Format("Outset selection by {0}.", amount));
		}
		void PointCmd(CommandArgs e)
		{
			if (e.Parameters.Count != 1)
			{
				e.Player.SendMessage("Invalid syntax! Proper syntax: //point <1|2>", Color.Red);
				return;
			}

			switch (e.Parameters[0])
			{
				case "1":
					Players[e.Player.Index].pt = 1;
					e.Player.SendMessage("Hit a block to set point 1.", Color.Yellow);
					break;
				case "2":
					Players[e.Player.Index].pt = 2;
					e.Player.SendMessage("Hit a block to set point 2.", Color.Yellow);
					break;
				default:
					e.Player.SendMessage("Invalid syntax! Proper syntax: //point <1|2>", Color.Red);
					break;
			}
		}
		void Redo(CommandArgs e)
		{
			if (e.Parameters.Count > 1)
			{
				e.Player.SendMessage("Invalid syntax! Proper syntax: //redo [steps]", Color.Red);
				return;
			}

			int steps = 1;
			if (e.Parameters.Count == 1 && (!int.TryParse(e.Parameters[0], out steps) || steps <= 0))
			{
				e.Player.SendMessage("Invalid number of steps.", Color.Red);
				return;
			}
			CommandQueue.Add(new RedoCommand(e.Player.Index, steps));
		}
        void Size(CommandArgs e)
        {
            PlayerInfo info = Players[e.Player.Index];
            if (info.x == -1 || info.y == -1 || info.x2 == -1 || info.y2 == -1)
            {
                e.Player.SendMessage("Invalid selection.", Color.Red);
                return;
            }
            int lenX = Math.Abs(info.x - info.x2) + 1;
            int lenY = Math.Abs(info.y - info.y2) + 1;
            e.Player.SendMessage(String.Format("Selection size: {0} x {1}", lenX, lenY), Color.Yellow);
        }
		void Undo(CommandArgs e)
		{
			if (e.Parameters.Count > 1)
			{
				e.Player.SendMessage("Invalid syntax! Proper syntax: //undo [steps]", Color.Red);
				return;
			}

			int steps = 1;
			if (e.Parameters.Count == 1 && (!int.TryParse(e.Parameters[0], out steps) || steps <= 0))
			{
				e.Player.SendMessage("Invalid number of steps.", Color.Red);
				return;
			}
			CommandQueue.Add(new UndoCommand(e.Player.Index, steps));
		}
	}
}