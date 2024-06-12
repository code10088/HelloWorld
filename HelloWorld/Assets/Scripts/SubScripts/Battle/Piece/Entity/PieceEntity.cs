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
    public partial class PieceEntity
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
        protected Vector3 pos;
        protected PieceEntity target;

        public int AllyId => allyId;
        public int ItemId => itemId;
        public Vector3 Pos => pos;
        public PieceEntity Target => target;
        public PieceAttr PieceAttr => pieceAttr;

        public virtual void Init(int id)
        {
            itemId = id;
        }
        public virtual bool Update(float t) 
        {
            return false;
        }
    }
}