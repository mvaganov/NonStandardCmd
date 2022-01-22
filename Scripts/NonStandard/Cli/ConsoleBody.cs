using NonStandard.Data;
using System.Collections.Generic;
using NonStandard.Extension;
using System;

namespace NonStandard.Cli {
	[Serializable] public class ConsoleBody {
		public int spacesPerTab = 4;
		public ConsoleTile defaultColors = new ConsoleTile(' ', ConsoleColor.Gray, ConsoleColor.Black);
		public ConsoleTile currentColors = new ConsoleTile(' ', ConsoleColor.Gray, ConsoleColor.Black);
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
		/// <summary>
		/// if not negative one, the cursor cannot move before this coordinate
		/// </summary>
		public Coord WriteCursorStartingPoint = Coord.NegativeOne;
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
			}
		}
		public bool IsAtOrBeforeStartingPoint() {
			return writeCursor.row < WriteCursorStartingPoint.row ||
				writeCursor.row == WriteCursorStartingPoint.row && writeCursor.col <= WriteCursorStartingPoint.col;
		}
		public void RestartWriteCursor() { WriteCursorStartingPoint = Cursor; }
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
		public void Write(string text) {
			//StringBuilder whatWasWritten = new StringBuilder();
			List<ConsoleTile> line;
			for (int i = 0; i < text.Length; ++i) {
				char c = text[i];
				bool printC = true;
				switch (c) {
				case Col.ColorSequenceDelim:
					ParseColorSequenceDelimeter(text, ref i, out byte f, out byte b);
					currentColors.fore = (f == Col.DefaultColorIndex) ? defaultColors.fore : f;
					currentColors.back = (b == Col.DefaultColorIndex) ? defaultColors.back : b;
					continue;
				case '\b':
					//if(lines.Count == 0) { printC = false; break; }
					line = lines[writeCursor.row];
					// TODO check if there is a bunch of empty space and a tab. if so, delete all the way to the tab, including the tab
					bool needToRediscover = false;
					//Show.Log(writeCursor.col+" "+ line.Count + "         max:"+size.col);
					if (writeCursor.col == line.Count) {
						needToRediscover = writeCursor.col + 1 >= size.col;
						printC = false; // don't print, that will add a character, we're removing a character
						if (!IsAtOrBeforeStartingPoint()) {
							if (line.Count == 0) {
								if (writeCursor.row == lines.Count - 1) {
									lines.RemoveAt(lines.Count - 1);
									size.row = (short)lines.Count;
								}
							} else {
								line.RemoveAt(line.Count - 1);
							}
						}
					}
					--writeCursor.col;
					if (needToRediscover) {
						size.x = CalculateWidth();
					}
					while (writeCursor.col < 0) {
						if (writeCursor.row <= 0) { writeCursor.col = writeCursor.row = 0; break; }
						--writeCursor.row;
						line = lines[writeCursor.row];
						writeCursor.col += (short)(line.Count + 1);
						printC = false; // don't print, that will add a character to the end of the previous line
					}
					if (writeCursor.row < 0) { writeCursor.row = 0; }
					if (IsAtOrBeforeStartingPoint()) {
						printC = false;
						//Show.Log("there's probably a better algorithm for this. "+ writeCursor+" should be at "+ WriteCursorStartingPoint);
						writeCursor = WriteCursorStartingPoint;
					}
					break;
				case '\n':
					++writeCursor.row;
					writeCursor.col = 0;
					printC = false; // don't print, that will add a character to the end of the line
					break;
				}
				while (writeCursor.row >= lines.Count) { lines.Add(new List<ConsoleTile>()); }
				if (printC) {
					ConsoleTile thisLetter = GetPrintable(c, out short letterWidth, out short cursorSkip);
					if(writeCursor.row >= lines.Count || writeCursor.row < 0) {
						throw new Exception("bad write cursor state "+writeCursor+" with "+lines.Count+" rows");
					}
					line = lines[writeCursor.row];
					int endOfChange = writeCursor.col + letterWidth + cursorSkip;
					for (int s = writeCursor.col; s < endOfChange; ++s) {
						if (s < line.Count) { line[s] = currentColors; }
						if (s >= line.Count) { line.Add(currentColors); }
					}
					while (writeCursor.col + letterWidth > line.Count) { line.Add(currentColors); }
					writeCursor.col += cursorSkip;
					line[writeCursor.col] = thisLetter;
					writeCursor.col += letterWidth;
				}
				if (writeCursor.col >= size.col) { size.col = (short)(writeCursor.col + 1); }
			}
			size.row = (short)Math.Max(lines.Count, Cursor.row + 1);
		}
		public int CalculateWidth() {
			int w = 0;
			for (int r = 0; r < lines.Count; ++r) {
				if (lines[r].Count >= w) {
					w = lines[r].Count;
				}
			}
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
			ConsoleTile thisLetter = currentColors;
			cursorSkip = 0;
			switch (c) {
			case '\b':
				thisLetter.Set(' ', currentColors.Fore, currentColors.Back);
				letterWidth = 0;
				return thisLetter;
			case '\t':
				thisLetter.Set('t', currentColors.Back, currentColors.Fore);
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
				return currentColors.CloneWithLetter('\0');
			}
			List<ConsoleTile> line = lines[cursor.row];
			if (cursor.col >= line.Count) {
				return currentColors.CloneWithLetter('\0');
			}
			return line[cursor.col];
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