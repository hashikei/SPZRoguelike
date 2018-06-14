using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonController : MonoBehaviour {
	
	private struct Vec2Int {
		public int x;
		public int y;

		public Vec2Int(int _x, int _y) {
			x = _x;
			y = _y;
		}
	}

	private class Rect {
		public int left;
		public int right;
		public int top;
		public int bottom;

		public int width { get { return right - left; } }
		public int height { get { return bottom - top; } }

		public Rect (int _left, int _right, int _top, int _bottom) {
			left = _left;
			right = _right;
			top = _top;
			bottom = _bottom;
		}
	}

	private class Area : Rect {
		public Rect Room;

		public Area(int _left, int _right, int _top, int _bottom) : base (_left, _right, _top, _bottom){}
	}

	const int FLOOR_WIDTH = 25;
	const int FLOOR_HEIGHT = 25;

	const int MIN_AREA_SIZE = 6;
	const int MIN_ROOM_SIZE = MIN_AREA_SIZE - 2;

	[SerializeField] private GameObject wallPrefab;
	[SerializeField] private GameObject roadPrefab;

	private int[,] board;

	private List<Area> areaList;

	void Awake() {
		board = new int[FLOOR_WIDTH, FLOOR_HEIGHT];
		areaList = new List<Area> ();
	}

	// Use this for initialization
	void Start () {
		GenerateFloor ();
	}

	void Update () {
		if (Input.GetKeyDown(KeyCode.Return)) {
			Reset ();
		}
	}

	void GenerateFloor() {
		GenerateArea ();

		GenerateRoom ();

		GenerateRoad ();

		for (int i = 0; i < FLOOR_WIDTH; ++i) {
			for (int j = 0; j < FLOOR_HEIGHT; ++j) {
				var prefab = wallPrefab;
				if (board[i, j] == 1 || board[i, j] == 2) {
					prefab = roadPrefab;
				}

				var obj = Instantiate (prefab, transform, false);
				var pos = obj.transform.position;
				pos.x = i;
				pos.y = -j;
				obj.transform.position = pos;
			}
		}

//		int cnt = 0;
//		foreach (var area in areaList) {
//			for (int i = area.left; i < area.right; ++i) {
//				for (int j = area.top; j < area.bottom; ++j) {
//					var obj = Instantiate (wallPrefab, transform, false);
//					var pos = obj.transform.position;
//					pos.x = i;
//					pos.y = -j;
//					obj.transform.position = pos;
//					obj.GetComponent<SpriteRenderer> ().color = new Color (cnt * 0.1f, cnt * 0.1f, cnt * 0.1f);
//				}
//			}
//			cnt++;
//		}
	}

	void GenerateArea() {
		int left = 0;
		int right = 0;

		bool isWidth = false;
		while (!isWidth) {
			int width = Random.Range (MIN_AREA_SIZE, FLOOR_WIDTH - right);

			// HACK: バグってます
			if (FLOOR_WIDTH - (left + width) < MIN_AREA_SIZE * 1.5f) {
				right = FLOOR_WIDTH - 1;

				Virtical (left, right);

				isWidth = true;
			} else {
				right = left + width;

				Virtical (left, right);

				left = right + 1;
			}
		}
	}

	void GenerateRoom() {
		foreach(var area in areaList) {
			int width = Random.Range (MIN_ROOM_SIZE, area.width - 2);
			int height = Random.Range (MIN_ROOM_SIZE, area.height - 2);

			int left = Random.Range (area.left + 1, area.right - width - 1);
			int top = Random.Range (area.top + 1, area.bottom - height - 1);
			int right = left + width;
			int bottom = top + height;
			area.Room = new Rect (left, right, top, bottom);

			for (int x = left; x < right; ++x) {
				for (int y = top; y < bottom; ++y) {
					board [x, y] = 1;
				}
			}
		}
	}

	void GenerateRoad() {
		var leftRoadList = new List<Vec2Int> ();
		var rightRoadList = new List<Vec2Int> ();
		var topRoadList = new List<Vec2Int> ();
		var bottomRoadList = new List<Vec2Int> ();

		foreach (var area in areaList) {
			if (area.left > 0) {
				int pointY = Random.Range (area.Room.top, area.Room.bottom);

				for (int i = area.left; i < area.Room.left; ++i) {
					board [i, pointY] = 2;
				}

				leftRoadList.Add (new Vec2Int (area.left, pointY));
			}
			if (area.right < FLOOR_WIDTH - 1) {
				int pointY = Random.Range (area.Room.top, area.Room.bottom);

				for (int i = area.Room.right; i < area.right; ++i) {
					board [i, pointY] = 2;
				}

				rightRoadList.Add (new Vec2Int (area.right, pointY));
			}
			if (area.top > 0) {
				int pointX = Random.Range (area.Room.left, area.Room.right);

				for (int i = area.top; i < area.Room.top; ++i) {
					board [pointX, i] = 2;
				}

				topRoadList.Add (new Vec2Int (pointX, area.top));
			}
			if (area.bottom < FLOOR_HEIGHT - 1) {
				int pointX = Random.Range (area.Room.left, area.Room.right);

				for (int i = area.Room.bottom; i < area.bottom; ++i) {
					board [pointX, i] = 2;
				}

				bottomRoadList.Add (new Vec2Int (pointX, area.bottom));
			}
		}

		// 左方向の道を走査する
		foreach (var leftRoad in leftRoadList) {
			if (board[leftRoad.x - 1, leftRoad.y] == 2) {
				continue;
			}
			if (board[leftRoad.x - 2, leftRoad.y] == 2) {
				board [leftRoad.x - 1, leftRoad.y] = 2;
				continue;
			}

			bool isComplete = false;
			Vec2Int endPoint = new Vec2Int ();
			for (int i = 1; !isComplete; ++i) {
				int y = Mathf.CeilToInt (i / 2f);
				if (i % 2 != 0) {
					y = -y;
				}

				y += leftRoad.y;
				if (y < 0 || y > FLOOR_HEIGHT - 1) {
					continue;
				}

				if (board[leftRoad.x - 1, y] == 2 || board[leftRoad.x - 2, y] == 2) {
					endPoint.x = leftRoad.x - 1;
					endPoint.y = y;

					isComplete = true;
					continue;
				}
			}

			if (endPoint.y - leftRoad.y < 0) {
				for (int j = endPoint.y; j <= leftRoad.y; ++j) {
					board [endPoint.x, j] = 2;
				}
			} else {
				for (int j = leftRoad.y; j <= endPoint.y; ++j) {
					board [endPoint.x, j] = 2;
				}
			}
		}

		// 右方向の道を走査する
		foreach (var rightRoad in rightRoadList) {
			if (board[rightRoad.x + 1, rightRoad.y] == 2) {
				continue;
			}
			if (board[rightRoad.x + 2, rightRoad.y] == 2) {
				board [rightRoad.x + 1, rightRoad.y] = 2;
				continue;
			}

			bool isComplete = false;
			Vec2Int endPoint = new Vec2Int ();
			for (int i = 1; !isComplete; ++i) {
				int y = Mathf.CeilToInt (i / 2f);
				if (i % 2 != 0) {
					y = -y;
				}

				y += rightRoad.y;
				if (y < 0 || y > FLOOR_HEIGHT - 1) {
					continue;
				}

				if (board[rightRoad.x + 1, y] == 2 || board[rightRoad.x + 2, y] == 2) {
					endPoint.x = rightRoad.x;
					endPoint.y = y;

					isComplete = true;
					continue;
				}
			}

			if (endPoint.y - rightRoad.y < 0) {
				for (int j = endPoint.y; j <= rightRoad.y; ++j) {
					board [endPoint.x, j] = 2;
				}
			} else {
				for (int j = rightRoad.y; j <= endPoint.y; ++j) {
					board [endPoint.x, j] = 2;
				}
			}
		}

		// 上方向の道を走査する
		foreach (var topRoad in topRoadList) {
			if (board[topRoad.x, topRoad.y - 1] == 2) {
				continue;
			}
			if (board[topRoad.x, topRoad.y - 2] == 2) {
				board [topRoad.x, topRoad.y - 1] = 2;
				continue;
			}

			bool isComplete = false;
			Vec2Int endPoint = new Vec2Int ();
			for (int i = 1; !isComplete; ++i) {
				int x = Mathf.CeilToInt (i / 2f);
				if (i % 2 != 0) {
					x = -x;
				}

				x += topRoad.x;
				if (x < 0 || x > FLOOR_WIDTH - 1) {
					continue;
				}

				if (board[x, topRoad.y - 1] == 2 || board[x, topRoad.y - 2] == 2) {
					endPoint.x = x;
					endPoint.y = topRoad.y - 1;

					isComplete = true;
					continue;
				}
			}

			if (endPoint.x - topRoad.x < 0) {
				for (int j = endPoint.x; j <= topRoad.x; ++j) {
					board [j, endPoint.y] = 2;
				}
			} else {
				for (int j = topRoad.x; j <= endPoint.x; ++j) {
					board [j, endPoint.y] = 2;
				}
			}
		}

		// 下方向の道を走査する
		foreach (var bottomRoad in bottomRoadList) {
			if (board[bottomRoad.x, bottomRoad.y + 1] == 2) {
				continue;
			}
			if (board[bottomRoad.x, bottomRoad.y + 2] == 2) {
				board [bottomRoad.x, bottomRoad.y + 1] = 2;
				continue;
			}

			bool isComplete = false;
			Vec2Int endPoint = new Vec2Int ();
			for (int i = 1; !isComplete; ++i) {
				int x = Mathf.CeilToInt (i / 2f);
				if (i % 2 != 0) {
					x = -x;
				}

				x += bottomRoad.x;
				if (x < 0 || x > FLOOR_WIDTH - 1) {
					continue;
				}

				if (board[x, bottomRoad.y + 1] == 2 || board[x, bottomRoad.y + 2] == 2) {
					endPoint.x = x;
					endPoint.y = bottomRoad.y;

					isComplete = true;
					continue;
				}
			}

			if (endPoint.x - bottomRoad.x < 0) {
				for (int j = endPoint.x; j <= bottomRoad.x; ++j) {
					board [j, endPoint.y] = 2;
				}
			} else {
				for (int j = bottomRoad.x; j <= endPoint.x; ++j) {
					board [j, endPoint.y] = 2;
				}
			}
		}
	}

	void Virtical(int left, int right) {
		int top = 0;
		int bottom = 0;

		bool isHeight = false;
		while (!isHeight) {
			int height = Random.Range (MIN_AREA_SIZE, FLOOR_HEIGHT - bottom);

			// FIXME: バグってます
			if (FLOOR_HEIGHT - (top + height) < MIN_AREA_SIZE * 1.5f) {
				bottom = FLOOR_HEIGHT - 1;

				var area = new Area (left, right, top, bottom);
				areaList.Add (area);

				isHeight = true;
			} else {
				bottom = top + height;

				var area = new Area (left, right, top, bottom);
				areaList.Add (area);

				top = bottom + 1;
			}
		}
	}

	void Reset () {
		foreach(Transform childTransform in transform) {
			Destroy (childTransform.gameObject);
		}

		for (int i = 0; i < FLOOR_WIDTH; ++i) {
			for (int j = 0; j < FLOOR_HEIGHT; ++j) {
				board [i, j] = 0;
			}
		}

		areaList.Clear ();

		GenerateFloor ();
	}
}
