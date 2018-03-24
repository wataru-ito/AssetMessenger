using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace AssetMessageService
{
	[Serializable]
	class AssetMessageData
	{
		public string guid;
		public string message;
		public MessageType type;
		public string source;
	}

	class AssetMessageMap
	{
		const string kFilePath = "ProjectSettings/AssetMessagengerData.txt";

		[Serializable]
		class SaveData
		{
			public AssetMessageData[] messages;
		}

		Dictionary<string, AssetMessageData> m_map = new Dictionary<string, AssetMessageData>(); // key:guid


		//------------------------------------------------------
		// save/load
		//------------------------------------------------------

		public static AssetMessageMap Load()
		{
			var data = new AssetMessageMap();
			try
			{
				if (File.Exists(kFilePath))
				{
					var savedata = JsonUtility.FromJson<SaveData>(File.ReadAllText(kFilePath));
					data.m_map = savedata.messages.ToDictionary(i => i.guid, i => i);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}

			return data;
		}

		public void Save()
		{
			var savedata = new SaveData();
			savedata.messages = m_map.Values.ToArray();
			File.WriteAllText(kFilePath, EditorJsonUtility.ToJson(savedata));
		}


		//------------------------------------------------------
		// accessor
		//------------------------------------------------------

		public void Clear()
		{
			m_map.Clear();
		}

		public void Set(AssetMessageData message)
		{
			m_map[message.guid] = message;
		}

		public bool Remove(string guid)
		{
			return m_map.Remove(guid);
		}

		public bool TryGetValue(string guid, out AssetMessageData message)
		{
			return m_map.TryGetValue(guid, out message);
		}

		public IEnumerable<AssetMessageData> Messages
		{
			get { return m_map.Values; }
		}
	}
}