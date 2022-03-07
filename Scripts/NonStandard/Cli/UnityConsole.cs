using NonStandard.Data;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NonStandard.Extension;

namespace NonStandard.Cli {
	public class UnityConsole : MonoBehaviour {
		public TMP_InputField inputField;
		public Console Console;
		TMP_Text text;
		TMP_Text charBack;
		public UnityConsoleCursor cursorUi = new UnityConsoleCursor();
		internal UnityConsoleOutput consoleOutput = new UnityConsoleOutput();
		List<Tile> foreTile = new List<Tile>();
		List<Tile> backTile = new List<Tile>();
		public ColorSettings colorSettings = new ColorSettings();
		public CharSettings defaultEmptyCharacter = new CharSettings();

		public ConsoleDiff Input { get => Console.Input; set => Console.Input = value; }
		public ConsoleBody Output => Console.Output;
		public bool CursorVisible {
			get => cursorUi.cursorObject.gameObject.activeSelf;
			set => cursorUi.cursorObject.gameObject.SetActive(value);
		}

		public struct Tile {
			public ColorRGBA color;
			public float height;
			public char letter;
			public Tile(char letter, ColorRGBA color, float height) { this.letter = letter; this.height = height; this.color = color; }
		}

		[System.Serializable] public class UnityConsoleCursor {
			public GameObject cursorObject;
			Vector3[] cursorMeshPosition = new Vector3[4];
			[HideInInspector] public Console.CursorState state;
			public Vector3 CalculateCursorPosition() {
				return (cursorMeshPosition[0] + cursorMeshPosition[1] + cursorMeshPosition[2] + cursorMeshPosition[3]) / 4;
			}
			public void RefreshCursorPosition(UnityConsole console) {
				if (cursorObject == null) return;
				if (console.CursorVisible && state.indexInConsole >= 0) {
					Transform t = cursorObject.transform;
					Vector3 p = CalculateCursorPosition();
					t.localPosition = p;
					t.rotation = console.transform.rotation;
					cursorObject.SetActive(true);
				} else {
					cursorObject.SetActive(false);
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
				if (cursorObject != null) {
					Cli.UnityConsoleCursor ucc = cursorObject.GetComponent<Cli.UnityConsoleCursor>();
					if (ucc == null) { ucc = cursorObject.AddComponent<Cli.UnityConsoleCursor>(); }
					ucc.Initialize(console.Text.fontSize);
				}
			}
		}
		[System.Serializable] public class UnityConsoleOutput {
			public Vector2 sizeCalculated = -Vector2.one;
			public float fontSizeCalculated = -1;
		}

		public Vector2 TextAreaSize() {
			return (inputField != null ? inputField.textViewport : text.GetComponent<RectTransform>()).rect.size;
		}
		public bool NeedsCalculation() {
			return consoleOutput.fontSizeCalculated != text.fontSize || TextAreaSize() != consoleOutput.sizeCalculated;
		}
		public void DoCalculation(DisplayCalculations calc) {
			Vector2 ideal = calc.CalculateIdealSize();
			Console.Window.viewRect.Size = new Coord((short)ideal.x, (short)ideal.y);
			Console.Window.UpdatePosition();
			// if the calculated values are reasonable limits
			if (ideal.x < Coord.Max.X - 2 || ideal.y < Coord.Max.Y - 2) {
				// cache these calculations as valid, which means they won't be recalculated as much later
				consoleOutput.fontSizeCalculated = text.fontSize;
				consoleOutput.sizeCalculated = TextAreaSize();
			}
		}
		public void ScrollRenderWindow(Coord direction) {
			Console.Window.ScrollRenderWindow(direction);
			Console.textNeedsRefresh = true;
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
			if (cursorUi.cursorObject != null) { cursorUi.cursorObject.GetComponent<Cli.UnityConsoleCursor>().ScaleToFontSize(FontSize); }
			RefreshText();
		}

		[System.Serializable] public class CharSettings {
			public char Foreground = ' '; // normal space
			public char Background = '\u2588'; // █
		}

		public int AddConsoleColorPalette(ColorRGBA colorRgba) { return colorSettings.AddConsoleColor(colorRgba); }
		public int GetConsoleColorPaletteCount() { return colorSettings.ConsoleColorPalette.Count; }
		public ColorRGBA GetConsoleColor(int code, bool foreground) {
			if(code < 0 || code == Col.DefaultColorIndex) { code = foreground ? Output.defaultColors.fore : Output.defaultColors.back; }
			//if(code < 0 || code >= colorSettings.ConsoleColorPalette.Count) {
			//	Show.Log(code + "? max is "+ colorSettings.ConsoleColorPalette.Count);
			//}
			return colorSettings.ConsoleColorPalette[code];
		}

		public int CursorSize {
			get { return (int)(cursorUi.cursorObject.transform.localScale.MagnitudeManhattan() / 3); }
			set { cursorUi.cursorObject.transform.localScale = Vector3.one * (value / 100f); }
		}

		private void Awake() {
			cursorUi.state = Console.cursor;
			colorSettings.FillInDefaultPalette();
			Console.Init();
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
			cursorUi.Init(this);
		}
		public void Update() {
			if (Console.textNeedsRefresh) {
				cursorUi.RefreshCursorPosition(this);
				RefreshText();
			}
		}

		public void RestartInput() => Console.RestartInput();

		public void RefreshText() {
			CoordRect limit = Console.Window.Limit;
			CalculateText(Output, limit, foreTile, true, colorSettings.foregroundAlpha);
			DisplayCalculations calc = null;
			if (Console.Window.IsAutosizing && NeedsCalculation()) {
				calc = new DisplayCalculations(this);
			}
			TransferToTMP(true, foreTile, calc);
			if (calc != null) {
				DoCalculation(calc);
			}
			if (charBack) {
				CalculateText(Output, limit, backTile, false, colorSettings.backgroundAlpha);
				TransferToTMP(false, backTile);
			}
			cursorUi.RefreshCursorPosition(this);
			Console.textNeedsRefresh = false;
			//Show.Log(body.Cursor);
		}

		public void Write(char c) => Console.Write(c);
		public void Write(object o) => Console.Write(o);
		public void Write(string text) => Console.Write(text);
		public void WriteLine(string text) => Console.Write(text + "\n");

		void CalculateText(ConsoleBody body, CoordRect window, List<Tile> out_tile, bool foreground, float alpha) {
			out_tile.Clear();
			ConsoleTile current = body.defaultColors;
			Coord limit = new Coord(window.Max.col, Math.Min(window.Max.row, Math.Max(body.lines.Count, Console.Cursor.row + 1)));
			int rowsPrinted = 0;
			//Coord cursor = body.Cursor;
			Console.cursor.indexInConsole = -1;
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
							if (!foreground) { current.Letter = defaultEmptyCharacter.Background; }
						} else if (line.Count > 0) {
							current.Letter = foreground ? defaultEmptyCharacter.Foreground : defaultEmptyCharacter.Background;
						}
						if (!foreground && Console.Cursor.col == col && Console.Cursor.row == row) {
							Console.cursor.indexInConsole = out_tile.Count;
						}
						ColorRGBA colorRgba = GetConsoleColor(foreground ? current.fore : current.back, foreground);
						colorRgba.a = (byte)(colorRgba.a * alpha);
						out_tile.Add(new Tile(current.Letter, colorRgba, 0));
					}
				}
				// make sure the cursor has a character underneath it
				if (Console.Cursor.row == row && Console.Cursor.col >= limit.col && window.Contains(Console.Cursor)) {
					int col = limit.col;
					ColorRGBA colorRgba = GetConsoleColor(foreground ? current.fore : current.back, foreground);
					colorRgba.a = (byte)(colorRgba.a * alpha);
					while (col <= Console.Cursor.col) {
						current.Letter = foreground ? defaultEmptyCharacter.Foreground : defaultEmptyCharacter.Background;
						if (!foreground && Console.Cursor.col == col && Console.Cursor.row == row) {
							Console.cursor.indexInConsole = out_tile.Count;
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
					if(i == Console.cursor.indexInConsole) {
						if (vertexIndex >= verts.Length) {
							Debug.LogWarning("weirdness happening. "+tmpText.text);
						} else {
							cursorUi.SetCursorPositionPoints(verts, vertexIndex);
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