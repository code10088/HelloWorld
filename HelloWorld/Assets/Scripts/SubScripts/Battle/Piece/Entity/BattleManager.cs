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

        public void Init()
        {
            sceneId = SceneManager.Instance.OpenScene(SceneType.BattleScene, Init);
        }
        private void Init(bool success)
        {
            battleScene = SceneManager.Instance.GetScene(sceneId) as BattleScene;
            updateId = Updater.Instance.StartUpdate(Update);
            input = new BattleInput();

            FightManager.Instance.AddMonster(1, 0, Vector3.right * 2);
            FightManager.Instance.AddMonster(1, 1, Vector3.left * 2);
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
