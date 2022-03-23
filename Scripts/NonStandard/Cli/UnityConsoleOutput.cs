// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using NonStandard.Data;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NonStandard.Extension;

namespace NonStandard.Cli {
	public class UnityConsoleOutput : MonoBehaviour {
		public UnityConsole unityConsole;
		public ConsoleState Console => unityConsole.State;

		#region Unity Lifecycle
		private void Awake() {
			unityConsole = GetComponent<UnityConsole>();
			unityConsole.cursorUI.state = Console.Cursor;
			Console.Init();
		}

		void Start() {
			FindUiTransform();
			CreateBackgroundTextArea();
		}

		public void Update() {
			if (Console.textNeedsRefresh) {
				unityConsole.RefreshCursorPosition();
				RefreshText();
			}
		}
		#endregion Unity Lifecycle

		#region API
		public void Write(char c) => Console.Write(c);
		public void Write(object o) => Console.Write(o);
		public void Write(string text) => Console.Write(text);
		public void WriteLine(string text) => Console.Write(text + "\n");

		public Vector2 TextAreaSize() {
			return (inputField != null ? inputField.textViewport : _foreText.GetComponent<RectTransform>()).rect.size;
		}
		public float FontSize {
			get => inputField != null ? inputField.pointSize : Text.fontSize;
			set {
				if (inputField != null) {
					inputField.pointSize = _backText.fontSize = value;
				} else {
					Text.fontSize = _backText.fontSize = value;
				}
			}
		}
		#endregion API

		#region User Interface
		public TMP_InputField inputField;

		public TMP_Text Text => inputField != null ? inputField.textComponent : _foreText;

		private void CreateBackgroundTextArea() {
			TMP_Text pTmp = Text;
			GameObject backgroundObject = Instantiate(Text.gameObject);
			UnityConsoleOutput extra = backgroundObject.GetComponent<UnityConsoleOutput>();
			if (extra != null) { DestroyImmediate(extra); }
			RectTransform brt = backgroundObject.GetComponent<RectTransform>();
			if (brt == null) { brt = backgroundObject.AddComponent<RectTransform>(); }
			backgroundObject.transform.SetParent(pTmp.transform.parent);
			if (pTmp.transform.parent != null) {
				backgroundObject.transform.SetSiblingIndex(0); // put the background in the background
			}
			backgroundObject.transform.localPosition = Vector3.zero;
			backgroundObject.transform.localScale = Vector3.one;
			_backText = backgroundObject.GetComponent<TMP_Text>();
			_backText.fontMaterial.renderQueue -= 1;
			if (inputField) {
				inputField.targetGraphic.material.renderQueue -= 2;
			}
			RectTransform rt = pTmp.GetComponent<RectTransform>();
			brt.anchorMin = rt.anchorMin;
			brt.anchorMax = rt.anchorMax;
			brt.offsetMin = rt.offsetMin;
			brt.offsetMax = rt.offsetMax;
		}

		public RectTransform FindUiTransform() {
			if (inputField == null) { inputField = GetComponentInChildren<TMP_InputField>(); }
			if (!inputField) {
				_foreText = GetComponentInChildren<TMP_Text>();
			} else {
				_foreText = inputField.textComponent;
				inputField.readOnly = true;
				inputField.richText = false;
			}
			if (inputField != null) { return inputField.GetComponent<RectTransform>(); }
			return _foreText.GetComponent<RectTransform>();
		}
		#endregion User Interface

		#region calculations
		internal Vector2 sizeCalculated = -Vector2.one;
		internal float fontSizeCalculated = -1;

		public void AddToFontSize(float value) {
			FontSize += value;
			if (FontSize < 1) { FontSize = 1; }
			//if (unityConsole.cursorUi.cursorObject != null) {
			//	unityConsole.cursorUi.cursorObject.GetComponent<Cli.UnityConsoleCursor>().ScaleToFontSize(FontSize);
			//}
			if (unityConsole.cursorUI != null) {
				unityConsole.cursorUI.GetComponent<Cli.UnityConsoleCursor>().ScaleToFontSize(FontSize);
			}
			RefreshText();
		}

		public void RefreshText() {
			CoordRect limit = Console.Window.Limit;
			ConsoleBody OutputData = unityConsole.State.Output;
			CalculateText(OutputData, limit, _foreTile, true, unityConsole.colorSettings.foregroundAlpha);
			DisplayCalculations calc = null;
			if (Console.Window.IsAutosizing && NeedsCalculation()) {
				calc = new DisplayCalculations(this);
			}
			TransferToTMP(true, _foreTile, calc);
			if (calc != null) {
				DoCalculation(calc);
			}
			if (_backText) {
				CalculateText(OutputData, limit, _backTile, false, unityConsole.colorSettings.backgroundAlpha);
				TransferToTMP(false, _backTile);
			}
			unityConsole.RefreshCursorPosition();
			Console.textNeedsRefresh = false;
			//Show.Log(body.Cursor);
		}

