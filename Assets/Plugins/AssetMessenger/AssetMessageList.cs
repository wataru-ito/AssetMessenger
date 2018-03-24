using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AssetMessageService
{
	class AssetMessageList : EditorWindow
	{
		class Data
		{
			public Object asset;
			public string assetPath;
			public string guid;
			public Message message;

			public Data(KeyValuePair<string, Message> kvp)
			{
				guid = kvp.Key;
				assetPath = AssetDatabase.GUIDToAssetPath(kvp.Key);
				asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
				message = kvp.Value;
			}
		}

		Data[] m_datas;

		Texture[] m_icons;
		Vector2 m_scrollPosition;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		public static AssetMessageList Open(IEnumerable<KeyValuePair<string,Message>> messages)
		{
			var win = GetWindow<AssetMessageList>();
			win.Init(messages);
			return win;
		}

		public void Init(IEnumerable<KeyValuePair<string, Message>> messages)
		{
			m_datas = messages.Select(i => new Data(i)).ToArray();
			System.Array.Sort(m_datas, (x, y) => x.assetPath.CompareTo(y.assetPath));

			Repaint();
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		void OnEnable()
		{
			titleContent = new GUIContent("Message一覧");

			m_icons = new Texture[System.Enum.GetValues(typeof(MessageType)).Length];
			m_icons[(int)MessageType.None] =
			m_icons[(int)MessageType.Info] = EditorGUIUtility.LoadRequired("console.infoicon") as Texture;
			m_icons[(int)MessageType.Warning] = EditorGUIUtility.LoadRequired("console.warnicon") as Texture;
			m_icons[(int)MessageType.Error] = EditorGUIUtility.LoadRequired("console.erroricon") as Texture;
		}

		void OnGUI()
		{
			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
			System.Array.ForEach(m_datas, DrawData);
			EditorGUILayout.EndScrollView();
		}


		//------------------------------------------------------
		// gui
		//------------------------------------------------------

		void DrawData(Data data)
		{
			var itemPosition = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.ExpandWidth(true));
			
			var icon = itemPosition;
			icon.width = itemPosition.height;
			icon.x = itemPosition.xMax - icon.width;
			itemPosition.width -= icon.width;

			var e = Event.current;
			switch (e.type)
			{
				case EventType.Repaint:
					icon.x -= 2;
					icon.width += 4;
					icon.y -= 2;
					icon.height += 4;
					GUI.DrawTexture(icon, m_icons[(int)data.message.type]);
					break;

				case EventType.MouseDown:
					if (icon.Contains(e.mousePosition) && e.button == 0)
					{
						AssetMessageBoard.Open(data.guid, data.message, position.position + e.mousePosition);
						e.Use();
					}
					break;
			}

			EditorGUI.ObjectField(itemPosition, data.asset, typeof(Object), false);
		}
	}
}