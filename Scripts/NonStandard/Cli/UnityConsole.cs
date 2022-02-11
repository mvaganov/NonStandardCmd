using NonStandard.Data;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NonStandard.Extension;

namespace NonStandard.Cli {
	public class UnityConsole : MonoBehaviour {
		public TMP_InputField inputField;
		TMP_Text text;
		TMP_Text charBack;
		List<Tile> foreTile = new List<Tile>(), backTile = new List<Tile>();
		private bool textNeedsRefresh = false;
		public CursorSettings cursor = new CursorSettings();
		public DisplayWindowSettings Window = new DisplayWindowSettings();
		internal ConsoleBody body = new ConsoleBody();
		private List<byte> _colorStack = new List<byte>();

		// TODO move this code into UnityConsoleCursor?
		[System.Serializable] public class CursorSettings {
			public bool cursorVisible = true;
			public GameObject cursor;
			public Coord position;
			internal int index;
			Vector3[] cursorMeshPosition = new Vector3[4];
			public Vector3 CalculateCursorPosition() {
				return (cursorMeshPosition[0] + cursorMeshPosition[1] + cursorMeshPosition[2] + cursorMeshPosition[3]) / 4;
			}
			public void RefreshCursorPosition(UnityConsole console) {
				if (cursor == null) return;
				if (cursorVisible && index >= 0) {
					Transform t = cursor.transform;
					Vector3 p = CalculateCursorPosition();
					t.localPosition = p;
					t.rotation = console.transform.rotation;
					cursor.SetActive(true);
				} else {
					cursor.SetActive(false);
				}
			}
			internal void SetCursorPositionPoints(Vector3[] verts, int vertexIndex) {
				if (vertexIndex >= verts.Length) {
					Debug.LogWarning("too much? "+vertexIndex+" / "+verts.Length+" ");
					return;
				}
				cursorMeshPosition[0] = verts[vertexIndex + 0];
				cursorMeshPosition[1] = verts[vertexIndex + 1];
				cursorMeshPosition[2] = verts[vertexIndex + 2];
				cursorMeshPosition[3] = verts[vertexIndex + 3];
			}
			public void Init(UnityConsole console) {
				if (cursor != null) {
					UnityConsoleCursor ucc = cursor.GetComponent<UnityConsoleCursor>();
					if (ucc == null) { ucc = cursor.AddComponent<UnityConsoleCursor>(); }
					ucc.Initialize(console.Text.fontSize);
				}
			}
		}
		[System.Serializable] public class DisplayWindowSettings {
			public enum WindowSizing { Unconstrained, UseStaticViewRectangle, AutoCalculateViewRectangle }
			internal ConsoleBody body;
			public static readonly CoordRect Maximum = new CoordRect(Coord.Zero, Coord.Max);
			[Tooltip("only render characters contained in the render window")]
			public WindowSizing windowSizing = WindowSizing.AutoCalculateViewRectangle;
			private Vector2 sizeCalculated = -Vector2.one;
			private float fontSizeCalculated = -1;
			public CoordRect viewRect = Maximum;
			public enum FollowBehavior { No, Yes }
			public FollowBehavior followCursor = FollowBehavior.Yes;
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
				get => (float) viewRect.Left / (body.Size.X - viewRect.Width);
				set => viewRect.PositionX = (short)(value * (body.Size.X - viewRect.Width));
			}
			internal void UpdatePosition() {
				if (viewRect.PositionX < 0) { viewRect.PositionX -= viewRect.PositionX; }
				else if (viewRect.Right > body.Size.col) {
					if (viewRect.Width >= body.Size.col) { viewRect.PositionX = 0; }
					else { viewRect.PositionX -= (short)(viewRect.Right - body.Size.col); }
				}
				if (viewRect.PositionY < 0) { viewRect.PositionY -= viewRect.PositionY; }
				else if (viewRect.Bottom > body.Size.row) {
					if (viewRect.Height >= body.Size.row) { viewRect.PositionY = 0; }
					else { viewRect.PositionY -= (short)(viewRect.Bottom - body.Size.row); }
				}
			}
			public void ScrollRenderWindow(Coord direction) {
				viewRect.Position += direction;
				UpdatePosition();
			}
			public static Vector2 TextAreaSize(UnityConsole console) {
				return (console.inputField != null ? console.inputField.textViewport : console.text.GetComponent<RectTransform>()).rect.size;
			}
			public bool NeedsCalculation(UnityConsole console) {
				return windowSizing == WindowSizing.AutoCalculateViewRectangle &&
					(fontSizeCalculated != console.text.fontSize || TextAreaSize(console) != sizeCalculated);
			}
			public void DoCalculation(UnityConsole console, DisplayCalculations calc) {
				Vector2 ideal = calc.CalculateIdealSize();
				viewRect.Size = new Coord((short)ideal.x, (short)ideal.y);
				UpdatePosition();
				// if the calculated values are reasonable limits
				if (ideal.x < Coord.Max.X-2 || ideal.y < Coord.Max.Y - 2) {
					// cache these calculations as valid, which means they won't be recalculated as much later
					fontSizeCalculated = console.text.fontSize;
					sizeCalculated = TextAreaSize(console);
				}
			}
			public void ResetWindowSize() { viewRect.Size = new Coord(Coord.Max.X - 1, Coord.Max.Y - 1); }
		}
		public void ScrollRenderWindow(Coord direction) {
			Window.ScrollRenderWindow(direction);
			textNeedsRefresh = true;
		}
		[System.Serializable] public class ColorSettings {
			[Range(0, 1)] public float foregroundAlpha = 1f;
			[Range(0, 1)] public float backgroundAlpha = 0.5f;
			public List<Color> ConsoleColorPalette = new List<Color>(Array.ConvertAll(ColorRGBA.defaultColors, c => (Color)c));
			public void FillInDefaultPalette() {
				while (ConsoleColorPalette.Count < 16) {
					ConsoleColorPalette.Add((Color)(ColorRGBA)(ConsoleColor)ConsoleColorPalette.Count);
				}
			}
			public int AddConsoleColor(ColorRGBA colorRgba) {
				if (ConsoleColorPalette.Count >= 0xff) {
					Show.Error("too many colors");
					return -1;
				}
				ConsoleColorPalette.Add(colorRgba);
				return ConsoleColorPalette.Count - 1;
			}
		}
		public ColorSettings colorSettings = new ColorSettings();

