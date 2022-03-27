// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using NonStandard.Extension;
using UnityEngine;

namespace NonStandard.Cli {
	public class UnityConsoleCursor : MonoBehaviour {
		public UnityConsole console;
		float fontSizeRatio = 1;
		[HideInInspector] public ConsoleState.CursorState state;

		public int CursorSize {
			get { return (int)(transform.localScale.MagnitudeManhattan() / 3); }
			set { transform.localScale = Vector3.one * (value / 100f); }
		}

		private void Reset() {
			console = GetComponentInParent<UnityConsole>();
		}
		private void Awake() {
			Initialize(console.cout.Text.fontSize);
			state = console.State.Cursor;
		}
		public void Initialize(float initialFontSize) {
			fontSizeRatio = transform.localScale.x / initialFontSize;
		}
		public void ScaleToFontSize(float fontSize) {
			transform.localScale = Vector3.one * fontSize * fontSizeRatio;
		}

		public Vector3 CalculateCursorPosition() {
			return console.calc.GetTileCenter(console.State.Cursor.position2d);
		}
		public void RefreshCursorPosition(Cli.UnityConsole console) {
			if (console.CursorVisible && state.indexInConsole >= 0) {
				Transform t = transform;
				Vector3 p = CalculateCursorPosition();
				t.position = p;//localPosition = p;
				t.rotation = console.transform.rotation;
				gameObject.SetActive(true);
			} else {
				gameObject.SetActive(false);
			}
		}
	}
}
