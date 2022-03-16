// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using Nonstandard.Inputs;
using NonStandard.Data;
using NonStandard.Inputs;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

namespace NonStandard.Cli {
	/// <summary>
	/// Connects input to a <see cref="UnityConsoleOutput"/> using a <see cref="UserInput"/> handler and manages that input
	/// </summary>
	[RequireComponent(typeof(UnityConsoleOutput))]
	public class UnityConsoleInput : KeyboardInput {
		private UnityConsoleOutput _cout;
		private UnityConsole _console;

		#region Unity Lifecycle
#if UNITY_EDITOR
		public void ResetInternal() => Reset();
		protected override void Reset() {
			keyMapNames.normal = "console";
			keyMapNames.ctrl = "consoleCtrl";
			keyMapNames.alt = "consoleAlt";
			keyMapNames.shift = "consoleShift";
			base.Reset();
			UnityConsoleOutput console = GetComponent<UnityConsoleOutput>();
			BindDefaultKeyInput();
			EventBind.IfNotAlready(callbacks.OnTextSubmit, this, nameof(InputErrorCallback));
			EditorUtility.SetDirty(this);
		}

		private void BindDefaultKeyInput() {
			UserInput uinput = GetComponent<UserInput>();
			// bind expected non-key-char console inputs
			uinput.AddBindingIfMissing(new InputControlBinding("console submit input", keyMapNames.normal + "/SubmitInput",
				ControlType.Button, new EventBind(this, nameof(FinishCurrentInput)), KeyboardInput.Path("enter")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move up", keyMapNames.normal + "/UpArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorUp)), KeyboardInput.Path("upArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move left", keyMapNames.normal + "/LeftArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorLeft)), KeyboardInput.Path("leftArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move down", keyMapNames.normal + "/DownArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorDown)), KeyboardInput.Path("downArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move right", keyMapNames.normal + "/RightArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorRight)), KeyboardInput.Path("rightArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl decrease font", keyMapNames.ctrl + "/DecreaseFont",
				ControlType.Button, new EventBind(this, nameof(DecreaseFontSize)), KeyboardInput.Path("minus")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl increase font", keyMapNames.ctrl + "/IncreaseFont",
				ControlType.Button, new EventBind(this, nameof(IncreaseFontSize)), KeyboardInput.Path("equals")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl copy", keyMapNames.ctrl + "/Copy",
				ControlType.Button, new EventBind(this, nameof(CopyToClipboard)), KeyboardInput.Path("c")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl paste", keyMapNames.ctrl + "/Paste",
				ControlType.Button, new EventBind(this, nameof(PasteFromClipboard)), KeyboardInput.Path("v")));
		}
