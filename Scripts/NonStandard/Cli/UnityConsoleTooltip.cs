using NonStandard.Ui;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NonStandard.Cli {
	public class UnityConsoleTooltip : MonoBehaviour {
		public GameObject prefab_tooltip;
		private Dictionary<int, TooltipData> tooltips = new Dictionary<int, TooltipData>();
		public class TooltipData {
			public int pointer;
			public string text;
			public GameObject gameObject;
			public TooltipData(int pointer, string text, GameObject gameObject) {
				this.pointer = pointer; this.text = text; this.gameObject = gameObject;
			}
		}
		public void Tooltip(Vector3 position, string text, int pointer) {
			TooltipData data;
			if (text == null) {
				if (tooltips.TryGetValue(pointer, out data) && data.gameObject != null) {
					data.gameObject.SetActive(false);
				}
				return;
			}
			if (!tooltips.TryGetValue(pointer, out data)) {
				data = tooltips[pointer] = new TooltipData(pointer, null, null);
			}
			if (data.gameObject == null) {
				data.gameObject = Instantiate(prefab_tooltip);
				data.gameObject.transform.SetParent(prefab_tooltip.transform.parent, false);
			}
			if (data.text != text) {
				data.text = text;
				UiText.SetText(data.gameObject, text);
				//Debug.Log(UiText.GetText(data.gameObject));
				LayoutRebuilder.ForceRebuildLayoutImmediate(data.gameObject.transform as RectTransform);
			}
			data.gameObject.transform.position = position + (data.gameObject.transform.forward * -1f / 128);
			data.gameObject.SetActive(true);
		}

		void Update() {

		}
	}
}