using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Net.VO;

internal static class SerializableExtensions
{
    extension<T>(ISerializable<T> self)
        where T : class, ISerializable<T>
    {
        public string Serialize()
        {
            if (JsonContext.Default.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T> info)
            {
                System.Console.Error.WriteLine(
                    $"Error serializing {typeof(T).FullName}: Type not registered."
                );
                return string.Empty;
            }

            try
            {
                return JsonSerializer.Serialize((T)self, info);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error serializing {typeof(T).FullName}: {ex}");
                return string.Empty;
            }
        }

        public static T? Deserialize(string json)
        {
            if (JsonContext.Default.GetTypeInfo(typeof(T)) is not JsonTypeInfo<T> info)
            {
                System.Console.Error.WriteLine(
                    $"Error deserializing {typeof(T).FullName}: Type not registered."
                );
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize(json, info);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error Deserializing {typeof(T).FullName}: {ex}");
                return null;
            }
        }
    }
}
