using NonStandard.Inputs;
using NonStandard.Process;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace NonStandard.Cli {
	[RequireComponent(typeof(UserInput))]
	public class UnityConsoleUiToggle : MonoBehaviour {
		public const string UiKeyMapName = "UI";
		bool disabledRectMask2d = false;
		[Tooltip("which state the console input is active in"), ContextMenuItem("Bind Controls", nameof(BindControls))]
		public ConsoleUiState consoleInputActive = ConsoleUiState.ScreenSpace;
		public enum ConsoleUiState { None, ScreenSpace, WorldSpace, Both }
		[System.Serializable]
		public class Callbacks {
			public bool enable = true;
			public UnityEvent WhenThisActivates = new UnityEvent(), WhenThisDeactivates = new UnityEvent();
		}
		public Callbacks callbacks = new Callbacks();
		RectTransform uiTransform;
		public Canvas _screenSpaceCanvas;
		public Canvas _worldSpaceCanvas;
#if UNITY_EDITOR
		private void Reset() {
			BindControls();
			Canvas[] canvases = transform.GetComponentsInChildren<Canvas>();
			_worldSpaceCanvas = System.Array.Find(canvases, c => c.renderMode == RenderMode.WorldSpace);
			_screenSpaceCanvas = System.Array.Find(canvases, c => c.renderMode == RenderMode.ScreenSpaceOverlay);
		}

		private void BindControls() {
			UserInput uinput = GetComponent<UserInput>();
			uinput.AddBindingIfMissing(new InputControlBinding(
				"console pause game and put console in foreground", UiKeyMapName + "/ShowCmdLine",
				ControlType.Button, new EventBind(this, nameof(SetScreenSpaceCanvas)), "<Keyboard>/backquote"));
			uinput.AddBindingIfMissing(new InputControlBinding(
				"console unpause game and hide console in foreground", UiKeyMapName + "/HideCmdLine",
				ControlType.Button, new EventBind(this, nameof(SetScreenSpaceCanvas)), "<Keyboard>/escape"));
			uinput.AddActionMapToBind(UiKeyMapName);
			EventBind.On(callbacks.WhenThisActivates, this, nameof(Pause));
			EventBind.On(callbacks.WhenThisDeactivates, this, nameof(Unpause));
		}
#endif
		public Canvas ScreenSpaceCanvas => _screenSpaceCanvas ? _screenSpaceCanvas : _screenSpaceCanvas = GetScreenSpaceCanvas(null);
		public void EnqueueConsoleTextRefresh() {
			Proc.Enqueue(() => {
				UnityConsole console = GetComponent<UnityConsole>();
				console.Console.Window.ResetWindowSize();
				console.RefreshText();
			});
		}
		public void SetScreenSpaceCanvas(InputAction.CallbackContext context) {
			if (context.phase != InputActionPhase.Started) { return; }
			uiTransform.SetParent(ScreenSpaceCanvas.transform, false);
			UnityConsole console = GetComponent<UnityConsole>();
			if (console.inputField != null) {
				RectMask2D rm2d = console.inputField.textViewport != null ? console.inputField.textViewport.GetComponent<RectMask2D>() : null;
				if (rm2d != null && rm2d.enabled) {
					disabledRectMask2d = true;
					rm2d.enabled = false;
				}
			}
			Debug.Log((context.control as KeyControl).name);
			ActivateConsoleInput(consoleInputActive == ConsoleUiState.ScreenSpace || consoleInputActive == ConsoleUiState.Both);
			EnqueueConsoleTextRefresh();
		}
		public void SetWorldSpaceCanvas() {
			uiTransform.SetParent(_worldSpaceCanvas.transform, false);
			if (disabledRectMask2d) {
				UnityConsole console = GetComponent<UnityConsole>();
				RectMask2D rm2d = console.inputField.textViewport.GetComponent<RectMask2D>();
				disabledRectMask2d = false;
				rm2d.enabled = true;
			}
			ActivateConsoleInput(consoleInputActive == ConsoleUiState.WorldSpace || consoleInputActive == ConsoleUiState.Both);
			EnqueueConsoleTextRefresh();
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
		private Canvas FindCanvas(RenderMode mode) {
			Canvas[] canvases = GetComponentsInChildren<Canvas>();
			for (int i = 0; i < canvases.Length; ++i) {
				if (canvases[i].renderMode == mode) return canvases[i];
			}
			return null;
		}
		public void Pause() { GameClock.Instance().Pause(); }
		public void Unpause() { GameClock.Instance().Unpause(); }
		private void Awake() {
			UnityConsole console = GetComponent<UnityConsole>();
			uiTransform = console.GetUiTransform();
			if (_screenSpaceCanvas == null) {
				Canvas c = uiTransform.parent.GetComponent<Canvas>();
				if (c.renderMode == RenderMode.ScreenSpaceOverlay) { _screenSpaceCanvas = c; } else {
					_screenSpaceCanvas = FindCanvas(RenderMode.ScreenSpaceOverlay);
				}
			}
			if (_worldSpaceCanvas == null) {
				Canvas c = uiTransform.parent.GetComponent<Canvas>();
				if (c.renderMode == RenderMode.WorldSpace) { _worldSpaceCanvas = c; } else {
					_worldSpaceCanvas = FindCanvas(RenderMode.WorldSpace);
				}
			}
		}
		public ConsoleUiState GetCurrentState() {
			if (uiTransform.parent == _worldSpaceCanvas.transform) return ConsoleUiState.WorldSpace;
			if (uiTransform.parent == _screenSpaceCanvas.transform) return ConsoleUiState.ScreenSpace;
			return ConsoleUiState.None;
		}
		private void Start() {
			ConsoleUiState state = GetCurrentState();
			if (state != ConsoleUiState.None) {
				ActivateConsoleInput(consoleInputActive == state || consoleInputActive == ConsoleUiState.Both);
			}
		}

		public static Canvas GetScreenSpaceCanvas(Transform self, string canvasObjectNameOnCreate = "<console screen canvas>") {
			Canvas canvas;// = self.GetComponentInParent<Canvas>();
						  //if (!canvas) {
			canvas = (new GameObject(canvasObjectNameOnCreate)).AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			if (!canvas.GetComponent<UnityEngine.UI.CanvasScaler>()) {
				canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); // so that text is pretty when zoomed
			}
			if (!canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()) {
				canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>(); // so that mouse can select input area
			}
			canvas.transform.SetParent(self);
			//}
			return canvas;
		}
	}
}