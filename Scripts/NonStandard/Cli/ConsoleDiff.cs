using NonStandard.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NonStandard.Cli {
	[Serializable]
	public class ConsoleDiff {
		public List<ConsoleDiffUnit> input = new List<ConsoleDiffUnit>();
		public CoordRect inputArea = CoordRect.Invalid;

		public Coord Start = Coord.NegativeOne;
		public bool IsAtOrBeforeStartingPoint(Coord writeCursor) {
			return writeCursor.row < Start.row ||
				writeCursor.row == Start.row && writeCursor.col <= Start.col;
		}

		/// <summary>
		/// returns how many characters were changed after the given index
		/// </summary>
		public void Insert(int index, ConsoleTile tile, ConsoleBody body) {
			if (tile.Letter == '\n') {
				InsertNewline(index, body);
				return;
			}
			int tilesRemainingInRow = CountCharsInRow(index);
			int tilesToShift = tilesRemainingInRow;
			int indexAtEndOfLine = index + tilesToShift;
			Coord endPoint = Start + (Coord.Right * tilesRemainingInRow);
			UnityEngine.Debug.Log("inserting ' ' at "+ endPoint+":"+indexAtEndOfLine + " in input("+input.Count+") for '" + tile.Letter+"', with "+ tilesRemainingInRow+" columns after it");
			input.Insert(indexAtEndOfLine, new ConsoleDiffUnit(endPoint, ConsoleTile.DefaultTile, body.GetAt(endPoint)));
			for (int i = indexAtEndOfLine; i > index; --i) {
				input[i] = input[i].WithDifferentTile(input[i - 1].next);
			}
			input[index] = input[index].WithDifferentTile(tile);
			WriteNext(body, index, tilesToShift);
		}

		public void InsertNewline(int index, ConsoleBody body) {
			WritePrev(body, index, input.Count - index);
			Coord insertionPoint = GetCoord(index);
			input.Insert(index, new ConsoleDiffUnit(insertionPoint, ConsoleTile.DefaultTile.CloneWithLetter('\n'), body.GetAt(insertionPoint)));
			int nextLineStartOffset = 0;
			Coord offset;
			if (insertionPoint.col != 0) {
				offset = new Coord(-input[index + 1].coord.col, 1);
				for (int i = index + 1; i < input.Count; ++i) {
					input[i].OffsetCoord(offset);
					++nextLineStartOffset;
					if (input[i].next == '\n') {
						break;
					}
				}
			}
			offset = new Coord(0, 1);
			for (int i = index + 1 + nextLineStartOffset; i < input.Count; ++i) {
				input[i].OffsetCoord(offset);
			}
			WriteNext(body, index, input.Count - index);
		}

		public void RemoveNewline(int index, ConsoleBody body) {
			WritePrev(body, index, input.Count - index);
			Coord insertionPoint = GetCoord(index);
			input.RemoveAt(index);
			int nextLineStartOffset = 0;
			Coord offset;
			if (insertionPoint.col != 0) {
				offset = new Coord(input[index].coord.col, -1);
				for (int i = index + 1; i < input.Count; ++i) {
					input[i].OffsetCoord(offset);
					++nextLineStartOffset;
					if (input[i].next == '\n') {
						break;
					}
				}
			}
			offset = new Coord(0, -1);
			for (int i = index + 1 + nextLineStartOffset; i < input.Count; ++i) {
				input[i].OffsetCoord(offset);
			}
			WriteNext(body, index, input.Count - index);
		}

		public bool TryGetIndexOf(Coord c, out int index) {
			if (c == Start && input.Count == 0) {
				index = 0;
				return true;
			}
			if (c.row < Start.row) {
				index = 0;
				return false;
			}
			Coord limit = FinalIndexCoord();
			if (c == limit) { index = input.Count; return true; }
			if (c.row > limit.row || c.row == limit.row && c.col > limit.col) {
				index = input.Count;
				return false;
			}
			for (index = 0; index < input.Count; ++index) {
				Coord coord = input[index].coord;
				if (coord == c) {
					return true;
				}
				if (coord.Row > c.Row) {
					--index;
					return false;
				}
			}
			throw new Exception("should return before this point");
		}

		public Coord FinalIndexCoord() {
			if (input.Count == 0) {
				UnityEngine.Debug.Log("final index is start");
				return Start;
			}
			Coord lastOne = input[input.Count - 1].coord;
			Coord limit = lastOne + Coord.Right;
			if (input[input.Count - 1].next.Letter == '\n' || (inputArea.IsValid && limit.X >= inputArea.Max.X)) {
				++limit.Row;
				limit.Col = inputArea.IsValid ? inputArea.Min.Col : 0;
				UnityEngine.Debug.Log("final index crosses row boundary!");
			}
			UnityEngine.Debug.Log("final index is "+limit);
			return limit;
		}

		public void RemoveAt(int index, ConsoleBody body) {
			ConsoleTile tile = input[index].next;
			if (tile.Letter == '\n') {
				RemoveNewline(index, body);
				return;
			}
			int tilesRemainingInRow = CountCharsInRow(index);
			int tilesToShift = tilesRemainingInRow - 1;
			int indexAtEndOfLine = index + tilesToShift;
			for (int i = index; i < indexAtEndOfLine; ++i) {
				input[i] = input[i].WithDifferentTile(input[i + 1].next);
			}
			body.SetAt(input[indexAtEndOfLine].coord, input[indexAtEndOfLine].prev);
			input.RemoveAt(indexAtEndOfLine);
			WriteNext(body, index, tilesToShift);
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
		public void WriteNext(ConsoleBody body, int startIndex, int count) { Write(body, true, startIndex, count); }
		public void WritePrev(ConsoleBody body, int startIndex, int count) { Write(body, false, startIndex, count); }

		private void Write(ConsoleBody body, bool next, int start, int count) {
			int end = start + count;
			for (int i = start; i < end; ++i) {
				ConsoleDiffUnit unit = input[i];
				body.SetAt(unit.coord, next ? unit.next : unit.prev);
			}
		}

		public Coord GetCoord(int inputIndex) {
			if (inputIndex < 0) return Start;
			if (inputIndex >= input.Count) { return FinalIndexCoord(); }
			UnityEngine.Debug.Log("not end, not start: "+inputIndex+":"+ input[inputIndex].coord);
			return input[inputIndex].coord;
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			Coord c = Start;
			sb.Append(c).Append("\"");
			for (int i = 0; i < input.Count; ++i) {
				ConsoleDiffUnit du = input[i];
				if (c != du.coord) {
					sb.Append("\" ").Append(du.coord).Append("\"");
					c = du.coord;
				}
				switch(du.next.Letter) {
					case '\"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					default: sb.Append(du.next.Letter); break;
				}
				c += Coord.Right;
			}
			sb.Append("\"");
			return sb.ToString();
		}
	}
}