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

        public List<PieceSkillItem> Skills => skills;

        public void Init(PieceEntity piece, List<int> list)
        {
            this.piece = piece;
            for (int i = 0; i < list.Count; i++)
            {
                PieceSkillItem skillItem = new PieceSkillItem();
                skillItem.Init(piece, list[i]);
                skills.Add(skillItem);
            }
        }
        public PieceSkillState PlaySkill(int skillId)
        {
            var item = skills.Find(a => a.SkillId == skillId);
            if (item == null) return PieceSkillState.None;
            if (item.CooldownTimer > 0) return PieceSkillState.NoCooldown;
            if (item.NeedTarget && piece.Target == null) return PieceSkillState.NoTarget;
            if (item.AtkDis < Vector3.Distance(piece.PieceModel.Pos, piece.Target.PieceModel.Pos)) return PieceSkillState.NoDistance;
            if (skill != null && item.InterruptPriority < skill.BeInterruptPriority) return PieceSkillState.NoInterrupt;
            if (skill != null) skill.Reset();
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
        public void Update(float t)
        {
            for (int i = 0; i < skills.Count; i++) skills[i].Update(t);
        }
    }

    public class PieceSkillItem
    {
        protected PieceEntity piece;
        protected SkillConfig config;
        protected float cooldownTimer;
        private int timerId;

        public int SkillId => config.ID;
        public bool NeedTarget => config.NeedTarget;
        public float CooldownTimer => cooldownTimer;
        public float AtkDis => config.AtkDis;
        public float InterruptPriority => config.InterruptPriority;
        public float BeInterruptPriority => config.BeInterruptPriority;

        public void Init(PieceEntity piece, int skillId)
        {
            this.piece = piece;
            config = ConfigManager.Instance.GameConfigs.TbSkillConfig[skillId];
            cooldownTimer = 0;
        }
        public PieceSkillState PlaySkill()
        {
            piece.PieceModel.PlayAni("Skill");
            if (config.Anticipation == 0) _PlaySkill();
            else timerId = TimeManager.Instance.StartTimer(config.Anticipation, finish: _PlaySkill);
            return PieceSkillState.Success;
        }
        private void _PlaySkill()
        {
            PieceManager.Instance.AddSkillPiece(SkillId, piece.PieceModel.Pos, piece);
        }
        public void Update(float t)
        {
            cooldownTimer -= t;
            cooldownTimer = Mathf.Max(0, cooldownTimer);
        }
        public void Reset()
        {
            cooldownTimer = config.Cooldown;
            TimeManager.Instance.StopTimer(timerId);
            timerId = -1;
        }
    }
}