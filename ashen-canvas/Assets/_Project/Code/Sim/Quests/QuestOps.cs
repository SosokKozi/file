using System.Collections.Generic;
using AshenCanvas.Sim.Core;
using AshenCanvas.Sim.Items;
using AshenCanvas.Sim.Progression;
using AshenCanvas.Sim.Run;

namespace AshenCanvas.Sim.Quests
{
    public sealed class QuestReward
    {
        public QuestDef Quest;
        public int Xp, Gold, LevelsGained;
        public Item Item;
    }

    public static class QuestOps
    {
        public static QuestState StateOf(GameState gs, string id)
        {
            foreach (var q in gs.quests) if (q.id == id) return q;
            var s = new QuestState { id = id, status = (int)QuestStatus.Locked };
            gs.quests.Add(s);
            return s;
        }

        public static QuestStatus Status(GameState gs, string id) => (QuestStatus)StateOf(gs, id).status;

        /// <summary>Открывает задания, чьи условия выполнены. Звать после любых изменений мира.</summary>
        public static void Refresh(GameState gs)
        {
            foreach (var def in QuestDb.All)
            {
                var s = StateOf(gs, def.Id);
                if (s.status != (int)QuestStatus.Locked) continue;
                if (def.RequiresQuest != null && Status(gs, def.RequiresQuest) != QuestStatus.Done) continue;
                if (def.RequiresZone != null && !gs.IsUnlocked(def.RequiresZone)) continue;
                if (def.AutoStart && gs.IsUnlocked(def.ZoneId)) s.status = (int)QuestStatus.Active;
                else if (!def.AutoStart) s.status = (int)QuestStatus.Available;
            }
        }

        public static bool Accept(GameState gs, string id)
        {
            var s = StateOf(gs, id);
            if (s.status != (int)QuestStatus.Available) return false;
            s.status = (int)QuestStatus.Active;
            s.progress = 0;
            return true;
        }

        /// <summary>Отмечает убийство. Задания с автозавершением сразу выдают награду.</summary>
        public static List<QuestReward> OnKill(GameState gs, string zoneId, bool boss, Rng rng)
        {
            var done = new List<QuestReward>();
            foreach (var def in QuestDb.All)
            {
                var s = StateOf(gs, def.Id);
                if (s.status != (int)QuestStatus.Active || def.ZoneId != zoneId) continue;
                bool complete = false;
                if (def.Objective == Objective.KillBoss && boss) complete = true;
                if (def.Objective == Objective.KillCount && ++s.progress >= def.Count) complete = true;
                if (!complete) continue;
                s.status = (int)QuestStatus.ReadyToTurnIn;
                if (def.AutoComplete) done.Add(TurnIn(gs, def.Id, rng));
            }
            return done;
        }

        public static QuestReward TurnIn(GameState gs, string id, Rng rng)
        {
            var s = StateOf(gs, id);
            if (s.status != (int)QuestStatus.ReadyToTurnIn) return null;
            var def = QuestDb.Get(id);
            s.status = (int)QuestStatus.Done;
            var reward = new QuestReward { Quest = def, Xp = def.RewardXp, Gold = def.RewardGold };
            gs.hero.gold += def.RewardGold;
            reward.LevelsGained = HeroOps.GainXp(gs.hero, def.RewardXp);
            if (def.RewardItem)
            {
                reward.Item = ItemGen.Generate(rng, gs.hero.level + 1, def.RewardRarity);
                if (!HeroOps.AddToInventory(gs.hero, reward.Item)) reward.Item = null;
            }
            Refresh(gs);
            return reward;
        }

        /// <summary>Что NPC может сказать прямо сейчас: задания, которые он выдаёт или принимает.</summary>
        public static List<QuestDef> ForGiver(GameState gs, string giver)
        {
            var list = new List<QuestDef>();
            foreach (var def in QuestDb.All)
            {
                if (def.Giver != giver) continue;
                var st = Status(gs, def.Id);
                if (st == QuestStatus.Available || st == QuestStatus.Active || st == QuestStatus.ReadyToTurnIn) list.Add(def);
            }
            return list;
        }
    }
}
