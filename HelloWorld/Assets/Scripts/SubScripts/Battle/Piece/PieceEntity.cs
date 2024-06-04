using UnityEngine;

namespace HotAssembly
{
    public class PieceEntity
    {
        private static int uniqueId = 0;

        protected int allyId;
        protected int teamId;
        protected int itemId;
        protected Vector3 pos;
        protected TriggerManager triggerManager;
        protected BuffManager buffManager;
        protected PieceModel pieceModel;
        protected PieceSkill pieceSkill;
        public PieceAttr pieceAttr;

        public int AllyId => allyId;
        public int ItemId => itemId;
        public Vector3 Pos => pos;

        public virtual void Init()
        {

        }
        public virtual bool Update(float t) 
        {
            return false;
        }
    }
}