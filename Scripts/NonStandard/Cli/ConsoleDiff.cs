using NonStandard.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace NonStandard.Cli {
	[Serializable]
	public class ConsoleDiff {
		public List<ConsoleDiffUnit> delta = new List<ConsoleDiffUnit>();
		public CoordRect inputArea = CoordRect.Invalid;

		public Coord Start = Coord.NegativeOne;
		public bool IsAtOrBeforeStartingPoint(Coord writeCursor) {
			return writeCursor.row < Start.row ||
				writeCursor.row == Start.row && writeCursor.col <= Start.col;
		}

		/// <summary>
		/// returns how many characters were changed after the given index
		/// </summary>
		public void Insert(int index, ConsoleTile tile, ConsoleBody body, ref Coord Cursor) {
			if (tile.Letter == '\n') {
				InsertNewline(index, body);
				if (!inputArea.IsValid) {
					Cursor = new Coord(0, Cursor.y + 1);
					UnityEngine.Debug.Log("cursor is beyond acceptible area");
				} else {
					Cursor = new Coord(inputArea.min.col, Cursor.y + 1);
				}
				return;
			}
			int tilesRemainingInRow = CountCharsInRow(index);
			//int tilesToShift = tilesRemainingInRow + 1;
			int indexAtEndOfLine = index + tilesRemainingInRow;
			Coord coordOfIndex = GetCoord(index);
			Coord endPoint = coordOfIndex + (Coord.Right * tilesRemainingInRow);
			string currentString = ToSimpleString();
			ConsoleTile overwritten = body.GetAt(coordOfIndex);
			if (overwritten.Letter == '\0') {
				//UnityEngine.Debug.LogWarning("nothing here " + coordOfIndex);
			} else {
				//UnityEngine.Debug.LogWarning((int)overwritten.Letter+" {"+overwritten.Letter+ "} " + coordOfIndex + " : " + body.ToString()); // TODO why is it finding a space where it should find letters?
			}
			//UnityEngine.Debug.Log("inserting " + tile + " over '" + overwritten + "' @" + coordOfIndex + ":" + index + "/" + delta.Count+ " between ["+currentString.Substring(0, index)+ "] and ("+currentString.Substring(index)+")");
			delta.Insert(index, new ConsoleDiffUnit(coordOfIndex, tile, overwritten));
			//for (int i = indexAtEndOfLine; i > index; --i) {
			//	input[i].OffsetCoord(Coord.Right);
			//}
			// push forward the other letters
			for (int i = index+1; i < delta.Count && delta[i].coord.Row == coordOfIndex.Row; ++i) {
				Coord testCoord = delta[i].coord;
				delta[i] = delta[i].WithOffsetCoord(Coord.Right);
				if (testCoord == delta[i].coord) {
					throw new Exception("bruh, not working.");
				}
			}
			//UnityEngine.Debug.Log("wrote " + tile.Letter + " at " + endPoint + ":" + indexAtEndOfLine
			//	+ " (" + delta.Count + ", " + tilesRemainingInRow + " after) " + ToString());

			//input[index] = input[index].WithDifferentTile(tile);
			Cursor += Coord.Right;
			//if (!inputArea.IsValid) {
			//	if (Cursor.col >= body.size.col) {
			//		Cursor = new Coord(0, Cursor.y + 1);
			//	}
			//}
			//if (inputArea.IsValid) {
			//	if (Cursor.col >= inputArea.max.col) {
			//		Cursor = new Coord(inputArea.min.col, Cursor.y + 1);
			//	}
			//}
			WriteNext(body, index, tilesRemainingInRow + 1);
		}

		public void InsertNewline(int index, ConsoleBody body) {
			WritePrev(body, index, delta.Count - index);
			Coord insertionPoint = GetCoord(index);
			delta.Insert(index, new ConsoleDiffUnit(insertionPoint, ConsoleTile.DefaultTile.CloneWithLetter('\n'), body.GetAt(insertionPoint)));
			int nextLineStartOffset = 0;
			Coord offset;
			if (insertionPoint.col != 0) {
				offset = new Coord(-delta[index + 1].coord.col, 1);
				for (int i = index + 1; i < delta.Count; ++i) {
					delta[i] = delta[i].WithOffsetCoord(offset);
					++nextLineStartOffset;
					if (delta[i].next == '\n') {
						break;
					}
				}
			}
			offset = new Coord(0, 1);
			for (int i = index + 1 + nextLineStartOffset; i < delta.Count; ++i) {
				delta[i] = delta[i].WithOffsetCoord(offset);
			}
			WriteNext(body, index, delta.Count - index);
		}

		public void RemoveNewline(int index, ConsoleBody body) {
			WritePrev(body, index, delta.Count - index);
			Coord insertionPoint = GetCoord(index);
			delta.RemoveAt(index);
			int nextLineStartOffset = 0;
			Coord offset;
			if (insertionPoint.col != 0) {
				offset = new Coord(delta[index].coord.col, -1);
				for (int i = index + 1; i < delta.Count; ++i) {
					delta[i] = delta[i].WithOffsetCoord(offset);
					++nextLineStartOffset;
					if (delta[i].next == '\n') {
						break;
					}
				}
			}
			offset = new Coord(0, -1);
			for (int i = index + 1 + nextLineStartOffset; i < delta.Count; ++i) {
				delta[i] = delta[i].WithOffsetCoord(offset);
			}
			WriteNext(body, index, delta.Count - index);
		}

		public bool TryGetIndexOf(Coord c, out int index) {
			if (c == Start && delta.Count == 0) {
				index = 0;
				return true;
			}
			if (c.row < Start.row) {
				index = 0;
				return false;
			}
			Coord limit = FinalIndexCoord();
			if (c == limit) { index = delta.Count; return true; }
			if (c.row > limit.row || c.row == limit.row && c.col > limit.col) {
				index = delta.Count;
				return false;
			}
			Coord start = delta.Count > 0 ? delta[0].coord : Start;
			if (c.row < start.row || (c.row == start.row && c.col < start.Col)) {
				index = 0;
				return false;
			}
			for (index = 0; index < delta.Count; ++index) {
				Coord coord = delta[index].coord;
				if (coord == c) {
					return true;
				}
				if (coord.Row > c.Row) {
					--index;
					return false;
				}
			}
			index = delta.Count;
			return true;
			//throw new Exception("should return before this point");
		}

		public Coord FinalIndexCoord() {
			if (delta.Count == 0) {
				//UnityEngine.Debug.Log("final index is start");
				return Start;
			}
			Coord lastOne = delta[delta.Count - 1].coord;
			Coord limit = lastOne + Coord.Right;
			if (delta[delta.Count - 1].next.Letter == '\n' || (inputArea.IsValid && limit.X >= inputArea.Max.X)) {
				++limit.Row;
				limit.Col = inputArea.IsValid ? inputArea.Min.Col : 0;
				//UnityEngine.Debug.Log("final index crosses row boundary!");
			}
			//UnityEngine.Debug.Log("final index is "+limit);
			return limit;
		}

		public void RemoveAt(int index, ConsoleBody body) {
			ConsoleTile tile = delta[index].next;
			if (tile.Letter == '\n') {
				RemoveNewline(index, body);
				return;
			}
			int tilesRemainingInRow = CountCharsInRow(index);
			int tilesToShift = tilesRemainingInRow - 1;
			int indexAtEndOfLine = index + tilesToShift;
			for (int i = index; i < indexAtEndOfLine; ++i) {
				delta[i] = delta[i].WithDifferentTile(delta[i + 1].next);
			}
			body.SetAt(delta[indexAtEndOfLine].coord, delta[indexAtEndOfLine].prev);
			delta.RemoveAt(indexAtEndOfLine);
			WriteNext(body, index, tilesToShift);
		}

		private int CountCharsInRow(int index) {
			int tilesRemainingInRow = 0;
			if (index < delta.Count) {
				int row = delta[index].coord.Row;
				for (int i = index; i < delta.Count && delta[i].coord.Row == row; ++i) {
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
				ConsoleDiffUnit unit = delta[i];
				body.SetAt(unit.coord, next ? unit.next : unit.prev);
			}
		}

		public Coord GetCoord(int inputIndex) {
			if (inputIndex < 0) return Start;
			if (inputIndex >= delta.Count) { return FinalIndexCoord(); }
			UnityEngine.Debug.Log("not end, not start: "+inputIndex+":"+ delta[inputIndex].coord);
			return delta[inputIndex].coord;
		}

		internal void Clear() {
			delta.Clear();
		}

		public string ToSimpleString() {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < delta.Count; ++i) {
				sb.Append(delta[i].next.Letter);
			}
			return sb.ToString();
		}
		public string ToSimpleStringPrev() {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < delta.Count; ++i) {
				sb.Append(delta[i].prev.Letter);
			}
			return sb.ToString();
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			Coord c = Start;
			sb.Append(c).Append("\"");
			for (int i = 0; i < delta.Count; ++i) {
				ConsoleDiffUnit du = delta[i];
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