#if UNITY_EDITOR

using System.IO;
using UnityEditor;

namespace UnityExtensions.Editor
{
    public class EditorSettings<T> where T : EditorSettings<T>, new()
    {
        static T _instance;
        public static T instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                    EditorJsonUtility.FromJsonOverwrite(_instance._json = _instance.Load(), _instance);
                }
                return _instance;
            }
        }

        string _json;

        internal void Serialize()
        {
            var json = EditorJsonUtility.ToJson(this, true);
            if (json != _json) Save(_json = json);
        }

        protected virtual void Save(string data)
        {
            try
            {
                using (var stream = new FileStream($"ProjectSettings/{typeof(T).Name}", FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(data);
                    }
                }
            }
            catch
            {
            }
        }

        protected virtual string Load()
        {
            try
            {
                using (var stream = new FileStream($"ProjectSettings/{typeof(T).Name}", FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// SettingsWindow<T>
    /// </summary>
    public abstract class SettingsWindow<T> : BaseWindow where T : EditorSettings<T>, new()
    {
        protected static T settings => EditorSettings<T>.instance;


        protected virtual void OnLostFocus()
        {
            settings.Serialize();
        }

    } // class SettingsWindow<T>

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR