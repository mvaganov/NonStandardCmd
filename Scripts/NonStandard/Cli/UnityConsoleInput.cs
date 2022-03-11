using Nonstandard.Inputs;
using NonStandard.Data;
using NonStandard.Inputs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NonStandard.Cli {
	/// <summary>
	/// Connects input to a <see cref="UnityConsole"/> using a <see cref="UserInput"/> handler and manages that input
	/// </summary>
	[RequireComponent(typeof(UnityConsole))]
	public class UnityConsoleInput : KeyboardInput {
		private UnityConsole _console;

		#region Unity Lifecycle
#if UNITY_EDITOR
		protected override void Reset() {
			keyMapName.normal = "console";
			keyMapName.ctrl = "consoleCtrl";
			keyMapName.alt = "consoleAlt";
			keyMapName.shift = "consoleShift";
			base.Reset();
			UnityConsole console = GetComponent<UnityConsole>();
			BindDefaultKeyInput();
			EventBind.IfNotAlready(OnTextSubmit, this, nameof(InputErrorCallback));
		}

		private void BindDefaultKeyInput() {
			UserInput uinput = GetComponent<UserInput>();
			// bind expected non-key-char console inputs
			uinput.AddBindingIfMissing(new InputControlBinding("console submit input", keyMapName.normal + "/SubmitInput",
				ControlType.Button, new EventBind(this, nameof(FinishCurrentInput)), KeyboardInput.Path("enter")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move up", keyMapName.normal + "/UpArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorUp)), KeyboardInput.Path("upArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move left", keyMapName.normal + "/LeftArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorLeft)), KeyboardInput.Path("leftArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move down", keyMapName.normal + "/DownArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorDown)), KeyboardInput.Path("downArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console cursor move right", keyMapName.normal + "/RightArrow",
				ControlType.Button, new EventBind(this, nameof(MoveCursorRight)), KeyboardInput.Path("rightArrow")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl decrease font", keyMapName.ctrl + "/DecreaseFont",
				ControlType.Button, new EventBind(this, nameof(DecreaseFontSize)), KeyboardInput.Path("minus")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl increase font", keyMapName.ctrl + "/IncreaseFont",
				ControlType.Button, new EventBind(this, nameof(IncreaseFontSize)), KeyboardInput.Path("equals")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl copy", keyMapName.ctrl + "/Copy",
				ControlType.Button, new EventBind(this, nameof(CopyToClipboard)), KeyboardInput.Path("c")));
			uinput.AddBindingIfMissing(new InputControlBinding("console ctrl paste", keyMapName.ctrl + "/Paste",
				ControlType.Button, new EventBind(this, nameof(PasteFromClipboard)), KeyboardInput.Path("v")));
		}
#endif
		protected override void Awake() {
			base.Awake();
			_console = GetComponent<UnityConsole>();
		}

		private void Start() {
			colors.ApplyColorsTo(_console);
			_console.Write("testing");
		}

		void Update() {
			string txt = Flush();
			if (string.IsNullOrEmpty(txt)) { return; }
			WriteInputText(txt);
			_console.Console.RefreshCursorValid();
		}

		private void OnDisable() {
			//Debug.Log("disabled console input");
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

		public UnityEvent_string OnTextSubmit;
		public string LastInputText => _lastInput.ToSimpleString();

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

		public void RefreshLastInputGood() => RefreshLastInput(colors.codeCorrectInput);
		public void RefreshLastInputError() => RefreshLastInput(colors.codeInvalidInput);
		public void RefreshLastInput(int colorCode) {
			List<ConsoleDiffUnit> characterDifferences = _lastInput.delta;
			for (int i = 0; i < characterDifferences.Count; ++i) {
				characterDifferences[i] = characterDifferences[i].WithDifferentColor((byte)colorCode);
			}
			_console.Console.RefreshInput(_lastInput);
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
		private void MovCur(Coord dir) {
			_console.Console.Cursor += dir;
			_console.Console.RefreshCursorValid();
		}

		private void MovWin(Coord dir) { _console.ScrollRenderWindow(dir); }

		public void PasteFromClipboard() {
			_pastedText = GUIUtility.systemCopyBuffer.Replace("\r","");
			_keyBuffer.Append(_pastedText);
		}

		public void CopyToClipboard() {
			//Show.Log(GUIUtility.systemCopyBuffer.StringifySmall());
		}
		public void IncreaseFontSize() { _console.AddToFontSize(1); }
		public void DecreaseFontSize() { _console.AddToFontSize(-1); }
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
			_console.Write("\n");
			_console.Console.RestartInput();
			OnTextSubmit.Invoke(LastInputText);
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
			if (colors.codeActiveInput > 0) { _console.Console.PushForeColor((byte)colors.codeActiveInput); }
			_console.Console.WriteInput(inputText);
			if (colors.codeActiveInput > 0) { _console.Console.PopForeColor(); }
		}
		#endregion Console Controls
	}
}
