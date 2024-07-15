using cfg;
using UnityEngine;

namespace HotAssembly
{
    public class PieceEntity
    {
        protected TriggerManager triggerManager;
        protected BuffManager buffManager;
        protected PieceModel pieceModel;
        protected PieceSkill pieceSkill;
        protected PieceAttr pieceAttr;
        protected PieceMove pieceMove;

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
            triggerManager = new TriggerManager();
            buffManager = new BuffManager();
            pieceModel = new PieceModel();
            var scene = SceneManager.Instance.GetScene(SceneType.BattleScene) as BattleScene;
            var parent = scene.GetTransform(config.PieceType.ToString());
            pieceModel.Init(config.ModelPath, parent, pos);
            pieceSkill = new PieceSkill();
            pieceSkill.Init(this, config.Skills);
            pieceAttr = new PieceAttr();
            foreach (var item in config.Attrs) pieceAttr.SetAttr(item.Key, item.Value);
            pieceMove = new PieceMove();
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
            triggerManager = null;
            buffManager = null;
            pieceModel.Clear();
            pieceModel = null;
            pieceSkill.Clear();
            pieceSkill = null;
            pieceAttr = null;
            pieceMove = null;
            target = null;
        }
    }
}