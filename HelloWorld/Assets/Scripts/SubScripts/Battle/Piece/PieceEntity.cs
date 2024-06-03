using UnityEngine;

namespace HotAssembly
{
    public class PieceEntity
    {
        private static int uniqueId = 0;

        protected int id;
        protected TriggerManager triggerManager;
        protected BuffManager buffManager;
        protected PieceState pieceState;
        protected PieceModel pieceModel;
        protected PieceSkill pieceSkill;
        public PieceAttr pieceAttr;

    }
}