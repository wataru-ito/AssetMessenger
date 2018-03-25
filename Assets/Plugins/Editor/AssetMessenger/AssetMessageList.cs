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
			public AssetMessageData message;

			public Data(AssetMessageData message)
			{
				assetPath = AssetDatabase.GUIDToAssetPath(message.guid);
				asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
				this.message = message;
			}
		}

		Data[] m_datas;

		Texture[] m_icons;
		Vector2 m_scrollPosition;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		public static AssetMessageList Open(AssetMessageMap data)
		{
			var win = GetWindow<AssetMessageList>();
			win.Init(data);
			return win;
		}

		public void Init(AssetMessageMap data)
		{
			m_datas = data.Messages.Select(i => new Data(i)).ToArray();
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
			// ウィンドウ表示したまま再起動するとこうなる
			// > すぐにAssetMessenger.csから再設定されるので待つ
			if (m_datas == null)
			{
				EditorGUILayout.HelpBox("AssetMessenger 初期化中...", MessageType.Info);
				return;
			}

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
						AssetMessageBoard.Open(data.message.guid, data.message, GUIUtility.GUIToScreenPoint(e.mousePosition));
						e.Use();
					}
					break;
			}

			EditorGUI.ObjectField(itemPosition, data.asset, typeof(Object), false);
		}
	}
}