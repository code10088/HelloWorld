namespace HotAssembly
{
    public class Entity
    {
        private static int uniqueId = 0;
        protected int itemId;

        public int ItemId => itemId;
        public bool Active => itemId > 0;

        public void Init()
        {
            itemId = ++uniqueId;
        }
        public virtual void Clear()
        {
            itemId = -1;
        }
    }
}