using NonStandard.Data.Parse;
using System;
using System.Collections.Generic;

namespace NonStandard.Commands {
	public class Command {
		/// <summary>
		/// an execution context: what is being executed, with what arguments, from what source, and where is realtime output going
		/// </summary>
		public class Exec {
			/// <summary>
			/// what command is executing this handler
			/// </summary>
			public Command cmd;
			/// <summary>
			/// parsed tokens from a command string. use <see cref="Tokenizer.Tokenize"/>
			/// </summary>
			public Tokenizer tok;
			public object src;
			public Show.PrintFunc print;
			/// <param name="cmd">what command is executing this handler</param>
			/// <param name="tokenizer">parsed tokens from a command string. use <see cref="Tokenizer.Tokenize"/></param>
			/// <param name="source"></param>
			/// <param name="printFunction"></param>
			public Exec(Command cmd, Tokenizer tokenizer, object source, Show.PrintFunc printFunction) {
				this.cmd = cmd;
				this.tok = tokenizer;
				this.src = source;
				this.print = printFunction;
			}
			public Arguments GetArgs() { return Arguments.Parse(cmd, tok, src); }
		}

		public delegate void Handler(Exec executionContext);
		public enum DevelopmentState {
			Normal,
			/// <summary>
			/// this feature is discouraged, and may be removed soon
			/// </summary>
			Deprecated,
			/// <summary>
			/// this feature is not entirely finished, and future updates will likely change the way it behaves
			/// </summary>
			Preview
		}
		public string Name;
		public virtual Handler handler { get; set; }
		public Argument[] arguments;
		public string description;
		public DevelopmentState devState = DevelopmentState.Normal;
		private ParseRuleSet argumentParsingRuleset;
		public Command(string command, Handler handler, Argument[] arguments = null, string help = null, DevelopmentState devState = DevelopmentState.Normal) {
			this.Name = command;
			this.handler = handler;
			this.arguments = arguments;
			this.description = help;
			this.devState = devState;
			GenerateArgumentParsingRuleset();
		}
		public static Delim[] BaseArgumentParseDelimiters = CodeRules.CombineDelims(
			CodeRules._instruction_finished_delimiter_ignore_rest,
			CodeRules._string_delimiter,
			CodeRules._char_delimiter,
			CodeRules._expression_delimiter,
			CodeRules._code_body_delimiter,
			CodeRules._square_brace_delimiter);
		public static ParseRuleSet BaseArgumentParseRules = new ParseRuleSet("Cmd_DefaultArgument", BaseArgumentParseDelimiters);
		Delim[] GenerateArgumentDelims() {
			List<Delim> delims = new List<Delim>(BaseArgumentParseDelimiters);
			if (arguments == null || arguments.Length == 0) return delims.ToArray();
			for (int i = 0; i < arguments.Length; ++i) {
				Argument arg = arguments[i];
				delims.Add(new Delim(arg.id, arg.Name, arg.description));
				//if(arg.Name != null) { delims.Add(new Delim(arg.Name, arg.id, arg.description)); }
			}
			return delims.ToArray();
		}
		void GenerateArgumentParsingRuleset() {
			argumentParsingRuleset = new ParseRuleSet("Cmd_" + Name, GenerateArgumentDelims(), CodeRules.StandardWhitespace);
			argumentParsingRuleset.AddDelimiterFallback(ParseRuleSet.GetContext("default"));
		}
		public Tokenizer Tokenize(string text) {
			Tokenizer tokenizer = new Tokenizer();
			tokenizer.Tokenize(text, argumentParsingRuleset);
			return tokenizer;
		}
	}
}