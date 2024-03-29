﻿using System.Text.Json;

namespace RemoteReduxDevTools.Shared.Serialization;

public class SystemTextJsonAdapter : IJsonSerialization
{
	private readonly JsonSerializerOptions? Options;

	public SystemTextJsonAdapter(JsonSerializerOptions? options = null)
	{
		Options = options;
	}

	public object? Deserialize(string json, Type type) =>
		JsonSerializer.Deserialize(json, type, Options);

	public string Serialize(object source, Type type) =>
		JsonSerializer.Serialize(source, type, Options);
}