using NonStandard.Data;
using System.Collections.Generic;
using NonStandard.Extension;
using System;
using System.Text;

namespace NonStandard.Cli {
	/// <summary>
	/// logic for a command-line console, agnostic of rendering layer
	/// </summary>
	[Serializable] public class ConsoleBody {
		public int spacesPerTab = 4;
		public ConsoleTile defaultColors = new ConsoleTile(' ', ConsoleColor.Gray, ConsoleColor.Black);
		public ConsoleTile currentDefaultTile = new ConsoleTile(' ', ConsoleColor.Gray, ConsoleColor.Black);
		public ConsoleColor[] unprintableColors = new ConsoleColor[] {
			ConsoleColor.DarkGray, ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkYellow,
			ConsoleColor.DarkRed, ConsoleColor.DarkGreen, ConsoleColor.DarkBlue,
		};

		/// <summary>
		/// allows exceptions to be made for specific otherwise unprintable characters to be printed
		/// </summary>
		public Dictionary<char, ConsoleTile> printableCharacters = new Dictionary<char, ConsoleTile>();
		public List<List<ConsoleTile>> lines = new List<List<ConsoleTile>>();
		public Coord size;

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			for(int row = 0; row < lines.Count; ++row) {
				List<ConsoleTile> line = lines[row];
				for (int col = 0; col < line.Count; ++col) {
					sb.Append(line[col]);
					if (line[col] == '\n' && col != line.Count - 1) {
						throw new Exception("newline characters should not be in the middle of a line");
					}
				}
				if (line[line.Count - 1] != '\n' && row != lines.Count - 1) {
					sb.Append('\n');
				}
			}
			return sb.ToString();
		}

		public Coord Size {
			get => size;
		}

		public void Clear() {
			size = Coord.Zero;
			lines.Clear();
		}

		public static void ParseColorSequenceDelimeter(string text, ref int i, out byte foreColor, out byte backColor) {
			foreColor = Col.DefaultColorIndex;
			backColor = Col.DefaultColorIndex;
			char c = text[i];
			if (c != Col.ColorSequenceDelim) { return; }
			int amountToAdvance = 0;
			if (i + 1 < text.Length) {
				++amountToAdvance;
				foreColor = (byte)text[i + 1].GetDigitValueHexadecimalPattern();
			}
			if (i + 2 < text.Length) {
				++amountToAdvance;
				backColor = (byte)text[i + 2].GetDigitValueHexadecimalPattern();
			}
			i += amountToAdvance;
		}

		private void ConsoleBackspace(ref Coord writeCursor) {
			List<ConsoleTile> line = lines[writeCursor.row];
			if (writeCursor.col == line.Count) {
				if (line.Count == 0) {
					if (writeCursor.row == lines.Count - 1) {
						lines.RemoveAt(lines.Count - 1);
						size.row = (short)lines.Count;
					}
				} else {
					line.RemoveAt(line.Count - 1);
					if (writeCursor.col + 1 >= size.col) {
						size.x = CalculateWidth();
					}
				}
			}
			--writeCursor.col;
			while (writeCursor.col < 0) {
				if (writeCursor.row <= 0) { writeCursor.col = writeCursor.row = 0; break; }
				--writeCursor.row;
				line = lines[writeCursor.row];
				if (line.Count > 0) {
					writeCursor.col += (short)(line.Count);
				}
			}
			if (writeCursor.row < 0) { writeCursor.row = 0; }
		}
		private void ConsoleNewline(ref Coord writeCursor) {
			++writeCursor.row;
			writeCursor.col = 0;
		}

		private void AssertValidWriteCursor(Coord writeCursor, string text) {
			if (writeCursor.row >= lines.Count || writeCursor.row < 0) {
				throw new Exception("bad write cursor state " + writeCursor + " with " + lines.Count + " rows");
			}
			if (writeCursor.col < 0) {
				throw new Exception("negative column? " + writeCursor + " trying to write " + text.Length + " chars: " + text);
			}
		}

