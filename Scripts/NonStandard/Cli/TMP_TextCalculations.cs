// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
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
		private const char TMProTextOutputTerminator = '\u200B';
		public UnityConsoleCalculations.Text calc = null;
		public void RefreshText() {
			CoordRect limit = console.Window.Limit;
			ConsoleBody OutputData = unityConsole.State.Output;
			CalculateText(OutputData, limit, _foreTile, true, unityConsole.colorSettings.foregroundAlpha, console.Cursor);
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

		/// <summary>
		/// transfers the given tiles to the Text Mesh Pro text output.
		/// </summary>
		/// <param name="foreground">it true, use the inputField if available. otherwise, use the background</param>
		/// <param name="tiles"></param>
		/// <param name="foundCursorVerts"></param>
		/// <param name="calc"></param>
		public void TransferToTMP(bool foreground, List<Tile> tiles, UnityConsoleCalculations.Text calc = null) {
			TMP_Text tmpText;
			char[] letters = new char[tiles.Count];
			for (int i = 0; i < letters.Length; ++i) { letters[i] = tiles[i].letter; }
			string text = new string(letters);
			if (foreground) {
				tmpText = inputField ? SetTmpText(inputField, text) : SetTmpText(_foreText, text);
			} else {
				tmpText = SetTmpText(_backText, text);
			}
			TMP_CharacterInfo[] chars = tmpText.textInfo.characterInfo;
			Vector3 normal = -transform.forward;
			bool vertChange = false, colorChange = false;

			// TMPro may have multiple meshes per char, in layers. each layer is in meshInfo
			for (int m = 0; m < tmpText.textInfo.meshInfo.Length; ++m) {
				UpdateTmpMesh(tiles, tmpText.textInfo.meshInfo[m], chars, normal, ref colorChange, ref vertChange, calc);
			}
			if (colorChange) { tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32); }
			if (vertChange) { tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices); }
		}

		private void UpdateTmpMesh(List<Tile> tiles, TMP_MeshInfo meshInfo, TMP_CharacterInfo[] chars, Vector3 normal, 
		ref bool colorChange, ref bool vertChange, UnityConsoleCalculations.Text calc) {
			Color32[] vertColors = meshInfo.colors32;
			Vector3[] verts = meshInfo.vertices;
			calc?.CalculateTextBoundaries(verts);
			calc?.StartCalculatingText();
			for (int i = 0; i < chars.Length; ++i) {
				TMP_CharacterInfo cinfo = chars[i];
				if (cinfo.character == TMProTextOutputTerminator) break;
				calc?.UpdateTextCalculation(cinfo.character);
				if (!cinfo.isVisible) continue;
				int vertexIndex = cinfo.vertexIndex;
				if (i >= tiles.Count) { continue; }
				colorChange |= SetTmpTextQuadColor(vertexIndex, vertColors, tiles[i].color);
				vertChange |= SetTmpTextQuadHeight(vertexIndex, verts, normal, tiles[i].height);
			}
		}
		private TMP_Text SetTmpText(TMP_InputField inputField, string text) {
			inputField.text = text;
			inputField.ForceLabelUpdate();
			return inputField.textComponent;
		}

		private TMP_Text SetTmpText(TMP_Text tmp_text, string text) {
			tmp_text.text = text;
			tmp_text.ForceMeshUpdate();
			return tmp_text;
		}

		private static bool SetTmpTextQuadColor(int vertexIndex, Color32[] vertColors, Color color) {
			if (vertexIndex >= vertColors.Length || vertColors[vertexIndex].EqualRgba(color)) { return false; }
			vertColors[vertexIndex + 0] = color;
			vertColors[vertexIndex + 1] = color;
			vertColors[vertexIndex + 2] = color;
			vertColors[vertexIndex + 3] = color;
			return true;
		}

		private bool SetTmpTextQuadHeight(int vertexIndex, Vector3[] verts, Vector3 normal, float height) {
			if (height == 0) { return false; }
			Vector3 h = height * normal;
			verts[vertexIndex + 0] = verts[vertexIndex + 0] + h;
			verts[vertexIndex + 1] = verts[vertexIndex + 1] + h;
			verts[vertexIndex + 2] = verts[vertexIndex + 2] + h;
			verts[vertexIndex + 3] = verts[vertexIndex + 3] + h;
			return true;
		}
	}
}
