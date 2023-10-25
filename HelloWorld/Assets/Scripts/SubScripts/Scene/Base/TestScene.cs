using Cysharp.Threading.Tasks;

namespace HotAssembly
{
    public class TestScene : SceneBase
    {
        private TestSceneComponent component = new TestSceneComponent();
        protected override void Init()
        {
            component.Init(SceneObj);
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            GameDebug.Log("TestScene OnEnable");
        }
        public override void OnDisable()
        {
            base.OnDisable();
            GameDebug.Log("TestScene OnDisable");
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            GameDebug.Log("TestScene OnDestroy");
        }
    }
}