		public void Write(string text, ref Coord writeCursor) {
			for (int i = 0; i < text.Length; ++i) {
				char c = text[i];
				bool printCharacter = false;
				switch (c) {
					case Col.ColorSequenceDelim: ProcessColorSequenceDelimiter(text, i); continue;
					case '\b': ConsoleBackspace(ref writeCursor); break;
					case '\n': ConsoleNewline(ref writeCursor); break;
					default: printCharacter = true; break;
				}
				EnsureSufficientLines(writeCursor.row + 1);
				if (printCharacter) {
					AssertValidWriteCursor(writeCursor, text);
					ConsoleTile consoleTile = currentDefaultTile.CloneWithLetter(c);
					PrintTile(consoleTile, ref writeCursor);
				}
				if (writeCursor.col >= size.col) { size.col = (short)(writeCursor.col + 1); }
			}
			size.row = (short)Math.Max(lines.Count, writeCursor.row + 1);
		}

		public void Write(string text, ref Coord writeCursor, ConsoleDiff diff, ref int inputIndex) {
			if (diff.Start == Coord.NegativeOne) {
				diff.Start = writeCursor;
				UnityEngine.Debug.LogWarning("...needed to initialize diff.Start...");
			}
			//UnityEngine.Debug.Log("writing \'" + text + "\' at " + inputIndex + " " + writeCursor + "   " + diff);
			for (int i = 0; i < text.Length; ++i) {
				char c = text[i];
				bool printCharacter = false;
				switch (c) {
					case Col.ColorSequenceDelim: ProcessColorSequenceDelimiter(text, i); continue;
					case '\b': diff.InsertBackspace(this, ref writeCursor, ref inputIndex); break;
					case '\n':
						// write the newline into the input field
						diff.Insert(ref inputIndex, currentDefaultTile.wLetter('\n'), this, ref writeCursor);
						// show it in the command line? nah.
						//printCharacter = true;
						break;
					default: printCharacter = true; break;
				}
				EnsureSufficientLines(writeCursor.row + 1);
				if (printCharacter) {
					AssertValidWriteCursor(writeCursor, text);
					ConsoleTile consoleTile = currentDefaultTile.wLetter(c);
					//UnityEngine.Debug.Log(c + " " + inputIndex);
					PrintTile(consoleTile, diff, ref writeCursor, ref inputIndex);
				}
				if (writeCursor.col >= size.col) { size.col = (short)(writeCursor.col + 1); }
			}
			size.row = (short)Math.Max(lines.Count, writeCursor.row + 1);
		}

		private void ProcessColorSequenceDelimiter(string text, int i) {
			ParseColorSequenceDelimeter(text, ref i, out byte f, out byte b);
			currentDefaultTile.fore = (f == Col.DefaultColorIndex) ? defaultColors.fore : f;
			currentDefaultTile.back = (b == Col.DefaultColorIndex) ? defaultColors.back : b;
		}

		private void PrintTile(ConsoleTile c, ConsoleDiff diff, ref Coord writeCursor, ref int inputIndex) {
			List<ConsoleTile> line = lines[writeCursor.row];
			// calculate how much space this character should take on the line
			ConsoleTile thisLetter = GetPrintable(c, writeCursor, out short letterWidth, out short cursorSkip);
			int endOfChange = writeCursor.col + letterWidth + cursorSkip;
			for (int s = writeCursor.col; s < endOfChange; ++s) {
				if (s >= line.Count) {
					line.Add(currentDefaultTile);
				}
			}
			EnsureSufficientColumns(line, writeCursor.col + letterWidth);//while (writeCursor.col + letterWidth > line.Count) { line.Add(currentDefaultTile); }
			diff.Insert(ref inputIndex, thisLetter, this, ref writeCursor);
		}

		public void PrintTile(ConsoleTile c, ref Coord writeCursor) {
			List<ConsoleTile> line = lines[writeCursor.row];
			// calculate how much space this character should take on the line
			ConsoleTile thisLetter = GetPrintable(c, writeCursor, out short letterWidth, out short cursorSkip);
			int endOfChange = writeCursor.col + letterWidth + cursorSkip;
			for (int s = writeCursor.col; s < endOfChange; ++s) {
				if (s < line.Count) {
					line[s] = currentDefaultTile;
				} else if (s >= line.Count) {
					line.Add(currentDefaultTile);
				}
			}
			EnsureSufficientColumns(line, writeCursor.col + letterWidth);//while (writeCursor.col + letterWidth > line.Count) { line.Add(currentDefaultTile); }
			writeCursor.col += cursorSkip;
			if (writeCursor.col < line.Count) {
				line[writeCursor.col] = thisLetter;
			} else if (writeCursor.col == line.Count) {
				line.Add(thisLetter);
			} else {
				throw new Exception("Unable to add letter beyond the length of the line!");
			}
			writeCursor.col += letterWidth;
		}

