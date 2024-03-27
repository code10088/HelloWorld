using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class UIGuide : UIBase
    {
        private UIGuideComponent component = new UIGuideComponent();
        private Guide cfg;
        private int updateId = -1;

        private Vector3 record = Vector3.zero;
        private Mesh mesh = null;

        protected override void Init()
        {
            base.Init();
            component.Init(UIObj);
            component.maskRectTransform.anchorMin = UIManager.anchorMinFull;
            component.maskUIButton.onClick.AddListener(OnClickMask);
            component.skipBtnUIButton.onClick.AddListener(OnClickSkip);

            mesh = new Mesh();
            mesh.vertices = new Vector3[8];
            mesh.uv = new Vector2[8];
            mesh.triangles = new int[24];
            component.maskMeshFilter.mesh = mesh;
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
        }
        private void Update()
        {
            var t = DataManager.Instance.GuideData.GetGuideT();
            Vector3 p = t == null ? component.maskRectTransform.position : t.position;
            if (p == record) return;

            record = p;
            float w = t == null ? 0.1f : cfg.Width / 2;
            float h = t == null ? 0.1f : cfg.Height / 2;
            Material mat = component.maskMeshRenderer.sharedMaterial;
            if (cfg.MaskType == 0) mat.SetColor("_Color", new Color(1,1,1,0));
            else mat.SetColor("_Color", new Color(1, 1, 1, 1));
            if (cfg.MaskType == 1) mat.EnableKeyword("CIRCLE");
            else mat.DisableKeyword("CIRCLE");
            mat.SetVector("_Center", p);
            mat.SetFloat("_Width", w);
            mat.SetFloat("_Height", h);

            var corners = new Vector3[4];
            component.maskRectTransform.GetWorldCorners(corners);
            Vector3 a0 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[0]);
            Vector3 a1 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[1]);
            Vector3 a2 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[2]);
            Vector3 a3 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[3]);
            corners[0] = new Vector3(p.x - w, p.y - h, p.z);
            corners[1] = new Vector3(p.x + w, p.y - h, p.z);
            corners[2] = new Vector3(p.x + w, p.y + h, p.z);
            corners[3] = new Vector3(p.x - w, p.y + h, p.z);
            Vector3 b0 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[0]);
            Vector3 b1 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[1]);
            Vector3 b2 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[2]);
            Vector3 b3 = UIManager.Instance.UICamera.WorldToScreenPoint(corners[3]);

            mesh.vertices[0] = a0;
            mesh.vertices[1] = a1;
            mesh.vertices[2] = a2;
            mesh.vertices[3] = a3;
            mesh.vertices[4] = b0;
            mesh.vertices[5] = b1;
            mesh.vertices[6] = b2;
            mesh.vertices[7] = b3;

            mesh.uv[0] = Vector2.zero;
            mesh.uv[1] = Vector2.right;
            mesh.uv[2] = Vector2.one;
            mesh.uv[3] = Vector2.up;
            float _w = a1.x - a0.x;
            float _h = a3.y - a0.y;
            mesh.uv[4] = new Vector2((b0.x - a0.x) / _w, (b0.y - a0.y) / _h);
            mesh.uv[5] = new Vector2((b1.x - a0.x) / _w, (b1.y - a0.y) / _h);
            mesh.uv[6] = new Vector2((b2.x - a0.x) / _w, (b2.y - a0.y) / _h);
            mesh.uv[7] = new Vector2((b3.x - a0.x) / _w, (b3.y - a0.y) / _h);

            mesh.triangles[0] = 0;
            mesh.triangles[1] = 4;
            mesh.triangles[2] = 1;
            mesh.triangles[3] = 1;
            mesh.triangles[4] = 4;
            mesh.triangles[5] = 5;
            mesh.triangles[6] = 1;
            mesh.triangles[7] = 5;
            mesh.triangles[8] = 2;
            mesh.triangles[9] = 2;
            mesh.triangles[10] = 5;
            mesh.triangles[11] = 6;
            mesh.triangles[12] = 2;
            mesh.triangles[13] = 6;
            mesh.triangles[14] = 3;
            mesh.triangles[15] = 3;
            mesh.triangles[16] = 6;
            mesh.triangles[17] = 7;
            mesh.triangles[18] = 3;
            mesh.triangles[19] = 7;
            mesh.triangles[20] = 0;
            mesh.triangles[21] = 0;
            mesh.triangles[22] = 7;
            mesh.triangles[23] = 4;
        }
        private void Next()
        {
            DataManager.Instance.GuideData.Next();
        }
        private void OnClickMask()
        {
            if (cfg.MaskType > 0) return;
            if (cfg.ClickRange > 0) return;
            Next();
        }
        private void OnClickSkip()
        {
            DataManager.Instance.GuideData.Skip();
        }
    }
}