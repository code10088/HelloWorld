using cfg;
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

        public void Init(PieceEntity piece)
        {
            this.piece = piece;
        }
        public PieceSkillState PlaySkill(int skillId)
        {
            var item = skills.Find(a => a.SkillId == skillId);
            if (item == null) return PieceSkillState.None;
            if (!item.Cooldown) return PieceSkillState.NoCooldown;
            if (item.NeedTarget && piece.Target == null) return PieceSkillState.NoTarget;
            if (item.AtkDis < Vector3.Distance(piece.Pos, piece.Target.Pos)) return PieceSkillState.NoDistance;
            if (skill != null && !skill.Interval && item.InterruptPriority < skill.BeInterruptPriority) return PieceSkillState.NoInterrupt;
            PieceSkillState result = item.PlaySkill();
            if (result == PieceSkillState.Success) skill = item;
            return result;
        }
        public PieceSkillState AutoPlaySkill()
        {
            for (int i = 0; i < skills.Count; i++)
            {
                var state = PlaySkill(skills[i].SkillId);
                if (state == PieceSkillState.Success) return PieceSkillState.Success;
            }
            return PieceSkillState.None;
        }
    }

    public class PieceSkillItem
    {
        protected PieceEntity piece;
        protected SkillConfig config;
        protected float cooldownTimer;
        protected float intervalTimer;
        private int timerId;

        public int SkillId => config.ID;
        public bool NeedTarget => config.NeedTarget;
        public bool Cooldown => cooldownTimer == 0;
        public bool Interval => intervalTimer == 0;
        public float AtkDis => config.AtkDis;
        public float InterruptPriority => config.InterruptPriority;
        public float BeInterruptPriority => config.BeInterruptPriority;

        public void Init(PieceEntity piece)
        {
            this.piece = piece;
        }
        public PieceSkillState PlaySkill()
        {
            piece.PlayAni("Skill");
            if (config.Delay == 0) Trigger();
            else timerId = TimeManager.Instance.StartTimer(config.Delay, finish: Trigger);
            return PieceSkillState.Success;
        }
        private void Trigger()
        {
            var attrs = piece.PieceAttr.CopyAttr();
            PieceManager.Instance.AddSkillPiece(attrs);
        }
        public void Clear()
        {
            TimeManager.Instance.StopTimer(timerId);
        }
    }

}