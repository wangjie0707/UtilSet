using System;
using Json.Net;

namespace Tools
{
    public static class JsonHelper
    {
        public static string ToJson(object message)
        {
            return JsonNet.Serialize(message);
        }
        
        public static T ToObject<T>(string json)
        {
            return (T)JsonNet.Deserialize<T>(json);
        }
        
    }
}