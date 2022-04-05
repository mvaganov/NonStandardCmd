// code by michael vaganov, released to the public domain via the unlicense (https://unlicense.org/)
using UnityEngine;
using NonStandard.Data;

namespace NonStandard.Cli {
	public class UnityConsoleCalculations {
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
		public Vector3 worldTileWidth;
		public Vector3 worldTileHeight;
		public float screenTileWidth, screenTileHeight, worldScreenTtileDescent;
		public UnityConsoleCalculations(TMPro.TMP_Text text) {
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
			tileSize = CalculateTileSize();
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
			screenTileWidth = tileSize.x * windowScale.x;
			screenTileHeight = tileSize.y * windowScale.y;
			worldTileWidth = xhat * screenTileWidth;
			worldTileHeight = yhat * screenTileHeight;
		}

		private Vector2 CalculateTileSize() {
			string fullChar = UnityConsoleOutput.CharSettings.DefaultBackground.ToString();
			Vector2 oneLine = text.GetPreferredValues(fullChar);
			Vector2 twoLines = text.GetPreferredValues(fullChar + "\n" + fullChar);
			Vector2 tileSize = oneLine;
			worldScreenTtileDescent = (twoLines.y - (oneLine.y * 2)) * windowScale.y;
			tileSize.y = twoLines.y - oneLine.y; // make sure the tileSize height includes the line padding.
			return tileSize;
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
			Vector3[] tileCorners = new Vector3[6];
			GetTileCorners(tilePosition, tileCorners);
			tileCorners[4] = tileCorners[0];
			tileCorners[5] = tileCorners[1];
			Lines.Make(lineName).Line(tileCorners, color, startSize: 1f / 64);
		}

		public void GetTileCorners(Coord tilePosition, Vector3[] out_corners) {
			Vector3 cursorWorldPos = GetTileCornerWorldPosition(tilePosition);
			out_corners[0] = cursorWorldPos;
			out_corners[1] = cursorWorldPos + worldTileWidth;
			out_corners[2] = cursorWorldPos + worldTileWidth + worldTileHeight;
			out_corners[3] = cursorWorldPos                  + worldTileHeight;
		}

		public Vector3 GetTileCornerWorldPosition(Coord tilePosition) {
			Vector2 cursorScreenPos = new Vector2(tilePosition.x * screenTileWidth, tilePosition.y * screenTileHeight);
			cursorScreenPos.y -= worldScreenTtileDescent;
			return xhat * cursorScreenPos.x + yhat * cursorScreenPos.y + windowUpperLeft;
		}

		public Vector3 GetTileCenter(Coord tilePosition) {
			return GetTileCornerWorldPosition(tilePosition) + (worldTileHeight + worldTileWidth) / 2;
		}

		public class Text {
			public Text(UnityConsoleOutput console) { rt = console.TextRect; }
			public RectTransform rt;
			public Coord textContentSize = Coord.Zero;
			public int currentLineCharCount;
			public Vector2 min, max;
			public void CalculateTextBoundaries(Vector3[] verts) {
				min = max = verts[0];
				Vector3 v;
				for (int i = 0; i < verts.Length; ++i) {
					v = verts[i];
					if (v.x < min.x) min.x = v.x;
					if (v.y < min.y) min.y = v.y;
					if (v.x > max.x) max.x = v.x;
					if (v.y > max.y) max.y = v.y;
				}
			}
			public void StartCalculatingText() {
				currentLineCharCount = 0;
				textContentSize = Coord.Zero;
			}
			public void UpdateTextCalculation(char c) {
				if (currentLineCharCount == 0) { ++textContentSize.Y; }
				switch (c) {
					case '\0': break;
					case '\n': currentLineCharCount = 0; break;
					default:
						if (++currentLineCharCount > textContentSize.X) {
							textContentSize.X = currentLineCharCount;
						}
						break;
				}
			}
			public Vector2 CalculateIdealSize() {
				Vector2 totSize = (max - min);
				Rect rect = rt.rect;
				Vector2 maxInArea = new Vector2(Coord.Max.X - 1, Coord.Max.Y - 1);
				// only limit the size if there is not enough space
				if (totSize.x > rect.width || totSize.y > rect.height) {
					Vector2 glyphSize = new Vector2(totSize.x / textContentSize.X, totSize.y / textContentSize.Y);
					maxInArea = new Vector2(rect.width / glyphSize.x, rect.height / glyphSize.y - 1);
					//Show.Log(maxInArea + " <-- " + glyphSize + " chars: " + size + "   sized: " + totSize + "   / " + rt.rect.width + "," + rt.rect.height);
				}
				return maxInArea;
			}
		}
	}
}
