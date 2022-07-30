using NonStandard.Cli;
using NonStandard.Data.Parse;
using NonStandard.Extension;
using NonStandard.Ui;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMPro.TMP_InputField))]
public class ColorizeTMProText : MonoBehaviour {
#if UNITY_EDITOR
	[ContextMenuItem("Colorize text",nameof(ColorizeText))]
	[SerializeField] private bool _colorizeInEditor;
#endif
	public List<SyntaxColor> _colorList;
	private Dictionary<string, SyntaxColor> _colorDictionary = new Dictionary<string, SyntaxColor>();

	private static Color _unsetColor = new Color(1f, 0f, 1f, 0f);
	private Color _defaultColor = _unsetColor;
	private TMP_Text textComponent;
	private string _calculatedText;
	public TMP_Text TextComponent => textComponent != null ? textComponent : textComponent = GetComponent<TMP_InputField>().textComponent;
	[System.Serializable] public class SyntaxColor {
		public string syntax;
		public Color color;
		public bool colorizeEndsByDepth;
	}

	[System.Serializable] public class ColorSegment {
		public Color color;
		public int index;
		public int count;
		public ColorSegment(int index, int count, Color color)
			{ this.index = index; this.count = count; this.color = color; }
	}

	private List<Color> nestedDepth = new List<Color>() {
		Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan
	};

	void FindDefaultTextColor() {
		if (_defaultColor != _unsetColor) return;
		TMPro.TMP_Text txt = GetComponentInChildren<TMPro.TMP_Text>();
		_defaultColor = txt.color;
	}

	private void OnValidate() {
		if (_colorizeInEditor) {
			if (TextComponent.text != _calculatedText) { ColorizeText(); }
		}
	}

	void Start() {
		ColorizeText();
	}

	void ColorizeText() {
		TMP_Text txt = TextComponent;
		if (txt == null) {
			throw new System.Exception("no TMP text in "+name+"??");
		}
		_calculatedText = txt.text;
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
		List<ColorSegment> colorSegs = new List<ColorSegment>();
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
				Color thisColor = nestedDepth[st.Depth % nestedDepth.Count];
				color[0] = thisColor;
				//Debug.Log(beginDelim.text + "@"+begin.index+"   "+ endDelim.text + "@"+end.index);
				colorSegs.Add(new ColorSegment(begin.index, beginDelim.text.Length, thisColor));
				colorSegs.Add(new ColorSegment(end.index, endDelim.text.Length, thisColor));
				//BADCODE = true;
				//SetCharColor(textComponent, begin.index, thisColor, beginDelim.text.Length);
				//SetCharColor(textComponent, end.index, thisColor, endDelim.text.Length);
				//BADCODE = false;
			} else {
				Token token = tokens[i];
				string t = token.ToString();
				color[0] = syntaxColor.color;
				colorSegs.Add(new ColorSegment(token.index, t.Length, syntaxColor.color));
				//SetCharColor(textComponent, tokens[i].index, syntaxColor.color, t.Length);
			}
		}
		colorSegs.Sort((a,b)=>a.index.CompareTo(b.index));
		foreach (ColorSegment colorSeg in colorSegs) {
			SetCharColor(textComponent, colorSeg.index, colorSeg.color, colorSeg.count);
		}
		//SetCharColor(textComponent, 1, new Color[] { Color.red, Color.red, Color.red, Color.red });
		textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		//textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
	}
	//private static bool BADCODE = false;

	private const char TMProTextOutputTerminator = '\u200B';
	public static void SetCharColor(TMP_Text textOutput, int index, Color color, int count) {
		TMP_CharacterInfo[] chars = textOutput.textInfo.characterInfo;
		//bool SHOWME = BADCODE && index == 0;
		for (int i = 0; i < count ; ++i) {
			//if (SHOWME) { Debug.LogWarning("index " + index); }
			TMP_CharacterInfo cinfo = chars[index++];
			if (cinfo.character == TMProTextOutputTerminator) {
				//if (SHOWME) { Debug.LogWarning("breaking! " + (int)TMProTextOutputTerminator); }
				break;
			}
			if (!cinfo.isVisible) {
				//if (SHOWME) { Debug.LogWarning("invisible! " + (int)cinfo.character); }
				continue;
			}
			int vertexIndex = cinfo.vertexIndex;
			for (int m = 0; m < textOutput.textInfo.meshInfo.Length; ++m) {
				TMP_MeshInfo meshInfo = textOutput.textInfo.meshInfo[m];
				if (vertexIndex < 0 || vertexIndex >= meshInfo.colors32.Length) {
					//if (SHOWME) { Debug.LogWarning("vertindex " + vertexIndex); }
					continue;
				}
				//if (SHOWME) { Debug.LogWarning("colorchanging at " + vertexIndex); }
				SetTmpTextQuadColor(vertexIndex, meshInfo.colors32, color);
			}
		}
	}
	private static bool SetTmpTextQuadColor(int vertexIndex, Color32[] vertColors, Color color) {
		if (vertexIndex >= vertColors.Length || vertColors[vertexIndex].EqualRgba(color)) {
			//if (BADCODE && vertexIndex == 0) { Debug.LogWarning("jumping out. "+ vertColors[vertexIndex]+" vs "+color); }
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
