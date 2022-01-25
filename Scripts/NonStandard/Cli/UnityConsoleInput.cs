using NonStandard.Data;
using NonStandard.Extension;
using NonStandard.Inputs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace NonStandard.Cli {
	[RequireComponent(typeof(UnityConsole)),RequireComponent(typeof(UserInput))]
	public class UnityConsoleInput : MonoBehaviour {
		protected StringBuilder currentLine = new StringBuilder();
		protected int indexInCurrentLine = 0;
		/// <summary>
		/// when a key was pressed
		/// </summary>
		protected Dictionary<KeyControl,int> keysDown = new Dictionary<KeyControl,int>();
		internal string _pastedText;
		/// <summary>
		/// added to by the <see cref="ReadLine(Show.PrintFunc)"/> function
		/// </summary>
		protected List<Show.PrintFunc> tempLineInputListeners = new List<Show.PrintFunc>();
		/// <summary>
		/// if false, typing (and pasting) will not add characters to the current input, or to the console
		/// </summary>
		public bool textInput = true;
		public bool clipboardPaste = true;
		private bool _keyMapInitialized;
		private bool _keyInputNormalAvailable = true;
		private bool _keyInputControlAvailable = false;

		public Color inputColor = new Color(0, 1, 0);
		int inputColorCode = -1;
		private UnityConsole console;

		public UnityEvent_string inputListener;

		private struct KMap {
			public object normal, shift, ctrl;
			public KMap(object n, object s=null, object c=null) { normal = n; shift = s; ctrl = c; }
		}
		Dictionary<KeyControl, KMap> _keyMap = null;
		private Dictionary<KeyControl, KMap> _KeyMap() => _keyMap != null
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
			[Keyboard.current.minusKey] = new KMap('-', '_', new Action(DecreaseFontSize)),
			[Keyboard.current.equalsKey] = new KMap('=', '+', new Action(IncreaseFontSize)),
			[Keyboard.current.tabKey] = new KMap('\t'),
			[Keyboard.current.leftBracketKey] = new KMap('[', '{'),
			[Keyboard.current.rightBracketKey] = new KMap(']', '}'),
			[Keyboard.current.backslashKey] = new KMap('\\', '|'),
			[Keyboard.current.semicolonKey] = new KMap(';', ':'),
			[Keyboard.current.quoteKey] = new KMap('\'', '\"'),
			[Keyboard.current.commaKey] = new KMap(',', '<'),
			[Keyboard.current.periodKey] = new KMap('.', '>'),
			[Keyboard.current.slashKey] = new KMap('/', '?'),
			[Keyboard.current.spaceKey] = new KMap(' ', ' '),
			[Keyboard.current.backspaceKey] = new KMap('\b', '\b'),
			[Keyboard.current.enterKey] = new KMap(null, '\n'),
			[Keyboard.current.aKey] = new KMap('a', 'A'),
			[Keyboard.current.bKey] = new KMap('b', 'B'),
			[Keyboard.current.cKey] = new KMap('c', 'C', new Action(CopyToClipboard)),
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
			[Keyboard.current.vKey] = new KMap('v', 'V', new Action(PasteFromClipboard)),
			[Keyboard.current.wKey] = new KMap('w', 'W'),
			[Keyboard.current.xKey] = new KMap('x', 'X'),
			[Keyboard.current.yKey] = new KMap('y', 'Y'),
			[Keyboard.current.zKey] = new KMap('z', 'Z'),
			[Keyboard.current.upArrowKey] = new KMap(new Action(MoveCursorUp), null),
			[Keyboard.current.leftArrowKey] = new KMap(new Action(MoveCursorLeft), null),
			[Keyboard.current.downArrowKey] = new KMap(new Action(MoveCursorDown), null),
			[Keyboard.current.rightArrowKey] = new KMap(new Action(MoveCursorRight), null),
			};
		public static bool IsShiftDown() { return Keyboard.current.shiftKey.isPressed; }
		public static bool IsControlDown() { return Keyboard.current.ctrlKey.isPressed; }
		public static bool IsAltDown() { return Keyboard.current.altKey.isPressed; }
		/// <summary>
		/// each of these should be prefixed by "<Keyboard>/"
		/// </summary>
		public string[] keyboardKeys = new string[] {
			//"escape","f1","f2","f3","f4","f5","f6","f7","f8","f9","f10","f11","f12","printScreen","delete",
			"backquote", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "minus", "equals", "backspace", "home",
			"tab","q","w","e","r","t","y","u","i","o","p","leftBracket","rightBracket","backslash","pageUp",
			"capsLock","a","s","d","f","g","h","j","k","l","semicolon","quote","enter","pageDown",
			/*"leftShift",*/"z","x","c","v","b","n","m","comma","period","slash","rightShift","end",
			/*"leftControl","leftAlt",*/"space",//"rightAlt","rightControl",
			"upArrow","leftArrow","downArrow","rightArrow",
			//"numpad1","numpad2","numpad3","numpad4","numpad5","numpad6","numpad7","numpad8","numpad9","numpad0",
		};
		private void MovCur(Coord dir) { console.Cursor += dir; }
		private void MovWin(Coord dir) { console.ScrollRenderWindow(dir); ; }
		public void PasteFromClipboard() {
			if (!clipboardPaste) return;
			_pastedText = GUIUtility.systemCopyBuffer.Replace("\r","");
		}
		public void CopyToClipboard() {
			Show.Log("copy mechanism from Input should be working: " + GUIUtility.systemCopyBuffer.StringifySmall());
		}
		public bool KeyAvailable {
			get {
				//foreach (KeyValuePair<KeyControl, KMap> kvp in KepMap()) { if (kvp.Key.IsHeld()) return true; }
				//return false;
				return keysDown.Count > 0;
			}
		}
		//public List<KCode> GetKeyAvailabe(List<KCode> list) {
		//	foreach (KeyValuePair<KCode, KMap> kvp in KepMap()) { if (kvp.Key.IsHeld()) { list.Add(kvp.Key); } }
		//	return list;
		//}
		//public List<KCode> GetKeyDown(List<KCode> list) {
		//	foreach (KeyValuePair<KCode, KMap> kvp in KepMap()) { if (kvp.Key.IsDown()) { list.Add(kvp.Key); } }
		//	return list;
		//}
		//public void Read(Action<KCode> kCodeListener) { tempKeyCodeListeners.Add(kCodeListener); }
		public void ReadLine(Show.PrintFunc lineInputListener) { tempLineInputListeners.Add(lineInputListener); }
		public string ResolveInput(bool alsoResolveNonText) {
			StringBuilder sb = textInput ? new StringBuilder() : null;
			AddPastedTextToInput(sb);
			AddKeysToInput(sb, alsoResolveNonText);
			return sb?.ToString() ?? null;
		}
		void AddPastedTextToInput(StringBuilder sb) {
			if (_pastedText == null) { return; }
			sb.Append(_pastedText);
			_pastedText = null;
		}
		void AddKeysToInput(StringBuilder sb, bool alsoResolveNonText = true) {
			keysDown.Clear();
			if (!KeyAvailable) return;
			//GetKeyDown(keysDown);
			bool isShift = IsShiftDown(), isCtrlDown = IsControlDown(), isNormal = !isShift && !isCtrlDown;
			foreach (var kvp in keysDown) {
				if (_keyMap.TryGetValue(kvp.Key, out KMap kmap)) {
					if (isCtrlDown) { DoTheThing(kmap.ctrl); } if (isShift) { DoTheThing(kmap.shift); } else if (isNormal) { DoTheThing(kmap.normal); }
				}
			}
			void DoTheThing(object context) {
				switch (context) {
				case char c: sb?.Append(c); break;
				case Action a: if (alsoResolveNonText) a.Invoke(); break;
				}
			}
		}
		public static string ProcessInput(string currentLine) {
			StringBuilder finalString = new StringBuilder();
			for (int i = 0; i < currentLine.Length; ++i) {
				char c = currentLine[i];
				switch (c) {
				case '\b':
					if (finalString.Length > 0) {
						finalString.Remove(finalString.Length - 1, 1);
					}
					break;
				//case '\n': return finalString.ToString();
				case '\r': break;
				case '\\':
					if (++i < currentLine.Length) { c = currentLine[i]; }
					finalString.Append(c); break;
				default: finalString.Append(c); break;
				}
			}
			return finalString.ToString();
		}
		bool IsListeningToLine() { return tempLineInputListeners != null && tempLineInputListeners.Count > 0; }
		public void CommandLineUpdate(string txt) {
			currentLine.Insert(indexInCurrentLine, txt);
			indexInCurrentLine += txt.Length;
		}
		public void FinishCurrentInput() {
			string processedInput = ProcessInput(currentLine.ToString());
			//Show.Log(currentLine.ToString().StringifySmall()+" -> "+processedInput.StringifySmall());
			currentLine.Clear();
			indexInCurrentLine = 0;
			console.Write("\n");
			console.body.RestartWriteCursor();
			if (string.IsNullOrEmpty(processedInput)) { return; }
			inputListener.Invoke(processedInput);
			if (IsListeningToLine()) {
				tempLineInputListeners.ForEach(action => action.Invoke(processedInput));
				tempLineInputListeners.Clear();
			}
		}
#if UNITY_EDITOR
		private void Reset() {
			UnityConsole console = GetComponent<UnityConsole>();
			UnityConsoleCommander consoleCommander = GetComponent<UnityConsoleCommander>();
			if (consoleCommander != null) {
				EventBind.On(inputListener, consoleCommander, nameof(consoleCommander.DoCommand));
			}
			UserInput uinput = GetComponent<UserInput>();
			string[] keyboardInputs = new string[keyboardKeys.Length];
			for (int i = 0; i < keyboardKeys.Length; ++i) {
				keyboardInputs[i] = "<Keyboard>/" + keyboardKeys[i];
			}
			uinput.AddBindingIfMissing(new InputControlBinding("unpause the game and hide the command line console", "CmdLine/KeyInput",
				ControlType.Button, new EventBind(this, nameof(KeyInput)), keyboardInputs));

			//KeyBind(KCode.Equals, KModifier.AnyCtrl, "+ console font", nameof(IncreaseFontSize), target: this);
			//KeyBind(KCode.Minus, KModifier.AnyCtrl, "- console font", nameof(DecreaseFontSize), target: this);
			//KeyBind(KCode.Return, KModifier.NoShift, "submit console input", nameof(FinishCurrentInput), target:this);
			//KeyBind(KCode.C, KModifier.AnyCtrl, "copy from command console", nameof(CopyToClipboard), target: this);
			//KeyBind(KCode.V, KModifier.AnyCtrl, "paste into command console", nameof(PasteFromClipboard), target: this);
			//KeyBind(KCode.UpArrow, KModifier.AnyAlt, "move cursor up", nameof(MoveCursorUp), target: this);
			//KeyBind(KCode.LeftArrow, KModifier.AnyAlt, "move cursor left", nameof(MoveCursorLeft), target: this);
			//KeyBind(KCode.DownArrow, KModifier.AnyAlt, "move cursor down", nameof(MoveCursorDown), target: this);
			//KeyBind(KCode.RightArrow, KModifier.AnyAlt, "move cursor right", nameof(MoveCursorRight), target: this);
			//KeyBind(KCode.UpArrow, KModifier.AnyShift, "shift window up", nameof(ShiftWindowUp), target: this);
			//KeyBind(KCode.LeftArrow, KModifier.AnyShift, "shift window left", nameof(ShiftWindowLeft), target: this);
			//KeyBind(KCode.DownArrow, KModifier.AnyShift, "shift window down", nameof(ShiftWindowDown), target: this);
			//KeyBind(KCode.RightArrow, KModifier.AnyShift, "shift window right", nameof(ShiftWindowRight), target: this);
		}
#endif
		public string[] actionsEnabledWithControl = new string[] {
			"CmdLine/biggerFont","CmdLine/smallerFont","CmdLine/biggerFont","CmdLine/copy","CmdLine/paste",
		};
		public string[] actionsEnabledWithoutControl = new string[] {
			"CmdLine/submitInput",
		};
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
		void _KeyDown(KeyControl kc) { keysDown[kc] = Environment.TickCount; }
		void _KeyUp(KeyControl kc) { keysDown.Remove(kc); }
		public void KeyInput(InputAction.CallbackContext context) {
			if (!_keyInputNormalAvailable) return;
			switch (context.phase) {
				case InputActionPhase.Started: _KeyDown(context.control as KeyControl); return;
				case InputActionPhase.Canceled: _KeyUp(context.control as KeyControl); return;
			}
		}
		public void KeyInputControl(InputAction.CallbackContext context) {
			switch (context.phase) {
				case InputActionPhase.Started: _keyInputNormalAvailable = false; return;
				case InputActionPhase.Canceled: _keyInputNormalAvailable = true; return;
			}
		}
		private void Awake() {
			console = GetComponent<UnityConsole>();
			if (!_keyMapInitialized) {
				_keyMapInitialized = true;
			}
		}
		private void Start() {
			inputColorCode = console.AddConsoleColor(inputColor);
		}
		public void WriteInputText(string inputText) {
			if (inputColorCode > 0) { console.PushForeColor((byte)inputColorCode); }
			console.Write(inputText, true);
			if (inputColorCode > 0) { console.PopForeColor(); }
		}
		void Update() {
			string txt = ResolveInput(true);
			if (string.IsNullOrEmpty(txt)) { return; }
			WriteInputText(txt);
			CommandLineUpdate(txt);
		}
	}
}