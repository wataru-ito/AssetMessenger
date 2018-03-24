using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace AssetMessageService
{
	struct Message
	{
		public string message;
		public MessageType type;
		public string source;
	}

	public static class AssetMessenger
	{
		static Dictionary<string,Message> m_messageMap = new Dictionary<string, Message>();
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
			m_messageMap = AssetMessageSaveData.Load();

			m_listWindow = Resources.FindObjectsOfTypeAll<AssetMessageList>().FirstOrDefault();
			if (m_listWindow)
			{
				m_listWindow.Init(m_messageMap);
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

			AssetMessageSaveData.Save(m_messageMap);
		}


		//------------------------------------------------------
		// accessor
		//------------------------------------------------------

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


		public static void Clear(UnityEngine.Object assetObject, string source = null)
		{
			Clear(GetGUID(assetObject), source);
		}

		public static void Clear(string guid, string source = null)
		{
			Message m;
			if (!m_messageMap.TryGetValue(guid, out m))
				return;

			if (!string.IsNullOrEmpty(m.source) && m.source != source)
			{
				Debug.LogWarningFormat("[{0}]のメッセージは[{1}]じゃないと消せない",
					Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)),
					m.source);
				return;
			}

			m_messageMap.Remove(guid);
			Save();

			if (m_listWindow)
			{
				m_listWindow.Init(m_messageMap);
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

			SetMessage(guid, new Message()
			{
				message = message,
				type = type,
				source = source,
			});
		}

		static void SetMessage(string guid, Message message)
		{
			Assert.IsFalse(string.IsNullOrEmpty(guid));
			
			m_messageMap[guid] = message;
			Save();

			if (m_listWindow)
			{
				m_listWindow.Init(m_messageMap);
			}

			EditorApplication.RepaintProjectWindow();
		}


		//------------------------------------------------------
		// menu item
		//------------------------------------------------------

		const string kListMenuPath = "Tools/AssetMessenger/全てのメッセージを表示";
		const string kClearAllMenuPath = "Tools/AssetMessenger/全メッセージ削除";

		[MenuItem(kListMenuPath)]
		static void OpenList()
		{
			m_listWindow = AssetMessageList.Open(m_messageMap);
		}

		[MenuItem(kClearAllMenuPath)]
		static void ClearAllOnMenu()
		{
			if (!EditorUtility.DisplayDialog("メッセージ全削除", "重要なメッセージも全て消えてしまいますが\n本当に削除しますか？", "実行"))
				return;

			m_messageMap.Clear();
			Save();

			if (m_listWindow)
			{
				m_listWindow.Init(m_messageMap);
			}

			EditorApplication.RepaintProjectWindow();
		}


		//------------------------------------------------------
		// asset menu
		//------------------------------------------------------

		const string kClearMenuPath = "Assets/AssetMessenger/Clear";
		const string kWriteMenuPath = "Assets/AssetMessenger/Write";

		static string GetSelectionGUID()
		{
			return Selection.assetGUIDs.FirstOrDefault();
		}

		[MenuItem(kClearMenuPath, true, 300)]
		static bool IsClearableOnMenu()
		{
			var guid = GetSelectionGUID();
			if (string.IsNullOrEmpty(guid)) return false;

			Message m;
			return m_messageMap.TryGetValue(guid, out m) && string.IsNullOrEmpty(m.source);
		}

		[MenuItem(kClearMenuPath, false, 300)]
		static void ClearOnMenu()
		{
			var guid = GetSelectionGUID();
			Clear(guid);
		}

		[MenuItem(kWriteMenuPath, true, 301)]
		static bool IsWritableOnMenu()
		{
			var guid = GetSelectionGUID();
			if (string.IsNullOrEmpty(guid)) return false;

			// 消せないメッセージが既に設定されていたら上書きできない
			Message m;
			return !m_messageMap.TryGetValue(guid, out m) || string.IsNullOrEmpty(m.source);
		}

		[MenuItem(kWriteMenuPath, false, 301)]
		static void WriteOnMenu()
		{
			var guid = GetSelectionGUID();
			AssetMessageWriter.Open(guid, SetMessage);
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		static void OnGUI(string guid, Rect selectionRect)
		{
			Message msg;
			if (!m_messageMap.TryGetValue(guid, out msg)) return;

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
						var win = EditorWindow.focusedWindow; // MouseDownが来てるってことはfocusedWindowはここ
						AssetMessageBoard.Open(guid, msg, e.mousePosition + win.position.position);
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