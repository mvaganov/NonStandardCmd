using NonStandard.Data;
using NonStandard.Extension;
using NonStandard.Inputs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
		private UnityConsoleOutput _cout;
		public ConsoleState State;
		public ColorSettings colorSettings = new ColorSettings();
		public UnityConsoleCursor cursorUI;
		public bool watchMouse = true;
		public Coord mouseOver;
		public UnityConsoleOutput cout => _cout ? _cout : _cout = GetComponent<UnityConsoleOutput>();
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
		}

		private void Update() {
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
		private List<RaycastResult> list = new List<RaycastResult>();
		private void WatchMouse() {
			//UnityConsoleUiToggle uiToggle = GetComponent<UnityConsoleUiToggle>();
			//UnityConsoleUiToggle.ConsoleUiState state = uiToggle.GetCurrentState();
			//switch (state) {
			//	case UnityConsoleUiToggle.ConsoleUiState.ScreenSpace:
			//		list.Clear();
			//		Vector2 screenPoint = Mouse.current.position.ReadValue();// Camera.main.WorldToScreenPoint(target.position);
			//		PointerEventData ed = new PointerEventData(EventSystem.current);
			//		ed.position = screenPoint;
			//		canvasRaycaster.Raycast(ed, list);
			//		break;
			//	case UnityConsoleUiToggle.ConsoleUiState.WorldSpace:
			//		Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
			//		Canvas canvas = _cout.inputField.GetComponentInParent<Canvas>();
			//		break;
			//}
			list.Clear();
			Vector2 screenPoint = Mouse.current.position.ReadValue();// Camera.main.WorldToScreenPoint(target.position);
			PointerEventData ed = new PointerEventData(EventSystem.current);
			ed.position = screenPoint;
			EventSystem.current.RaycastAll(ed, list);
			//canvasRaycaster.Raycast(ed, list);
			debugtest = list.JoinToString(", ", r => r.gameObject.name+":"+r.worldPosition);
			if (list.Count > 0) {
				RectTransform rt = list[0].gameObject.GetComponent<RectTransform>();
				Vector3 worldPos = list[0].worldPosition;
				Vector3[] corners = new Vector3[4];
				rt.GetWorldCorners(corners);
				Vector3 lowerLeft = corners[0];
				Vector3 upperLeft = corners[1];
				Vector3 upperRight = corners[2];
				Vector3 lowerRight = corners[3];
				Vector3 xhat = lowerRight - lowerLeft;
				Vector3 yhat = upperLeft - lowerLeft;
				Vector2 worldSize = new Vector2(xhat.magnitude, yhat.magnitude);
				Vector3 delta = worldPos - lowerLeft;
				Vector2 panelPos = new Vector2(Vector3.Dot(delta, xhat), Vector3.Dot(delta, yhat));
				debugtest = panelPos+" : "+worldSize+" --- "+corners.JoinToString();
			}
			//canvas.
			//Vector3 hit = 
		}
		public string debugtest;

		private void Application_logMessageReceived(string condition, string stackTrace, LogType type) {
			Coord whereCursorStarted = State.Cursor.position2d;
			switch (type) {
				case LogType.Error:
				case LogType.Exception:
				case LogType.Assert:
					LogError(condition);
					break;
				case LogType.Warning:
					LogWarning(condition);
					break;
				case LogType.Log:
					Log(condition);
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
		private void Log(object message) => WriteLine(message.ToString());
		private void LogError(object message) => WriteLine(message.ToString());
		private void LogWarning(object message) => WriteLine(message.ToString());
		public void Write(char c) => State.Write(c);
		public void Write(string text) => State.Write(text);
		public void Write(object o) => State.Write(o);
		public void WriteLine(string text) => Write(text + "\n");
		public void WriteLine(string text, byte color) => State.Write(text + "\n", color);
	}
}
