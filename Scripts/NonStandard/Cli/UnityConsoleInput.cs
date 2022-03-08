using NonStandard.Data;
using NonStandard.Extension;
using NonStandard.Inputs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace NonStandard.Cli {
	[RequireComponent(typeof(UnityConsole)),RequireComponent(typeof(UserInput))]
	public class UnityConsoleInput : MonoBehaviour {
		public const string CmdLine = "CmdLine";
		public const string CmdCtrl = "CmdLineCtrl";
		public const string CmdAlt= "CmdLineAlt";
		public const string CmdShift = "CmdLineShift";
		private UnityConsole console;
		/// <summary>
		/// very short-term key storage, processed and added to the <see cref="UnityConsole.Console"/> each update
		/// </summary>
		protected StringBuilder _keyBuffer = new StringBuilder();
		private static bool ctrlIsDown = false;
		private static bool altIsDown = false;
		private static bool shiftIsDown = false;

		//protected List<ConsoleDiffUnit> _replaced = new List<ConsoleDiffUnit>();

		/// <summary>
		/// when a key was pressed
		/// </summary>
		protected Dictionary<KeyControl,int> keysDown = new Dictionary<KeyControl,int>();
		internal string _pastedText;
		ConsoleDiff lastInput = new ConsoleDiff();

		public Color activeInputColor = new Color(0.25f, 0.75f, 1);
		public Color correctInput = new Color(0, 0.75f, 0);
		public Color invalidInput = new Color(0.75f, 0, 0); // TODO
		/// <summary>
		/// the color code to use for user input. will be set during initialization
		/// </summary>
		private int activeInputColorCode = -1;
		private int submittedInputColorCode = -1;
		private int errorInputColorCode = -1;

		Dictionary<KeyControl, KMap> _keyMap = null;
		public const string KbPrefix = "<Keyboard>/";
		[Tooltip("each of these will be prefixed by "+ KbPrefix + " and added as a key bind for "+nameof(KeyInput))]
		public string[] defaultKeyboardKeys = new string[] {
			//"escape","f1","f2","f3","f4","f5","f6","f7","f8","f9","f10","f11","f12","printScreen","delete",
			"backquote", //"`", "~",
			"1", //"1", "!",
			"2", //"2", "@",
			"3", //"3", "#",
			"4", //"4", "$",
			"5", //"5", "%",
			"6", //"6", "^",
			"7", //"7", "&",
			"8", //"8", "*",
			"9", //"9", "(",
			"0", //"0", ")",
			"minus", //"-", "_",
			"equals", //"=", "+",
			"backspace", //"\b", "",
			//"home", "", "",
			"tab", //"\t", "",
			"q", //"q", "Q",
			"w", //"w", "W",
			"e", //"e", "E",
			"r", //"r", "R",
			"t", //"t", "T",
			"y", //"y", "Y",
			"u", //"u", "U",
			"i", //"i", "I",
			"o", //"o", "O",
			"p", //"p", "P",
			"leftBracket", //"[", "{",
			"rightBracket", //"]", "}",
			"backslash", //"\\", "|",
			//"pageUp", "", "",
			//"capsLock", "", "",
			"a", //"a", "A",
			"s", //"s", "S",
			"d", //"d", "D",
			"f", //"f", "F",
			"g", //"g", "G",
			"h", //"h", "H",
			"j", //"j", "J",
			"k", //"k", "K",
			"l", //"l", "L",
			"semicolon", //";", ":",
			"quote", //"'", "\"",
			"enter", //"\n", "\n",
			//"pageDown", "", "",
			/*"leftShift",*/
			"z", //"z", "Z",
			"x", //"x", "X",
			"c", //"c", "C",
			"v", //"v", "V",
			"b", //"b", "B",
			"n", //"n", "N",
			"m", //"m", "M",
			"comma", //",", "<",
			"period", //".", ">",
			"slash", //"/", "?",
			//"rightShift", "", "",
			//"end", "", "",
			/*"leftControl","leftAlt",*/
			"space", //" ", "",
			//"rightAlt","rightControl",
			//"upArrow","leftArrow","downArrow","rightArrow",
			//"numpad1","numpad2","numpad3","numpad4","numpad5","numpad6","numpad7","numpad8","numpad9","numpad0",
		};
		public string LastInputText => lastInput.ToSimpleString();
		public bool KeyAvailable => keysDown.Count > 0;

		private Dictionary<KeyControl, KMap> GetKeyMap() => _keyMap != null
			? _keyMap : _keyMap = new Dictionary<KeyControl, KMap>() {
			[Keyboard.current.backquoteKey] = new KMap('`', '~'),
			[Keyboard.current.digit0Key] = new KMap('0', ')'),
			[Keyboard.current.digit1Key] = new KMap('1', '!'),
			[Keyboard.current.digit2Key] = new KMap('2', '@'),
			[Keyboard.current.digit3Key] = new KMap('3', '#'),
			[Keyboard.current.digit4Key] = new KMap('4', '$'),
			[Keyboard.current.digit5Key] = new KMap('5', '%'),
			[Keyboard.current.digit6Key] = new KMap('6', '^'),
			[Keyboard.current.digit7Key] = new KMap('7', '&'),
			[Keyboard.current.digit8Key] = new KMap('8', '*'),
			[Keyboard.current.digit9Key] = new KMap('9', '('),
			[Keyboard.current.aKey] = new KMap('a', 'A'),
			[Keyboard.current.bKey] = new KMap('b', 'B'),
			[Keyboard.current.cKey] = new KMap('c', 'C'),
			[Keyboard.current.dKey] = new KMap('d', 'D'),
			[Keyboard.current.eKey] = new KMap('e', 'E'),
			[Keyboard.current.fKey] = new KMap('f', 'F'),
			[Keyboard.current.gKey] = new KMap('g', 'G'),
			[Keyboard.current.hKey] = new KMap('h', 'H'),
			[Keyboard.current.iKey] = new KMap('i', 'I'),
			[Keyboard.current.jKey] = new KMap('j', 'J'),
			[Keyboard.current.kKey] = new KMap('k', 'K'),
			[Keyboard.current.lKey] = new KMap('l', 'L'),
			[Keyboard.current.mKey] = new KMap('m', 'M'),
			[Keyboard.current.nKey] = new KMap('n', 'N'),
			[Keyboard.current.oKey] = new KMap('o', 'O'),
			[Keyboard.current.pKey] = new KMap('p', 'P'),
			[Keyboard.current.qKey] = new KMap('q', 'Q'),
			[Keyboard.current.rKey] = new KMap('r', 'R'),
			[Keyboard.current.sKey] = new KMap('s', 'S'),
			[Keyboard.current.tKey] = new KMap('t', 'T'),
			[Keyboard.current.uKey] = new KMap('u', 'U'),
			[Keyboard.current.vKey] = new KMap('v', 'V'),
			[Keyboard.current.wKey] = new KMap('w', 'W'),
			[Keyboard.current.xKey] = new KMap('x', 'X'),
			[Keyboard.current.yKey] = new KMap('y', 'Y'),
			[Keyboard.current.zKey] = new KMap('z', 'Z'),
			[Keyboard.current.minusKey] = new KMap('-', '_'),
			[Keyboard.current.equalsKey] = new KMap('=', '+'),
			[Keyboard.current.leftBracketKey] = new KMap('[', '{'),
			[Keyboard.current.rightBracketKey] = new KMap(']', '}'),
			[Keyboard.current.backslashKey] = new KMap('\\', '|'),
			[Keyboard.current.semicolonKey] = new KMap(';', ':'),
			[Keyboard.current.quoteKey] = new KMap('\'', '\"'),
			[Keyboard.current.commaKey] = new KMap(',', '<'),
			[Keyboard.current.periodKey] = new KMap('.', '>'),
			[Keyboard.current.slashKey] = new KMap('/', '?'),
			[Keyboard.current.spaceKey] = new KMap(' ', ' '),
			[Keyboard.current.tabKey] = new KMap('\t','\t'),
			[Keyboard.current.enterKey] = new KMap('\n', '\n'),
			[Keyboard.current.backspaceKey] = new KMap('\b','\b'),
		};

		public enum KMapMod { None = 0, Shift = 1, Ctrl = 2 }

		[System.Serializable] public class KeyEventMap {
			[InputControl] public string key;
			public KMapMod modifier;
			public UnityEvent action;
			public KeyEventMap(string key, KMapMod mod, EventBind eventBind) {
				this.key = KMap.Path(key);
				this.modifier = mod;
				action = new UnityEvent();
				//Debug.Log("binding "+eventBind+" to "+action);
				eventBind.Bind(action);
			}
			public void ApplyTo(ref KMap kmap) {
				switch (modifier) {
					case KMapMod.None: kmap.press = new Action(action.Invoke); break;
					case KMapMod.Shift:       kmap.shift = new Action(action.Invoke); break;
					case KMapMod.Ctrl:        kmap.ctrl = new Action(action.Invoke); break;
				}
			}
		}

		public struct KMap {
			public object press, shift, ctrl;
			public KMap(object p, object s = null, object c = null) { press = p; shift = s; ctrl = c; }
			public KMap wPress(object press) { return new KMap(press, shift, ctrl); }
			public KMap wShift(object shift) { return new KMap(press, shift, ctrl); }
			public KMap wCtrl(object ctrl) { return new KMap(press, shift, ctrl); }
			public void Absorb(KeyEventMap kep) {
				switch (kep.modifier) {
					case KMapMod.None:  press = new Action(kep.action.Invoke); break;
					case KMapMod.Shift: shift = new Action(kep.action.Invoke); break;
					case KMapMod.Ctrl:  ctrl = new Action(kep.action.Invoke); break;
				}
			}
			public static string Path(string key) {
				if (!key.StartsWith(KbPrefix)) { return KbPrefix + key; }
				return key;
			}
			public static string[] Path(string[] keys) {
				string[] p = new string[keys.Length];
				for (int i = 0; i < keys.Length; ++i) {
					p[i] = (!keys[i].StartsWith(KbPrefix)) ? KbPrefix + keys[i] : keys[i];
				}
				return p;
			}
		}
		public static bool IsShiftDown() { return shiftIsDown; }
		public static bool IsControlDown() { return ctrlIsDown; }
		public static bool IsAltDown() { return altIsDown; }
		private void MovCur(Coord dir) {
			console.Console.Cursor += dir;
			console.Console.RefreshCursorValid();
		}
		private void MovWin(Coord dir) { console.ScrollRenderWindow(dir); }

		public void PasteFromClipboard(InputAction.CallbackContext c) { if (c.performed) PasteFromClipboard(); }
		public void CopyToClipboard(InputAction.CallbackContext c) { if (c.performed) CopyToClipboard(); }
		public void PasteFromClipboard() {
			_pastedText = GUIUtility.systemCopyBuffer.Replace("\r","");
			_keyBuffer.Append(_pastedText);
		}

		public void CopyToClipboard() {
			Show.Log("copy mechanism from Input should be working automatically: " + GUIUtility.systemCopyBuffer.StringifySmall());
		}
		
		public static string ProcessInput(string input) {
			StringBuilder finalString = new StringBuilder();
			for (int i = 0; i < input.Length; ++i) {
				char c = input[i];
				switch (c) {
				case '\b':
					if (finalString.Length > 0) {
						finalString.Remove(finalString.Length - 1, 1);
					}
					break;
				//case '\n': return finalString.ToString();
				case '\r': break;
				case '\\':
					if (++i < input.Length) { c = input[i]; }
					finalString.Append(c); break;
				default: finalString.Append(c); break;
				}
			}
			return finalString.ToString();
		}
		public void FinishCurrentInput(InputAction.CallbackContext c) { if (c.performed) FinishCurrentInput(); }
		public void FinishCurrentInput() {
			string processedInput = ProcessInput(_keyBuffer.ToString());
			ConsoleDiff temp = lastInput;
			lastInput = console.Input;
			console.Input = temp;
			console.Input.Clear();
			console.RestartInput();
			Debug.Log("simple input: " + LastInputText);
			console.Write("\n");
			console.Console.RestartInput();
			RefreshLastInputError();
		}
		public void RefreshLastInputGood() => RefreshLastInput(submittedInputColorCode);
		public void RefreshLastInputError() => RefreshLastInput(errorInputColorCode);
		public void RefreshLastInput(int colorCode) {
			List<ConsoleDiffUnit> characterDifferences = lastInput.delta;
			for (int i = 0; i < characterDifferences.Count; ++i) {
				characterDifferences[i] = characterDifferences[i].WithDifferentColor((byte)colorCode);
			}
			console.Console.RefreshInput(lastInput);
		}

#if UNITY_EDITOR
		private void Reset() {
			UnityConsole console = GetComponent<UnityConsole>();

			UserInput uinput = GetComponent<UserInput>();
			string[] keyboardInputs = new string[defaultKeyboardKeys.Length];
			for (int i = 0; i < defaultKeyboardKeys.Length; i += 1) {
				keyboardInputs[i] = KbPrefix + defaultKeyboardKeys[i];
			}

			uinput.AddBindingIfMissing(new InputControlBinding("command line standard key input", "CmdLine/KeyInput",
				ControlType.Button, new EventBind(this, nameof(KeyInput)), keyboardInputs));

			uinput.AddBindingIfMissing(new InputControlBinding("command line submit input", CmdLine+"/SubmitInput",
				ControlType.Button, new EventBind(this, nameof(FinishCurrentInput)), KMap.Path("enter")));
			uinput.AddBindingIfMissing(new InputControlBinding("command line cursor move up", CmdLine+"/UpArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorUp)), KMap.Path("upArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("command line cursor move left", CmdLine+"/LeftArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorLeft)), KMap.Path("leftArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("command line cursor move down", CmdLine+"/DownArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorDown)), KMap.Path("downArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("command line cursor move right", CmdLine+"/RightArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorRight)), KMap.Path("rightArrow")));

			uinput.AddBindingIfMissing(new InputControlBinding("command line ctrl", CmdLine + "/ModifierCtrl",
				ControlType.Button, new EventBind(this, nameof(ModifierAltHandler)), KMap.Path(
					new string[] { "ctrl", "leftCtrl", "rightCtrl" })));
			uinput.AddBindingIfMissing(new InputControlBinding("command line alt", CmdLine + "/ModifierAlt",
				ControlType.Button, new EventBind(this, nameof(ModifierAltHandler)), KMap.Path(
					new string[] { "alt", "leftAlt", "rightAlt" })));
			uinput.AddBindingIfMissing(new InputControlBinding("command line shift", CmdLine + "/ModifierShift",
				ControlType.Button, new EventBind(this, nameof(ModifierShiftHandler)), KMap.Path(
					new string[] { "shift", "leftShift", "rightShift" })));

			uinput.AddBindingIfMissing(new InputControlBinding("command line ctrl decrease font", CmdCtrl+"/DecreaseFont",
				ControlType.Button, new EventBind(this, nameof(DecreaseFontSize)), KMap.Path("minus")));
			uinput.AddBindingIfMissing(new InputControlBinding("command line ctrl increase font", CmdCtrl+"/IncreaseFont",
				ControlType.Button, new EventBind(this, nameof(IncreaseFontSize)), KMap.Path("equals")));
			uinput.AddBindingIfMissing(new InputControlBinding("command line ctrl copy", CmdCtrl+"/Copy",
				ControlType.Button, new EventBind(this, nameof(CopyToClipboard)), KMap.Path("c")));
			uinput.AddBindingIfMissing(new InputControlBinding("command line ctrl paste", CmdCtrl+"/Paste",
				ControlType.Button, new EventBind(this, nameof(PasteFromClipboard)), KMap.Path("v")));

			uinput.AddActionMapToBind("CmdLine");
		}
#endif
		public void ModifierAltHandler(InputAction.CallbackContext ctx) {
			SpecialModifierHandler(ctx, CmdCtrl, ref ctrlIsDown);
		}
		public void SpecialAltHandler(InputAction.CallbackContext ctx) {
			SpecialModifierHandler(ctx, CmdAlt, ref altIsDown);
		}
		public void ModifierShiftHandler(InputAction.CallbackContext ctx) {
			SpecialModifierHandler(ctx, CmdShift, ref shiftIsDown);
		}
		private void SpecialModifierHandler(InputAction.CallbackContext ctx, string mapName, ref bool state) {
			UserInput uinput = GetComponent<UserInput>();
			bool oldState = state;
			switch (ctx.phase) {
				case InputActionPhase.Performed: state = true; break;
				case InputActionPhase.Canceled: state = false; break;
			}
			if (oldState != state) {
				if (state) {
					uinput.EnableActionMap(mapName);
				} else {
					uinput.DisableActionMap(mapName);
				}
				//Debug.Log(mapName+" " + state + " via " + ctx.control.path + " " + ctx.phase);
			}
		}
		public void IncreaseFontSize(InputAction.CallbackContext c) { if (c.performed) IncreaseFontSize(); }
		public void DecreaseFontSize(InputAction.CallbackContext c) { if (c.performed) DecreaseFontSize(); }
		public void MoveCursorUp(InputAction.CallbackContext c) { if (c.performed) MoveCursorUp(); }
		public void MoveCursorLeft(InputAction.CallbackContext c) { if (c.performed) MoveCursorLeft(); }
		public void MoveCursorDown(InputAction.CallbackContext c) { if (c.performed) MoveCursorDown(); }
		public void MoveCursorRight(InputAction.CallbackContext c) { if (c.performed) MoveCursorRight(); }
		public void ShiftWindowUp(InputAction.CallbackContext c) { if (c.performed) ShiftWindowUp(); }
		public void ShiftWindowLeft(InputAction.CallbackContext c) { if (c.performed) ShiftWindowLeft(); }
		public void ShiftWindowDown(InputAction.CallbackContext c) { if (c.performed) ShiftWindowDown(); }
		public void ShiftWindowRight(InputAction.CallbackContext c) { if (c.performed) ShiftWindowRight(); }
		public void IncreaseFontSize() { console.AddToFontSize(1); }
		public void DecreaseFontSize() { console.AddToFontSize(-1); }
		public void MoveCursorUp() { MovCur(Coord.Up); }
		public void MoveCursorLeft() { MovCur(Coord.Left); }
		public void MoveCursorDown() { MovCur(Coord.Down); }
		public void MoveCursorRight() { MovCur(Coord.Right); }
		public void ShiftWindowUp() { MovWin(Coord.Up); }
		public void ShiftWindowLeft() { MovWin(Coord.Left); }
		public void ShiftWindowDown() { MovWin(Coord.Down); }
		public void ShiftWindowRight() { MovWin(Coord.Right); }
		private static int AddStr(StringBuilder sb, string str) {
			//Debug.Log("buffer index: "+index);
			sb.Append(str);
			return str.Length;
		}

		private int ProcessKeyMappedTarget(object context, StringBuilder sb, bool alsoResolveNonText = true) {
			//Debug.Log(context);
			switch (context) {
				case char c:     return AddStr(sb, c.ToString());
				case string str: return AddStr(sb, str);
				case Action a: if (alsoResolveNonText) { a.Invoke(); } return 0;
				case Func<string> fstr: return AddStr(sb, fstr.Invoke());
			}
			return 0;
		}

		protected void KeyDown(KeyControl kc) {
			if (!enabled) {
				Debug.Log("ignoring "+kc.name+", ConsoleInput is disabled.");
				return;
			}
			keysDown[kc] = Environment.TickCount;
			bool isShift = IsShiftDown(), isCtrl = IsControlDown(), isNormal = !isShift && !isCtrl;
			Dictionary<KeyControl, KMap> keyMap = GetKeyMap();
			if (keyMap.TryGetValue(kc, out KMap kmap)) {
				object target = null;
				/**/ if (isCtrl) { target = kmap.ctrl; }
				else if (isShift) { target = kmap.shift; }
				else if (isNormal) { target = kmap.press; }
				if (target != null) {
					ProcessKeyMappedTarget(target, _keyBuffer, true);
				}
			}
		}

		protected void KeyUp(KeyControl kc) {
			if (!enabled) { return; }
			keysDown.Remove(kc);
		}

		public void KeyInput(InputAction.CallbackContext context) {
			//if (!_keyInputNormalAvailable) return;
			switch (context.phase) {
				// performed happens for each key, started only happens when the first keypress in a sequence happens
				case InputActionPhase.Performed: KeyDown(context.control as KeyControl); return;
				case InputActionPhase.Canceled: KeyUp(context.control as KeyControl); return;
			}
		}

		private void Awake() {
			console = GetComponent<UnityConsole>();
		}

		private void Start() {
			activeInputColorCode = console.AddConsoleColorPalette(activeInputColor);
			submittedInputColorCode = console.AddConsoleColorPalette(correctInput);
			errorInputColorCode = console.AddConsoleColorPalette(invalidInput);
			console.Write("testing");
		}

		public void WriteInputText(string inputText) {
			if (activeInputColorCode > 0) { console.Console.PushForeColor((byte)activeInputColorCode); }
			console.Console.WriteInput(inputText);
			if (activeInputColorCode > 0) { console.Console.PopForeColor(); }
		}
		void Update() {
			string txt = _keyBuffer.ToString();
			if (string.IsNullOrEmpty(txt)) { return; }
			_keyBuffer.Clear();
			WriteInputText(txt);
			console.Console.RefreshCursorValid();
		}
		private void OnDisable() {
			//Debug.Log("disabled console input");
		}
	}
}
