using Microsoft.AspNetCore.Http;
using System.Text.Json;

//Aufgabe 3: Wiederverwendbarer Session-Helper
public static class SessionHelper
{
    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObjectFromJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    public static void Remove(this ISession session, string key)
    {
        session.Remove(key);
    }
}