#endif
		protected override void Awake() {
			base.Awake();
			_cout = GetComponent<UnityConsoleOutput>();
			_console = GetComponent<UnityConsole>();
		}

		private void Start() {
			colors.ApplyColorsTo(_console);
			_cout.Write("testing");
		}

		void Update() {
			string txt = Flush();
			if (string.IsNullOrEmpty(txt)) { return; }
			WriteInputText(txt);
			_console.State.RefreshCursorValid();
		}
		#endregion Unity Lifecycle

		#region Input Processing
		/// <summary>
		/// temporary storage area for pasted text. will be ingested as key input shortly.
		/// </summary>
		internal string _pastedText;
		/// <summary>
		/// the last entered input (including it's characters on the screen) is saved here
		/// </summary>
		private ConsoleDiff _lastInput = new ConsoleDiff();
		public ColorSemantics colors = new ColorSemantics();

		public Callbacks callbacks;
		public string LastInputText => _lastInput.ToSimpleString();

		[System.Serializable] public class Callbacks {
			public bool enable = true;
			public UnityEvent_string OnTextSubmit = new UnityEvent_string();
		}

		[Serializable] public class ColorSemantics {
			public Color activeInput = new Color(0.25f, 0.75f, 1);
			public Color correctInput = new Color(0, 0.75f, 0);
			public Color invalidInput = new Color(0.75f, 0, 0);
			/// <summary>
			/// the color code to use for user input. will be set during initialization
			/// </summary>
			[HideInInspector] public int codeActiveInput, codeCorrectInput, codeInvalidInput;
			public void ApplyColorsTo(UnityConsole console) {
				codeActiveInput = console.AddConsoleColorPalette(activeInput);
				codeCorrectInput = console.AddConsoleColorPalette(correctInput);
				codeInvalidInput = console.AddConsoleColorPalette(invalidInput);
			}
		}
		#endregion Input Processing

		#region Input Callbacks
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
		public void PasteFromClipboard(InputAction.CallbackContext c) { if (c.performed) PasteFromClipboard(); }
		public void CopyToClipboard(InputAction.CallbackContext c) { if (c.performed) CopyToClipboard(); }
		public void FinishCurrentInput(InputAction.CallbackContext c) { if (c.performed) FinishCurrentInput(); }
		#endregion Input Callbacks

		#region Console Controls
		public void RefreshLastInputGood() => RefreshLastInput(colors.codeCorrectInput);
		public void RefreshLastInputError() => RefreshLastInput(colors.codeInvalidInput);
		public void RefreshLastInput(int colorCode) {
			List<ConsoleDiffUnit> characterDifferences = _lastInput.delta;
			for (int i = 0; i < characterDifferences.Count; ++i) {
				characterDifferences[i] = characterDifferences[i].WithDifferentColor((byte)colorCode);
			}
			_console.State.RefreshInput(_lastInput);
		}

		private void MovCur(Coord dir) {
			_console.State.CursorPosition += dir;
			_console.State.RefreshCursorValid();
		}

		private void MovWin(Coord dir) {
			_console.State.Window.ScrollRenderWindow(dir);
			_console.State.textNeedsRefresh = true;
		}


		public void PasteFromClipboard() {
			_pastedText = GUIUtility.systemCopyBuffer.Replace("\r","");
			KeyBuffer.Append(_pastedText);
		}

		public void CopyToClipboard() {
			//Show.Log(GUIUtility.systemCopyBuffer.StringifySmall());
		}
		public void IncreaseFontSize() { _cout.AddToFontSize(1); }
		public void DecreaseFontSize() { _cout.AddToFontSize(-1); }
		public void MoveCursorUp() { MovCur(Coord.Up); }
		public void MoveCursorLeft() { MovCur(Coord.Left); }
		public void MoveCursorDown() { MovCur(Coord.Down); }
		public void MoveCursorRight() { MovCur(Coord.Right); }
		public void ShiftWindowUp() { MovWin(Coord.Up); }
		public void ShiftWindowLeft() { MovWin(Coord.Left); }
		public void ShiftWindowDown() { MovWin(Coord.Down); }
		public void ShiftWindowRight() { MovWin(Coord.Right); }
		public void FinishCurrentInput() {
			ConsoleDiff temp = _lastInput;
			_lastInput = _console.Input;
			_console.Input = temp;
			_console.Input.Clear();
			_console.RestartInput();
			_cout.Write("\n");
			_console.State.RestartInput();
			string input = LastInputText;
			if (callbacks.enable) {
				callbacks.OnTextSubmit.Invoke(input);
			} else {
				Debug.LogWarning("ignoring " + input);
			}
		}

		public void InputErrorCallback(string input) {
			Debug.LogError(input);
			RefreshLastInputError();
		}

		public void InputGoodCallback(string input) {
			Debug.Log(input);
			RefreshLastInputGood();
		}

		public void WriteInputText(string inputText) {
			if (colors.codeActiveInput > 0) { _console.State.PushForeColor((byte)colors.codeActiveInput); }
			_console.State.WriteInput(inputText);
			if (colors.codeActiveInput > 0) { _console.State.PopForeColor(); }
		}
		#endregion Console Controls
	}
}
