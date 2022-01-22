using System;
using NonStandard.Extension;

namespace NonStandard.Cli {
	public static class Col {
		public const char ColorSequenceDelim = (char)26;
		public const byte DefaultColorIndex = 17;
		public static string r() { return r(DefaultColorIndex, DefaultColorIndex); }
		public static string r(ConsoleColor foreColor) { return r((byte)foreColor, DefaultColorIndex); }
		public static string r(byte foreColor) { return r(foreColor, DefaultColorIndex); }
		public static string r(ConsoleColor foreColor, ConsoleColor backColor) { return r((byte)foreColor, (byte)backColor); }
		public static string r(byte foreColor, byte backColor) {
			return ColorSequenceDelim.ToString() + 
				CharExtension.ConvertToHexadecimalPattern(foreColor) + 
				CharExtension.ConvertToHexadecimalPattern(backColor);
		}
	}
}