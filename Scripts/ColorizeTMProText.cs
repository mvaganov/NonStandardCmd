using NonStandard;
using NonStandard.Data.Parse;
using NonStandard.Extension;
using NonStandard.Ui;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ColorizeTMProText : MonoBehaviour {
#if UNITY_EDITOR
	[ContextMenuItem("Colorize text",nameof(ColorizeText)),
	ContextMenuItem("LF Endings",nameof(ForceLfEndings))]
	[SerializeField] private bool _colorizeInEditor;
#endif
	public List<SyntaxColor> _colorList = new List<SyntaxColor>() {
		new SyntaxColor("text:", new Color(.5f,.5f,.5f)),
		new SyntaxColor("delim:", new Color(0,.5f,1)),
		new SyntaxColor("subst:", new Color(0,1,1)),
		new SyntaxColor("syntax:string", new Color(1,.5f,0)),
		new SyntaxColor("syntax://", new Color(0,.5f,0)),
		new SyntaxColor("syntax:{}", new Color(0,.5f,0)){ colorizeEndsByDepth = true },
	};
	private Dictionary<string, SyntaxColor> _colorDictionary = new Dictionary<string, SyntaxColor>();
	private Color inputColor = Color.magenta;
	public Tokenizer.State parseState = Tokenizer.State.None;
	private float _waitingToParseTime = 3;
	private float _waitingToparseTimer = 0;
	private Tokenizer _tokenizer;

	private static Color _unsetColor = new Color(1f, 0f, 1f, 0f);
	[SerializeField] private TMP_Text textComponent;
	[SerializeField] private TMPro.TMP_InputField textInput;
	private string _calculatedText;
	public TMP_Text TextComponent => textComponent;// != null ? textComponent : textComponent = GetComponent<TMP_InputField>().textComponent;
	[System.Serializable] public class SyntaxColor {
		public string syntax;
		public Color color;
		public bool colorizeEndsByDepth;
		public SyntaxColor(string syntax, Color color) { this.syntax = syntax; this.color = color; }
	}

	[System.Serializable] public class ColorChunk {
		public Color color;
		public int startIndex;
		public int endIndex;
		public int Count {
			get => endIndex - startIndex;
			set => endIndex = startIndex + value;
		}
		public void Move(int delta) {
			startIndex += delta;
			endIndex += delta;
		}
		public bool Contains(int index) {
			return index >= startIndex && index < endIndex;
		}
		public ColorChunk(int index, int count, Color color)
			{ this.startIndex = index; Count = count; this.color = color; }
	}

	private List<Color> nestedDepth = new List<Color>() {
		Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan
	};

	Color FindDefaultTextColor() {
		TMPro.TMP_Text txt = GetComponentInChildren<TMPro.TMP_Text>();
		return txt.color;
	}

	private void OnValidate() {
#if UNITY_EDITOR
		if (_colorizeInEditor) {
			RefreshColorize rc = textInput.GetComponent<RefreshColorize>();
			if (rc == null) {
				rc = textInput.gameObject.AddComponent<RefreshColorize>();
				rc.colorizer = this;
			}
		}
#endif
		if (textInput != null) {
			EventBind.IfNotAlready(textInput.onValueChanged, this, nameof(NotifyTouchedText));
		}
	}

	public class RefreshColorize : MonoBehaviour {
#if UNITY_EDITOR
		[SerializeField] public ColorizeTMProText colorizer;
		private void OnValidate() {
			if (colorizer != null && colorizer._colorizeInEditor) {
				colorizer.ColorizeText();
			}
		}
#endif
	}

	public void NotifyTouchedText(string text) {
		parseState = Tokenizer.State.Touched;
		_waitingToparseTimer = 0;
	}

	void Start() {
		ColorizeText();
	}

	public void ForceLfEndings() {
		string[] splitToCutOutBadChars = textInput.text.Split('\r');
		if (splitToCutOutBadChars.Length > 1) {
			textInput.text = splitToCutOutBadChars.JoinToString(""); ;
			Debug.Log("getting rid of " + (splitToCutOutBadChars.Length - 1) + " bad chars");
		}
	}

	void ColorizeText() {
		TMP_Text txt = TextComponent;
		if (txt == null) {
			throw new System.Exception("no TMP text in " + name + "??");
		}
		_calculatedText = txt.text;
		ColorDictionaryShouldMatchColorList();
		Tokenizer tokenizer = new Tokenizer();
		string text = UiText.GetText(gameObject);
		int iterations = 0;
		while (tokenizer.TokenizeIncrementally(text) != Tokenizer.State.Finished) {
			if(++iterations > 10000) {
				Debug.Log("woah. " + iterations);
				break;
			}
		}
		List<Token> tokens = tokenizer.GetStandardTokens();
		List<ColorChunk> colorChunks = CalculateColorChunks(tokens);
		ColorizeTextWith(colorChunks);
		//textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
	}

	private void ColorDictionaryShouldMatchColorList() {
		if (_colorList.Count == _colorDictionary.Count) { return; }
		_colorList.ForEach(sc => _colorDictionary[sc.syntax] = sc);
	}

	public List<ColorChunk> CalculateColorChunks(List<Token> tokens) {
		List<ColorChunk> colorChunks = new List<ColorChunk>();
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
				Color thisColor = nestedDepth[st.Depth % nestedDepth.Count];
				colorChunks.Add(new ColorChunk(begin.index, beginDelim.text.Length, thisColor));
				colorChunks.Add(new ColorChunk(end.index, endDelim.text.Length, thisColor));
			} else {
				Token token = tokens[i];
				string t = token.ToString();
				colorChunks.Add(new ColorChunk(token.index, t.Length, syntaxColor.color));
			}
		}
		colorChunks.Sort((a,b)=>a.startIndex.CompareTo(b.startIndex));
		return colorChunks;
	}

	public void InsertOffset(ColorChunk newChunk, IList<ColorChunk> chunks) {
		int i;
		int len = newChunk.Count;
		for (i = 0; i < chunks.Count; i++) {
			ColorChunk c = chunks[i];
			if (c.startIndex >= newChunk.startIndex) { break; }
			if (c.Contains(newChunk.startIndex)) {
				c.endIndex += len;
			}
		}
		for(; i < chunks.Count; ++i) {
			chunks[i].Move(len);
		}
	}
	
	public void ColorizeTextWith(IList<ColorChunk> colors) {
		//Debug.Log(textComponent);
		//Debug.Log(textComponent.textInfo);
		//Debug.Log(textComponent.textInfo.meshInfo);
		//Debug.Log(textComponent.textInfo.meshInfo.Length);
		if (textComponent.textInfo.meshInfo.Length > 1) {
			StringBuilder richErrorMessage = new StringBuilder();
			TMP_CharacterInfo[] chars = textComponent.textInfo.characterInfo;
			int found = 0;
			for (int i = 0; i < chars.Length; ++i) {
				if(chars[i].materialReferenceIndex != 0) {
					if (found > 0) { richErrorMessage.Append(", "); }
					richErrorMessage.Append((int)chars[i].character).Append('@').Append(i).
						Append(" '").Append(chars[i].character).Append('\'');
					++found;
				}
			}
			if (found > 0) {
				Debug.LogWarning("presence of unexpected characters: " + richErrorMessage.ToString());
			}
		}
		foreach (ColorChunk colorSeg in colors) {
			SetCharColor(textComponent, colorSeg.startIndex, colorSeg.color, colorSeg.Count);
		}
		textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
	}

	private const char TMProTextOutputTerminator = '\u200B';
	public static void SetCharColor(TMP_Text textOutput, int index, Color color, int count) {
		TMP_CharacterInfo[] chars = textOutput.textInfo.characterInfo;
		//bool SHOWME = false;
		for (int i = 0; i < count ; ++i) {
			if (index >= chars.Length) {
				Debug.LogWarning("can't set color of index " + index + ", limit " + chars.Length+" "+textOutput.text);
				return;
			}
			//if (SHOWME) { Debug.LogWarning("index " + index); }
			TMP_CharacterInfo cinfo = chars[index++];
			if (cinfo.character == TMProTextOutputTerminator) {
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
		switch (parseState) {
			case Tokenizer.State.Touched:
				ColorizeWithCurrentTokens();
				parseState = Tokenizer.State.Waiting;
				break;
			case Tokenizer.State.Waiting:
				_waitingToparseTimer += Time.deltaTime;
				if (_waitingToparseTimer >= _waitingToParseTime) {
					parseState = Tokenizer.State.Initialize;
				}
				break;
			case Tokenizer.State.Initialize:
				_tokenizer = new Tokenizer();
				string text = UiText.GetText(gameObject);
				_tokenizer.TokenizeIncrementally(text);
				parseState = Tokenizer.State.Iterating;
				break;
			case Tokenizer.State.Iterating:
				long maxCalcTime = System.Environment.TickCount + 5; // target 200fps
				do {
					parseState = _tokenizer.TokenizeIncrementally();
				} while (parseState == Tokenizer.State.Iterating && System.Environment.TickCount < maxCalcTime);
				break;
			case Tokenizer.State.Finished:
				ColorizeWithCurrentTokens();
				parseState = Tokenizer.State.None;
				break;
		}
	}
	public void ColorizeWithCurrentTokens() {
		if (_tokenizer == null) { return; }
		List<Token> tokens = _tokenizer.GetStandardTokens();
		List<ColorChunk> colorChunks = CalculateColorChunks(tokens);
		ColorizeTextWith(colorChunks);
	}
}
