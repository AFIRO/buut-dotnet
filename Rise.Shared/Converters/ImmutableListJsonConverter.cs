using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ImmutableListJsonConverter<T> : JsonConverter<ImmutableList<T>>
{
    public override ImmutableList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize JSON into a List<T> and convert to ImmutableList<T>
        var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
        return list?.ToImmutableList() ?? ImmutableList<T>.Empty;
    }

    public override void Write(Utf8JsonWriter writer, ImmutableList<T> value, JsonSerializerOptions options)
    {
        // Serialize ImmutableList<T> as a List<T>
        JsonSerializer.Serialize(writer, value.ToList(), options);
    }
}