using System;
using System.Collections.Generic;

namespace Kingdoms
{
    [Serializable]
    public sealed class CampaignHistoryEntry
    {
        public string runId;
        public int mission, raiders, archers, tanks, stars;
        public PracticeOutcome outcome;
        public bool abandoned, resolved;
        public long completedUtc;
        // Zero time and -1 statistics mean the old save never recorded these details.
        public int durationTicks = -1, destruction = -1;
        public int goldAwarded, elixirAwarded;
        public string replayJson;
    }

    public sealed partial class VillageState
    {
        public const int BattleHistoryLimit = 20;
        public List<CampaignHistoryEntry> battleHistory = new List<CampaignHistoryEntry>();

        CampaignHistoryEntry FindHistory(string runId) => battleHistory.Find(entry => entry.runId == runId);

        void RecordCampaignHistory(PracticeBattle battle = null, bool abandoned = false, bool legacy = false)
        {
            if (FindHistory(campaignRunId) != null) return;
            battleHistory.Insert(0, new CampaignHistoryEntry {
                runId = campaignRunId, mission = campaignMission,
                raiders = campaignArmy - campaignArchers - campaignTanks,
                archers = campaignArchers, tanks = campaignTanks,
                outcome = campaignOutcome, stars = campaignStars,
                abandoned = abandoned, resolved = abandoned,
                completedUtc = legacy ? 0 : Now,
                durationTicks = battle == null ? -1 : battle.Tick,
                destruction = battle == null ? -1 : battle.Destruction,
                replayJson = battle == null ? null : SavedBattleReplay.Encode(battle.Record())
            });
            if (battleHistory.Count > BattleHistoryLimit) battleHistory.RemoveAt(battleHistory.Count - 1);
        }

        void MigrateBattleHistory()
        {
            battleHistory = new List<CampaignHistoryEntry>();
            if (HasCampaignRun && campaignOutcome != PracticeOutcome.Running) RecordCampaignHistory(legacy: true);
        }

        public bool IsBattleHistoryValid()
        {
            if (battleHistory == null || battleHistory.Count > BattleHistoryLimit) return false;
            var ids = new HashSet<string>();
            foreach (var entry in battleHistory)
            {
                if (entry == null || !Guid.TryParseExact(entry.runId, "N", out _) || !ids.Add(entry.runId)) return false;
                var mission = CampaignCatalog.Find(entry.mission);
                if (mission == null || !CampaignUnlocked(entry.mission) || entry.completedUtc < 0 || entry.completedUtc > 253402300799L ||
                    entry.raiders < 0 || entry.archers < 0 || entry.tanks < 0 ||
                    (long)entry.raiders + entry.archers + entry.tanks < 1 ||
                    (long)entry.raiders + 2L * entry.archers + 4L * entry.tanks > PracticeBattle.MaximumArmySize ||
                    !Enum.IsDefined(typeof(PracticeOutcome), entry.outcome) || entry.stars < 0 || entry.stars > 3 ||
                    entry.durationTicks < -1 || entry.durationTicks > PracticeBattle.TimeLimitTicks ||
                    entry.destruction < -1 || entry.destruction > 100 ||
                    (entry.durationTicks == -1) != (entry.destruction == -1)) return false;
                if (entry.abandoned)
                {
                    if (!entry.resolved || entry.outcome != PracticeOutcome.Running || entry.stars != 0 || entry.durationTicks != -1) return false;
                }
                else if (entry.outcome == PracticeOutcome.Running ||
                    (entry.outcome == PracticeOutcome.Victory ? entry.stars != 3 : entry.stars == 3) ||
                    (entry.destruction >= 0 && (entry.outcome == PracticeOutcome.Victory ? entry.destruction != 100 : entry.destruction == 100))) return false;
                bool paid = entry.goldAwarded != 0 || entry.elixirAwarded != 0;
                if (paid && (!entry.resolved || entry.abandoned || entry.outcome != PracticeOutcome.Victory || !CampaignCleared(entry.mission) ||
                    entry.goldAwarded != mission.Gold || entry.elixirAwarded != mission.Elixir)) return false;
                if (!entry.resolved && (!HasCampaignRun || entry.runId != campaignRunId)) return false;
                if (HasCampaignRun && entry.runId == campaignRunId && (entry.resolved || entry.mission != campaignMission ||
                    entry.raiders + entry.archers + entry.tanks != campaignArmy || entry.archers != campaignArchers || entry.tanks != campaignTanks ||
                    entry.outcome != campaignOutcome || entry.stars != campaignStars)) return false;
            }
            return !HasCampaignRun || campaignOutcome == PracticeOutcome.Running || FindHistory(campaignRunId) != null;
        }
    }
}
