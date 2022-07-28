using NonStandard.Cli;
using NonStandard.Data.Parse;
using NonStandard.Extension;
using NonStandard.Ui;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ColorizeTMProText : MonoBehaviour {
#if UNITY_EDITOR
	[ContextMenuItem("Colorize text",nameof(ColorizeText))]
	[SerializeField] private bool _colorizeInEditor;
#endif
	public UnityConsoleOutput consoleOutput;
	public List<SyntaxColor> _colorList;
	private Dictionary<string, SyntaxColor> _colorDictionary = new Dictionary<string, SyntaxColor>();

	private static Color _unsetColor = new Color(1f, 0f, 1f, 0f);
	public Color _defaultColor = _unsetColor;
	public TMP_Text textComponent;
	private string _calculatedText;
	[System.Serializable] public class SyntaxColor {
		public string syntax;
		public Color color;
		public bool colorizeEndsByDepth;
	}
	public List<Color> nestedDepth = new List<Color>() {
		Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan
	};

	void FindDefaultTextColor() {
		if (_defaultColor != _unsetColor) return;
		TMPro.TMP_Text txt = GetComponentInChildren<TMPro.TMP_Text>();
		_defaultColor = txt.color;
	}

	private void OnValidate() {
		if (_colorizeInEditor) {
			if (textComponent.text != _calculatedText) { ColorizeText(); }
		}
	}

	void Start() {
		ColorizeText();
	}

	void ColorizeText() {
		_calculatedText = textComponent.text;
		_colorList.ForEach(sc => _colorDictionary[sc.syntax] = sc);
		Tokenizer tok = new Tokenizer();
		string text = UiText.GetText(gameObject);
		tok.Tokenize(text);
		//for (int i = 0; i < tok.Tokens.Count; i++) {
		//	Debug.
		//}
		List<Token> tokens = new List<Token>();
		tok.GetStandardTokens(tokens);
		//Debug.Log(tokens.Count);
		Debug.Log(tokens.JoinToString("\n", t => {
			return t.MetaType + " " + t.index;
		}));
		//Debug.Log(tok.DebugPrint());
		Color[] color = new Color[1];
		for (int i = 0; i < tokens.Count; i++) {
			if (!_colorDictionary.TryGetValue(tokens[i].MetaType, out SyntaxColor syntaxColor)) {
				continue;
			}
			if (syntaxColor.colorizeEndsByDepth) {
				SyntaxTree st = tokens[i].GetAsSyntaxNode();
				Token begin = st.GetBeginToken();
				Token end = st.GetEndToken();
				Delim beginDelim = st.beginDelim;
				Delim endDelim = st.endDelim;
				string beginStr = begin.ToString();
				string endStr = end.ToString();
				color[0] = nestedDepth[st.Depth % nestedDepth.Count];
				Debug.Log(beginDelim.text + "@"+begin.index+"   "+ endDelim.text + "@"+end.index);
				BADCODE = true;
				SetCharColor(textComponent, begin.index, color, beginDelim.text.Length);
				SetCharColor(textComponent, end.index, color, endDelim.text.Length);
				BADCODE = false;
			} else {
				string t = tokens[i].ToString();
				color[0] = syntaxColor.color;
				SetCharColor(textComponent, tokens[i].index, color, t.Length);
			}
		}
		//SetCharColor(textComponent, 1, new Color[] { Color.red, Color.red, Color.red, Color.red });
		textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		//textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
	}
	private static bool BADCODE = false;

	private const char TMProTextOutputTerminator = '\u200B';
	public static void SetCharColor(TMP_Text textOutput, int index, Color[] colors, int count = -1) {
		TMP_CharacterInfo[] chars = textOutput.textInfo.characterInfo;
		bool SHOWME = BADCODE && index == 0;
		if (SHOWME) { Debug.LogWarning("count "+count); }
		if (count < 0) { count = colors.Length; }
		for (int i = 0; i < count; ++i) {
			if (SHOWME) { Debug.LogWarning("index " + index); }
			TMP_CharacterInfo cinfo = chars[index++];
			if (cinfo.character == TMProTextOutputTerminator) {
				if (SHOWME) { Debug.LogWarning("breaking! " + (int)TMProTextOutputTerminator); }
				break;
			}
			if (!cinfo.isVisible) {
				if (SHOWME) { Debug.LogWarning("invisible! " + (int)cinfo.character); }
				continue;
			}
			int vertexIndex = cinfo.vertexIndex;
			Color color = colors[i % colors.Length];
			for (int m = 0; m < textOutput.textInfo.meshInfo.Length; ++m) {
				TMP_MeshInfo meshInfo = textOutput.textInfo.meshInfo[m];
				if (vertexIndex < 0 || vertexIndex >= meshInfo.colors32.Length) {
					if (SHOWME) { Debug.LogWarning("vertindex " + vertexIndex); }
					continue;
				}
				if (SHOWME) { Debug.LogWarning("colorchanging at " + vertexIndex); }
				SetTmpTextQuadColor(vertexIndex, meshInfo.colors32, color);
			}
		}
	}
	private static bool SetTmpTextQuadColor(int vertexIndex, Color32[] vertColors, Color color) {
		if (vertexIndex >= vertColors.Length || vertColors[vertexIndex].EqualRgba(color)) {
			if (BADCODE && vertexIndex == 0) { Debug.LogWarning("jumping out. "+ vertColors[vertexIndex]+" vs "+color); }
			return false;
		}
		vertColors[vertexIndex + 0] = color;
		vertColors[vertexIndex + 1] = color;
		vertColors[vertexIndex + 2] = color;
		vertColors[vertexIndex + 3] = color;
		return true;
	}

	void Update() {

	}
}
