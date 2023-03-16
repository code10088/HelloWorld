namespace MainAssembly
{
    public class ConfigManager : Singletion<ConfigManager>
    {
        public delegate dynamic ConfigAction1(string name, int id);
        public delegate dynamic ConfigAction2(int id);
        private ConfigAction1 getConfigAction;
        private ConfigAction2 getUIConfigAction;

        public ConfigAction1 GetConfigAction { set { getConfigAction = value; } }
        public ConfigAction2 GetUIConfigAction { set { getUIConfigAction = value; } }
        /// <summary>
        /// ½÷É÷Ê¹ÓÃ
        /// </summary>
        internal dynamic GetConfig(string name, int id)
        {
            return getConfigAction?.Invoke(name, id);
        }
        internal dynamic GetUIConfig(int id)
        {
            return getUIConfigAction?.Invoke(id);
        }
    }
}