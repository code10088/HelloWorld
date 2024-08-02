namespace HotAssembly
{
    public enum MoveMode
    {
        Dir = 1,
        Target = 2,
    }
    public class PieceMove
    {
        protected PieceEntity piece;

        public virtual void Init(PieceEntity piece)
        {
            this.piece = piece;
        }
    }
}