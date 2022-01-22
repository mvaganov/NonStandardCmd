using NonStandard.Cli;
using NonStandard.Extension;
using System;
using System.Collections.Generic;

namespace NonStandard.Commands {
	public partial class Commander {
		public static class Cmd_Help {
			public static byte type = (byte)ConsoleColor.Green;
			public static byte error = (byte)ConsoleColor.Red;
			public static byte command = (byte)ConsoleColor.Magenta;
			public static byte argument = (byte)ConsoleColor.Yellow;
			public static byte preview = (byte)ConsoleColor.DarkMagenta;
			public static byte deprecated = (byte)ConsoleColor.DarkYellow;
			public static byte description = (byte)ConsoleColor.DarkGray;
			public static byte optional = (byte)ConsoleColor.DarkGray;
			public static bool useColor = true;
			public static string colorStd, colorCommand, colorPreview, colorArgument, colorDeprecated, colorDescription, colorError, colorOptional, colorType;
			public static void RecalculateColor() {
				colorStd = (useColor ? Col.r() : "");
				colorType = (useColor ? Col.r(type) : "");
				colorError = (useColor ? Col.r(error) : "");
				colorCommand = (useColor ? Col.r(command) : "");
				colorPreview = (useColor ? Col.r(preview) : "");
				colorOptional = (useColor ? Col.r(optional) : "");
				colorArgument = (useColor ? Col.r(argument) : "");
				colorDeprecated = (useColor ? Col.r(deprecated) : "");
				colorDescription = (useColor ? Col.r(description) : "");
			}
			private static string ColorFor(Command.DevelopmentState devState, string defaultColor) {
				switch (devState) {
				case Command.DevelopmentState.Deprecated: return colorDeprecated;
				case Command.DevelopmentState.Preview: return colorPreview;
				default: return defaultColor;
				}
			}
			public static void General_Handler(Command.Exec e, Dictionary<string, Command> commandTable) {
				RecalculateColor();
				List<Command> commands = new List<Command>();
				foreach (KeyValuePair<string, Command> kvp in commandTable) { commands.Add(kvp.Value); }
				commands.Sort((a, b) => a.Name.CompareTo(b.Name));
				for (int i = 0; i < commands.Count; ++i) {
					Command cmd = commands[i];
					string colorTxt = ColorFor(cmd.devState, colorCommand);
					string line = $"{colorTxt}{cmd.Name}{colorStd} : {colorDescription}{cmd.description}{colorStd}\n";
					e.print.Invoke(line);
				}
				string extraInstructions = $"for more information, type {colorCommand}help {colorOptional}-c {colorArgument}nameOfCommand{colorStd}\n";
				e.print.Invoke(extraInstructions);
			}
			public static void Command_Handler(Command.Exec e, string commandName, Command command) {
				RecalculateColor();
				if (command == null) {
					string error = $"{colorDescription}unknown command {colorError}{commandName}{colorStd}\n";
					e.print.Invoke(error);
					return;
				}
				string colorTxt = ColorFor(command.devState, colorCommand);
				string line = $"Usage: {colorTxt}{command.Name}{colorStd}";
				List<Argument> args = new List<Argument>();
				if (command.arguments != null) {
					for (int i = 0; i < command.arguments.Length; ++i) { args.Add(command.arguments[i]); }
					line += colorArgument;
				} else {
					line += $" {colorDescription}(no arguments){colorStd}";
				}
				args.Sort((a, b) => {
					if (a.order > 0) { return (b.order > 0) ? a.order.CompareTo(b.order) : -1; }
					else if (b.order > 0) { return 1; }
					return 0;
				});
				for (int i = 0; i < args.Count; ++i) {
					line += " ";
					Argument arg = args[i];
					char open = '[', close = ']';
					if (arg.required) { open = '<'; close = '>'; }
					if (arg.order > 0 && arg.required) { open = '\0'; close = '\0'; }
					if (open != '\0') {
						line += open;
					}
					//if (arg.order <= 0 || arg.flag) {
						if (arg.order > 0) { line += colorOptional; }
						line += arg.id;
						if (arg.order > 0) { line += colorArgument; }
						if (!arg.flag) { line += ' '; }
					//}
					if (!arg.flag) {
						line += $"{arg.Name}{colorType}({arg.valueType.ToString().SubstringAfterLast(".")}){colorArgument}";
					}
					if (close != '\0') {
						line += close;
					}
				}
				e.print.Invoke(line + "\n");
				for(int i = 0; i < args.Count; ++i) {
					Argument arg = args[i];
					colorTxt = ColorFor(arg.devState, colorArgument);
					line = $"{colorTxt}{arg.id} {colorStd}{arg.Name} : ";
					if (!arg.flag) {
						line += $"{colorType}({arg.valueType.ToString().SubstringAfterLast(".")}) ";
					}
					line += $"{colorDescription}{arg.description}{colorStd}\n";
					e.print.Invoke(line);
				}
				colorTxt = ColorFor(command.devState, colorCommand);
				line = $"{colorTxt}{command.Name}{colorStd} : {colorDescription}{command.description}{colorStd}\n";
				e.print.Invoke(line);
			}
		}
		public void Cmd_Help_Handler(Command.Exec e) {
			Cmd_Help_Handler_General(e, commandLookup);
		}
		public static void Cmd_Help_Handler_General(Command.Exec e, Dictionary<string,Command> commandLookup) {
			Arguments args = Arguments.Parse(e.cmd, e.tok, e.src);
			//Show.Log(args);
			if (!args.TryGet("-c", out string commandName)) {
				Cmd_Help.General_Handler(e, commandLookup);
			} else {
				commandLookup.TryGetValue(commandName, out Command cmd);
				Cmd_Help.Command_Handler(e, commandName, cmd);
			}
		}
		/// <summary>
		/// creates a <see cref="Command"/> structure, which is connected to the command parser through Unity, visible in the Unity UI
		/// </summary>
		/// <returns></returns>
		public static Command Cmd_GenerateHelpCommand_static() {
			return new Command("help", null, new Argument[] {
				new Argument("-c", "command", "which specific command to get help info for", type:typeof(string), order:1),
				//new Argument("-n", "numbers", "test parameter, an array of integers", type:typeof(int[])),
			}, "prints this help text");
		}
	}
}
