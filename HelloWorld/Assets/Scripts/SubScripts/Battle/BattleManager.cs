using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class BattleManager : Singletion<BattleManager>
    {
        private int updateId = -1;
        private int sceneId = -1;
        private BattleScene battleScene;

        public BattleScene BattleScene => battleScene;
        public Vector2 InputWorldPos => battleScene.ScreenToWorldPoint(Input.mousePosition);

        public void Init(SceneType type)
        {
            sceneId = SceneManager.Instance.OpenScene(type, Init);
        }
        private void Init(int id, bool success)
        {
            battleScene = SceneManager.Instance.GetScene(id) as BattleScene;
            updateId = Updater.Instance.StartUpdate(Update);
        }
        public void Exit()
        {
            Updater.Instance.StopUpdate(updateId);
            SceneManager.Instance.CloseScene(sceneId);
            battleScene = null;
            sceneId = -1;
        }
        private void Update()
        {
            FightManager.Instance.Update();
            SkillManager.Instance.Update();
        }
    }
}
