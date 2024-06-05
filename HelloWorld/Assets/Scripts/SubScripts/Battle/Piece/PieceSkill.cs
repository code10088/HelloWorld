using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public enum PieceSkillState
    {
        None,
        Success,
        NoCooldown,
        NoTarget,
        NoDistance,
        NoInterrupt,
    }
    public class PieceSkill
    {
        private List<PieceSkillItem> skills = new List<PieceSkillItem>();
        private PieceSkillItem skill;
        private PieceEntity piece;

        public float AtkDis => skill.atkDis;

        public PieceSkillState PlaySkill(int skillId)
        {
            var item = skills.Find(a => a.skillId == skillId);
            if (item == null) return PieceSkillState.None;
            if (!item.Cooldown) return PieceSkillState.NoCooldown;
            if (item.needTarget && piece.Target == null) return PieceSkillState.NoTarget;
            if (item.atkDis < Vector3.Distance(piece.Pos, piece.Target.Pos)) return PieceSkillState.NoDistance;
            if (skill != null && !skill.Interval && item.interruptPriority < skill.beInterruptPriority) return PieceSkillState.NoInterrupt;
            return item.PlaySkill();
        }
        public PieceSkillState AutoPlaySkill()
        {
            for (int i = 0; i < skills.Count; i++)
            {
                var state = PlaySkill(skills[i].skillId);
                if (state == PieceSkillState.Success) return PieceSkillState.Success;
            }
            return PieceSkillState.None;
        }
    }
    public class PieceSkillItem
    {
        public int skillId;
        public bool needTarget;
        public float cooldown;//冷却时间
        public float interval;//攻击间隔
        public float delay;//前摇
        public float atkDis;
        public int interruptPriority;//打断优先级
        public int beInterruptPriority;//被打断优先级

        private float cooldownTimer;
        private float intervalTimer;

        public bool Cooldown => cooldownTimer == 0;
        public bool Interval => intervalTimer == 0;

        public PieceSkillState PlaySkill()
        {
            return PieceSkillState.Success;
        }
    }
}