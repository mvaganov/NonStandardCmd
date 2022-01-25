using UnityEngine;

namespace NonStandard.Cli {
	public class UnityConsoleCursor : MonoBehaviour {
		float fontSizeRatio = 1;
		public void Initialize(float initialFontSize) {
			fontSizeRatio = transform.localScale.x / initialFontSize;
		}
		public void ScaleToFontSize(float fontSize) {
			transform.localScale = Vector3.one * fontSize * fontSizeRatio;
		}
	}
}