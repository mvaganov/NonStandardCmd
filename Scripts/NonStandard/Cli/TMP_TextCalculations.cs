using NonStandard.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NonStandard.Extension;

namespace NonStandard.Cli {
	public partial class UnityConsoleOutput {
		private Vector2 sizeCalculated = -Vector2.one;
		private float fontSizeCalculated = -1;

		public void RefreshText() {
			CoordRect limit = console.Window.Limit;
			ConsoleBody OutputData = unityConsole.State.Output;
			CalculateText(OutputData, limit, _foreTile, true, unityConsole.colorSettings.foregroundAlpha, console.Cursor);
			UnityConsoleCalculations.Text calc = null;
			if (console.Window.IsAutosizing && NeedsCalculation()) {
				calc = new UnityConsoleCalculations.Text(this);
			}
			TransferToTMP(true, _foreTile, calc);
			if (calc != null) {
				DoCalculation(calc);
			}
			if (_backText) {
				CalculateText(OutputData, limit, _backTile, false, unityConsole.colorSettings.backgroundAlpha, console.Cursor);
				TransferToTMP(false, _backTile);
			}
			unityConsole.RefreshCursorPosition();
			console.textNeedsRefresh = false;
			//Show.Log(body.Cursor);
		}

		public bool NeedsCalculation() {
			return fontSizeCalculated != _foreText.fontSize || TextAreaSize() != sizeCalculated;
		}
		public void DoCalculation(UnityConsoleCalculations.Text calc) {
			Vector2 ideal = calc.CalculateIdealSize();
			console.Window.viewRect.Size = new Coord((short)ideal.x, (short)ideal.y);
			console.Window.UpdatePosition();
			// if the calculated values are reasonable limits
			if (ideal.x < Coord.Max.X - 2 || ideal.y < Coord.Max.Y - 2) {
				// cache these calculations as valid, which means they won't be recalculated as much later
				fontSizeCalculated = _foreText.fontSize;
				sizeCalculated = TextAreaSize();
			}
		}

		void CalculateText(ConsoleBody body, CoordRect window, List<Tile> out_tile, bool foreground, float alpha, ConsoleState.CursorState cursorState) {
			out_tile.Clear();
			ConsoleTile current = body.defaultColors;
			Coord limit = new Coord(window.Max.col, Math.Min(window.Max.row,
				Math.Max(body.lines.Count, cursorState.position2d.row + 1)));
			int rowsPrinted = 0;
			//Coord cursor = body.Cursor;
			cursorState.indexInConsole = -1;
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
						if (!foreground && cursorState.position2d.col == col && cursorState.position2d.row == row) {
							cursorState.indexInConsole = out_tile.Count;
						}
						ColorRGBA colorRgba = unityConsole.GetConsoleColor(foreground ? current.fore : current.back, foreground);
						colorRgba.a = (byte)(colorRgba.a * alpha);
						out_tile.Add(new Tile(current.Letter, colorRgba, 0));
					}
				}
				// make sure the cursor has a character underneath it
				if (cursorState.position2d.row == row && cursorState.position2d.col >= limit.col
				&& window.Contains(cursorState.position2d)) {
					int col = limit.col;
					ColorRGBA colorRgba = unityConsole.GetConsoleColor(foreground ? current.fore : current.back, foreground);
					colorRgba.a = (byte)(colorRgba.a * alpha);
					while (col <= cursorState.position2d.col) {
						current.Letter = foreground ? defaultEmptyCharacter.Foreground : defaultEmptyCharacter.Background;
						if (!foreground && cursorState.position2d.col == col && cursorState.position2d.row == row) {
							cursorState.indexInConsole = out_tile.Count;
						}
						out_tile.Add(new Tile(current.Letter, colorRgba, 0));
						++col;
					}
				}
			}
		}

		public void TransferToTMP(bool foreground, List<Tile> tiles, UnityConsoleCalculations.Text calc = null) {
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
					if (i == console.Cursor.indexInConsole) {
						if (vertexIndex >= verts.Length) {
							Debug.LogWarning("weirdness happening. " + tmpText.text);
						} else {
							unityConsole.cursorUI.SetCursorPositionPoints(verts, vertexIndex);
						}
					}
					if (vertexIndex < vertColors.Length && i < tiles.Count
					&& !vertColors[vertexIndex].EqualRgba(color = tiles[i].color)) {
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

	}
}
