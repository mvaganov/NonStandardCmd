using NonStandard.Data;
using System.Collections.Generic;
using NonStandard.Extension;
using System;

namespace NonStandard.Cli {
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
		public Dictionary<char, char> printableCharacters = new Dictionary<char, char>();
		public List<List<ConsoleTile>> lines = new List<List<ConsoleTile>>();
		public Coord writeCursor;
		public Coord size;
		public bool CursorAllowedInEmptyAreaInRow = false;

//		public bool isValidInputIndex;
//		private int inputIndex;

//		public ConsoleDiff input = new ConsoleDiff();

		public Coord Cursor {
			get => writeCursor;
			set {
				writeCursor = value;
				Coord limit = size;
				if(limit.row < 1) { limit.row = 1; }
				writeCursor.row = writeCursor.row.Clamp((short)0, (short)(limit.row - 1));
				if (!CursorAllowedInEmptyAreaInRow) {
					limit.col = (short)(writeCursor.row < lines.Count && writeCursor.row >= 0 ? lines[writeCursor.row].Count : 0);
				}
				writeCursor.col = writeCursor.col.Clamp((short)0, limit.col);
//				isValidInputIndex = input.TryGetIndexOf(writeCursor, out inputIndex);
			}
		}
//		public void RestartWriteCursor() { input.Start = Cursor; }
		public int CursorLeft { get => writeCursor.X; set => writeCursor.X = value; }
		public int CursorTop { get => writeCursor.Y; set => writeCursor.Y = value; }
		public Coord Size {
			get => size;
		}
		public void Clear() {
			Cursor = Coord.Zero;
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

		private void ConsoleBackspace() {
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

		public void Write(string text, ConsoleDiff diff, ref int inputIndex) {
			List<ConsoleTile> line;
			if (diff != null && diff.Start == Coord.NegativeOne) {
				diff.Start = writeCursor;
				UnityEngine.Debug.LogWarning("...needed to initialize diff.Start...");
			}
			UnityEngine.Debug.Log("writing \'" + text + "\' at " + inputIndex + " " + writeCursor + "   " + diff);
			for (int i = 0; i < text.Length; ++i) {
				char c = text[i];
				bool printCharacter = true;
				switch (c) {
					case Col.ColorSequenceDelim:
						ParseColorSequenceDelimeter(text, ref i, out byte f, out byte b);
						currentDefaultTile.fore = (f == Col.DefaultColorIndex) ? defaultColors.fore : f;
						currentDefaultTile.back = (b == Col.DefaultColorIndex) ? defaultColors.back : b;
						continue;
					case '\b':
						if (diff != null) {
							if (inputIndex > 0) {
								--inputIndex;
								diff.RemoveAt(inputIndex, this);
								writeCursor = diff.GetCoord(inputIndex);
								UnityEngine.Debug.Log(writeCursor + " <- new writecursor, index " + inputIndex + " after backspace");
							}
						} else {
							ConsoleBackspace();
						}
						printCharacter = false;
						break;
					case '\n':
						++writeCursor.row;
						writeCursor.col = 0;
						printCharacter = false; // don't print, that will add a character to the end of the line
						if (diff != null) {
							diff.Insert(inputIndex, currentDefaultTile.CloneWithLetter('\n'), this);
							++inputIndex;
							writeCursor = diff.GetCoord(inputIndex);
							UnityEngine.Debug.Log(writeCursor + " <- new writecursor, index " + inputIndex + " after newline");
						}
						break;
				}
				EnsureSufficientLines(writeCursor.row + 1);
				if (printCharacter) {
					ConsoleTile thisLetter = GetPrintable(c, out short letterWidth, out short cursorSkip);
					if(writeCursor.row >= lines.Count || writeCursor.row < 0) {
						throw new Exception("bad write cursor state "+writeCursor+" with "+lines.Count+" rows");
					}
					line = lines[writeCursor.row];
					int endOfChange = writeCursor.col + letterWidth + cursorSkip;
					if (writeCursor.col < 0) {
						throw new Exception("negative column? "+ writeCursor+" trying to write "+text.Length+" chars: "+text);
					}
					for (int s = writeCursor.col; s < endOfChange; ++s) {
						if (s < line.Count) {
							line[s] = currentDefaultTile;
						} else if (s >= line.Count) {
							line.Add(currentDefaultTile);
						}
						//if (diff != null) {
						//	diff.Insert(inputIndex, currentDefaultTile, this);
						//	//ioDelta.Add(new ConsoleArtifact(new Coord(s, writeCursor.row), currentDefaultTile));
						//}
					}
					while (writeCursor.col + letterWidth > line.Count) { line.Add(currentDefaultTile); }
					if (diff != null) {
						diff.Insert(inputIndex, thisLetter, this);
						++inputIndex;
						//writeCursor = diff.GetCoord(inputIndex);
						//UnityEngine.Debug.Log(writeCursor + " '" + thisLetter + "' <- new writecursor, index "+ inputIndex+ " ");
					}
					else {
						writeCursor.col += cursorSkip;
						line[writeCursor.col] = thisLetter;
						writeCursor.col += letterWidth;
					}
				}
				if (writeCursor.col >= size.col) { size.col = (short)(writeCursor.col + 1); }
			}
			size.row = (short)Math.Max(lines.Count, Cursor.row + 1);
		}

		public void EnsureSufficientLines(int lineCount) {
			while (lineCount > lines.Count) { lines.Add(new List<ConsoleTile>()); }
		}

		public int CalculateWidth() {
			int w = 0;
			lines.ForEach(line => w = Math.Max(w, line.Count));
			return w;
		}

		public bool MoveCursor(Coord direction) {
			Coord next = Cursor + direction;
			if (next.x < 0 || next.y < 0 || next.y >= size.y || next.x >= size.x) { return false; }
			Cursor = next;
			return true;
		}

		/// <param name="c">character to print</param>
		/// <param name="letterWidth">how far to move after printing the <see cref="ConsoleTile"/></param>
		/// <param name="cursorSkip">how far to move before printing the <see cref="ConsoleTile"/></param>
		/// <returns></returns>
		public ConsoleTile GetPrintable(char c, out short letterWidth, out short cursorSkip) {
			ConsoleTile thisLetter = currentDefaultTile;
			cursorSkip = 0;
			switch (c) {
			case '\b':
				thisLetter.Set(' ', currentDefaultTile.Fore, currentDefaultTile.Back);
				letterWidth = 0;
				return thisLetter;
			case '\t':
				thisLetter.Set('t', currentDefaultTile.Back, currentDefaultTile.Fore);
				letterWidth = (short)(spacesPerTab - (writeCursor.col % spacesPerTab));//1;//
				//cursorSkip = (short)(spacesPerTab - (writeCursor.col % spacesPerTab) - 1);
				return thisLetter;
			default:
				letterWidth = 1;
				if (c >= 32 && c < 128) {
					thisLetter.Letter = c;
				} else {
					if (printableCharacters.TryGetValue(c, out char printableAs)) {
						thisLetter.Letter = printableAs;
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

		public void Draw(CoordRect location, Coord offset) {
			location.ForEach(c => {
				ConsoleTile tile = GetAt(c - offset);
				c.SetConsoleCursorPosition();
				tile.Write();
			});
		}
	}
}