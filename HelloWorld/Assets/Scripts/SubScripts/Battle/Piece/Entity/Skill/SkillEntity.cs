using cfg;

namespace HotAssembly
{
    public class SkillEntity : PieceEntity
    {
        protected SkillConfig config;
        protected PieceEntity piece;
        protected float timer = 0;

        public virtual void Init(SkillConfig config, PieceEntity piece)
        {
            this.config = config;
            this.piece = piece;
            if (pieceAttr == null) pieceAttr = new PieceAttr();
            pieceAttr = piece.PieceAttr.CopyAttr();
            target = piece.Target;
        }
        public override void Clear()
        {
            base.Clear();
            pieceAttr.Clear();
            config = null;
            piece = null;
            target = null;
            timer = 0;
        }
        public override bool Update(float t)
        {
            base.Update(t);
            timer += t;
            if (config.Duration > 0 && timer >= config.Duration) return true;
            if (timer <= config.Delay) return false;
            PlaySkill();
            return config.Duration == 0;
        }
        protected virtual void PlaySkill()
        {
            
        }
    }
}