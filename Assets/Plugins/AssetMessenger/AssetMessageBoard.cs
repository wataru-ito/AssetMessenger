using System.IO;
using UnityEngine;
using UnityEditor;

namespace AssetMessageService
{
	class AssetMessageBoard : EditorWindow
	{
		Message m_message;

		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		public static AssetMessageBoard Open(string guid, Message message, Vector2 displayPosition)
		{
			var win = CreateInstance<AssetMessageBoard>();
			win.Init(guid, message);
			win.ShowAuxWindow();
			//win.ShowPopup(); // 表示がちょっと遅いんだけど...
			win.SetDisplayPosition(displayPosition);
			return win;
		}

		void Init(string guid, Message message)
		{
			m_message = message;
			titleContent = new GUIContent(Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)));
		}

		void SetDisplayPosition(Vector2 displayPosition)
		{
			var w = 300f;
			var h = 100f;
			position = new Rect(displayPosition.x - w * 0.5f, displayPosition.y - h, w, h);
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
			EditorGUILayout.HelpBox(m_message.message, m_message.type);
			if (!string.IsNullOrEmpty(m_message.source))
			{
				GUILayout.FlexibleSpace();
				EditorGUILayout.LabelField("Posted by", m_message.source);
			}
		}
	}
}