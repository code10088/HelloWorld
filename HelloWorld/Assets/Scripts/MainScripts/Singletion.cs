using UnityEngine;

public abstract class Singletion<T> where T : new()
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
                if (instance is SingletionInterface) ((SingletionInterface)instance).Init();
            }
            return instance;
        }
    }
}
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T instance = null;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = new GameObject(typeof(T).Name);
                instance = obj.AddComponent<T>();
                if (instance is SingletionInterface) ((SingletionInterface)instance).Init();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }
}
public interface SingletionInterface
{
    public void Init();
}