		public float FontSize {
			get => inputField != null ? inputField.pointSize : Text.fontSize;
			set {
				if (inputField != null) {
					inputField.pointSize = charBack.fontSize = value;
				} else {
					Text.fontSize = charBack.fontSize = value;
				}
			}
		}
		public void AddToFontSize(float value) {
			FontSize += value;
			if (FontSize < 1) { FontSize = 1; }
			if (cursor.cursor != null) { cursor.cursor.GetComponent<UnityConsoleCursor>().ScaleToFontSize(FontSize); }
			RefreshText();
		}

		[System.Serializable] public class CharSettings {
			public char EmptyChar = ' ';
			public char BackgroundChar = '\u2588'; // █
		}
		public CharSettings charSettings = new CharSettings();

		public int AddConsoleColor(ColorRGBA colorRgba) { return colorSettings.AddConsoleColor(colorRgba); }
		public int GetConsoleColorCount() { return colorSettings.ConsoleColorPalette.Count; }
		public ColorRGBA GetConsoleColor(int code, bool foreground) {
			if(code == Col.DefaultColorIndex) { code = foreground ? body.defaultColors.fore : body.defaultColors.back; }
			//if(code < 0 || code >= colorSettings.ConsoleColorPalette.Count) {
			//	Show.Log(code + "? max is "+ colorSettings.ConsoleColorPalette.Count);
			//}
			return colorSettings.ConsoleColorPalette[code];
		}
		public Coord Cursor {
			get => body.Cursor;
			set {
				body.Cursor = value;
				cursor.position = body.Cursor;
				if (Window.followCursor == DisplayWindowSettings.FollowBehavior.Yes) {
					Window.viewRect.MoveToContain(body.Cursor);
				}
				cursor.RefreshCursorPosition(this);
				textNeedsRefresh = true;
			}
		}

