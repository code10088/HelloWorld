using UnityEngine;

namespace MainAssembly
{
    public class MonoSingletion<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static string MonoSingletionName = "MonoSingletion";
        private static GameObject MonoSingletionRoot;
        private static T instance;

        public static T Instance
        {
            get
            {
                if (MonoSingletionRoot == null)
                {
                    MonoSingletionRoot = new GameObject(MonoSingletionName);
                    DontDestroyOnLoad(MonoSingletionRoot);
                }
                if (instance == null)
                {
                    instance = MonoSingletionRoot.AddComponent<T>();
                }
                return instance;
            }
        }
    }

    public abstract class Singletion<T> where T : new()
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null) instance = new T();
                return instance;
            }
        }
    }
}