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
	private Dictionary<string, Color> _colorDictionary = new Dictionary<string, Color>();

	private static Color _unsetColor = new Color(1f, 0f, 1f, 0f);
	public Color _defaultColor = _unsetColor;
	public TMP_Text textComponent;
	[System.Serializable] public class SyntaxColor {
		public string syntax;
		public Color color;
	}

	void FindDefaultTextColor() {
		if (_defaultColor != _unsetColor) return;
		TMPro.TMP_Text txt = GetComponentInChildren<TMPro.TMP_Text>();
		_defaultColor = txt.color;
	}

	private void OnValidate() {
		if (_colorizeInEditor) {
		}
	}

	void Start() {
		ColorizeText();
	}

	void ColorizeText() {
		_colorList.ForEach(sc => _colorDictionary[sc.syntax] = sc.color);
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
			return t.MetaType + " " + t.ToString();
		}));
		//Debug.Log(tok.DebugPrint());
		// generate dictionary of syntax tree color list
		// calculate syntax tree
		// go through text and apply color based on color dictionary
		//SetCharColor(textComponent, 1, new Color[] { Color.green, Color.red, Color.cyan, Color.white, Color.blue, Color.yellow });
		Color[] color = new Color[1];
		for (int i = 0; i < tokens.Count; i++) {
			if (!_colorDictionary.TryGetValue(tokens[i].MetaType, out Color syntaxColor)) {
				continue;
			}
			string t = tokens[i].ToString();
			color[0] = syntaxColor;
			SetCharColor(textComponent, tokens[i].index, color, t.Length);
		}
		//SetCharColor(textComponent, 1, new Color[] { Color.red, Color.red, Color.red, Color.red });
		textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		//textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
	}

	private const char TMProTextOutputTerminator = '\u200B';
	public static void SetCharColor(TMP_Text textOutput, int index, Color[] colors, int count = -1) {
		TMP_CharacterInfo[] chars = textOutput.textInfo.characterInfo;
		if (count < 0) { count = colors.Length; }
		for (int i = 0; i < count; ++i) {
			TMP_CharacterInfo cinfo = chars[index++];
			if (cinfo.character == TMProTextOutputTerminator) break;
			if (!cinfo.isVisible) continue;
			int vertexIndex = cinfo.vertexIndex;
			Color color = colors[i % colors.Length];
			for (int m = 0; m < 1; ++m) {
				TMP_MeshInfo meshInfo = textOutput.textInfo.meshInfo[m];
				if (vertexIndex < 0 || vertexIndex >= meshInfo.colors32.Length) { continue; }
				SetTmpTextQuadColor(vertexIndex, meshInfo.colors32, color);
			}
		}
	}
	private static bool SetTmpTextQuadColor(int vertexIndex, Color32[] vertColors, Color color) {
		if (vertexIndex >= vertColors.Length || vertColors[vertexIndex].EqualRgba(color)) { return false; }
		vertColors[vertexIndex + 0] = color;
		vertColors[vertexIndex + 1] = color;
		vertColors[vertexIndex + 2] = color;
		vertColors[vertexIndex + 3] = color;
		return true;
	}

	void Update() {

	}
}
