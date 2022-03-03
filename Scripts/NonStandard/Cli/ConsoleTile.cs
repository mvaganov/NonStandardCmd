using NonStandard.Data;
using System;

namespace NonStandard.Cli {
	public interface IDrawable {
		void Draw(ConsoleTile[,] screen, Coord offset);
	}

	public struct ConsoleTile : IDrawable {
		public char Letter;
		public byte fore, back;

		public static implicit operator ConsoleTile(char letter) {
			return new ConsoleTile { Letter = letter, fore = DefaultTile.fore, back = DefaultTile.back };
		}

		public static implicit operator char(ConsoleTile tile) => tile.Letter;

		public ConsoleTile(char letter, ConsoleColor foreColor, ConsoleColor backColor) {
			this.Letter = letter; fore = (byte)foreColor; back = (byte)backColor;
		}

		public ConsoleTile(char letter, byte foreColor, byte  backColor) {
			this.Letter = letter; fore = foreColor; back = backColor;
		}

		public ConsoleTile(char letter, ConsoleColor foreColor) {
			this.Letter = letter;
			fore = (byte)foreColor;
			back = DefaultTile.back;
		}

		static ConsoleTile() {
			DefaultTile = new ConsoleTile('?', Console.ForegroundColor, Console.BackgroundColor);
			DeletedTile = new ConsoleTile('\b', (byte)0, 0);
			EmptyTile = new ConsoleTile('\0', (byte)0, 0);
		}

		public ConsoleColor Fore { get => (ConsoleColor)fore; set => fore = (byte)value; }
		public ConsoleColor Back { get => (ConsoleColor)back; set => back = (byte)value; }

		public readonly static ConsoleTile DefaultTile;
		public readonly static ConsoleTile DeletedTile;
		public readonly static ConsoleTile EmptyTile;

		public void Set(char letter, ConsoleColor foreColor, ConsoleColor backColor) {
			this.Letter = letter; fore = (byte)foreColor; back = (byte)backColor;
		}

		public bool IsColorCurrent() {
			return Console.ForegroundColor == (ConsoleColor)fore && Console.BackgroundColor == (ConsoleColor)back;
		}

		public void SetColors(ConsoleColor fore, ConsoleColor back) { Fore = fore; Back = back; }

		public void ApplyColor() { Console.ForegroundColor = Fore; Console.BackgroundColor = Back; }

		public override string ToString() => $"[{Letter}]";
		public override int GetHashCode() => fore * 0x00010000 + back * 0x01000000 + (int)Letter;
		public override bool Equals(object o) {
			return (o == null || o.GetType() != typeof(ConsoleTile)) ? false : Equals((ConsoleTile)o);
		}
		public bool Equals(ConsoleTile ct) => fore == ct.fore && back == ct.back && Letter == ct.Letter;

		public static bool operator ==(ConsoleTile a, ConsoleTile b) { return a.Equals(b); }
		public static bool operator !=(ConsoleTile a, ConsoleTile b) { return !a.Equals(b); }

		public void Write() { ApplyColor(); Console.Write(Letter); }

		public void Draw(ConsoleTile[,] screen, Coord offset) { screen.SetAt(offset, this); }
		public ConsoleTile CloneWithLetter(char letter) => wLetter(letter);
		public ConsoleTile CloneWithForeColor(byte fore) => wForeColor(fore);
		public ConsoleTile wLetter(char letter) => new ConsoleTile(letter, Fore, Back);
		public ConsoleTile wForeColor(byte fore) => new ConsoleTile(Letter, fore, back);
	}

	/// <summary>
	/// used to make reversable changes to a console body. the input system uses this.
	/// </summary>
	public struct ConsoleDiffUnit : IComparable<ConsoleDiffUnit> {
		public Coord coord; // where "here" is
		public ConsoleTile next; // the next value being placed here
		public ConsoleTile prev; // what used to be here
		public ConsoleDiffUnit(Coord coord, ConsoleTile tile) : this(coord, tile, ConsoleTile.EmptyTile) { }
		public ConsoleDiffUnit(Coord coord, ConsoleTile tile, ConsoleTile prev) {
			this.coord = coord; this.next = tile; this.prev = prev;
			Validate();
		}
		public int CompareTo(ConsoleDiffUnit other) => coord.CompareTo(other.coord);
		public override int GetHashCode() => coord.GetHashCode();
		public ConsoleDiffUnit WithDifferentTile(ConsoleTile a_tile) => new ConsoleDiffUnit(coord, a_tile, prev);
		public ConsoleDiffUnit WithDifferentColor(byte color) => new ConsoleDiffUnit(coord, next.CloneWithForeColor(color), prev);
		public ConsoleDiffUnit WithDifferentCoord(Coord coord) => new ConsoleDiffUnit(coord, next, prev);
		public ConsoleDiffUnit WithOffsetCoord(Coord offset) => new ConsoleDiffUnit(coord+offset, next, prev);
		public void OffsetCoord(Coord coord) { this.coord += coord; Validate(); }

		private void Validate() {
			if (coord.row < 0 || coord.col < 0) {
				throw new Exception("something created an OOB diff unit...");
			}
		}
	}
}