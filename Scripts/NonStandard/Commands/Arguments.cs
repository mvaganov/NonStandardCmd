using System;
using System.Collections.Generic;
using NonStandard.Data;
using NonStandard.Data.Parse;
using NonStandard.Extension;

namespace NonStandard.Commands {
	public class Argument {
		/// <summary>
		/// a short name, and cannonical unique identifier
		/// </summary>
		public string id;
		/// <summary>
		/// a full name for the argument
		/// </summary>
		public string Name;
		/// <summary>
		/// description of what the argument does or what it is for
		/// </summary>
		public string description;
		/// <summary>
		/// optionally, what value to generate for this argument if no value is given
		/// </summary>
		public object defaultValue;
		/// <summary>
		/// the type of the value expected. optional if <see cref="defaultValue"/> is provided
		/// </summary>
		public System.Type valueType;
		/// <summary>
		/// if unnamed, the argument should be found in this unnamed slot in the argument listing. 0 is the command, 1 is the 1st argument
		/// </summary>
		public int order = -1;
		/// <summary>
		/// this argument is required. cause an error if the argument is missing. default true if order is greater than zero
		/// </summary>
		public bool required = false;
		public Command.DevelopmentState devState = Command.DevelopmentState.Normal;
		/// <summary>
		/// this argument doesn't have a value beyond being present or absent
		/// </summary>
		public bool flag = false;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">a short name, and cannonical unique identifier</param>
		/// <param name="name">a full name for the argument</param>
		/// <param name="description">description of what the argument does or what it is for</param>
		/// <param name="defaultValue">optionally, what value to generate for this argument if no value is given</param>
		/// <param name="type">the type of the value expected. optional if <see cref="defaultValue"/> is provided</param>
		/// <param name="order">if unnamed, the argument should be found in this unnamed slot in the argument listing. 0 is the command, 1 is the 1st argument</param>
		/// <param name="required">this argument is required. cause an error if the argument is missing. default true if order is greater than zero</param>
		/// <param name="deprecated">this feature is discouraged, and may be removed soon</param>
		/// <param name="preview">this feature is not entirely finished, and future updates will likely change the way it behaves</param>
		/// <param name="flag">this argument doesn't have a value beyond being present or absent</param>
		public Argument(string id, string name = null, string description = null, object defaultValue = null, System.Type type = null, int order = -1,
			bool required = false, Command.DevelopmentState devState = Command.DevelopmentState.Normal, bool flag = false) {
			this.id = id;
			this.Name = name;
			this.description = description;
			this.defaultValue = defaultValue;
			this.valueType = type;
			this.order = order;
			this.required = required;
			this.devState = devState;
			this.flag = flag;
			if (this.valueType == null && this.defaultValue != null) {
				this.valueType = defaultValue.GetType();
			}
			if (this.valueType == null && this.flag) {
				this.valueType = typeof(bool);
			}
		}
	}

	public class Arguments {
		public static Arguments Parse(Command command, Tokenizer tokenizer, object scriptVariables) {
			Arguments args = new Arguments(command);
			args.Parse(tokenizer, scriptVariables);
			return args;
		}

		public Command command;
		/// <summary>
		/// if there is a list of named or id'd arguments in the Command, this dicitonary will be populated
		/// </summary>
		public Dictionary<string, object> namedValues = new Dictionary<string, object>();
		/// <summary>
		/// if there are unnamed values, this list has them in order. argument zero is null
		/// </summary>
		public List<object> orderedValues = new List<object>();

		public override string ToString() {
			return "Arguments<" + command.Name + ">{" + namedValues.Stringify() + "," + orderedValues.Stringify() + "}";
		}

		public Arguments(Command command) { this.command = command; }

		public static int GetArgumentIndex(Command command, string text) {
			Argument[] args = command.arguments;
			for (int i = 0; i < args.Length; ++i) {
				if (args[i].id == text) { return i; }
			}
			for (int i = 0; i < args.Length; ++i) {
				if (args[i].Name == text) { return i; }
			}
			return -1;
		}

		public void Parse(Tokenizer tokenizer, object scriptVariables = null) {
			//Show.Log(tokenizer);
			//tokenizer = command.Tokenize(text);
			List<Token> tokens = new List<Token>(tokenizer.Tokens);
			orderedValues.Add(tokenizer.GetTokenAsString(0));
			Argument[] args = command.arguments;
			// add arguments to the dictionary
			for (int i = 1; i < tokens.Count; ++i) {
				Token tArg = tokens[i];
				//Show.Log(tArg+" "+tArg.IsSimpleString+" "+tArg.IsDelim);
				int argIndex = tArg.IsSimpleString || tArg.IsDelim ? GetArgumentIndex(command, tArg.ToString()) : -1;
				if (argIndex >= 0) {
					Argument arg = args[argIndex];
					bool argumentHasValidValue = AddArg(arg, ref i, tokens, tokenizer, scriptVariables);
					if (!argumentHasValidValue) {
						tokenizer.AddError(tArg.index, "expected <" + arg.valueType + "> for argument \"" + tArg.ToString() + "\"");
					}
				} else {
					orderedValues.Add(tokenizer.GetResolvedToken(i, scriptVariables));
				}
			}
			// put ordered arguments into the dictionary, include default values if needed, recognize required flag
			for (int i = 0; i < args.Length; ++i) {
				Argument arg = args[i];
				bool hasArg = namedValues.ContainsKey(arg.id);
				if (!hasArg && arg.order > 0 && arg.order < orderedValues.Count) {
					namedValues[arg.id] = orderedValues[arg.order];
					hasArg = true;
				}
				if (!hasArg && arg.defaultValue != null) {
					namedValues[arg.id] = arg.defaultValue;
					hasArg = true;
				}
				if (!hasArg && arg.required) {
					tokenizer.AddError("missing required argument \"" + arg.id + "\" (" + arg.Name + ")");
				}
			}
		}
		bool AddArg(Argument arg, ref int i, List<Token> tokens, Tokenizer tokenizer, object scriptVariables) {
			if (arg.flag) {
				namedValues[arg.id] = true;
				return true;
			}
			if (++i >= tokens.Count) { return false; }
			Token tValue = tokens[i];
			CodeConvert.TryParse(tokens[i], out object result, scriptVariables, tokenizer);
			Type type = arg.valueType;
			//Show.Log(resultType + " : " + result.StringifySmall());
			bool goodArgument = false;
			if (result != null) {
				if (!(goodArgument = result.GetType() == type)) {
					goodArgument = CodeConvert.TryConvert(ref result, type);
				}
				if (goodArgument) {
					namedValues[arg.id] = result;
				}
			}
			if (!goodArgument) {
				tokenizer.AddError(tValue.index, "Could not cast (" + type.Name + ") from (" +
					(result?.GetType().ToString() ?? "null") + ")");
			}
			return goodArgument;
		}

		public bool TryGet<T>(string argId, out T value) {
			value = default(T);
			if (!namedValues.TryGetValue(argId, out object obj)) { return false; }
			if (obj != null && typeof(T) != obj.GetType()) {
				CodeConvert.TryConvert(ref obj, typeof(T));
			}
			if (typeof(T) == obj?.GetType()) {
				value = (T)obj;
				return true;
			}
			return false;
		}
	}
}