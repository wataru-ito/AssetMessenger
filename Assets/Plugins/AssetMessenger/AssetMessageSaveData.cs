using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using KVP = System.Collections.Generic.KeyValuePair<string, AssetMessageService.Message>;

namespace AssetMessageService
{
	[Serializable]
	class AssetMessageSaveData
	{
		const string kFilePath = "ProjectSettings/AssetMessagengerData.txt";

		[Serializable]
		public struct MessageData
		{
			public string guid;
			public string message;
			public MessageType type;
			public string source;

			public static MessageData Encode(KVP kvp)
			{
				return new MessageData()
				{
					guid = kvp.Key,
					message = kvp.Value.message,
					type = kvp.Value.type,
					source = kvp.Value.source,
				};
			}

			public Message Decode()
			{
				return new Message()
				{
					message = message,
					type = type,
					source = source,
				};
			}
		}

		public MessageData[] messages;


		//------------------------------------------------------
		// accessor
		//------------------------------------------------------

		public static void Save(IEnumerable<KVP> kvps)
		{
			var savedata = new AssetMessageSaveData();
			savedata.messages = kvps.Select(i => MessageData.Encode(i)).ToArray();
			File.WriteAllText(kFilePath, EditorJsonUtility.ToJson(savedata));
		}

		public static Dictionary<string, Message> Load()
		{
			try
			{
				if (File.Exists(kFilePath))
				{
					var data = JsonUtility.FromJson<AssetMessageSaveData>(File.ReadAllText(kFilePath));
					return data.messages.ToDictionary(i => i.guid, i => i.Decode());
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}

			return new Dictionary<string, Message>();
		}
	}
}