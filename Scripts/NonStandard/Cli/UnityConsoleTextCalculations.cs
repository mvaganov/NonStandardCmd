using UnityEngine;
using NonStandard.Data;

namespace NonStandard.Cli {
	public class UnityConsoleTextCalculations {
		public TMPro.TMP_Text text;
		public Vector3 windowPosition;
		public Quaternion windowRotation;
		public Vector3 windowScale;
		public Vector3 tileSize;
		public Vector3 windowUpperLeft;
		public Vector3 windowLowerLeft;
		public Vector3 windowUpperRight;
		public Vector3 windowLowerRight;
		public Vector3 xhat;
		public Vector3 yhat;
		public UnityConsoleTextCalculations(TMPro.TMP_Text text) {
			Refresh(text);
		}
		public bool NeedsRefresh() {
			Transform transform = text.transform;
			return transform.position != windowPosition || transform.rotation != windowRotation
				|| transform.lossyScale != windowScale;
		}
		public void Update() { if (NeedsRefresh()) { Refresh(); } }
		public void Refresh() { Refresh(text); }
		public void Refresh(TMPro.TMP_Text text) {
			this.text = text;
			Transform textT = text.transform;
			windowPosition = textT.position;
			windowRotation = textT.rotation;
			windowScale = textT.lossyScale;
			tileSize = text.GetPreferredValues("#");
			RectTransform rt = text.GetComponent<RectTransform>();
			Vector3[] corners = new Vector3[4];
			rt.GetWorldCorners(corners);
			windowLowerLeft = corners[0];
			windowUpperLeft = corners[1];
			windowUpperRight = corners[2];
			windowLowerRight = corners[3];
			Vector3 widthDelta = windowLowerRight - windowLowerLeft;
			Vector3 heightDelta = windowLowerLeft - windowUpperLeft;
			Vector2 worldSize = new Vector2(widthDelta.magnitude, heightDelta.magnitude);
			xhat = widthDelta / worldSize.x;
			yhat = heightDelta / worldSize.y;
		}
		public Coord GetCursorIndex(Vector3 worldPosition) {
			Vector3 delta = worldPosition - windowUpperLeft;
			Vector2 panelPosWorldScale = new Vector2(Vector3.Dot(xhat, delta), Vector3.Dot(yhat, delta));
			Vector2 panelPosPixelScale = new Vector2(panelPosWorldScale.x / windowScale.x, panelPosWorldScale.y / windowScale.y);
			//Lines.Make("top").Line(windowUpperLeft, windowUpperLeft + xhat * panelPosWorldScale.x, Color.red, 1f/128);
			//Lines.Make("left").Line(windowUpperLeft, windowUpperLeft + yhat * panelPosWorldScale.y, Color.green, 1f / 128);
			Vector2 cursorPosition = new Vector2(panelPosPixelScale.x / tileSize.x, panelPosPixelScale.y / tileSize.y);
			Vector2 clippedPosition = new Vector2((int)cursorPosition.x, (int)cursorPosition.y);
			return new Coord((int)clippedPosition.x, (int)clippedPosition.y);
		}
		public void OutlineTile(Coord tilePosition, Color color, string lineName = "tile") {
			Vector2 cursorPixelPos = new Vector2(tilePosition.x * tileSize.x, tilePosition.y * tileSize.y);
			Vector2 cursorRealPos = new Vector2(cursorPixelPos.x * windowScale.x, cursorPixelPos.y * windowScale.y);
			Vector3 cursorWorldPos = xhat * cursorRealPos.x + yhat * cursorRealPos.y + windowUpperLeft;
			Vector3 worldTileWidth = xhat * tileSize.x * windowScale.x;
			Vector3 worldTileHeight = yhat * tileSize.y * windowScale.y;
			Vector3[] tileCorners = new Vector3[]{
					cursorWorldPos,
					cursorWorldPos+worldTileWidth,
					cursorWorldPos+worldTileWidth+worldTileHeight,
					cursorWorldPos+worldTileHeight,
					cursorWorldPos,
					cursorWorldPos+worldTileWidth,
				};
			Lines.Make(lineName).Line(tileCorners, color, startSize: 1f / 64);
		}
	}
}
