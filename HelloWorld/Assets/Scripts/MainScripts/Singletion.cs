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
public interface SingletionInterface
{
    public void Init();
}