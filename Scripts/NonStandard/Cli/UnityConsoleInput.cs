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
		private UnityConsole console;
		/// <summary>
		/// very short-term key storage, processed and added to the <see cref="UnityConsole.io"/> each update
		/// </summary>
		protected StringBuilder _keyBuffer = new StringBuilder();

		//protected List<ConsoleDiffUnit> _replaced = new List<ConsoleDiffUnit>();

		protected int _indexInInputBuffer = 0;
		/// <summary>
		/// when a key was pressed
		/// </summary>
		protected Dictionary<KeyControl,int> keysDown = new Dictionary<KeyControl,int>();
		internal string _pastedText;

		public Color activeInputColor = new Color(0.25f, 0.75f, 1);
		public Color correctInput = new Color(0, 0.75f, 0);
		public Color invalidInput = new Color(0.75f, 0, 0); // TODO
		/// <summary>
		/// the color code to use for user input. will be set during initialization
		/// </summary>
		private int activeInputColorCode = -1;
		private int submittedInputColorCode = -1;

		Dictionary<KeyControl, KMap> _keyMap = null;
		[Tooltip("each of these will be prefixed by \"<Keyboard>/\" and added as a key bind for "+nameof(KeyInput))]
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

		public bool KeyAvailable => keysDown.Count > 0;

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
			[Keyboard.current.enterKey] = new KMap(new Action(FinishCurrentInput), '\n'),
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
		private struct KMap {
			public object normal, shift, ctrl;
			public KMap(object n, object s = null, object c = null) { normal = n; shift = s; ctrl = c; }
		}
		public static bool IsShiftDown() { return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed; }
		public static bool IsControlDown() { return Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed; }
		public static bool IsAltDown() { return Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed; }
		private void MovCur(Coord dir) {
			console.io.Cursor += dir;
			console.io.RefreshCursorValid();
		}
		private void MovWin(Coord dir) { console.ScrollRenderWindow(dir); }

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

		public void FinishCurrentInput() {
			string processedInput = ProcessInput(_keyBuffer.ToString());
			//Show.Log(_inputBuffer.ToString().StringifySmall()+" -> "+processedInput.StringifySmall());
			List<ConsoleDiffUnit> characterDifferences = console.io.Input.delta;
			for(int i = 0; i < characterDifferences.Count; ++i) {
				characterDifferences[i] = characterDifferences[i].WithDifferentColor((byte)submittedInputColorCode);
			}
			console.io.RefreshInputText();
			//Debug.Log("simple input: " + console.io.Input.ToSimpleString());
			console.io.Input.Clear();
			_indexInInputBuffer = 0;
			console.Write("\n");
			console.io.RestartInput();
			if (string.IsNullOrEmpty(processedInput)) { return; }
		}

#if UNITY_EDITOR
		private void Reset() {
			UnityConsole console = GetComponent<UnityConsole>();

			UserInput uinput = GetComponent<UserInput>();
			string[] keyboardInputs = new string[keyboardKeys.Length];
			for (int i = 0; i < keyboardKeys.Length; ++i) {
				keyboardInputs[i] = "<Keyboard>/" + keyboardKeys[i];
			}
			uinput.AddBindingIfMissing(new InputControlBinding("read keyboard input into the command line", "CmdLine/KeyInput",
				ControlType.Button, new EventBind(this, nameof(KeyInput)), keyboardInputs));
			uinput.AddActionMapToBind("CmdLine");
		}
#endif
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
		private static int AddStr(StringBuilder sb, int index, string str) {
			//Debug.Log("buffer index: "+index);
			if (index >= sb.Length) {
				sb.Append(str);
			} else {
				sb.Insert(index, str);
			}
			return str.Length;
		}
		private int ProcessKeyMappedTarget(object context, StringBuilder sb, int index, bool alsoResolveNonText = true) {
			//Debug.Log(context);
			if (index < 0) { index = sb.Length + 1 + index; }
			switch (context) {
				case char c:      return AddStr(sb, index, c.ToString());
				case string str: return AddStr(sb, index, str);
				case Action a: if (alsoResolveNonText) a.Invoke(); return 0;
				case Func<string> fstr: return AddStr(sb, index, fstr.Invoke());
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
			if (_KeyMap().TryGetValue(kc, out KMap kmap)) {
				object target = null;
				/**/ if (isCtrl) { target = kmap.ctrl; }
				else if (isShift) { target = kmap.shift; }
				else if (isNormal) { target = kmap.normal; }
				if (target != null) {
					_indexInInputBuffer += ProcessKeyMappedTarget(target, _keyBuffer, _indexInInputBuffer, true);
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
			console.Write("testing");
		}
		public void WriteInputText(string inputText) {
			if (activeInputColorCode > 0) { console.io.PushForeColor((byte)activeInputColorCode); }
			console.io.WriteInput(inputText);
			if (activeInputColorCode > 0) { console.io.PopForeColor(); }
		}
		void Update() {
			string txt = _keyBuffer.ToString();
			if (string.IsNullOrEmpty(txt)) { return; }
			_keyBuffer.Clear();
			WriteInputText(txt);
			console.io.RefreshCursorValid();
		}
		private void OnDisable() {
			//Debug.Log("disabled console input");
		}
	}
}
