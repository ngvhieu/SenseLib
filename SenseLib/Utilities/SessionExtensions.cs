using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SenseLib.Utilities
{
    public static class SessionExtensions
    {
        public static void SetString(this ISession session, string key, string value)
        {
            session.SetString(key, value);
        }

        public static void SetInt32(this ISession session, string key, int value)
        {
            session.SetInt32(key, value);
        }

        public static void SetDecimal(this ISession session, string key, decimal value)
        {
            session.SetString(key, value.ToString());
        }

        public static string GetString(this ISession session, string key)
        {
            return session.GetString(key);
        }

        public static int? GetInt32(this ISession session, string key)
        {
            return session.GetInt32(key);
        }

        public static decimal? GetDecimal(this ISession session, string key)
        {
            string value = session.GetString(key);
            if (string.IsNullOrEmpty(value))
                return null;

            if (decimal.TryParse(value, out decimal result))
                return result;

            return null;
        }

        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }

        public static void Remove(this ISession session, string key)
        {
            session.Remove(key);
        }
    }
} 