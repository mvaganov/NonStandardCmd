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

		public ConsoleTile(char letter, ConsoleColor foreColor) {
			this.Letter = letter;
			fore = (byte)foreColor;
			back = DefaultTile.back;
		}

		static ConsoleTile() {
			DefaultTile = new ConsoleTile('?', Console.ForegroundColor, Console.BackgroundColor);
		}

		public ConsoleColor Fore { get => (ConsoleColor)fore; set => fore = (byte)value; }
		public ConsoleColor Back { get => (ConsoleColor)back; set => back = (byte)value; }

		public readonly static ConsoleTile DefaultTile;

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

		public ConsoleTile CloneWithLetter(char letter) => new ConsoleTile(letter, Fore, Back);
	}

}