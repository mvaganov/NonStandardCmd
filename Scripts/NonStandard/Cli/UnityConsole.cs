// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using NonStandard.Data;
using NonStandard.Extension;
using NonStandard.Inputs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NonStandard.Cli {
#if NONSTANDARD_UTILITY
	[InitializeOnLoad] public static class Utility_Define { static Utility_Define() { Utility.Define.Add("NONSTANDARD_CONSOLE"); } }
#endif
	/// <summary>
	/// a facade class that organizes the rest of the console classes
	/// </summary>
	public class UnityConsole : MonoBehaviour {
		public const int MaxColorPaletteSize = 0xff;
		public LogOptions logging;
		public UnityConsoleCalculations calc;
		private UnityConsoleOutput _cout;
		public ConsoleState State;
		public ColorSettings colorSettings = new ColorSettings();
		public UnityConsoleCursor cursorUI;
		public bool watchMouse = true;
		public bool debugShowMouseOverTile = true;
		public Coord mouseOver;
		public UnityConsoleOutput cout => _cout ? _cout : _cout = GetComponent<UnityConsoleOutput>();
		// TODO if Input is not empty, and new output is being written, un-write the Input, then write the new output, then re-write the Input at the new cursor.
		public ConsoleDiff Input { get => State.Input; set => State.Input = value; }
		public bool CursorVisible {
			get => cursorUI.gameObject.activeSelf;
			set => cursorUI.gameObject.SetActive(value);
		}
		public int CursorSize {
			get { return (int)(cursorUI.transform.localScale.MagnitudeManhattan() / 3); }
			set { cursorUI.transform.localScale = Vector3.one * (value / 100f); }
		}

		[Flags] public enum LogOptions { None = 0, UnityDebugLog = 1
#if NONSTANDARD_SHOW
			, NonstandardShowLog = 2
#endif
		}
		public void RefreshCursorPosition() => cursorUI.RefreshCursorPosition(this);
		public void PushForeColor(ConsoleColor color) => State.PushForeColor(color);
		public void PushForeColor(byte color) => State.PushForeColor(color);
		public void PopForeColor() => State.PopForeColor();

		private void Reset() {
			UserInput ui = GetComponent<UserInput>();
			UnityConsoleInput uci = GetComponent<UnityConsoleInput>();
			UnityConsoleUiToggle ucut = GetComponent<UnityConsoleUiToggle>();
			if (ui != null) {
				ui.InputControlBindings.Clear();
				ui.ActionMapToBindOnStart.Clear();
			}
			if (uci != null) {
				uci.KeyInput.Clear();
			}
			uci.ResetInternal();
			ucut.ResetInternal();
			cursorUI = GetComponentInChildren<UnityConsoleCursor>();
		}

		private void Awake() {
			colorSettings.FillInDefaultPalette();
			if (logging.HasFlag(LogOptions.UnityDebugLog)) {
				Application.logMessageReceived += Application_logMessageReceived;
			}
#if NONSTANDARD_SHOW
			if (logging.HasFlag(LogOptions.NonstandardShowLog)) {
				Show.Route.onLog += Log;
				Show.Route.onError += LogError;
				Show.Route.onWarning += LogWarning;
			}
#endif
			Show.Log("Show.Log test "+logging);
			Debug.Log("Debug.Log test "+logging);
			UpdateConsoleCalculations();
		}

		private void Update() {
			UpdateConsoleCalculations();
			if (watchMouse) {
				WatchMouse();
			}
		}

		private void OnDestroy() {
			Application.logMessageReceived -= Application_logMessageReceived;
#if NONSTANDARD_SHOW
			Show.Route.onLog -= Log;
			Show.Route.onError -= LogError;
			Show.Route.onWarning -= LogWarning;
#endif
		}

		//public GraphicRaycaster canvasRaycaster;

		private void UpdateConsoleCalculations() {
			if (calc == null) {
				calc = new UnityConsoleCalculations(_cout.inputField.textComponent);
			} else {
				calc.Update();
			}
		}
		private List<RaycastResult> _raycastResultList = new List<RaycastResult>();
		private void WatchMouse() {
			UnityConsoleTooltip tooltip = GetComponent<UnityConsoleTooltip>();
			if (tooltip == null) return;
			PointerEventData pointerEvent = new PointerEventData(EventSystem.current);
			pointerEvent.position = Mouse.current.position.ReadValue();
			_raycastResultList.Clear();
			EventSystem.current.RaycastAll(pointerEvent, _raycastResultList);
			GameObject uiElement = _raycastResultList.Count > 0 ? _raycastResultList[0].gameObject : null;
			if (uiElement && IsMyChild(uiElement.transform)) {
				Vector3 worldPos = _raycastResultList[0].worldPosition;
				mouseOver = calc.GetCursorIndex(worldPos);
				if (debugShowMouseOverTile) {
					calc.OutlineTile(mouseOver, Color.cyan);
				}
				List<TextSpan> spans = cout.GetTag(mouseOver);
				string text = spans.Count == 1 ? spans[0].tag.ToString() : null;
				if (spans.Count > 1) {
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < spans.Count; ++i) {
						if (i > 0) { sb.Append("\n"); }
						sb.Append(spans[i]);
					}
					text = sb.ToString();
				}
				tooltip.Tooltip(worldPos, text, 0);
			}
		}
		private bool IsMyChild(Transform t) {
			Transform self = transform;
			do {
				t = t.parent;
			} while (t != null && t != self);
			return t == self;
		}

		private void Application_logMessageReceived(string text, string stackTrace, LogType type) {
			Coord whereCursorStarted = State.Cursor.position2d;
			switch (type) {
				case LogType.Error:
				case LogType.Exception:
				case LogType.Assert:
					WriteLine(text);
					break;
				case LogType.Warning:
					WriteLine(text);
					break;
				case LogType.Log:
					WriteLine(text);
					break;
			}
			Coord whereCursorEnded = State.Cursor.position2d;
			_cout.AddTagToTextSpan(whereCursorStarted, whereCursorEnded, stackTrace);
		}

		[System.Serializable] public class ColorSettings {
			[Range(0, 1)] public float foregroundAlpha = 1f;
			[Range(0, 1)] public float backgroundAlpha = 0.5f;
			public List<Color> ConsoleColorPalette = new List<Color>(Array.ConvertAll(ColorRGBA.defaultColors, c => (Color)c));
			public void FillInDefaultPalette() {
				const int BaseColorCount = 16;
				const bool forceDefaultColors = true;
				if (forceDefaultColors) {
					int count = Mathf.Min(ConsoleColorPalette.Count, BaseColorCount);
					for (int i = 0; i < count; ++i) {
						ConsoleColorPalette[i] = (Color)(ColorRGBA)(ConsoleColor)i;
					}
				}
				while (ConsoleColorPalette.Count < BaseColorCount) {
					ConsoleColorPalette.Add((Color)(ColorRGBA)(ConsoleColor)ConsoleColorPalette.Count);
				}
			}
			public int AddConsoleColor(ColorRGBA colorRgba) {
				if (ConsoleColorPalette.Count >= MaxColorPaletteSize) {
					Show.Error("too many colors");
					return -1;
				}
				int index = ConsoleColorPalette.IndexOf(colorRgba);
				if (index >= 0) return index;
				ConsoleColorPalette.Add(colorRgba);
				return ConsoleColorPalette.Count - 1;
			}
		}

		public void RestartInput() => State.RestartInput();
		public int AddConsoleColorPalette(ColorRGBA colorRgba) { return colorSettings.AddConsoleColor(colorRgba); }
		public int GetConsoleColorPaletteCount() { return colorSettings.ConsoleColorPalette.Count; }
		public byte DefaultForegroundColor => State.Output.defaultColors.fore;
		public byte DefaultBackgroundColor => State.Output.defaultColors.back;
		public ColorRGBA GetConsoleColor(int code, bool foreground) {
			if (code < 0 || code == Col.DefaultColorIndex) {
				code = foreground ? DefaultForegroundColor : DefaultBackgroundColor;
			}
			return colorSettings.ConsoleColorPalette[code];
		}
		public void MoveCursor(Coord dir) {
			State.CursorPosition += dir;
			State.RefreshCursorValid();
		}
		public void MoveWindowView(Coord dir) {
			State.Window.ScrollRenderWindow(dir);
			State.textNeedsRefresh = true;
		}
		public void WriteInputText(string inputText, byte color = 0) {
			State.KeepInputCursorOnInput();
			State.WriteInputWithColor(inputText, color);
		}
		private string StackTrace(int stackOffset = 0) {
			StringBuilder sb = new StringBuilder();
			IList<string> stack = ReflectionExtension.GetStackFullPath(-1, 3);
			int start = 0;
			for(int i = 0; i < stack.Count; ++i) {
				string path = stack[i];
				int fileStart = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
				if (fileStart < 0) fileStart = path.LastIndexOf(System.IO.Path.AltDirectorySeparatorChar);
				string file = path.Substring(fileStart + 1);
				stack[i] = file;
				if (file.StartsWith("Show.cs:")) { start = i + 1; }
			}
			for (int i = start; i < stack.Count; ++i) {
				sb.Append(stack[i]);
			}
			return sb.ToString();
		}
		public void Log(object message) => Log(message.ToString(), 2);
		public void LogError(object message) => LogError(message.ToString(), 2);
		public void LogWarning(object message) => LogWarning(message, 2);
		public void Log(object message, int stackOffset) => Log(message.ToString(), StackTrace(stackOffset));
		public void LogError(object message, int stackOffset) => LogError(message.ToString(), StackTrace(stackOffset));
		public void LogWarning(object message, int stackOffset) => LogWarning(message, StackTrace(stackOffset));
		public void Log(object message, string tag) => Application_logMessageReceived(message.ToString(),
			tag, LogType.Log);
		public void LogError(object message, string tag) => Application_logMessageReceived(message.ToString(),
			tag, LogType.Error);
		public void LogWarning(object message, string tag) => Application_logMessageReceived(message.ToString(),
			tag, LogType.Warning);
		public void Write(char c) => Write(c.ToString());
		public void Write(object o) => Write(o.ToString());
		public void WriteLine(string text) => Write(text + "\n");
		public void Write(string text) => Write(text, -1);
		public void Write(string text, int color) {
			Coord originalStartPosition = Input.StartPosition;
			// un-write user input
			bool inputShouldFollowCursor = !Input.IsBoundToSpecificArea;
			if (inputShouldFollowCursor && Input.Count > 0) {
				Input.WritePrev(State.Output);
			}
			if (color >= 0) { PushForeColor((byte)color); }
			_cout.Write(text);
			if (color >= 0) { PopForeColor(); }
			// re-write user input in a new spot
			if (originalStartPosition != Coord.NegativeOne && inputShouldFollowCursor) {
				Coord delta = State.Cursor.position2d - originalStartPosition;
				Input.MoveAllChanges(delta);
				Input.WriteNext(State.Output);
			}
		}
		public void WriteLine(string text, byte color) => State.Write(text + "\n", color);
		public void Clear() { State.Clear(); }
	}
}
