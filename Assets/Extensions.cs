using System.Collections.Generic;

namespace ExtensionsMethods
{
    public static class Extensions
    {
        /// <summary>
        /// Adds a GetValue function with default return value.
        /// </summary>
        /// <typeparam name="TK">Type of the key</typeparam>
        /// <typeparam name="TV">Type of the value</typeparam>
        /// <param name="dict">the dictionary</param>
        /// <param name="key">the key</param>
        /// <param name="defaultValue">the default value to use</param>
        /// <returns></returns>
        public static TV GetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue = default(TV))
        {
            TV value;
            return dict.TryGetValue(key, out value) ? value : defaultValue;
        }
    }
}


