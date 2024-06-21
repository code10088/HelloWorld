using UnityEngine;

namespace HotAssembly
{
    public enum PieceType
    {
        None,
        Player,
        Monster,
        Skill,
    }
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
        protected PieceType pieceType;
        protected PieceEntity target;

        public int AllyId => allyId;
        public int ItemId => itemId;
        public PieceEntity Target => target;
        public PieceModel PieceModel => pieceModel;
        public PieceAttr PieceAttr => pieceAttr;

        public virtual void Init(int id)
        {
            itemId = id;
        }
        public virtual bool Update(float t) 
        {
            buffManager.Update();
            pieceMove.Update(t);
            return false;
        }
    }
}