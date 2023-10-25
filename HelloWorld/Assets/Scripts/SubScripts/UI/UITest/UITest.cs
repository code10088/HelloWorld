using cfg;
using Cysharp.Threading.Tasks;
using SuperScrollView;
using UnityEngine;

namespace HotAssembly
{
    public class UITest : UIBase
    {
        private UITestComponent component = new UITestComponent();
        private UISubTest subUI = new UISubTest("UISubTest");
        private GameObjectPool<GameObjectPoolItem> pool = new GameObjectPool<GameObjectPoolItem>();

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.bgRectTransform.anchorMin = UIManager.anchorMinFull;
            component.closeBtnUIButton.onClick.AddListener(OnClose);
            component.openSubBtnUIButton.onClick.AddListener(OnOpenSub);
            component.openMsgBtnUIButton.onClick.AddListener(OnOpenMessage);
            component.openSDKBtnUIButton.onClick.AddListener(SDKInit);
            component.loadSpriteUIButton.onClick.AddListener(LoadSprite);
            component.poolEnqueueUIButton.onClick.AddListener(LoadBulletFromPool);
            component.poolDequeueUIButton.onClick.AddListener(DelectBullet);
            component.openSceneUIButton.onClick.AddListener(OpenScene);
            component.closeSceneUIButton.onClick.AddListener(CloseScene);
            component.loopLoopListView2.InitListView(DataManager.Instance.TestData.testItemDatas.Count, OnGetItemByIndex);
            pool.Init($"{ZResConst.ResUIPrefabPath}TestBullet.prefab");
        }
        public override async UniTask OnEnable(params object[] param)
        {
            await base.OnEnable(param);
            GameDebug.Log("UITest OnEnable");
            await UniTask.Delay(1000);
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
            pool.Release();
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
            SetSprite(component.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon");
            SetSprite(component.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon2");
            SetSprite(component.imageImage, ZResConst.ResUIAtlasTestPath, "TestIcon3");
        }
        private void LoadBulletFromPool()
        {
            pool.Dequeue((a, b) => b.transform.localScale = Vector3.one * Random.Range(0, 10));
            pool.Dequeue((a, b) => b.transform.localScale = Vector3.one * Random.Range(0, 10));
            pool.Enqueue(pool.Use[0]);
            pool.Dequeue((a, b) => b.transform.localScale = Vector3.one * Random.Range(0, 10));
        }
        private void DelectBullet()
        {
            pool.Enqueue(pool.Use[0]);
        }
        private void OpenScene()
        {
            SceneManager.Instance.OpenScene(SceneType.TestScene);
        }
        private void CloseScene()
        {
            SceneManager.Instance.CloseScene(SceneType.TestScene);
        }

        LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
        {
            if (index < 0 || index >= DataManager.Instance.TestData.testItemDatas.Count)
            {
                return null;
            }
            LoopListViewItem2 item = listView.NewListViewItem("Item");
            if (item.IsInitHandlerCalled == false)
            {
                item.IsInitHandlerCalled = true;
                item.ItemData = new UITestItem();
                item.ItemData.Init(item.gameObject);
            }
            item.ItemData.SetData(index);
            return item;
        }
    }
    public partial class UITestItem
    {
        public void SetData(int index)
        {
            var data = DataManager.Instance.TestData.testItemDatas[index];
            itemTextTextMeshProUGUI.text = data.name;
        }
    }
}