		public int WindowHeight { get => Window.Height; set => Window.Height = value; }
		public int WindowWidth { get => Window.Width; set => Window.Width= value; }
		public ConsoleColor ForegoundColor { get => body.currentDefaultTile.Fore; set => body.currentDefaultTile.Fore = value; }
		public ConsoleColor BackgroundColor { get => body.currentDefaultTile.Back; set => body.currentDefaultTile.Back = value; }
		public byte ForeColor { get => body.currentDefaultTile.fore; set => body.currentDefaultTile.fore = value; }
		public byte BackColor { get => body.currentDefaultTile.back; set => body.currentDefaultTile.back = value; }
		public int BufferHeight => body.Size.Y;
		public int BufferWidth => body.Size.X;
		public int CursorLeft { get => body.CursorLeft; set => body.CursorLeft = value; }
		public int CursorTop { get => body.CursorTop; set => body.CursorTop = value; }
		public int CursorSize {
			get { return (int)(cursor.cursor.transform.localScale.MagnitudeManhattan() / 3); }
			set { cursor.cursor.transform.localScale = Vector3.one * (value / 100f); }
		}
		public bool CursorVisible { get => cursor.cursorVisible; set => cursor.cursorVisible = value; }

		public void PushForeColor(ConsoleColor c) { _colorStack.Add(ForeColor); ForegoundColor = c; }
		public void PushForeColor(byte c) { _colorStack.Add(ForeColor); ForeColor = c; }
		public void PopForeColor() {
			if (_colorStack.Count > 0) {
				ForeColor = _colorStack[_colorStack.Count-1];
				_colorStack.RemoveAt(_colorStack.Count - 1);
			}
		}

		public struct Tile {
			public ColorRGBA color;
			public float height;
			public char letter;
			public Tile(char letter, ColorRGBA color, float height) { this.letter = letter;this.height = height;this.color = color; }
		}

		public void ResetColor() { body.currentDefaultTile = body.defaultColors; }
		private void Awake() {
			colorSettings.FillInDefaultPalette();
			Window.body = body;
		}

		public TMP_Text Text => inputField != null ? inputField.textComponent : text;

		public RectTransform GetUiTransform() {
			if (inputField == null) { inputField = GetComponentInChildren<TMP_InputField>(); }
			if (!inputField) {
				text = GetComponentInChildren<TMP_Text>();
			} else {
				text = inputField.textComponent;
				inputField.readOnly = true;
				inputField.richText = false;
			}
			if (inputField != null) { return inputField.GetComponent<RectTransform>(); }
			return text.GetComponent<RectTransform>();
		}

		void Start() {
			GetUiTransform();
			TMP_Text pTmp = Text;
			GameObject backgroundObject = Instantiate(Text.gameObject);
			UnityConsole extra = backgroundObject.GetComponent<UnityConsole>();
			if (extra != null) { DestroyImmediate(extra); }
			RectTransform brt = backgroundObject.GetComponent<RectTransform>(); if (brt == null) { brt = backgroundObject.AddComponent<RectTransform>(); }
			backgroundObject.transform.SetParent(pTmp.transform.parent);
			if (pTmp.transform.parent != null) {
				backgroundObject.transform.SetSiblingIndex(0); // put the background in the background
			}
			backgroundObject.transform.localPosition = Vector3.zero;
			backgroundObject.transform.localScale = Vector3.one;
			charBack = backgroundObject.GetComponent<TMP_Text>();
			charBack.fontMaterial.renderQueue -= 1;
			if (inputField) {
				inputField.targetGraphic.material.renderQueue -= 2;
			}
			RectTransform rt = pTmp.GetComponent<RectTransform>();
			brt.anchorMin = rt.anchorMin;
			brt.anchorMax = rt.anchorMax;
			brt.offsetMin = rt.offsetMin;
			brt.offsetMax = rt.offsetMax;
			cursor.Init(this);
		}
		public void Update() {
			if(cursor.position != body.Cursor) {
				Cursor = cursor.position;
			}
			if (textNeedsRefresh) {
				RefreshText();
			}
		}

		public void RefreshText() {
			CoordRect limit = Window.Limit;
			CalculateText(body, limit, foreTile, true, colorSettings.foregroundAlpha);
			DisplayCalculations calc = null;
			if (Window.NeedsCalculation(this)) {
				calc = new DisplayCalculations(this);
			}
			TransferToTMP(true, foreTile, calc);
			if (calc != null) {
				Window.DoCalculation(this, calc);
			}
			if (charBack) {
				CalculateText(body, limit, backTile, false, colorSettings.backgroundAlpha);
				TransferToTMP(false, backTile);
			}
			cursor.RefreshCursorPosition(this);
			textNeedsRefresh = false;
			//Show.Log(body.Cursor);
		}

