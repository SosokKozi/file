using System;
using System.Collections.Generic;
using AshenCanvas.Sim.Items;

namespace AshenCanvas.Sim.Quests
{
    public enum Objective { KillBoss, KillCount }

    public enum QuestStatus { Locked, Available, Active, ReadyToTurnIn, Done }

    public sealed class QuestDef
    {
        public string Id, Title, Giver;
        public string Offer, Progress, Complete;   // реплики
        public Objective Objective;
        public string ZoneId;
        public int Count = 1;
        public string RequiresQuest;               // открывается после этого задания
        public string RequiresZone;                // открывается, когда эта зона доступна
        public bool AutoStart, AutoComplete;       // задание пролога не требует разговора
        public int RewardXp, RewardGold;
        public Rarity RewardRarity = Rarity.Normal;
        public bool RewardItem;
    }

    public static class QuestDb
    {
        public static readonly IReadOnlyList<QuestDef> All = new List<QuestDef>
        {
            new QuestDef {
                Id = "q_awakening", Title = "Пробуждение", Giver = null,
                Offer = "Выберитесь из часовни. Серый Глашатай стережёт выход.",
                Objective = Objective.KillBoss, ZoneId = World.ZoneDb.PrologueId,
                AutoStart = true, AutoComplete = true, RewardXp = 40, RewardGold = 20 },

            new QuestDef {
                Id = "q_marsh", Title = "Цвет из тины", Giver = World.HubLayout.Gouache, RequiresZone = "rot_marsh",
                Offer = "В топях засела Тинная Матрона. Она глотает краску, что течёт к лагерю. Остановите её, милая.",
                Progress = "Матрона всё ещё пьёт нашу краску.",
                Complete = "Вода снова зелёная! Держите — заслужили.",
                Objective = Objective.KillBoss, ZoneId = "rot_marsh",
                RewardXp = 150, RewardGold = 80, RewardItem = true, RewardRarity = Rarity.Rare },

            new QuestDef {
                Id = "q_forest_hunt", Title = "Очистить холст", Giver = World.HubLayout.Sanguine, RequiresZone = "faded_forest",
                Offer = "Лес заполонили наброски. Сотрите сорок штук — и я выкую вам кисть получше.",
                Progress = "Набросков всё ещё слишком много.",
                Complete = "Чисто. Вот кисть — держите крепче.",
                Objective = Objective.KillCount, ZoneId = "faded_forest", Count = 40,
                RewardXp = 300, RewardGold = 150, RewardItem = true, RewardRarity = Rarity.Rare },

            new QuestDef {
                Id = "q_ink", Title = "Пролитые чернила", Giver = World.HubLayout.Gouache, RequiresQuest = "q_marsh", RequiresZone = "ink_catacombs",
                Offer = "Под землёй Великий Ластик стирает всё подряд. Даже воспоминания. Найдите его.",
                Progress = "Я начинаю забывать, какого цвета небо.",
                Complete = "Небо голубое! Я вспомнила. Это вам — от всего лагеря.",
                Objective = Objective.KillBoss, ZoneId = "ink_catacombs",
                RewardXp = 600, RewardGold = 300, RewardItem = true, RewardRarity = Rarity.Unique },
        };

        public static QuestDef Get(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id) return All[i];
            throw new ArgumentException("Нет задания " + id);
        }
    }
}
