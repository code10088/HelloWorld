using HotAssembly;
using System.Collections.Generic;

namespace HotAssembly
{
    public class PieceManager : Singletion<PieceManager>
    {
        private List<PieceEntity> pieces = new List<PieceEntity>();
    }
}