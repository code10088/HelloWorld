using UnityEngine;

namespace HotAssembly
{
    public enum MonsterState
    {
        None,
        Enter,
        Idle,
        Moderate,
        Frozen,
        Vertigo,
    }
    public class MonsterEntity : PieceEntity
    {
        protected MonsterState monsterState = MonsterState.None;
        private MonsterAni pieceAni;

        public override void Init()
        {
            base.Init();
            ChangeState(MonsterState.Enter);
        }
        public override bool Update(float t)
        {
            return base.Update(t);
        }
        public void ChangeState(MonsterState state)
        {
            switch (monsterState)
            {
                //�˳�
                case MonsterState.Enter:
                    break;
            }
            var record = monsterState;
            monsterState = state;
            switch (monsterState)
            {
                //����
                case MonsterState.Enter:
                    pieceAni.PlayAni(MonsterAniEnum.Enter);
                    break;
            }
        }
    }
}