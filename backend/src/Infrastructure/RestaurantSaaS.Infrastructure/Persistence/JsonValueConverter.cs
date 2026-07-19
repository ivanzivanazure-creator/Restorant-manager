using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RestaurantSaaS.Infrastructure.Persistence;

/// <summary>Serializes a small scalar collection/dictionary property (e.g. feature flags, recovery code
/// hashes, supported languages) into a single jsonb column instead of a child table — appropriate for
/// data that's always read/written as a whole with its owner and never queried independently.</summary>
internal static class JsonValueConverter<T>
{
    public static ValueConverter<T, string> Converter { get; } = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null)!);

    public static ValueComparer<T> Comparer { get; } = new(
        (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
        v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);
}
