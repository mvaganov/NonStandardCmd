using NonStandard.Commands;
using NonStandard.Data;
using NonStandard.Data.Parse;
using NonStandard.Extension;
using NonStandard.Utility.UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NonStandard.Cli {
	[System.Serializable] public class UnityEvent_CommandExec : UnityEvent<Command.Exec> { }
	public class ConsoleCommands : MonoBehaviour {
		public Active whenToUse;
		public List<UnityConsoleCommander.CommandEntry> commands = new List<UnityConsoleCommander.CommandEntry>();
		NonStandard.Commands.Commander _commander;
		public enum Active {
			AlwaysUseCommands, DoNotUseCommands, UseOnlyIfComponentIsActive
		}
		public Active UseTheseCommands {
			get => whenToUse;
			set {
				switch (whenToUse) {
				case Active.AlwaysUseCommands: commands.ForEach(c => c.AddToCommander(GetCommander())); break;
				case Active.DoNotUseCommands: commands.ForEach(c => c.RemoveFromCommander(GetCommander())); break;
				case Active.UseOnlyIfComponentIsActive:
					if (isActiveAndEnabled) {
						commands.ForEach(c => c.AddToCommander(GetCommander()));
					} else {
						commands.ForEach(c => c.RemoveFromCommander(GetCommander()));
					}
					break;
				}
				whenToUse = value;
			}
		}
		public int GetCommandIndex(string name) {
			for (int i = 0; i < commands.Count; ++i) {
				if (commands[i].name == name) return i;
			}
			return -1;
		}
		public UnityConsoleCommander.CommandEntry GetCommand(string name) {
			int i = GetCommandIndex(name);
			if (i >= 0) return commands[i];
			return null;
		}
		public void SetCommand(UnityConsoleCommander.CommandEntry command) {
			int i = GetCommandIndex(command.name);
			if (i >= 0) { commands[i] = command; return; }
			commands.Add(command);
		}
		public NonStandard.Commands.Commander GetCommander() {
			return _commander != null ? _commander : _commander = GetComponent<UnityConsoleCommander>().CommanderInstance;
		}
		private void OnEnable() { if (whenToUse != Active.DoNotUseCommands) { UseTheseCommands = whenToUse; } }
		private void OnDisable() { if (whenToUse == Active.UseOnlyIfComponentIsActive) { UseTheseCommands = whenToUse; } }
		public static ConsoleCommands[] GetCommandsListing(Transform t) {
			ConsoleCommands[] cc = t.GetComponents<ConsoleCommands>();
			if (cc == null || cc.Length == 0) {
				cc = new ConsoleCommands[1];
				cc[0] = t.gameObject.AddComponent<ConsoleCommands>();
			}
			return cc;
		}

		public static void AddCommands(Transform t, IList<CommandEntry> commands) {
			ConsoleCommands[] cc = GetCommandsListing(t);
			for (int i = 0; i < commands.Count; ++i) {
				SetCommmandEntry(commands[i], cc);
			}
		}
		public void AddCommands(IList<CommandEntry> commands) {
			for (int i = 0; i < commands.Count; ++i) {
				SetCommand(commands[i]);
			}
		}

		[System.Serializable]
		public class CommandEntry {
			public string name;
			public string description;
			public Command.DevelopmentState devState = Command.DevelopmentState.Normal;
			public ArgumentEntry[] arguments;
			public UnityEvent_CommandExec commandExecution = new UnityEvent_CommandExec();
			public void AddToCommander(NonStandard.Commands.Commander cmdr) {
				Command cmd = cmdr.GetCommand(name);
				if (cmd != null && cmd.description == description) { return; }
				cmdr.AddCommand(GenerateProperCommand());
			}
			public void RemoveFromCommander(NonStandard.Commands.Commander cmdr) {
				Command cmd = cmdr.GetCommand(name);
				if (cmd == null) { return; }
				cmdr.RemoveCommand(cmd);
			}
			public CommandEntry(string name, string description, string functionName, object functionTarget,
				ArgumentEntry[] arguments = null, Command.DevelopmentState devState = Command.DevelopmentState.Normal) {
				this.name = name; this.description = description;
				EventBind.On(commandExecution, functionTarget, functionName);
				this.arguments = arguments; this.devState = devState;
			}
			public Command GenerateProperCommand() {
				Argument[] args = new Argument[arguments.Length];
				int orderedArgumentSlotsFilled = 0;
				for (int i = 0; i < args.Length; ++i) {
					args[i] = arguments[i].GenerateProperArgument();
					if (arguments[i].orderedArgument) {
						args[i].order = ++orderedArgumentSlotsFilled;
					}
				}
				return new Command(name, commandExecution.Invoke, args, description, devState);
			}
			public CommandEntry(Command c, string functionName, object functionTarget) {
				name = c.Name; description = c.description; c.devState = devState;
				EventBind.On(commandExecution, functionTarget, functionName);
				arguments = ArgumentEntry.GetEntriesFromRealArgs(c.arguments);
			}
		}
		[System.Serializable]
		public class ArgumentEntry {
			public string name, id, description;
			public Command.DevelopmentState devState = Command.DevelopmentState.Normal;
			public bool required, orderedArgument;
			public ValueType valueType = ValueType.Flag;
			public string defaultValue;
			public enum ValueType { Flag, String, Int, Float, IntArray, FloatArray, StringArray, ByDefaultValue }
			public ArgumentEntry(string name, string id, string description, bool required = false, Command.DevelopmentState devState = Command.DevelopmentState.Normal, bool orderOfValueImpliesArgument = false, ValueType valueType = ValueType.Flag, string defaultValue = null) {
				this.name = name; this.id = id; this.description = description;
				this.required = required; this.devState = devState;
				this.orderedArgument = orderOfValueImpliesArgument; this.valueType = valueType; this.defaultValue = defaultValue;
			}
			public ArgumentEntry(Argument arg) {
				name = arg.Name; id = arg.id; description = arg.description;
				required = arg.required; devState = arg.devState;
				if (arg.order > 0) { this.orderedArgument = true; }
				if (arg.flag) { valueType = ValueType.Flag; return; }
				if (arg.defaultValue != null) {
					defaultValue = arg.defaultValue.StringifySmall();
				} else {
					if (arg.valueType == typeof(string)) { valueType = GetValueType(arg.valueType); }
				}
			}
			public static ArgumentEntry[] GetEntriesFromRealArgs(Argument[] args) {
				ArgumentEntry[] arge = new ArgumentEntry[args.Length];
				for (int i = 0; i < arge.Length; ++i) {
					arge[i] = new ArgumentEntry(args[i]);
				}
				return arge;
			}
			public object GetDefaultValue() {
				if (string.IsNullOrEmpty(defaultValue)) { return null; }
				Tokenizer tokenizer = new Tokenizer();
				if (!CodeConvert.TryParse(default, out object result, null, tokenizer)) {
					Debug.LogError(tokenizer.GetErrorString());
				}
				return result;
			}
			public ValueType GetValueType(Type t) {
				if (t == typeof(string)) return ValueType.String;
				if (t == typeof(int)) return ValueType.Int;
				if (t == typeof(float)) return ValueType.Float;
				if (t == typeof(int[])) return ValueType.IntArray;
				if (t == typeof(float[])) return ValueType.FloatArray;
				if (t == typeof(string[])) return ValueType.StringArray;
				if (t == typeof(bool[])) return ValueType.Flag;
				return ValueType.ByDefaultValue;
			}
			public Type GetValueType() {
				switch (valueType) {
				case ValueType.String: return typeof(string);
				case ValueType.Int: return typeof(int);
				case ValueType.Float: return typeof(float);
				case ValueType.IntArray: return typeof(int[]);
				case ValueType.FloatArray: return typeof(float[]);
				case ValueType.StringArray: return typeof(string[]);
				}
				return null;
			}
			public bool IsFlag() { return valueType == ValueType.Flag; }
			public Argument GenerateProperArgument() {
				return new Argument(id, name, description, GetDefaultValue(), GetValueType(), -1, required, devState, IsFlag());
			}
		}
		public static CommandEntry GetCommandEntry(string name, ConsoleCommands[] cc) {
			for (int i = 0; i < cc.Length; ++i) {
				CommandEntry entry = cc[i].GetCommand(name);
				if (entry != null) { return entry; }
			}
			return null;
		}
		public static void SetCommmandEntry(CommandEntry entry, ConsoleCommands[] cc) {
			for (int i = 0; i < cc.Length; ++i) {
				CommandEntry found = cc[i].GetCommand(entry.name);
				if (found != null) {
					cc[i].SetCommand(entry);
					return;
				}
			}
			cc[0].SetCommand(entry);
		}
	}
}