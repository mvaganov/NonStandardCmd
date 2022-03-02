using NonStandard.Data;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NonStandard.Cli {
	/// <summary>
	/// responsible for arranging and inserting console input into console output
	/// </summary>
	[System.Serializable] public class ConsoleIo {
		internal bool textNeedsRefresh = false;
		public CursorState cursor;
		public DisplayWindowSettings Window = new DisplayWindowSettings();
		private ConsoleBody _output = new ConsoleBody();
		private ConsoleDiff _input = new ConsoleDiff();
		/// <summary>
		/// color of text being written, as a stack (enables push/pop without color state logic elsewehere).
		/// </summary>
		private List<byte> _colorStack = new List<byte>();

		public ConsoleDiff Input { get => _input; set => _input = value; }
		public ConsoleBody Output => _output;

		public ConsoleIo() {
			_output = new ConsoleBody();
			Window = new DisplayWindowSettings();
		}
		public void Init() {
			Window.body = _output;
		}

		public Coord Cursor {
			get => cursor.position;
			set {
				cursor.position = value;
				if (Window.followCursor == DisplayWindowSettings.FollowBehavior.Yes) {
					Window.viewRect.MoveToContain(cursor.position);
				}
				textNeedsRefresh = true;
			}
		}
		public int WindowHeight { get => Window.Height; set => Window.Height = value; }
		public int WindowWidth { get => Window.Width; set => Window.Width = value; }
		public ConsoleColor ForegoundColor { get => _output.currentDefaultTile.Fore; set => _output.currentDefaultTile.Fore = value; }
		public ConsoleColor BackgroundColor { get => _output.currentDefaultTile.Back; set => _output.currentDefaultTile.Back = value; }
		public byte ForeColor { get => _output.currentDefaultTile.fore; set => _output.currentDefaultTile.fore = value; }
		public byte BackColor { get => _output.currentDefaultTile.back; set => _output.currentDefaultTile.back = value; }
		public int BufferHeight => _output.Size.Y;
		public int BufferWidth => _output.Size.X;
		public int CursorLeft { get => cursor.position.Col; set => cursor.position.Col = (value); }
		public int CursorTop { get => cursor.position.Row; set => cursor.position.Row = (value); }

		public bool CursorVisible { get => cursor.cursorVisible; set => cursor.cursorVisible = value; }

		public void PushForeColor(ConsoleColor c) { _colorStack.Add(ForeColor); ForegoundColor = c; }
		public void PushForeColor(byte c) { _colorStack.Add(ForeColor); ForeColor = c; }
		public void PopForeColor() {
			if (_colorStack.Count > 0) {
				ForeColor = _colorStack[_colorStack.Count - 1];
				_colorStack.RemoveAt(_colorStack.Count - 1);
			}
		}

		public void ResetColor() { _output.currentDefaultTile = _output.defaultColors; }

		public void SetAt(Coord position, ConsoleTile tile) {
			_output.SetAt(position, tile);
		}

		public void Write(char c) { Write(c.ToString()); }
		public void Write(object o) { Write(o.ToString()); }
		public void WriteInput(string inputText) {
			Write(inputText, _input, ref cursor.indexInInput);
		}
		public void Write(string text) {
			Coord oldSize = _output.Size;
			_output.Write(text, ref cursor.position);
			if (_output.Size != oldSize) {
				//Show.Log("window update");
				Window.UpdatePosition();
			}
			textNeedsRefresh = true;
		}
		public void Write(string text, ConsoleDiff input, ref int inputIndex) {
			Coord oldSize = _output.Size;
			if (input != null && input.Start == Coord.NegativeOne) {
				input.Start = Cursor;
			}
			_output.Write(text, ref cursor.position, input, ref inputIndex);
			if (_output.Size != oldSize) {
				//Show.Log("window update");
				Window.UpdatePosition();
			}
			textNeedsRefresh = true;
		}
		public void RefreshInputText() {
			for (int i = 0; i < _input.delta.Count; i++) {
				ConsoleDiffUnit consoleDiffUnit = _input.delta[i];
				Coord cursor = consoleDiffUnit.coord;
				//body.Write(consoleDiffUnit.next.Letter.ToString(), null, ref writeIndex, ref cursor);
				_output.PrintTile(consoleDiffUnit.next, ref cursor);
			}
		}
		public void WriteLine(string text) { Write(text + "\n"); }

		public void RefreshCursorValid() {
			if (Cursor.col < 0) {
				Cursor = new Coord(0, Cursor.row);
			}
			if (Cursor.row < 0) {
				Cursor = new Coord(Cursor.col, 0);
			}
			cursor.validInputIndex = _input.TryGetIndexOf(Cursor, out cursor.indexInInput);
		}
		public void RestartInput() {
			cursor.indexInInput = 0;
			_input.Start = cursor.position;
		}

		[System.Serializable] public class CursorState {
			public bool cursorVisible = true;
			public bool validInputIndex;
			public Coord position;
			public int indexInInput;
			public int indexInConsole;
		}

		[System.Serializable]
		public class DisplayWindowSettings {
			public enum WindowSizing { Unconstrained, UseStaticViewRectangle, AutoCalculateViewRectangle }
			internal ConsoleBody body;
			public static readonly CoordRect Maximum = new CoordRect(Coord.Zero, Coord.Max);
			[Tooltip("only render characters contained in the render window")]
			public WindowSizing windowSizing = WindowSizing.AutoCalculateViewRectangle;
			public CoordRect viewRect = Maximum;
			public enum FollowBehavior { No, Yes }
			public FollowBehavior followCursor = FollowBehavior.Yes;
			public bool IsAutosizing => windowSizing == WindowSizing.AutoCalculateViewRectangle;
			public CoordRect Limit => windowSizing != WindowSizing.Unconstrained ? viewRect : Maximum;
			public Coord Size {
				get => windowSizing != WindowSizing.Unconstrained ? viewRect.Size : Maximum.Size;
				set { viewRect.Size = value; }
			}
			public int Height { get => windowSizing != WindowSizing.Unconstrained ? viewRect.Height : -1; set => viewRect.Height = value; }
			public int Width { get => windowSizing != WindowSizing.Unconstrained ? viewRect.Width : -1; set => viewRect.Width = value; }
			public float ScrollVertical {
				get => (float)viewRect.Top / (body.Size.Y - viewRect.Height);
				set => viewRect.PositionY = (short)(value * (body.Size.Y - viewRect.Height));
			}
			public float ScrollHorizontal {
				get => (float)viewRect.Left / (body.Size.X - viewRect.Width);
				set => viewRect.PositionX = (short)(value * (body.Size.X - viewRect.Width));
			}
			internal void UpdatePosition() {
				if (viewRect.PositionX < 0) { viewRect.PositionX -= viewRect.PositionX; } else if (viewRect.Right > body.Size.col) {
					if (viewRect.Width >= body.Size.col) { viewRect.PositionX = 0; } else { viewRect.PositionX -= (short)(viewRect.Right - body.Size.col); }
				}
				if (viewRect.PositionY < 0) { viewRect.PositionY -= viewRect.PositionY; } else if (viewRect.Bottom > body.Size.row) {
					if (viewRect.Height >= body.Size.row) { viewRect.PositionY = 0; } else { viewRect.PositionY -= (short)(viewRect.Bottom - body.Size.row); }
				}
			}
			public void ScrollRenderWindow(Coord direction) {
				viewRect.Position += direction;
				UpdatePosition();
			}
			public void ResetWindowSize() { viewRect.Size = new Coord(Coord.Max.X - 1, Coord.Max.Y - 1); }
		}
	}
}
