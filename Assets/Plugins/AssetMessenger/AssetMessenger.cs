using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AssetMessageService
{
	struct Message
	{
		public string message;
		public MessageType type;
		public MonoScript source;
	}

	public static class AssetMessenger
	{
		static Dictionary<string,Message> m_messageMap = new Dictionary<string, Message>();
		static Texture[] m_icons;

		static bool m_editing;
		static bool m_dirty;


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

		public static void Clear(string guid)
		{
			if (m_messageMap.Remove(guid))
			{
				Save();
			}
		}

		public static void Write(string guid, string message, MessageType type)
		{
			WriteInternal(guid, message, type, null);
		}

		public static void Write(string guid, string message, MessageType type, MonoBehaviour behaviour)
		{
			WriteInternal(guid, message, type, MonoScript.FromMonoBehaviour(behaviour));
		}

		public static void Write(string guid, string message, MessageType type, ScriptableObject scriptableObject)
		{
			WriteInternal(guid, message, type, MonoScript.FromScriptableObject(scriptableObject));
		}

		static void WriteInternal(string guid, string message, MessageType type, MonoScript script)
		{
			if (string.IsNullOrEmpty(guid)) return;

			m_messageMap[guid] = new Message()
			{
				message = message,
				type = type,
				source = script,
			};

			Save();
		}


		//------------------------------------------------------
		// from menu
		//------------------------------------------------------

		const string kClearMenuPath = "Assets/AssetMessenger/Clear";
		const string kWriteMenuPath = "Assets/AssetMessenger/Write";

		static string GetSelectGUID()
		{
			var instanceId = Selection.activeInstanceID;
			if (instanceId != 0)
			{
				var assetPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
				if (!string.IsNullOrEmpty(assetPath))
				{
					return AssetDatabase.AssetPathToGUID(assetPath);
				}
			}
			return string.Empty;
		}

		[MenuItem(kClearMenuPath, true, 300)]
		static bool IsClearable()
		{
			var guid = GetSelectGUID();
			return !string.IsNullOrEmpty(guid) && m_messageMap.ContainsKey(guid);
		}

		[MenuItem(kClearMenuPath, false, 300)]
		static void Clear()
		{
			var guid = GetSelectGUID();
			Clear(guid);
		}

		[MenuItem(kWriteMenuPath, true, 301)]
		static bool IsWritable()
		{
			return !string.IsNullOrEmpty(GetSelectGUID());
		}

		[MenuItem(kWriteMenuPath, false, 301)]
		static void Write()
		{
			var guid = GetSelectGUID();
			WriteInternal(guid, "TEST", MessageType.Warning, null);
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
						AssetMessageBoard.Create(msg, e.mousePosition + win.position.position);
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