		public void SetAt(Coord position, ConsoleTile tile) {
			body.SetAt(position, tile);
		}

		public void Write(char c) { Write(c.ToString()); }
		public void Write(object o) { Write(o.ToString()); }
		public void Write(string text) { Write(text, false, null); }
		public void Write(string text, bool isInput, List<ConsoleArtifact> out_replaced) {
			Coord oldSize = body.Size;
			body.Write(text, out_replaced);
			Cursor = body.Cursor;
			if (!isInput) {
				body.RestartWriteCursor();
			}
			//window.rect.MoveToContain(body.Cursor);
			if (body.Size != oldSize) {
				//Show.Log("window update");
				Window.UpdatePosition();
			}
			textNeedsRefresh = true;
		}
		public void WriteLine(string text) { Write(text + "\n"); }
		void CalculateText(ConsoleBody body, CoordRect window, List<Tile> out_tile, bool foreground, float alpha) {
			out_tile.Clear();
			ConsoleTile current = body.defaultColors;
			Coord limit = new Coord(window.Max.col, Math.Min(window.Max.row, Math.Max(body.lines.Count, body.Cursor.row + 1)));
			int rowsPrinted = 0;
			Coord cursor = body.Cursor;
			this.cursor.index = -1;
			for (int row = window.Min.row; row < limit.row; ++row, ++rowsPrinted) {
				if (rowsPrinted > 0) {
					ColorRGBA colorRgba = GetConsoleColor(foreground ? current.fore : current.back, foreground);
					colorRgba.a = (byte)(colorRgba.a * alpha);
					out_tile.Add(new Tile('\n', colorRgba, 0));
				}
				if (row < 0) { continue; }
				if (row < body.lines.Count) {
					List<ConsoleTile> line = body.lines[row];
					limit.col = Math.Min(window.Max.col, (short)line.Count);
					for (int col = window.Min.col; col < limit.col; ++col) {
						if (col >= 0) {
							ConsoleTile tile = line[col];
							current = tile;
							if (!foreground) { current.Letter = charSettings.BackgroundChar; }
						} else if (line.Count > 0) {
							current.Letter = foreground ? charSettings.EmptyChar : charSettings.BackgroundChar;
						}
						if (!foreground && this.cursor.cursorVisible && cursor.col == col && cursor.row == row) {
							this.cursor.index = out_tile.Count;
						}
						ColorRGBA colorRgba = GetConsoleColor(foreground ? current.fore : current.back, foreground);
						colorRgba.a = (byte)(colorRgba.a * alpha);
						out_tile.Add(new Tile(current.Letter, colorRgba, 0));
					}
				}
				if (cursor.row == row && cursor.col >= limit.col && window.Contains(cursor)) {
					int col = limit.col;
					ColorRGBA colorRgba = GetConsoleColor(foreground ? current.fore : current.back, foreground);
					colorRgba.a = (byte)(colorRgba.a * alpha);
					while (col <= cursor.col) {
						current.Letter = foreground ? charSettings.EmptyChar : charSettings.BackgroundChar;
						if (!foreground && this.cursor.cursorVisible && cursor.col == col && cursor.row == row) {
							this.cursor.index = out_tile.Count;
						}
						out_tile.Add(new Tile(current.Letter, colorRgba, 0));
						++col;
					}
				}
			}
		}
		public class DisplayCalculations {
			public DisplayCalculations(UnityConsole console) {
				rt = console.inputField != null ? console.inputField.textViewport : console.text.GetComponent<RectTransform>();
			}
			public RectTransform rt;
			public Coord size = Coord.Zero;
			public int currentLineWidth;
			public Vector2 min, max;
			public void CalculateVertices(Vector3[] verts) {
				min = max = verts[0];
				Vector3 v;
				for (int i = 0; i < verts.Length; ++i) {
					v = verts[i];
					if (v.x < min.x) min.x = v.x;
					if (v.y < min.y) min.y = v.y;
					if (v.x > max.x) max.x = v.x;
					if (v.y > max.y) max.y = v.y;
				}
			}
			public void StartCalculatingText() {
				currentLineWidth = 0;
				size = Coord.Zero;
			}
			public void UpdateTextCalculation(char c) {
				if (currentLineWidth == 0) { ++size.Y; }
				switch (c) {
				case '\0': break;
				case '\n': currentLineWidth = 0; break;
				default: if (++currentLineWidth > size.X) { size.X = currentLineWidth; } break;
				}
			}
			public Vector2 CalculateIdealSize() {
				Vector2 totSize = (max - min);
				Rect rect = rt.rect;
				Vector2 maxInArea = new Vector2(Coord.Max.X-1, Coord.Max.Y-1);
				// only limit the size if there is not enough space
				if (totSize.x > rect.width || totSize.y > rect.height) {
					Vector2 glyphSize = new Vector2(totSize.x / size.X, totSize.y / size.Y);
					maxInArea = new Vector2(rect.width / glyphSize.x, rect.height / glyphSize.y - 1);
					//Show.Log(maxInArea + " <-- " + glyphSize + " chars: " + size + "   sized: " + totSize + "   / " + rt.rect.width + "," + rt.rect.height);
				}
				return maxInArea;
			}
		}
		public void TransferToTMP(bool foreground, List<Tile> tiles, DisplayCalculations calc = null) {
			TMP_Text tmpText;
			char[] letters = new char[tiles.Count];
			for (int i = 0; i < letters.Length; ++i) { letters[i] = tiles[i].letter; }
			string text = new string(letters);
			if (foreground) {
				if (inputField) {
					inputField.text = text;
					inputField.ForceLabelUpdate();
					tmpText = inputField.textComponent;
				} else {
					tmpText = this.text;
					tmpText.text = text;
					tmpText.ForceMeshUpdate();
				}
			} else {
				tmpText = charBack;
				tmpText.text = text;
				tmpText.ForceMeshUpdate();
			}
			TMP_CharacterInfo[] chars = tmpText.textInfo.characterInfo;
			Vector3 normal = -transform.forward;
			Color32 color;
			float height;
			bool vertChange = false, colorChange = false;

			//StringBuilder sb = new StringBuilder();
			for (int m = 0; m < tmpText.textInfo.meshInfo.Length; ++m) {
				Color32[] vertColors = tmpText.textInfo.meshInfo[m].colors32;
				Vector3[] verts = tmpText.textInfo.meshInfo[m].vertices;
				calc?.CalculateVertices(verts);
				calc?.StartCalculatingText();
				for (int i = 0; i < chars.Length; ++i) {
					TMP_CharacterInfo cinfo = chars[i];
					// stop at the zero-width breaking space
					if (cinfo.character == '\u200B') break;
					calc?.UpdateTextCalculation(cinfo.character);
					//if (cinfo.isVisible) sb.Append(cinfo.character);
					//else sb.Append("(" + ((int)cinfo.character) + ")");
					if (!cinfo.isVisible) continue;
					int vertexIndex = cinfo.vertexIndex;
					if(i == cursor.index) {
						if (vertexIndex >= verts.Length) {
							Debug.LogWarning("weirdness happening. "+tmpText.text);
						} else {
							cursor.SetCursorPositionPoints(verts, vertexIndex);
						}
					}
					if (vertexIndex < vertColors.Length && i < tiles.Count && !vertColors[vertexIndex].Eq(color = tiles[i].color)) {
						colorChange = true;
						vertColors[vertexIndex + 0] = color;
						vertColors[vertexIndex + 1] = color;
						vertColors[vertexIndex + 2] = color;
						vertColors[vertexIndex + 3] = color;
					}
					if(vertexIndex < vertColors.Length && i < tiles.Count && (height = tiles[i].height) != 0) {
						vertChange = true;
						Vector3 h = height * normal;
						verts[vertexIndex + 0] = verts[vertexIndex + 0] + h;
						verts[vertexIndex + 1] = verts[vertexIndex + 1] + h;
						verts[vertexIndex + 2] = verts[vertexIndex + 2] + h;
						verts[vertexIndex + 3] = verts[vertexIndex + 3] + h;
					}
				}
			}
			//Show.Log(sb);
			if (colorChange) { tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32); }
			if (vertChange) { tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices); }
		}
	}
}