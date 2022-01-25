using UnityEngine;

namespace NonStandard.Cli {
	public class UnityConsoleOptions : MonoBehaviour {
#if UNITY_EDITOR
		[System.Serializable]
		public class Options {
			public bool StandardInput = true;
			public bool DoCommands = true;
			public bool ToggleMainUI = true;
			public bool TraverseScene = true;
			public bool System = false;
		}
		public Options options = new Options();

		void EnableCreate_or_Disable<T>(bool enable) where T : MonoBehaviour {
			T c = GetComponent<T>();
			if (enable) {
				if (c == null) {
					c = gameObject.AddComponent<T>();
				} else {
					c.enabled = true;
				}
			} else {
				if (c != null) {
					c.enabled = false;
				}
			}
		}

		private void OnValidate() {
			EnableCreate_or_Disable<UnityConsoleInput>(options.StandardInput);
			EnableCreate_or_Disable<UnityConsoleCommander>(options.DoCommands);
			EnableCreate_or_Disable<UnityConsoleUiToggle>(options.ToggleMainUI);
			EnableCreate_or_Disable<GameObjectTraverse>(options.TraverseScene);
			EnableCreate_or_Disable<SystemCommandLine>(options.System);
		}
#endif
	}
}