using Newtonsoft.Json;

namespace RemoteReduxDevTools.Shared.Serialization;

public class NewtonsoftJsonAdapter : IJsonSerialization
{
	private readonly JsonSerializerSettings? _settings;

	public NewtonsoftJsonAdapter(JsonSerializerSettings? settings = null)
	{
		_settings = settings;
	}

	public object? Deserialize(string json, Type type) =>
		JsonConvert.DeserializeObject(json, type, _settings);

	public string Serialize(object source, Type type) =>
		JsonConvert.SerializeObject(source, type, _settings);
}
