using NonStandard.Data;
using NonStandard.Extension;
using NonStandard.Inputs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NonStandard.Cli {
	/// <summary>
	/// a facade class that organizes the rest of the console classes
	/// </summary>
	public class UnityConsole : MonoBehaviour {
		public const int MaxColorPaletteSize = 0xff;
		private UnityConsoleOutput _cout;
		public ConsoleState State;
		public ColorSettings colorSettings = new ColorSettings();
		public UnityConsoleCursor cursorUI;

		public UnityConsoleOutput cout => _cout ? _cout : _cout = GetComponent<UnityConsoleOutput>();
		public ConsoleDiff Input { get => State.Input; set => State.Input = value; }
		public bool CursorVisible {
			get => cursorUI.gameObject.activeSelf;
			set => cursorUI.gameObject.SetActive(value);
		}
		//public int CursorSize {
		//	get { return (int)(cursorUI.transform.localScale.MagnitudeManhattan() / 3); }
		//	set { cursorUI.transform.localScale = Vector3.one * (value / 100f); }
		//}

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
		public void Write(char c) => State.Write(c);
		public void Write(string text) => State.Write(text);
		public void Write(object o) => State.Write(o);
		public void WriteLine(string text) => Write(text + "\n");
	}
}
