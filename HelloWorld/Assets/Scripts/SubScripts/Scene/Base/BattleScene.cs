using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class BattleScene : SceneBase
    {
        private BattleSceneComponent component = new BattleSceneComponent();
        private int coroutineId = -1;

        protected override void Init()
        {
            base.Init();
            component.Init(SceneObj);
        }
        public override void OnEnable(params object[] param)
        {
            base.OnEnable(param);
            //此时调用SceneManager.Instance.GetScene取不到当前scene的解决办法
            var enumerator = Start();
            coroutineId = CoroutineManager.Instance.StartCoroutine(enumerator);
        }
        public override void OnDisable()
        {
            base.OnDisable();
            CoroutineManager.Instance.Stop(coroutineId);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public Transform GetTransform(string path)
        {
            return SceneObj.transform.Find(path);
        }
        private IEnumerator<Coroutine> Start()
        {
            yield return new WaitForFrame(1);
            PieceManager.Instance.AddMonsterPiece(1, 0, Vector3.right * 2);
            PieceManager.Instance.AddMonsterPiece(1, 1, Vector3.left * 2);
        }
    }
}