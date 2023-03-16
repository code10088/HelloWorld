using MainAssembly;

namespace SubAssembly
{
    public class GameStart : MonoSingletion<GameStart>
    {
        public void Init()
        {
            ConfigManager.Instance.Init(EnterMainScene);
        }
        private void EnterMainScene()
        {

        }
    }
}