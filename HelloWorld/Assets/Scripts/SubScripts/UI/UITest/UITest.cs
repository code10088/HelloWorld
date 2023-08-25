using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        private UITestComponent component = new UITestComponent();
        private CustomLoopScroll<TestItem> clss = new CustomLoopScroll<TestItem>();
        private UISubTest subUI = new UISubTest("UISubTest");
        private GameObjectPool<TestBullet> pool = new GameObjectPool<TestBullet>();

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.closeBtnButton.onClick.AddListener(OnClose);
            component.openSubBtnButton.onClick.AddListener(OnOpenSub);
            component.openMsgBtnButton.onClick.AddListener(OnOpenMessage);
            component.openSDKBtnButton.onClick.AddListener(SDKInit);
            component.loadSpriteButton.onClick.AddListener(LoadSprite);
            component.poolEnqueueButton.onClick.AddListener(LoadBulletFromPool);
            component.poolDequeueButton.onClick.AddListener(DelectBullet);
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            GameDebug.Log("UITest OnEnable");
            await UniTask.Delay(1000);
            InitLoopScrollRect();
            pool.Init("TestBullet");
        }
        protected override void PlayInitAni()
        {
            base.PlayInitAni();
            GameDebug.Log("UITest OnPlayUIAnimation");
        }
        protected override void RefreshUILayer()
        {
            base.RefreshUILayer();
            GameDebug.Log("UITest RefreshUILayer");
        }
        public override void OnDisable()
        {
            base.OnDisable();
            subUI.Close();
            GameDebug.Log("UITest OnDisable");
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            subUI.Close(true);
            GameDebug.Log("UITest OnDestroy");
        }

        private void OnOpenMessage()
        {
            UIMessageBoxParam param = new UIMessageBoxParam();
            param.type = UIMessageBoxType.SureAndCancel;
            param.title = "Tips";
            param.content = "Content";
            param.sure = a => OnOpenMessage();
            UIManager.Instance.OpenMessageBox(param);
        }
        private void OnOpenSub()
        {
            if (subUI.Active) subUI.SetActive(component.subRootObj.transform, false);
            else subUI.Open(component.subRootObj.transform);
        }
        private void SDKInit()
        {
            SDKManager.Instance.InitSDK();
        }
        private void LoadSprite()
        {
            SetSprite(component.imageImage, "TestIcon");
            SetSprite(component.imageImage, "TestIcon2");
            SetSprite(component.imageImage, "TestIcon3");
        }
        private void LoadBulletFromPool()
        {
            pool.Dequeue();
        }
        private void DelectBullet()
        {
            pool.Enqueue(pool.Use[0]);
        }

        private void InitLoopScrollRect()
        {
            clss.Init(component.loopLoopVerticalScrollRect, component.itemObj, DataManager.Instance.TestData.testItemDatas.Count);
        }
        private class TestItem : CustomLoopItem
        {
            public TestData.TestItemData data;
            public UITestItem component = new UITestItem();
            public override void Init(GameObject _obj)
            {
                base.Init(_obj);
                component.Init(obj);
            }
            public override void SetData(int idx)
            {
                data = DataManager.Instance.TestData.testItemDatas[idx];
                component.itemTextTextMeshProUGUI.text = data.name;
            }
        }
        private class TestBullet : PoolItem
        {
            protected override void LoadFinish()
            {
                base.LoadFinish();
                obj.transform.localScale = Vector3.one * Random.Range(0, 10);
            }
        }
    }
}