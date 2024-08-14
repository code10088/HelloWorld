using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class UIGuide : UIBase
    {
        private UIGuideComponent component = new UIGuideComponent();
        private Guide cfg;
        private int updateId = -1;
        private InputControl input = new InputControl();

        private Vector3 record = Vector3.zero;
        private Vector3[] corners = new Vector3[4];
        private Vector3 v1 = Vector3.zero;
        private Vector3 v2 = Vector3.zero;
        private Vector3 v3 = Vector3.zero;
        private Vector3 v4 = Vector3.zero;
        private Material mat = null;

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.maskRectTransform.anchorMin = UIManager.anchorMinFull;
            component.maskUIButton.onClick.AddListener(OnClickMask);
            component.skipBtnUIButton.onClick.AddListener(OnClickSkip);

            mat = component.maskImage.material;
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            EventManager.Instance.RegisterEvent(EventType.RefreshGuide, Refresh);

            updateId = Updater.Instance.StartUpdate(Update);
            Refresh(null);
        }
        public override void OnDisable()
        {
            base.OnDisable();
            EventManager.Instance.UnRegisterEvent(EventType.RefreshGuide, Refresh);

            Updater.Instance.StopUpdate(updateId);
        }

        private void Refresh(object param)
        {
            cfg = DataManager.Instance.GuideData.GuideCfg;
            //TODO:ÆÁ±Î3DÊäÈë
            //cfg.Is3D

            component.skipBtnObj.SetActive(cfg.CanSkip > 0);
            Update();
        }
        private void Update()
        {
            var t = DataManager.Instance.GuideData.GetGuideT();
            Vector3 p = t == null ? Vector3.back : t.position;
            if (p == record) return;

            record = p;
            if (t == null || cfg.MaskType == 0) mat.SetColor("_Color", new Color(0, 0, 0, 0));
            else mat.SetColor("_Color", new Color(0, 0, 0, 0.6f));
            if (cfg.MaskType == 1) mat.EnableKeyword("CIRCLE");
            else mat.DisableKeyword("CIRCLE");

            component.maskRectTransform.GetWorldCorners(corners);
            v1 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[0]);
            v2 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[2]);
            v3 = UIManager.Instance.UICamera.WorldToScreenPoint(new Vector3(p.x - cfg.Width / 2, p.y - cfg.Height / 2, p.z));
            v4 = UIManager.Instance.UICamera.WorldToScreenPoint(new Vector3(p.x + cfg.Width / 2, p.y + cfg.Height / 2, p.z));
            mat.SetVector("_Center", (v3 + v4) / 2 - (v1 + v2) / 2);
            mat.SetFloat("_Width", (v4.x - v3.x) / 2);
            mat.SetFloat("_Height", (v4.y - v3.y) / 2);
        }
        private void OnClickMask()
        {
            if (cfg.MaskType == 0)
            {
                CheckClick();
                Next();
                return;
            }
            if (cfg.ClickRange == 0)
            {
                CheckClick();
                Next();
                return;
            }
            input.Update();
            if (input.MousePos.x < v3.x) return;
            if (input.MousePos.x > v4.x) return;
            if (input.MousePos.y < v3.y) return;
            if (input.MousePos.y > v4.y) return;
            CheckClick();
            Next();
        }
        private void CheckClick()
        {
            if (!cfg.Interactable) return;
            component.maskImage.raycastTarget = false;
            InputSystemUIInputModule.Click(input.MousePos);
            component.maskImage.raycastTarget = true;
        }
        private void Next()
        {
            DataManager.Instance.GuideData.Next();
        }
        private void OnClickSkip()
        {
            DataManager.Instance.GuideData.Skip();
        }
    }
}