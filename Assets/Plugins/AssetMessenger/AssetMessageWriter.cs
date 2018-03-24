using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using Callback = System.Action<AssetMessageService.AssetMessageData>;

namespace AssetMessageService
{
	class AssetMessageWriter : EditorWindow
	{
		AssetMessageData m_message = new AssetMessageData();
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
			m_message.guid = guid;
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
				m_callback(m_message);
				Close();
			}
		}
	}
}