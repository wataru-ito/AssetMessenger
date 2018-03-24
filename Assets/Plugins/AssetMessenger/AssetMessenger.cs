using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AssetMessageService
{
	public static class AssetMessenger
	{
		class Postprocessor : AssetPostprocessor
		{
			static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
			{
				StartEditing();
				Array.ForEach(deletedAssets, i => ClearForce(AssetDatabase.AssetPathToGUID(i)));
				StopEditing();
			}
		}

		static AssetMessageMap m_dataMap;
		static Texture[] m_icons;

		static bool m_editing;
		static bool m_dirty;

		static AssetMessageList m_listWindow;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		[InitializeOnLoadMethod]
		static void Init()
		{
			m_icons = new Texture[Enum.GetValues(typeof(MessageType)).Length];
			m_icons[(int)MessageType.None] = 
			m_icons[(int)MessageType.Info] = EditorGUIUtility.LoadRequired("console.infoicon") as Texture;
			m_icons[(int)MessageType.Warning] = EditorGUIUtility.LoadRequired("console.warnicon") as Texture;
			m_icons[(int)MessageType.Error] =  EditorGUIUtility.LoadRequired("console.erroricon") as Texture;

			m_editing = false;
			m_dataMap = AssetMessageMap.Load();

			m_listWindow = Resources.FindObjectsOfTypeAll<AssetMessageList>().FirstOrDefault();
			if (m_listWindow)
			{
				m_listWindow.Init(m_dataMap);
			}

			EditorApplication.projectWindowItemOnGUI += OnGUI;
		}


		//------------------------------------------------------
		// data
		//------------------------------------------------------

		/// <summary>
		/// まとめて編集するときは開始時にこれを呼んでおくと効率的。編集終わったらStopEditing().
		/// </summary>
		public static void StartEditing()
		{
			if (m_editing)
			{
				Debug.LogWarning("AssetMessenger is already editing.");
				return;
			}

			m_editing = true;
			m_dirty = false;
		}

		/// <summary>
		/// まとめて編集終了時に呼ぶ。
		/// </summary>
		public static void StopEditing()
		{
			if (!m_editing)
			{
				Debug.LogWarning("AssetMessenger isnt editing.");
				return;
			}

			m_editing = false;
			if (m_dirty)
			{
				Save();
				m_dirty = false;
			}
		}

		static void Save()
		{
			if (m_editing)
			{
				m_dirty = true;
				return;
			}

			m_dataMap.Save();
			EditorApplication.RepaintProjectWindow();
		}


		//------------------------------------------------------
		// accessor
		//------------------------------------------------------

		static void ClearForce(string guid)
		{
			if (!m_dataMap.Remove(guid)) return;
			Save();

			if (m_listWindow)
			{
				m_listWindow.Init(m_dataMap);
			}
		}

		public static void Clear(UnityEngine.Object assetObject, string source = null)
		{
			Clear(GetGUID(assetObject), source);
		}

		public static void Clear(string guid, string source = null)
		{
			AssetMessageData m;
			if (!m_dataMap.TryGetValue(guid, out m))
				return;

			if (!string.IsNullOrEmpty(m.source) && m.source != source)
			{
				Debug.LogWarningFormat("[{0}]のメッセージは[{1}]じゃないと消せない",
					Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)),
					m.source);
				return;
			}

			m_dataMap.Remove(guid);
			Save();

			if (m_listWindow)
			{
				m_listWindow.Init(m_dataMap);
			}
		}

		/// <summary>
		/// メッセージを設定。sourceを指定すると同じsourceからじゃないと消せない。
		/// </summary>
		public static void Set(UnityEngine.Object assetObject, string message, MessageType type, string source = null)
		{
			Set(GetGUID(assetObject), message, type, source);
		}

		/// <summary>
		/// メッセージを設定。sourceを指定すると同じsourceからじゃないと消せない。
		/// </summary>
		public static void Set(string guid, string message, MessageType type, string source = null)
		{
			if (string.IsNullOrEmpty(guid)) return;

			SetMessage(new AssetMessageData()
			{
				guid = guid,
				message = message,
				type = type,
				source = source,
			});
		}

		internal static void SetMessage(AssetMessageData message)
		{
			m_dataMap.Set(message);
			Save();

			if (m_listWindow)
			{
				m_listWindow.Init(m_dataMap);
			}
		}

		static string GetGUID(UnityEngine.Object assetObject)
		{
			var assetPath = AssetDatabase.GetAssetPath(assetObject);
			if (string.IsNullOrEmpty(assetPath))
			{
				Debug.LogWarning(assetObject.ToString() + "dont asset");
				return null;
			}

			return AssetDatabase.AssetPathToGUID(assetPath);
		}


		//------------------------------------------------------
		// menu item
		//------------------------------------------------------

		const string kListMenuPath = "Tools/AssetMessenger/全てのメッセージを表示";
		const string kClearAllMenuPath = "Tools/AssetMessenger/全メッセージ削除";

		[MenuItem(kListMenuPath)]
		static void OpenList()
		{
			m_listWindow = AssetMessageList.Open(m_dataMap);
		}

		[MenuItem(kClearAllMenuPath)]
		static void ClearAllOnMenu()
		{
			if (!EditorUtility.DisplayDialog("メッセージ全削除", "重要なメッセージも全て消えてしまいますが\n本当に削除しますか？", "実行"))
				return;

			m_dataMap.Clear();
			Save();

			if (m_listWindow)
			{
				m_listWindow.Init(m_dataMap);
			}
		}


		//------------------------------------------------------
		// asset menu
		//------------------------------------------------------

		const string kClearMenuPath = "Assets/AssetMessenger/Clear";
		const string kWriteMenuPath = "Assets/AssetMessenger/Write";

		[MenuItem(kClearMenuPath, true, 300)]
		static bool IsClearableOnMenu()
		{
			var guid = Selection.assetGUIDs.FirstOrDefault();
			if (string.IsNullOrEmpty(guid)) return false;

			AssetMessageData m;
			return m_dataMap.TryGetValue(guid, out m) && string.IsNullOrEmpty(m.source);
		}

		[MenuItem(kClearMenuPath, false, 300)]
		static void ClearOnMenu()
		{
			var guid = Selection.assetGUIDs.FirstOrDefault();
			Clear(guid);
		}

		[MenuItem(kWriteMenuPath, true, 301)]
		static bool IsWritableOnMenu()
		{
			var guids = Selection.assetGUIDs;
			if (guids.Length != 1) return false;

			// 消せないメッセージが既に設定されていたら上書きできない
			AssetMessageData m;
			return !m_dataMap.TryGetValue(guids[0], out m) || string.IsNullOrEmpty(m.source);
		}

		[MenuItem(kWriteMenuPath, false, 301)]
		static void WriteOnMenu()
		{
			AssetMessageWriter.Open(Selection.assetGUIDs[0]);
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		static void OnGUI(string guid, Rect selectionRect)
		{
			AssetMessageData msg;
			if (!m_dataMap.TryGetValue(guid, out msg)) return;

			bool twoColumnLayout = (selectionRect.width / selectionRect.height) < 1f;
			var itemPosition = GetIconRect(selectionRect, twoColumnLayout);

			var e = Event.current;
			var controlId = EditorGUIUtility.GetControlID(FocusType.Passive);
			switch (e.GetTypeForControl(controlId))
			{
				case EventType.Repaint:
					// ちょっと大きくしとこ
					if (!twoColumnLayout)
					{
						itemPosition.x -= 2;
						itemPosition.width += 4;
						itemPosition.y -= 2;
						itemPosition.height += 4;
					}
					GUI.DrawTexture(itemPosition, m_icons[(int)msg.type]);
					break;

				case EventType.MouseDown:
					if (itemPosition.Contains(e.mousePosition) && e.button == 0)
					{
						AssetMessageBoard.Open(guid, msg, GUIUtility.GUIToScreenPoint(e.mousePosition));
						e.Use();
					}
					break;
			}
		}

		static Rect GetIconRect(Rect selectionRect, bool twoColumnLayout)
		{
			var r = selectionRect;
			if (twoColumnLayout)
			{
				r.width = 
				r.height = selectionRect.height * 0.5f;
				r.x = selectionRect.xMax - r.width;
				r.y = selectionRect.yMax - r.height - selectionRect.height * 0.2f;
			}
			else
			{
				r.width = r.height;
				r.x = selectionRect.xMax - r.width - 4;
			}
			return r;
		}
	}
}