using System.IO;
using UnityEngine;
using UnityEditor;

namespace AssetMessageService
{
	class AssetMessageBoard : EditorWindow
	{
		AssetMessageData m_message;

		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		public static AssetMessageBoard Open(string guid, AssetMessageData message, Vector2 displayPosition)
		{
			var win = CreateInstance<AssetMessageBoard>();
			win.Init(guid, message);
			win.ShowAuxWindow();
			//win.ShowPopup(); // 表示がちょっと遅いんだけど...
			win.SetDisplayPosition(displayPosition);
			return win;
		}

		void Init(string guid, AssetMessageData message)
		{
			m_message = message;
			titleContent = new GUIContent(Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)));
		}

		void SetDisplayPosition(Vector2 displayPosition)
		{
			const float w = 300f; // あとで調整しよう
			const float h = 100f;
			var x = Mathf.Max(0, displayPosition.x - w * 0.5f);
			var y = Mathf.Max(0, displayPosition.y - h);

			position = new Rect(x, y, w, h);
		}


		//------------------------------------------------------
		// unity system function
		//------------------------------------------------------

		void OnLostFocus()
		{
			Close();
		}

		void OnGUI()
		{
			EditorGUILayout.HelpBox(m_message.message, m_message.type, true);

			GUILayout.FlexibleSpace();
			if (!string.IsNullOrEmpty(m_message.source))
			{
				EditorGUILayout.LabelField("Posted by", m_message.source);
			}
			if (string.IsNullOrEmpty(m_message.source))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("編集", "ButtonLeft"))
					{
						var win = AssetMessageWriter.Open(m_message);
						var p = win.position;
						p.center = position.center;
						win.position = p;
					}
					if (GUILayout.Button("削除", "ButtonRight"))
					{
						AssetMessenger.Clear(m_message.guid);
						Close();
					}
				}
			}
		}
	}
}