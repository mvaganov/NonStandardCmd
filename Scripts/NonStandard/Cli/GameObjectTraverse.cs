using NonStandard.Commands;
using NonStandard.Extension;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NonStandard.Cli {
	public class GameObjectTraverse : ConsoleCommands {
		public Transform workingTransform;

		public static List<KeyValuePair<string, object>> DirListing(IList<GameObject> list) {
			if (list == null) { return null; }
			List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();
			for (int i = 0; i < list.Count; ++i) {
				GameObject go = list[i];
				if (go != null) {
					result.Add(new KeyValuePair<string, object>(go.name, go.transform));
				}
			}
			return result;
		}
		public static List<KeyValuePair<string, object>> DirTransform(Transform t) {
			if (t == null) { return null; }
			List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();
			for (int i = 0; i < t.childCount; ++i) {
				Transform c = t.GetChild(i);
				if (c != null) {
					result.Add(new KeyValuePair<string, object>(c.name, c.transform));
				}
			}
			return result;
		}
		public static List<KeyValuePair<string, object>> DirGameObject(GameObject go) {
			if (go == null) { return null; }
			List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();
			Component[] comps = go.GetComponents<Component>();
			for (int i = 0; i < comps.Length; ++i) {
				Component c = comps[i];
				if (c != null) {
					result.Add(new KeyValuePair<string, object>(c.name, c));
				}
			}
			return result;
		}
		public static List<KeyValuePair<string, object>> Dir_object(object o) {
			if (o == null) { return null; }
			List<KeyValuePair<string, object>> result = new List<KeyValuePair<string, object>>();
			System.Type t = o.GetType();
			FieldInfo[] fi = t.GetFields();
			for (int i = 0; i < fi.Length; ++i) {
				FieldInfo f = fi[i];
				if (f != null) {
					result.Add(new KeyValuePair<string, object>(f.Name, f.GetValue(o)));
				}
			}
			PropertyInfo[] pi = t.GetProperties();
			for (int i = 0; i < pi.Length; ++i) {
				PropertyInfo p = pi[i];
				if (p != null) {
					result.Add(new KeyValuePair<string, object>(p.Name, new KeyValuePair<object, PropertyInfo>(o, p)));
				}
			}
			MethodInfo[] mi = t.GetMethods();
			for (int i = 0; i < mi.Length; ++i) {
				MethodInfo m = mi[i];
				if (m != null) {
					result.Add(new KeyValuePair<string, object>(m.Name, new KeyValuePair<object, MethodInfo>(o, m)));
				}
			}
			return result;
		}

		public static List<KeyValuePair<string, object>> Listing(Transform t) {
			List<KeyValuePair<string, object>> list = null;
			if (t == null) {
				GameObject[] root = SceneManager.GetActiveScene().GetRootGameObjects();
				list = DirListing(root);
			} else {
				list = DirTransform(t);
			}
			return list;
		}

		public void Cmd_Dir(Command.Exec e) {
			List<KeyValuePair<string, object>> list = Listing(workingTransform);
			UnityConsole console = GetComponent<UnityConsole>();
			for (int i = 0; i < list.Count; ++i) {
				console.WriteLine(list[i].Key);
			}
		}
		public void Cmd_Pwd(Command.Exec e) {
			UnityConsole console = GetComponent<UnityConsole>();
			string pwd = "/";
			if (workingTransform != null) {
				pwd += workingTransform.HierarchyPath();
			}
			console.WriteLine(pwd);
		}
		public void Cmd_Cd(Command.Exec e) {
			Arguments args = e.GetArgs();
			//Debug.Log(args);
			args.TryGet("/", out string dir);
			args.TryGet("..", out bool backDir);
			if ((dir == ".." || backDir) && workingTransform != null) {
				workingTransform = workingTransform.parent;
				//Show.Log("backing up");
				return;
			}
			List<KeyValuePair<string, object>> list = Listing(workingTransform);
			for (int i = 0; i < list.Count; ++i) {
				//Show.Log(list[i].Key+","+dir);
				if (list[i].Key == dir) {
					workingTransform = list[i].Value as Transform;
					//Show.Log("found " + dir + ", heading into " + list[i].Value.GetType());
					//Show.Log(workingTransform);
					return;
				}
			}
		}

#if UNITY_EDITOR
		public void Reset() {
			UnityConsoleCommander.CommandEntry[] DefaultCommandEntries = new UnityConsoleCommander.CommandEntry[] {
			new UnityConsoleCommander.CommandEntry("dir", "prints list of scene traversal options", nameof(Cmd_Dir), this),
			new UnityConsoleCommander.CommandEntry("pwd", "prints current working object being traversed", nameof(Cmd_Pwd), this),
			new UnityConsoleCommander.CommandEntry("cd", "traverses scene objects", nameof(Cmd_Cd), this, new UnityConsoleCommander.ArgumentEntry[]{
				new UnityConsoleCommander.ArgumentEntry("nextDirectory","/","name of the next directory to go into. may require quotes!",valueType:UnityConsoleCommander.ArgumentEntry.ValueType.String, orderOfValueImpliesArgument:true),
				new UnityConsoleCommander.ArgumentEntry("previousDirector","..","go back to the parent object")
			}),
		};
			AddCommands(DefaultCommandEntries);
		}
#endif
	}
}