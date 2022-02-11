using NonStandard.Data;
using System;
using System.Collections.Generic;

namespace NonStandard.Cli {
	[Serializable]
	public class ConsoleInput {
		public List<ConsoleArtifact> input = new List<ConsoleArtifact>();
		public int index;
		/// <summary>
		/// if not negative one, the cursor cannot move before this coordinate
		/// </summary>
		public Coord WriteCursorStartingPoint = Coord.NegativeOne;
		public bool IsAtOrBeforeStartingPoint(Coord writeCursor) {
			return writeCursor.row < WriteCursorStartingPoint.row ||
				writeCursor.row == WriteCursorStartingPoint.row && writeCursor.col <= WriteCursorStartingPoint.col;
		}

		/// <summary>
		/// returns where characters started changing and how many
		/// </summary>
		/// <param name="index"></param>
		/// <param name="tile"></param>
		/// <param name="body"></param>
		/// <returns></returns>
		public KeyValuePair<Coord, int> Insert(int index, ConsoleTile tile, ConsoleBody body) {
			int tilesRemainingInRow = CountCharsInRow(index);
			int tilesToShift = tilesRemainingInRow + 1;
			int indexAtEndOfLine = index + tilesToShift;
			Coord endPoint = WriteCursorStartingPoint + (Coord.Right * tilesRemainingInRow);
			input.Insert(indexAtEndOfLine, new ConsoleArtifact(endPoint, ConsoleTile.DefaultTile, body.GetAt(endPoint)));
			for (int i = indexAtEndOfLine; i > index; --i) {
				input[i] = input[i].WithDifferentTile(input[i - 1].tile);
			}
			input[index] = input[index].WithDifferentTile(tile);
			KeyValuePair<Coord, int> change = new KeyValuePair<Coord, int>(input[index].coord, tilesToShift);
			UpdateInputTiles(change.Key, change.Value, body);
			return change;
		}

		public KeyValuePair<Coord, int> Remove(int index, ConsoleBody body) {
			int tilesRemainingInRow = CountCharsInRow(index);
			int tilesToShift = tilesRemainingInRow - 1;
			int indexAtEndOfLine = index + tilesToShift;
			for (int i = index; i < indexAtEndOfLine; ++i) {
				input[i] = input[i].WithDifferentTile(input[i + 1].tile);
			}
			body.SetAt(input[indexAtEndOfLine].coord, input[indexAtEndOfLine].prev);
			input.RemoveAt(indexAtEndOfLine);
			KeyValuePair<Coord, int> change = new KeyValuePair<Coord, int>(input[index].coord, tilesToShift);
			UpdateInputTiles(change.Key, change.Value, body);
			return change;
		}

		private int CountCharsInRow(int index) {
			int tilesRemainingInRow = 0;
			if (index < input.Count) {
				int row = input[index].coord.Row;
				for (int i = index; i < input.Count && input[i].coord.Row == row; ++i) {
					++tilesRemainingInRow;
				}
			}
			return tilesRemainingInRow;
		}
		private void UpdateInputTiles(Coord start, int count, ConsoleBody body) {
			Coord cursor = start;
			for (int i = 0; i < count; ++i) {
				body.SetAt(cursor, input[index + i].tile);
				cursor += Coord.Right;
			}
		}
	}
}