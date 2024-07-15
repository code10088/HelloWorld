using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class PieceEntity
    {
        protected TriggerManager triggerManager = new TriggerManager();
        protected BuffManager buffManager = new BuffManager();
        protected PieceModel pieceModel = new PieceModel();
        protected PieceSkill pieceSkill = new PieceSkill();
        protected PieceAttr pieceAttr = new PieceAttr();
        protected PieceMove pieceMove = new PieceMove();

        protected int allyId;
        protected int teamId;
        protected int itemId;
        protected PieceEntity target;

        public int AllyId => allyId;
        public int ItemId => itemId;
        public PieceEntity Target => target;
        public PieceModel PieceModel => pieceModel;
        public PieceAttr PieceAttr => pieceAttr;

        public virtual void Init(int id, int allyId, PieceConfig config, Vector3 pos)
        {
            itemId = id;
            this.allyId = allyId;
            var scene = SceneManager.Instance.GetScene(SceneType.BattleScene) as BattleScene;
            var parent = scene.GetTransform(config.PieceType.ToString());
            pieceModel.Init(config.ModelPath, parent, pos);
            pieceSkill.Init(this, config.Skills);
            foreach (var item in config.Attrs) pieceAttr.SetAttr(item.Key, item.Value);
            pieceMove.Init(this);
            pieceMove.SetV(config.Speed);
            pieceMove.SetW(config.AngleSpeed);
        }
        public virtual bool Update(float t)
        {
            triggerManager.Update(t);
            buffManager.Update(t);
            pieceSkill.Update(t);
            pieceMove.Update(t);
            return false;
        }
        public virtual void Clear()
        {
            triggerManager.Clear();
            buffManager.Clear();
            pieceModel.Clear();
            pieceSkill.Clear();
            pieceAttr.Clear();
            pieceMove.Stop();
            target = null;
        }
    }
}