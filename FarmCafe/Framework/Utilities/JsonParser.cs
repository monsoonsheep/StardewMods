using Newtonsoft.Json;

namespace FarmCafe.Framework.Utilities
{
	public static class JsonParser
	{
		private static JsonSerializerSettings settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };

		public static string Serialize<Model>(Model model)
		{
			return JsonConvert.SerializeObject(model, Formatting.Indented, settings);
		}

		public static Model Deserialize<Model>(object json)
		{
			return Deserialize<Model>(json.ToString());
		}

		public static Model Deserialize<Model>(string json)
		{
			return JsonConvert.DeserializeObject<Model>(json, settings);
		}

		public static bool CompareSerializedObjects(object first, object second)
		{
			return JsonConvert.SerializeObject(first) == JsonConvert.SerializeObject(second);
		}

	}
}
