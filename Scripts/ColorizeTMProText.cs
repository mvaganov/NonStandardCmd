using NonStandard.Cli;
using NonStandard.Data.Parse;
using NonStandard.Extension;
using NonStandard.Ui;
using System.Collections.Generic;
using UnityEngine;

public class ColorizeTMProText : MonoBehaviour {
#if UNITY_EDITOR
	[ContextMenuItem("Colorize text",nameof(ColorizeText))]
	[SerializeField] private bool _colorizeInEditor;
#endif
	public UnityConsoleOutput consoleOutput;
	public List<SyntaxColor> _colorList;
	private static Color _unsetColor = new Color(1f, 0f, 1f, 0f);
	public Color _defaultColor = _unsetColor;
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
		Tokenizer tok = new Tokenizer();
		string text = UiText.GetText(gameObject);
		tok.Tokenize(text);
		//for (int i = 0; i < tok.Tokens.Count; i++) {
		//	Debug.
		//}
		List<Token> tokens = new List<Token>();
		tok.GetStandardTokens(tokens);
		Debug.Log(tokens.Count);
		Debug.Log(tokens.JoinToString("\n", t => {
			return t.MetaType + " " + t.ToString();
		}));
		//Debug.Log(tok.DebugPrint());
		// generate dictionary of syntax tree color list
		// calculate syntax tree
		// go through text and apply color based on color dictionary

		// TODO what are you doing cheif. didn't you solve this problem before already? isn't there a color grid that is updating?
		consoleOutput.SetCharColor(1, new Color[] { Color.red });
	}

	void Update() {

	}
}
