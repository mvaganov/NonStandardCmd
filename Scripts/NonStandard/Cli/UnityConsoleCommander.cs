﻿using NonStandard.Commands;
using NonStandard.Data;
using NonStandard.Data.Parse;
using NonStandard.Extension;
using System;
using System.Text;
using UnityEngine;

namespace NonStandard.Cli {
	public partial class UnityConsoleCommander : ConsoleCommands {
		[TextArea(1, 10)] public string firstCommandsToExecute;
		[Tooltip("access Commander singleton, showing all scriptable commands")]
		public bool globalCommander = true;
		public UnityEvent_string WhenCommandRuns;
		private UnityConsole console;

		private Commander _commander;
		public Commander CommanderInstance => _commander != null ? _commander : globalCommander ? _commander = Commander.Instance : null;
		public void DoCommand(string text) {
			console = GetComponent<UnityConsole>();
			CommanderInstance.ParseCommand(new Commander.Instruction(text, this), WriteOutput, out Tokenizer t);
			if (t?.errors?.Count > 0) {
				console.PushForeColor(ConsoleColor.Red);
				console.WriteLine(t.GetErrorString());
				Show.Log(t.GetErrorString());
				console.PopForeColor();
			}
			WhenCommandRuns?.Invoke(text);
		}
		public void WriteOutput(string text) {
			console.Write(text);
			console.RestartInput();
		}
		private void Start() {
			if (!string.IsNullOrEmpty(firstCommandsToExecute)) {
				DoCommand(firstCommandsToExecute);
			}
		}
		public void Cmd_Exit(Command.Exec e) { LifeCycle.Exit(); }
		public void Cmd_Pause(Command.Exec e) {
			Arguments args = e.GetArgs();
			if (args.TryGet("0", out bool unpause)) {
				GameClock.Instance().Unpause();
			} else {
				GameClock.Instance().Pause();
			}
		}
		public void Cmd_Help(Command.Exec e) { CommanderInstance.Cmd_Help_Handler(e); }
		public void Cmd_Echo(Command.Exec e) {
			Debug.Log(e.tok.DebugPrint());
			if (e.tok.HasError()) {
				UnityConsoleInput cin = GetComponent<UnityConsoleInput>();
				console.Write("errors:");
				console.Write(e.tok.GetErrorString(), (byte)cin.colors.codeInvalidInput);
			}
			StringBuilder sb = new StringBuilder();
			for (int i = 1; i < e.tok.Tokens.Count; ++i) {
				object result = e.tok.Tokens[i].Resolve(e.tok, e.src);
				if (result == null) { result = ""; }
				if (!(result is string)) { result = result.StringifySmall(); }
				sb.Append(result.ToString());
			}
			console.Log(sb.ToString());
			console.RestartInput();
		}
		public void Cmd_Clear(Command.Exec e) {
			console.Clear();
		}
#if UNITY_EDITOR
		public void Reset() {
			Command helpCmd = Commander.Cmd_GenerateHelpCommand_static();
			CommandEntry[] DefaultCommandEntries = new CommandEntry[] {
			new CommandEntry("echo", "prints messages to the command line", nameof(Cmd_Echo), this),
			new CommandEntry("clear", "clears messages in the command line", nameof(Cmd_Clear), this),
			new CommandEntry("exit", "ends this program", nameof(Cmd_Exit), this),
			new CommandEntry(helpCmd, nameof(Cmd_Help), this),
			new CommandEntry("pause", "pauses the game clock", nameof(Cmd_Pause), this, new ArgumentEntry[] {
				new ArgumentEntry("unpause","0","unpauses the game clock",valueType:ArgumentEntry.ValueType.Flag),
			}),
		};
			AddCommands(DefaultCommandEntries);
		}
#endif
	}
}