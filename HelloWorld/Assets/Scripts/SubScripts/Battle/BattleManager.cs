using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class BattleManager : Singletion<BattleManager>
    {
        private int updateId = -1;
        private int sceneId = -1;
        private BattleScene battleScene;
        private BattleInput input;

        public BattleScene BattleScene => battleScene;
        public Vector2 InputPos => input.MousePos;
        public Vector2 InputWorldPos => battleScene.ScreenToWorldPoint(input.MousePos);

        public void Init(SceneType type)
        {
            sceneId = SceneManager.Instance.OpenScene(type, Init);
        }
        private void Init(int id, bool success)
        {
            battleScene = SceneManager.Instance.GetScene(id) as BattleScene;
            updateId = Updater.Instance.StartUpdate(Update);
            input = new BattleInput();
        }
        public void Exit()
        {
            Updater.Instance.StopUpdate(updateId);
            SceneManager.Instance.CloseScene(sceneId);
            battleScene = null;
            sceneId = -1;
            input = null;
        }
        private void Update()
        {
            input.Update();
            FightManager.Instance.Update();
            SkillManager.Instance.Update();
        }
    }
}
