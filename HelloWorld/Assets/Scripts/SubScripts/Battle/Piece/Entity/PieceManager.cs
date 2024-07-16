using System;
using System.Collections.Generic;
using UnityEngine;

namespace HotAssembly
{
    public class PieceManager : Singletion<PieceManager>, SingletionInterface
    {
        private int uniqueId = 0;
        private List<PieceEntity> pieces = new List<PieceEntity>(10000);
        private List<PieceEntity> cache = new List<PieceEntity>(10000);

        public void Init()
        {
            Updater.Instance.StartUpdate(Update);
        }

        public void AddSimplePiece(int simpleId, int allyId, Vector3 pos)
        {
            var config = ConfigManager.Instance.GameConfigs.TbSimplePieceConfig[simpleId];
            var entity = cache.Find(a => a.GetType() == typeof(PieceEntity));
            if (entity == null) entity = new PieceEntity();
            entity.Init(++uniqueId, allyId, config.PieceConfig, pos);
            pieces.Add(entity);
        }
        public void AddMonsterPiece(int monsterId, int allyId, Vector3 pos)
        {
            var config = ConfigManager.Instance.GameConfigs.TbMonsterConfig[monsterId];
            Type t = Type.GetType("HotAssembly.MonsterEntity_" + config.MonsterType);
            if (t == null) t = typeof(MonsterEntity);
            object obj = cache.Find(a => a.GetType() == t);
            if (obj == null) obj = Activator.CreateInstance(t);
            MonsterEntity entity = obj as MonsterEntity;
            entity.Init(++uniqueId, allyId, config.PieceConfig, pos);
            entity.Init(config);
            pieces.Add(entity);
        }
        public void AddSkillPiece(int skillId, int allyId, Vector3 pos, PieceEntity piece)
        {
            var config = ConfigManager.Instance.GameConfigs.TbSkillConfig[skillId];
            Type t = Type.GetType("HotAssembly.SkillEntity_" + config.SkillType);
            if (t == null) t = typeof(SkillEntity);
            object obj = cache.Find(a => a.GetType() == t);
            if (obj == null) obj = Activator.CreateInstance(t);
            SkillEntity entity = obj as SkillEntity;
            entity.Init(++uniqueId, allyId, config.PieceConfig, pos);
            entity.Init(config, piece);
            pieces.Add(entity);
        }
        public PieceEntity GetPiece(int id)
        {
            var result = pieces.Find(a => a.ItemId == id);
            return result;
        }
        public T GetPiece<T>(int id) where T : PieceEntity
        {
            var result = GetPiece(id);
            if (result == null) return null;
            else return result as T;
        }
        public void Update()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var temp = pieces[i];
                if (temp.Update(Time.deltaTime))
                {
                    temp.Clear();
                    cache.Add(temp);
                    pieces.RemoveAt(i);
                    i--;
                }
            }
        }
        public PieceEntity FindNearArmyTarget(Vector3 pos, int allyId, List<int> exclude = null)
        {
            float dis = 10000;
            PieceEntity target = null;
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.AllyId == allyId) continue;
                if (exclude != null && exclude.Contains(p.ItemId)) continue;
                float f = Vector3.Distance(pos, p.PieceModel.Pos);
                if (f > dis) continue;
                dis = f;
                target = p;
            }
            return target;
        }
        public PieceEntity[] FindNearArmyTarget(Vector3 pos, int allyId, int num)
        {
            float[] dis = new float[num];
            PieceEntity[] target = new PieceEntity[num];
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.AllyId == allyId) continue;
                float f = Vector3.Distance(pos, p.PieceModel.Pos);
                for (int j = 0; j < num; j++)
                {
                    if (dis[j] == 0 || dis[j] > f)
                    {
                        dis[j] = f;
                        target[j] = p;
                        break;
                    }
                }
            }
            return target;
        }
        public PieceEntity FindNearSelfTarget(Vector3 pos, int allyId, List<int> exclude = null)
        {
            float dis = 10000;
            PieceEntity target = null;
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.AllyId != allyId) continue;
                if (exclude != null && exclude.Contains(p.ItemId)) continue;
                float f = Vector3.Distance(pos, p.PieceModel.Pos);
                if (f > dis) continue;
                dis = f;
                target = p;
            }
            return target;
        }
    }
}