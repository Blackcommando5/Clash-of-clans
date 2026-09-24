using System;
using System.Globalization;
using UnityEngine;

namespace Kingdoms
{
    // Optional history attachment. Incompatible recordings expire without invalidating the village.
    [Serializable]
    public sealed class SavedBattleReplay
    {
        public const int FormatVersion = 1, MaximumJsonLength = 20000;
        public int format, rules, layout, layoutRevision, army, archers, tanks, endTick;
        public PracticeOutcome outcome;
        public string finalHash;
        public string runId;
        public Command[] commands;
        [Serializable]
        public sealed class Command { public int tick, x, z; public string troop; }

        public static string Encode(PracticeBattle.Recording recording, string runId=null)
        {
            var saved = new SavedBattleReplay {
                runId=runId, format=FormatVersion, rules=PracticeBattle.Recording.RulesVersion,
                layout=recording.Layout.Id, layoutRevision=recording.Layout.Revision,
                army=recording.ArmyBudget, archers=recording.ArcherBudget, tanks=recording.TankBudget,
                endTick=recording.EndTick, outcome=recording.Outcome,
                finalHash=recording.FinalHash.ToString("X16", CultureInfo.InvariantCulture),
                commands=new Command[recording.Deployments.Count]
            };
            for(int i=0;i<saved.commands.Length;i++)
            {
                var command=recording.Deployments[i];
                saved.commands[i]=new Command{tick=command.Tick,x=command.X,z=command.Z,troop=command.Troop};
            }
            return JsonUtility.ToJson(saved);
        }

        // Header checks are cheap enough for menu rendering. Full simulation runs only on Watch.
        public static bool CanRead(CampaignHistoryEntry entry, out string reason) => TryRead(entry,out _,out reason);
        static bool TryRead(CampaignHistoryEntry entry, out SavedBattleReplay saved, out string reason, bool running=false)
        {
            saved=null;reason="Replay unavailable";
            if(entry==null || entry.abandoned || string.IsNullOrEmpty(entry.replayJson)) {reason="No saved replay";return false;}
            if(entry.replayJson.Length>MaximumJsonLength)return false;
            try { saved=JsonUtility.FromJson<SavedBattleReplay>(entry.replayJson); }
            catch(Exception) {return false;}
            if(saved==null)return false;
            var layout=EnemyLayoutCatalog.Find(saved.layout);
            if(saved.format!=FormatVersion || saved.rules!=PracticeBattle.Recording.RulesVersion || layout==null || saved.layoutRevision!=layout.Revision)
            {reason="Replay expired";return false;}
            if(saved.layout!=entry.mission || saved.army!=(long)entry.raiders+entry.archers+entry.tanks || saved.archers!=entry.archers || saved.tanks!=entry.tanks ||
                saved.army<1 || saved.archers<0 || saved.tanks<0 || (long)saved.archers+saved.tanks>saved.army ||
                (long)saved.army+saved.archers+3L*saved.tanks>PracticeBattle.MaximumArmySize ||
                saved.endTick!=entry.durationTicks || saved.endTick<0 || saved.endTick>PracticeBattle.TimeLimitTicks ||
                saved.outcome!=entry.outcome || (!running && saved.outcome==PracticeOutcome.Running) || !Enum.IsDefined(typeof(PracticeOutcome),saved.outcome) ||
                saved.finalHash==null || saved.finalHash.Length!=16 || !ulong.TryParse(saved.finalHash,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out _) ||
                saved.commands==null || saved.commands.Length>saved.army)return false;
            int previous=0;
            foreach(var command in saved.commands)
            {
                if(command==null || command.tick<previous || command.tick>saved.endTick || TroopCatalog.Find(command.troop)==null ||
                    command.x<layout.MinX || command.x>layout.MaxX || command.z<layout.MinZ || command.z>layout.MaxZ)return false;
                previous=command.tick;
            }
            reason="Watch replay";return true;
        }

        public static bool TryRestore(CampaignHistoryEntry entry, out PracticeBattle.Recording recording, out string reason)
        {
            recording=null;
            if(!TryRead(entry,out var saved,out reason))return false;
            reason="Replay could not be verified. Your battle summary is still available.";
            if(!TryRebuild(saved,out var battle) || battle.Stars!=entry.stars || battle.Destruction!=entry.destruction)return false;
            recording=battle.Record();reason="Watching saved campaign replay.";return true;
        }

        public static bool TryRestoreCheckpoint(VillageState state, out PracticeBattle battle, out string reason)
        {
            battle=null;reason="No compatible checkpoint is available. You can abandon this attack and prepare again.";
            if(!state.HasCampaignRun || state.campaignOutcome!=PracticeOutcome.Running || string.IsNullOrEmpty(state.campaignCheckpoint) || state.campaignCheckpoint.Length>MaximumJsonLength)return false;
            SavedBattleReplay data;
            try { data=JsonUtility.FromJson<SavedBattleReplay>(state.campaignCheckpoint); } catch(Exception){return false;}
            if(data==null || data.runId!=state.campaignRunId)return false;
            var entry=new CampaignHistoryEntry { replayJson=state.campaignCheckpoint,mission=state.campaignMission,
                raiders=state.campaignArmy-state.campaignArchers-state.campaignTanks,archers=state.campaignArchers,tanks=state.campaignTanks,
                durationTicks=data.endTick,outcome=PracticeOutcome.Running };
            if(!TryRead(entry,out data,out _,true) || !TryRebuild(data,out var restored))return false;
            battle=restored;reason="Attack restored. Your committed army was not charged again.";return true;
        }

        static bool TryRebuild(SavedBattleReplay saved, out PracticeBattle battle)
        {
            battle=new PracticeBattle(EnemyLayoutCatalog.Find(saved.layout),saved.army,saved.archers,saved.tanks);
            int next=0;
            for(int tick=0;tick<=saved.endTick;tick++)
            {
                while(next<saved.commands.Length && saved.commands[next].tick==tick)
                {
                    var command=saved.commands[next++];
                    if(!battle.DeployAt(command.x,command.z,command.troop))return false;
                }
                if(tick==saved.endTick)break;
                if(battle.Outcome!=PracticeOutcome.Running)return false;
                battle.Step();
            }
            if(saved.outcome==PracticeOutcome.Surrendered)battle.Surrender();
            if(next!=saved.commands.Length || battle.Outcome!=saved.outcome || battle.Tick!=saved.endTick ||
                battle.StateHash().ToString("X16",CultureInfo.InvariantCulture)!=saved.finalHash)return false;
            return true;
        }
    }
}
