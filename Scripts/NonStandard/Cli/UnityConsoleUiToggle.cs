// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using NonStandard.Inputs;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NonStandard.Cli {
	[RequireComponent(typeof(UserInput))]
	public class UnityConsoleUiToggle : MonoBehaviour {
		public const string UiKeyMapName = "consoleUi";
		[Tooltip("which state the console input is active in")]
		public ConsoleUiState consoleInputActive = ConsoleUiState.ScreenSpace;
		public Callbacks callbacks = new Callbacks();
		public Canvas _screenSpaceCanvas;
		public Canvas _worldSpaceCanvas;
		private RectTransform _uiTransform;
		private bool disabledRectMask2d = false;

		public Canvas ScreenSpaceCanvas => _screenSpaceCanvas
			? _screenSpaceCanvas : _screenSpaceCanvas = NewScreenCanvas(null);

		public enum ConsoleUiState { None, ScreenSpace, WorldSpace, Both }
		[System.Serializable] public class Callbacks {
			public bool enable = true;
			public UnityEvent WhenThisActivates = new UnityEvent(), WhenThisDeactivates = new UnityEvent();
		}
#if UNITY_EDITOR
		public void ResetInternal() => Reset();
		private void Reset() {
			BindControls();
			FindCanvases();
		}

		private void BindControls() {
			UserInput uinput = GetComponent<UserInput>();
			uinput.AddBindingIfMissing(new InputControlBinding(
				"console pause game and put console in foreground", UiKeyMapName + "/ShowCmdLine",
				ControlType.Button, new EventBind(this, nameof(SetScreenSpaceCanvas)), "<Keyboard>/backquote"));
			uinput.AddBindingIfMissing(new InputControlBinding(
				"console unpause game and hide console in foreground", UiKeyMapName + "/HideCmdLine",
				ControlType.Button, new EventBind(this, nameof(SetWorldSpaceCanvas)), "<Keyboard>/escape"));
			UnityConsoleInput uc = GetComponent<UnityConsoleInput>();
			uinput.RemoveDefaultActionMapToBind(uc.KeyMapName);
			uinput.AddDefaultActionMapToBind(UiKeyMapName);
			EventBind.IfNotAlready(callbacks.WhenThisActivates, this, nameof(Pause));
			EventBind.IfNotAlready(callbacks.WhenThisDeactivates, this, nameof(Unpause));
		}

		private void FindCanvases() {
			Canvas[] canvases = transform.GetComponentsInChildren<Canvas>();
			_worldSpaceCanvas = System.Array.Find(canvases, c => c.renderMode == RenderMode.WorldSpace);
			_screenSpaceCanvas = System.Array.Find(canvases, c => c.renderMode == RenderMode.ScreenSpaceOverlay);
		}
#endif
		private void Awake() {
			UnityConsoleOutput console = GetComponent<UnityConsoleOutput>();
			_uiTransform = console.FindUiTransform();
			if (_screenSpaceCanvas == null) {
				Canvas c = _uiTransform.parent.GetComponent<Canvas>();
				if (c.renderMode == RenderMode.ScreenSpaceOverlay) { _screenSpaceCanvas = c; } else {
					_screenSpaceCanvas = FindCanvas(RenderMode.ScreenSpaceOverlay);
				}
			}
			if (_worldSpaceCanvas == null) {
				Canvas c = _uiTransform.parent.GetComponent<Canvas>();
				if (c.renderMode == RenderMode.WorldSpace) { _worldSpaceCanvas = c; } else {
					_worldSpaceCanvas = FindCanvas(RenderMode.WorldSpace);
				}
			}
		}

		private void Start() {
			ConsoleUiState state = GetCurrentState();
			if (state != ConsoleUiState.None) {
				ActivateConsoleInput(MayActivate(state));
			}
		}

		private Canvas FindCanvas(RenderMode mode) {
			Canvas[] canvases = GetComponentsInChildren<Canvas>();
			for (int i = 0; i < canvases.Length; ++i) {
				if (canvases[i].renderMode == mode) return canvases[i];
			}
			return null;
		}

		private bool MayActivate(ConsoleUiState target) {
			return consoleInputActive == target || consoleInputActive == ConsoleUiState.Both;
		}

		public void ActivateConsoleInput(bool enable) {
			UnityConsoleInput uci = GetComponent<UnityConsoleInput>();
			if (uci != null) { uci.enabled = enable; }
			if (callbacks.enable) {
				if (enable) {
					callbacks.WhenThisActivates.Invoke();
				} else {
					callbacks.WhenThisDeactivates.Invoke();
				}
			}
		}

		public void SetScreenSpaceCanvas(InputAction.CallbackContext context) {
			if (context.phase != InputActionPhase.Performed) { return; }
			_uiTransform.SetParent(ScreenSpaceCanvas.transform, false);
			UnityConsoleOutput console = GetComponent<UnityConsoleOutput>();
			TMPro.TMP_InputField inputField = console.inputField;
			if (inputField != null) {
				RectTransform textViewport = inputField.textViewport;
				RectMask2D rm2d = textViewport != null ? textViewport.GetComponent<RectMask2D>() : null;
				if (rm2d != null && rm2d.enabled) {
					disabledRectMask2d = true;
					rm2d.enabled = false;
				}
			}
			// Debug.Log("set screenspace "+(context.control as KeyControl).name);
			ActivateConsoleInput(MayActivate(ConsoleUiState.ScreenSpace));
			EnqueueConsoleTextRefresh();
		}

		public void EnqueueConsoleTextRefresh() {
			StartCoroutine(RefreshConsoleWindowAndText());
		}

		private IEnumerator RefreshConsoleWindowAndText() {
			yield return null;
			UnityConsoleOutput console = GetComponent<UnityConsoleOutput>();
			console.Console.Window.ResetWindowSize();
			console.RefreshText();
		}

		public void SetWorldSpaceCanvas(InputAction.CallbackContext context) {
			if (context.phase != InputActionPhase.Performed) { return; }
			_uiTransform.SetParent(_worldSpaceCanvas.transform, false);
			if (disabledRectMask2d) {
				UnityConsoleOutput console = GetComponent<UnityConsoleOutput>();
				RectMask2D rm2d = console.inputField.textViewport.GetComponent<RectMask2D>();
				disabledRectMask2d = false;
				rm2d.enabled = true;
			}
			// Debug.Log("set worldspace "+(context.control as KeyControl).name);
			ActivateConsoleInput(MayActivate(ConsoleUiState.WorldSpace));
			EnqueueConsoleTextRefresh();
		}

		public void Pause() { GameClock.Instance().Pause(); }

		public void Unpause() { GameClock.Instance().Unpause(); }

		public ConsoleUiState GetCurrentState() {
			if (_uiTransform.parent == _worldSpaceCanvas.transform) return ConsoleUiState.WorldSpace;
			if (_uiTransform.parent == _screenSpaceCanvas.transform) return ConsoleUiState.ScreenSpace;
			return ConsoleUiState.None;
		}

		public static Canvas NewScreenCanvas(Transform parent, string canvasObjectNameOnCreate = "<console screen>") {
			Canvas canvas = (new GameObject(canvasObjectNameOnCreate)).AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			if (!canvas.GetComponent<CanvasScaler>()) {
				canvas.gameObject.AddComponent<CanvasScaler>(); // so that text is pretty when zoomed
			}
			if (!canvas.GetComponent<GraphicRaycaster>()) {
				canvas.gameObject.AddComponent<GraphicRaycaster>(); // so that mouse can select input area
			}
			if (parent) { canvas.transform.SetParent(parent); }
			return canvas;
		}
	}
}
