using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace AssetMessageService
{
	class AssetMessageWriter : EditorWindow
	{
		AssetMessageData m_message = new AssetMessageData();
		

		//------------------------------------------------------
		// static function
		//------------------------------------------------------

		public static AssetMessageWriter Open(string guid)
		{
			Assert.IsFalse(string.IsNullOrEmpty(guid));

			var win = CreateInstance<AssetMessageWriter>();
			win.m_message.guid = guid;
			win.Open();
			return win;
		}

		public static AssetMessageWriter Open(AssetMessageData data)
		{
			Assert.IsFalse(string.IsNullOrEmpty(data.guid));

			var win = CreateInstance<AssetMessageWriter>();
			win.m_message = data;
			win.Open();
			return win;
		}
		
		void Open()
		{
			titleContent = new GUIContent(Path.GetFileName(AssetDatabase.GUIDToAssetPath(m_message.guid)));
			ShowAuxWindow();
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
				AssetMessenger.SetMessage(m_message);
				Close();
			}
		}
	}
}