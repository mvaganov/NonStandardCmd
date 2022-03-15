// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using UnityEngine;

namespace NonStandard.Cli {
	public class UnityConsoleCursor : MonoBehaviour {
		public UnityConsole console;
		float fontSizeRatio = 1;
		private Vector3[] cursorMeshPosition = new Vector3[4];
		[HideInInspector] public ConsoleState.CursorState state;

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
			return (cursorMeshPosition[0] + cursorMeshPosition[1] + cursorMeshPosition[2] + cursorMeshPosition[3]) / 4;
		}
		public void RefreshCursorPosition(Cli.UnityConsole console) {
			if (console.CursorVisible && state.indexInConsole >= 0) {
				Transform t = transform;
				Vector3 p = CalculateCursorPosition();
				t.localPosition = p;
				t.rotation = console.transform.rotation;
				gameObject.SetActive(true);
			} else {
				gameObject.SetActive(false);
			}
		}
		internal void SetCursorPositionPoints(Vector3[] verts, int vertexIndex) {
			if (vertexIndex >= verts.Length) {
				Debug.LogWarning("too much? " + vertexIndex + " / " + verts.Length + " ");
				return;
			}
			cursorMeshPosition[0] = verts[vertexIndex + 0];
			cursorMeshPosition[1] = verts[vertexIndex + 1];
			cursorMeshPosition[2] = verts[vertexIndex + 2];
			cursorMeshPosition[3] = verts[vertexIndex + 3];
		}
	}
}
