// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using NonStandard.Data;
using System;
using System.Collections.Generic;

namespace NonStandard.Cli {
	/// <summary>
	/// responsible for arranging and inserting console input into console output
	/// </summary>
	[System.Serializable] public class ConsoleState {
		internal bool textNeedsRefresh = false;
		public CursorState Cursor = new CursorState();
		public WindowState Window = new WindowState();
		private ConsoleBody _output = new ConsoleBody();
		private ConsoleDiff _input = new ConsoleDiff();
		/// <summary>
		/// color of text being written, as a stack (enables push/pop without color state logic elsewehere).
		/// </summary>
		private List<byte> _colorStack = new List<byte>();

		public ConsoleDiff Input { get => _input; set => _input = value; }
		public ConsoleBody Output => _output;

		public ConsoleState() {
			_output = new ConsoleBody();
			Window = new WindowState();
		}
		public void Init() {
			Window.body = _output;
		}

		public Coord CursorPosition {
			get => Cursor.position2d;
			set {
				Cursor.position2d = value;
				if (Window.followCursor == WindowState.FollowBehavior.Yes) {
					Window.viewRect.MoveToContain(Cursor.position2d);
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
		public int CursorLeft { get => Cursor.position2d.Col; set => Cursor.position2d.Col = (value); }
		public int CursorTop { get => Cursor.position2d.Row; set => Cursor.position2d.Row = (value); }

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

		public void WriteInput(string inputText) {
			Write(inputText, _input, ref Cursor.indexInInput);
		}

		public void WriteInputWithColor(string inputText, byte color) {
			if (color > 0) { PushForeColor(color); }
			WriteInput(inputText);
			if (color > 0) { PopForeColor(); }
			RefreshCursorValid();
		}

		public void Write(string text) {
			Coord oldSize = _output.Size;
			_output.Write(text, ref Cursor.position2d);
			if (_output.Size != oldSize) {
				//Show.Log("window update");
				Window.UpdatePosition();
			}
			textNeedsRefresh = true;
		}

		public void Write(string text, byte color) {
			PushForeColor(color); Write(text); PopForeColor();
		}

		public void Write(string text, ConsoleDiff input, ref int inputIndex) {
			Coord oldSize = _output.Size;
			if (input != null && input.StartPosition == Coord.NegativeOne) {
				input.StartPosition = CursorPosition;
			}
			_output.Write(text, ref Cursor.position2d, input, ref inputIndex);
			if (_output.Size != oldSize) {
				//Show.Log("window update");
				Window.UpdatePosition();
			}
			textNeedsRefresh = true;
		}

		public void Clear() {
			Output.Clear();
			CursorPosition = Coord.Zero;
			Window.viewRect.Position = Coord.Zero;
			Window.UpdatePosition();
			RestartInput();
		}

		public void RefreshInputText() {
			RefreshInput(_input);
		}

		public void RefreshInput(ConsoleDiff diff) {
			for (int i = 0; i < diff.changes.Count; i++) {
				ConsoleDiffUnit consoleDiffUnit = diff.changes[i];
				Coord cursor = consoleDiffUnit.coord;
				//body.Write(consoleDiffUnit.next.Letter.ToString(), null, ref writeIndex, ref cursor);
				_output.PrintTile(consoleDiffUnit.next, ref cursor);
			}
		}

		public void RefreshCursorValid() {
			if (CursorPosition.col < 0) {
				CursorPosition = new Coord(0, CursorPosition.row);
			}
			if (CursorPosition.row < 0) {
				CursorPosition = new Coord(CursorPosition.col, 0);
			}
			Cursor.validInputIndex = _input.TryGetIndexOf(CursorPosition, out Cursor.indexInInput);
		}
		public void RestartInput() {
			Cursor.indexInInput = 0;
			_input.StartPosition = Cursor.position2d;
		}

		public void KeepInputCursorOnInput() {
			if (Cursor.indexInInput == 0 && Input.StartPosition != Coord.NegativeOne) {
				Cursor.position2d = Input.StartPosition;
			} else if (Cursor.indexInInput > 0 && Cursor.indexInInput == Input.changes.Count) {
				ConsoleDiffUnit last = Input.changes[Input.changes.Count - 1];
				if (last.next != '\n') {
					Cursor.position2d = last.coord + Coord.Right;
				} else {
					Cursor.position2d = new Coord(Input.inputArea.Min.x, last.coord.row + 1);
				}
			}
		}

		[System.Serializable] public class CursorState {
			public bool validInputIndex;
			public Coord position2d;
			public int indexInInput;
			public int indexInConsole;
		}

		[System.Serializable] public class WindowState {
			public enum WindowSizing { Unconstrained, UseStaticViewRectangle, AutoCalculateViewRectangle }
			internal ConsoleBody body;
			public static readonly CoordRect Maximum = new CoordRect(Coord.Zero, Coord.Max);
			/// [Tooltip("only render characters contained in the render window")]
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
			public int Height {
				get => windowSizing != WindowSizing.Unconstrained ? viewRect.Height : -1;
				set => viewRect.Height = value;
			}
			public int Width {
				get => windowSizing != WindowSizing.Unconstrained ? viewRect.Width : -1;
				set => viewRect.Width = value;
			}
			public float ScrollVertical {
				get => (float)viewRect.Top / (body.Size.Y - viewRect.Height);
				set => viewRect.PositionY = (short)(value * (body.Size.Y - viewRect.Height));
			}
			public float ScrollHorizontal {
				get => (float)viewRect.Left / (body.Size.X - viewRect.Width);
				set => viewRect.PositionX = (short)(value * (body.Size.X - viewRect.Width));
			}
			internal void UpdatePosition() {
				if (viewRect.PositionX < 0) {
					viewRect.PositionX -= viewRect.PositionX;
				} else if (viewRect.Right > body.Size.col) {
					if (viewRect.Width >= body.Size.col) {
						viewRect.PositionX = 0;
					} else {
						viewRect.PositionX -= (short)(viewRect.Right - body.Size.col);
					}
				}
				if (viewRect.PositionY < 0) {
					viewRect.PositionY -= viewRect.PositionY;
				} else if (viewRect.Bottom > body.Size.row) {
					if (viewRect.Height >= body.Size.row) { viewRect.PositionY = 0;
					} else {
						viewRect.PositionY -= (short)(viewRect.Bottom - body.Size.row);
					}
				}
			}
			public void ScrollRenderWindow(Coord direction) {
				viewRect.Position += direction;
				UpdatePosition();
			}
			public void ResetWindowSize() {
				viewRect.Size = new Coord(Coord.Max.X - 1, Coord.Max.Y - 1);
			}
		}
	}
}
