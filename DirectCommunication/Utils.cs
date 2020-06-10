using System;
using System.Collections.Generic;
using System.Text;

namespace DirectCommunication
{
    public class GenericDictionary
    {
        private Dictionary<string, object> _dict = new Dictionary<string, object>();

        public void Add<T>(string key, T value) where T : class
        {
            _dict.Add(key, value);
        }

        public T GetValue<T>(string key) where T : class
        {
            return _dict[key] as T;
        }

        public bool ContainsKey(String key)
        {
            return _dict.ContainsKey(key);
        }
    }
}
