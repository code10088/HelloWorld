using cfg;
using TMPro;
using UnityEngine;
using UnityExtensions.Tween;

namespace HotAssembly
{
    public class DamageNumEntity : Entity
    {
        private Vector3 pos;
        private string content;

        private int loadId = -1;
        private TextMeshPro tmp;
        private TweenPlayer tp;

        public void Init(DamageNumConfig config, string content, Vector3 pos)
        {
            this.pos = pos;
            this.content = content;
            var parent = BattleManager.Instance.BattleScene.GetTransform("DamageNum");
            if (loadId < 0) loadId = DamageNumManager.Instance.Dequeue(config.PrefabPath, parent, LoadFinish);
            else Enable();
        }
        public override void Clear()
        {
            base.Clear();
            content = null;
        }


        private void LoadFinish(int itemId, GameObject obj, object[] param)
        {
            obj.transform.position = pos;
            tmp = obj.transform.GetChild(0).GetComponent<TextMeshPro>();
            tp = obj.GetComponent<TweenPlayer>();
            tp.onForwardArrived += Disable;
            Enable();
        }
        private void Enable()
        {
            tmp.SetText(content);
            tp.SetForwardDirectionAndEnabled();
        }
        private void Disable()
        {
            Clear();
            DamageNumManager.Instance.Remove(this);
        }
    }
}