		void CalculateText(ConsoleBody body, CoordRect window, List<Tile> out_tile, bool foreground, float alpha) {
			out_tile.Clear();
			ConsoleTile current = body.defaultColors;
			Coord limit = new Coord(window.Max.col, Math.Min(window.Max.row, Math.Max(body.lines.Count, Console.CursorPosition.row + 1)));
			int rowsPrinted = 0;
			//Coord cursor = body.Cursor;
			Console.Cursor.indexInConsole = -1;
			for (int row = window.Min.row; row < limit.row; ++row, ++rowsPrinted) {
				if (rowsPrinted > 0) {
					ColorRGBA colorRgba = unityConsole.GetConsoleColor(foreground ? current.fore : current.back, foreground);
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
						if (!foreground && Console.CursorPosition.col == col && Console.CursorPosition.row == row) {
							Console.Cursor.indexInConsole = out_tile.Count;
						}
						ColorRGBA colorRgba = unityConsole.GetConsoleColor(foreground ? current.fore : current.back, foreground);
						colorRgba.a = (byte)(colorRgba.a * alpha);
						out_tile.Add(new Tile(current.Letter, colorRgba, 0));
					}
				}
				// make sure the cursor has a character underneath it
				if (Console.CursorPosition.row == row && Console.CursorPosition.col >= limit.col && window.Contains(Console.CursorPosition)) {
					int col = limit.col;
					ColorRGBA colorRgba = unityConsole.GetConsoleColor(foreground ? current.fore : current.back, foreground);
					colorRgba.a = (byte)(colorRgba.a * alpha);
					while (col <= Console.CursorPosition.col) {
						current.Letter = foreground ? defaultEmptyCharacter.Foreground : defaultEmptyCharacter.Background;
						if (!foreground && Console.CursorPosition.col == col && Console.CursorPosition.row == row) {
							Console.Cursor.indexInConsole = out_tile.Count;
						}
						out_tile.Add(new Tile(current.Letter, colorRgba, 0));
						++col;
					}
				}
			}
		}
		public bool NeedsCalculation() {
			return fontSizeCalculated != _foreText.fontSize || TextAreaSize() != sizeCalculated;
		}
		public void DoCalculation(DisplayCalculations calc) {
			Vector2 ideal = calc.CalculateIdealSize();
			Console.Window.viewRect.Size = new Coord((short)ideal.x, (short)ideal.y);
			Console.Window.UpdatePosition();
			// if the calculated values are reasonable limits
			if (ideal.x < Coord.Max.X - 2 || ideal.y < Coord.Max.Y - 2) {
				// cache these calculations as valid, which means they won't be recalculated as much later
				fontSizeCalculated = _foreText.fontSize;
				sizeCalculated = TextAreaSize();
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
					tmpText = this._foreText;
					tmpText.text = text;
					tmpText.ForceMeshUpdate();
				}
			} else {
				tmpText = _backText;
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
					if (i == Console.Cursor.indexInConsole) {
						if (vertexIndex >= verts.Length) {
							Debug.LogWarning("weirdness happening. " + tmpText.text);
						} else {
							unityConsole.cursorUI.SetCursorPositionPoints(verts, vertexIndex);
						}
					}
					if (vertexIndex < vertColors.Length && i < tiles.Count && !vertColors[vertexIndex].Eq(color = tiles[i].color)) {
						colorChange = true;
						vertColors[vertexIndex + 0] = color;
						vertColors[vertexIndex + 1] = color;
						vertColors[vertexIndex + 2] = color;
						vertColors[vertexIndex + 3] = color;
					}
					if (vertexIndex < vertColors.Length && i < tiles.Count && (height = tiles[i].height) != 0) {
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
		public class DisplayCalculations {
			public DisplayCalculations(Cli.UnityConsoleOutput console) {
				rt = console.inputField != null ? console.inputField.textViewport : console._foreText.GetComponent<RectTransform>();
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
				Vector2 maxInArea = new Vector2(Coord.Max.X - 1, Coord.Max.Y - 1);
				// only limit the size if there is not enough space
				if (totSize.x > rect.width || totSize.y > rect.height) {
					Vector2 glyphSize = new Vector2(totSize.x / size.X, totSize.y / size.Y);
					maxInArea = new Vector2(rect.width / glyphSize.x, rect.height / glyphSize.y - 1);
					//Show.Log(maxInArea + " <-- " + glyphSize + " chars: " + size + "   sized: " + totSize + "   / " + rt.rect.width + "," + rt.rect.height);
				}
				return maxInArea;
			}
		}
		#endregion calculations

		#region Settings
		public CharSettings defaultEmptyCharacter = new CharSettings();

		[System.Serializable] public class CharSettings {
			public char Foreground = ' '; // normal space
			public char Background = '\u2588'; // █
		}
		#endregion Settings

		#region Tile
		private TMP_Text _foreText;
		private TMP_Text _backText;
		private List<Tile> _foreTile = new List<Tile>();
		private List<Tile> _backTile = new List<Tile>();

		public struct Tile {
			public ColorRGBA color;
			public float height;
			public char letter;
			public short tag;
			public Tile(char letter, ColorRGBA color, float height) {
				this.letter = letter; this.height = height; this.color = color; tag = 0;
			}
			public Tile(char letter, ColorRGBA color, float height, short tag) {
				this.letter = letter; this.height = height; this.color = color; this.tag = tag;
			}
		}
		#endregion Tile

		#region Tags
		public Dictionary<short, TextSpan> tags = new Dictionary<short, TextSpan>();
		private short nextFreeTag = 0;
		public List<TextSpan> _textSpans = new List<TextSpan>();

		public short AddTagToTextSpan(Coord start, Coord end, object tag) {
			// create a text span with the stack trace as metadata to that span
			TextSpan ts = new TextSpan(start, end, tag);
			// add the span to a sorted data structure
			_textSpans.BinarySearchInsert(ts);
			short tagId = nextFreeTag++;
			tags[tagId] = ts;
			return tagId;
		}

		public List<TextSpan> GetTag(Coord coord) {
			List<TextSpan> found = TextSpan.GetSpans(_textSpans, coord);
			return found;
		}

		#endregion Tags
	}
}
