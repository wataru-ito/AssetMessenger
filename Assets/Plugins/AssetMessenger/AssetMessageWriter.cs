using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Callback = System.Action<string,AssetMessageService.Message>;

namespace AssetMessageService
{
	class AssetMessageWriter : EditorWindow
	{
		string m_guid;
		Message m_message = new Message();
		Callback m_callback;


		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		public static AssetMessageWriter Open(string guid, Callback callback)
		{
			Assert.IsFalse(string.IsNullOrEmpty(guid));
			Assert.IsNotNull(callback);

			var win = CreateInstance<AssetMessageWriter>();
			win.Init(guid, callback);
			win.ShowAuxWindow();
			return win;
		}

		void Init(string guid, Callback callback)
		{
			m_guid = guid;
			m_callback = callback;

			titleContent = new GUIContent(Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)));
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
			EditorGUIUtility.labelWidth = 100;
			m_message.type = (MessageType)EditorGUILayout.EnumPopup("Type", m_message.type);
			m_message.message = EditorGUILayout.TextArea(m_message.message, GUILayout.ExpandHeight(true));

			EditorGUILayout.Space();

			GUI.enabled = !string.IsNullOrEmpty(m_message.message);
			if (GUILayout.Button("設定"))
			{
				m_callback(m_guid, m_message);
				Close();
			}
		}
	}
}