		public void EnsureSufficientLines(int lineCount) {
			while (lineCount > lines.Count) { lines.Add(new List<ConsoleTile>()); }
		}

		public void EnsureSufficientColumns(List<ConsoleTile> line, int columnCount) {
			while (columnCount > line.Count) { line.Add(currentDefaultTile); }
		}

		public int CalculateWidth() {
			int w = 0;
			lines.ForEach(line => w = Math.Max(w, line.Count));
			return w;
		}

		public bool MoveCursor(Coord direction, ref Coord Cursor) {
			Coord next = Cursor + direction;
			if (next.x < 0 || next.y < 0 || next.y >= size.y || next.x >= size.x) { return false; }
			Cursor = next;
			return true;
		}

		/// <param name="c">character to print</param>
		/// <param name="letterWidth">how far to move after printing the <see cref="ConsoleTile"/></param>
		/// <param name="moveBefore">how far to move before printing the <see cref="ConsoleTile"/></param>
		/// <param name="currentDefaultTile">used to determine color</param>
		/// <returns></returns>
		public ConsoleTile GetPrintable(ConsoleTile c, Coord cursor, out short letterWidth, out short moveBefore) {
			ConsoleTile thisLetter = c;
			moveBefore = 0;
			switch (c.Letter) {
			case '\b':
				thisLetter.Set(' ', c.Fore, c.Back);
				letterWidth = 0;
				return thisLetter;
			case '\t':
				thisLetter.Set('t', c.Back, c.Fore);
				letterWidth = (short)(spacesPerTab - (cursor.col % spacesPerTab));//1;//
				//moveBefore = (short)(spacesPerTab - (writeCursor.col % spacesPerTab) - 1);
				return thisLetter;
			default:
				letterWidth = 1;
				if (c >= 32 && c < 128) {
					thisLetter.Letter = c;
				} else {
					if (printableCharacters.TryGetValue(c, out ConsoleTile printableAs)) {
						thisLetter = printableAs;
					} else {
						int weirdColor = (c / 32) % unprintableColors.Length;
						thisLetter.Letter = CharExtension.ConvertToHexadecimalPattern(c);
						thisLetter.Fore = unprintableColors[weirdColor];
					}
				}
				return thisLetter;
			}
		}

		public ConsoleTile GetAt(Coord cursor) {
			if (cursor.row < 0 || cursor.row >= lines.Count || cursor.col < 0) {
				return currentDefaultTile.CloneWithLetter('\0');
			}
			List<ConsoleTile> line = lines[cursor.row];
			if (cursor.col >= line.Count) {
				return currentDefaultTile.CloneWithLetter('\0');
			}
			return line[cursor.col];
		}

		public void SetAt(Coord cursor, ConsoleTile tile) {
			EnsureSufficientLines(cursor.row + 1);
			//UnityEngine.Debug.Log("getting row " + cursor.row + " of " + lines.Count);
			List<ConsoleTile> line = lines[cursor.row];
			while (line.Count <= cursor.col) { line.Add(currentDefaultTile); }
			line[cursor.col] = tile;
		}

		public void InsertAt(Coord cursor, ConsoleTile tile) {
			EnsureSufficientLines(cursor.row + 1);
			List<ConsoleTile> line = lines[cursor.row];
			while (line.Count < cursor.col) { line.Add(currentDefaultTile); }
			line.Insert(cursor.col, tile);
		}

		public bool RemoveAt(Coord cursor) {
			if (lines.Count <= cursor.row) return false;
			List<ConsoleTile> line = lines[cursor.row];
			if (line.Count <= cursor.col) { return false; }
			line.RemoveAt(cursor.col);
			return true;
		}

		public void ConsoleDraw(CoordRect location, Coord offset) {
			location.ForEach(c => {
				ConsoleTile tile = GetAt(c - offset);
				c.SetConsoleCursorPosition();
				tile.ConsoleWrite();
			});
		}
	}
}
