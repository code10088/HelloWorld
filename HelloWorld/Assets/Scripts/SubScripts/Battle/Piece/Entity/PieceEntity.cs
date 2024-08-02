namespace HotAssembly
{
    public class PieceEntity : Entity
    {
        protected TriggerManager triggerManager;
        protected BuffManager buffManager;
        protected PieceModel pieceModel;
        protected PieceSkill pieceSkill;
        protected PieceAttr pieceAttr;
        protected PieceMove pieceMove;

        protected int allyId;
        protected int teamId;
        protected PieceEntity target;

        public TriggerManager TriggerManager => triggerManager;
        public BuffManager BuffManager => buffManager;
        public PieceModel PieceModel => pieceModel;
        public PieceSkill PieceSkill => pieceSkill;
        public PieceAttr PieceAttr => pieceAttr;
        public PieceMove PieceMove => pieceMove;
        public int AllyId => allyId;
        public PieceEntity Target => target;

        public virtual void Init(int allyId)
        {
            this.allyId = allyId;
        }
        public virtual bool Update(float t)
        {
            return false;
        }
        public override void Clear()
        {

        }
    }
}