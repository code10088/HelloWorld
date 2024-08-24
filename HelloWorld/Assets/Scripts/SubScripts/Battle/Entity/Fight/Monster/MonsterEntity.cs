using cfg;
using System;
using UnityEngine;

namespace HotAssembly
{
    [Flags]
    public enum MonsterState
    {
        None = 0,
        Enter = 1,
        Idle = 2,
        Attack = 4,
        Moderate = 8,
        Frozen = 16,
        Vertigo = 32,
        Dead = 64,
    }
    public class MonsterEntity : FightEntity, PlaySkillSystemInterface, StateSystemInterface
    {
        protected MonsterConfig config;
        protected MonsterState monsterState = MonsterState.Enter;
        protected PlaySkillComponent play;
        private StateComponent state;

        public virtual void Init(MonsterConfig config, Vector3 pos)
        {
            this.config = config;
            if (attr == null) attr = new AttrComponent();
            foreach (var item in config.FightConfig.Attrs) attr.SetAttr(item.Key, item.Value);
            if (obj == null) obj = new GameObjectComponent();
            var parent = BattleManager.Instance.BattleScene.GetTransform(config.FightConfig.FightType.ToString());
            obj.Init(config.FightConfig.ModelPath, parent, LoadFinish);
            if (transform == null) transform = new TransformComponent();
            transform.Init(obj);
            transform.SetPos(pos);
            if (ani == null) ani = new AniComponent();
            ani.Init(obj);
            if (play == null) play = new PlaySkillComponent();
            play.Init(this, config.FightConfig.Skills);
            SystemManager.Instance.PlaySkillSystem.AddEntity(this);
            if (state == null) state = new StateComponent();
            state.Init(this);
            SystemManager.Instance.StateSystem.AddEntity(this);
        }
        private void LoadFinish()
        {
            transform.SetPos();
            ani.Init(obj);
        }
        public override void Clear()
        {
            base.Clear();
            attr.Clear();
            obj.Clear();
            transform.Clear();
            ani.Clear();
            play.Clear();
            state.Clear();
            target = null;
            config = null;
            SystemManager.Instance.PlaySkillSystem.RemoveEntity(this);
            SystemManager.Instance.StateSystem.RemoveEntity(this);
            EntityCacheManager.Instance.FightCache.Remove(this);
        }
        public void UpdatePlaySkill(float t)
        {
            play.Update(t);
        }
        public void UpdateState(float t)
        {
            state.Update(t);
        }
        public virtual void UpdateState()
        {
            if (attr.GetAttr(AttrEnum.Hp) <= 0)
            {
                monsterState |= MonsterState.Dead;
            }
            if ((monsterState & MonsterState.Dead) == MonsterState.Dead)
            {
                Remove();
            }
            if ((monsterState & MonsterState.Enter) == MonsterState.Enter)
            {
                monsterState &= ~MonsterState.Enter;
                monsterState |= MonsterState.Idle;
            }
        }
    }
}