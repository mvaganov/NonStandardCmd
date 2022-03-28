// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using NonStandard.Data;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NonStandard.Extension;

namespace NonStandard.Cli {
	public partial class UnityConsoleOutput : MonoBehaviour {
		public UnityConsole unityConsole;
		public ConsoleState console => unityConsole.State;

		#region Unity Lifecycle
		private void Awake() {
			unityConsole = GetComponent<UnityConsole>();
			unityConsole.cursorUI.state = console.Cursor;
			console.Init();
		}

		void Start() {
			FindUiTransform();
			CreateBackgroundTextArea();
		}

		public void Update() {
			if (console.textNeedsRefresh) {
				unityConsole.RefreshCursorPosition();
				RefreshText();
			}
		}
		#endregion Unity Lifecycle

		#region API
		public void Write(char c) => console.Write(c);
		public void Write(object o) => console.Write(o);
		public void Write(string text) => console.Write(text);
		public void Write(string text, byte fcolor) => console.Write(text, fcolor);
		public void WriteLine(string text) => console.Write(text + "\n");
		public void RestartInput() {
			console.RestartInput();
			if (inputField != null) {
				int inputIndex = console.Cursor.indexInInput;
				inputField.selectionFocusPosition = inputIndex;
				inputField.selectionAnchorPosition = inputIndex;
			}
		}
		public Vector2 TextAreaSize() {
			return (inputField != null ? inputField.textViewport : _foreText.GetComponent<RectTransform>()).rect.size;
		}
		public float FontSize {
			get => inputField != null ? inputField.pointSize : Text.fontSize;
			set {
				if (inputField != null) {
					inputField.pointSize = _backText.fontSize = value;
				} else {
					Text.fontSize = _backText.fontSize = value;
				}
			}
		}
		#endregion API

		#region User Interface
		public TMP_InputField inputField;

		public TMP_Text Text => inputField != null ? inputField.textComponent : _foreText;
		public RectTransform TextRect => inputField != null ? inputField.textViewport : _foreText.GetComponent<RectTransform>();

		private void CreateBackgroundTextArea() {
			TMP_Text pTmp = Text;
			GameObject backgroundObject = Instantiate(Text.gameObject);
			UnityConsoleOutput extra = backgroundObject.GetComponent<UnityConsoleOutput>();
			if (extra != null) { DestroyImmediate(extra); }
			RectTransform brt = backgroundObject.GetComponent<RectTransform>();
			if (brt == null) { brt = backgroundObject.AddComponent<RectTransform>(); }
			backgroundObject.transform.SetParent(pTmp.transform.parent);
			if (pTmp.transform.parent != null) {
				backgroundObject.transform.SetSiblingIndex(0); // put the background in the background
			}
			backgroundObject.transform.localPosition = Vector3.zero;
			backgroundObject.transform.localScale = Vector3.one;
			_backText = backgroundObject.GetComponent<TMP_Text>();
			_backText.fontMaterial.renderQueue -= 1;
			if (inputField) {
				inputField.targetGraphic.material.renderQueue -= 2;
			}
			RectTransform rt = pTmp.GetComponent<RectTransform>();
			brt.anchorMin = rt.anchorMin;
			brt.anchorMax = rt.anchorMax;
			brt.offsetMin = rt.offsetMin;
			brt.offsetMax = rt.offsetMax;
		}

		public RectTransform FindUiTransform() {
			if (inputField == null) { inputField = GetComponentInChildren<TMP_InputField>(); }
			if (!inputField) {
				_foreText = GetComponentInChildren<TMP_Text>();
			} else {
				_foreText = inputField.textComponent;
				inputField.readOnly = true;
				inputField.richText = false;
			}
			if (inputField != null) { return inputField.GetComponent<RectTransform>(); }
			return _foreText.GetComponent<RectTransform>();
		}
		#endregion User Interface

		#region calculations
		public void AddToFontSize(float value) {
			FontSize += value;
			if (FontSize < 1) { FontSize = 1; }
			//if (unityConsole.cursorUi.cursorObject != null) {
			//	unityConsole.cursorUi.cursorObject.GetComponent<Cli.UnityConsoleCursor>().ScaleToFontSize(FontSize);
			//}
			if (unityConsole.cursorUI != null) {
				unityConsole.cursorUI.GetComponent<UnityConsoleCursor>().ScaleToFontSize(FontSize);
			}
			RefreshText();
		}
		#endregion calculations

		#region Settings
		public CharSettings defaultEmptyCharacter = new CharSettings();

		[System.Serializable] public class CharSettings {
			public const char DefaultBackground = '\u2588';
			public char Foreground = ' '; // normal space
			public char Background = DefaultBackground; // █
		}
		#endregion Settings

		#region Tile
		private TMP_Text _foreText;
		private TMP_Text _backText;
		private List<Tile> _foreTile = new List<Tile>();
		private List<Tile> _backTile = new List<Tile>();

		public struct Tile {
			public ColorRGBA color;
			public float height;
			public char letter;
			public short tag;
			public Tile(char letter, ColorRGBA color, float height) {
				this.letter = letter; this.height = height; this.color = color; tag = 0;
			}
			public Tile(char letter, ColorRGBA color, float height, short tag) {
				this.letter = letter; this.height = height; this.color = color; this.tag = tag;
			}
		}
		#endregion Tile

		#region Tags
		public Dictionary<short, TextSpan> tags = new Dictionary<short, TextSpan>();
		private short nextFreeTag = 0;
		public List<TextSpan> _textSpans = new List<TextSpan>();

		public short AddTagToTextSpan(Coord start, Coord end, object tag) {
			// create a text span with the stack trace as metadata to that span
			TextSpan ts = new TextSpan(start, end, tag);
			// add the span to a sorted data structure
			_textSpans.BinarySearchInsert(ts);
			short tagId = nextFreeTag++;
			tags[tagId] = ts;
			return tagId;
		}

		public List<TextSpan> GetTag(Coord coord) {
			List<TextSpan> found = TextSpan.GetSpans(_textSpans, coord);
			return found;
		}

		#endregion Tags
	}
}
