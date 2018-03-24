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

		public static AssetMessageBoard Create(Message message, Vector2 position)
		{
			var win = CreateInstance<AssetMessageBoard>();
			win.m_message = message;
			win.ShowAuxWindow();
			//win.ShowPopup(); // 表示がちょっと遅いんだけど...

			var w = 300f;
			var h = 100f;
			win.position = new Rect(position.x - w * 0.5f, position.y - h, w, h);

			return win;
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
			if (m_message.source)
			{
				GUILayout.FlexibleSpace();
				EditorGUILayout.ObjectField(m_message.source, typeof(MonoScript), false);
			}
		}
	}
}