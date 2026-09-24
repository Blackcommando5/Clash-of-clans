using System;

namespace Kingdoms
{
    public sealed class CampaignMission
    {
        public readonly int Id, Gold, Elixir;
        public readonly string Name;
        public readonly bool Sealed;
        public CampaignMission(int id, string name, bool sealedWalls, int gold, int elixir)
        { Id=id; Name=name; Sealed=sealedWalls; Gold=gold; Elixir=elixir; }
    }

    public static class CampaignCatalog
    {
        public static readonly CampaignMission Gate = new CampaignMission(0,"Gate Outpost",false,500,300);
        public static readonly CampaignMission Keep = new CampaignMission(1,"Sealed Keep",true,1000,600);
        public static CampaignMission Find(int id) => id==0 ? Gate : id==1 ? Keep : null;
    }

    public sealed partial class VillageState
    {
        public int campaignCleared;
        public string campaignRunId = "";
        public int campaignMission = -1, campaignArmy, campaignStars, campaignArchers;
        public PracticeOutcome campaignOutcome;
        public bool HasCampaignRun => !string.IsNullOrEmpty(campaignRunId);
        public bool CampaignCleared(int mission) => CampaignCatalog.Find(mission)!=null && (campaignCleared & (1<<mission))!=0;
        public bool CampaignUnlocked(int mission) => CampaignCatalog.Find(mission)!=null && (mission==0 || CampaignCleared(mission-1));

        public bool IsCampaignValid()
        {
            if (campaignCleared!=0 && campaignCleared!=1 && campaignCleared!=3) return false;
            if (!HasCampaignRun) return campaignMission==-1 && campaignArmy==0 && campaignArchers==0 && campaignStars==0 && campaignOutcome==PracticeOutcome.Running;
            if (!Guid.TryParseExact(campaignRunId,"N",out _) || !CampaignUnlocked(campaignMission) || campaignArmy<1 || campaignArmy>PracticeBattle.MaximumArmySize) return false;
            if(campaignArchers<0 || campaignArchers>campaignArmy || (long)campaignArmy+campaignArchers>PracticeBattle.MaximumArmySize)return false;
            if((long)campaignArmy+campaignArchers>ArmyCapacity || (campaignArchers>0 && !TroopUnlocked("Archer")))return false;
            if (!Enum.IsDefined(typeof(PracticeOutcome),campaignOutcome) || campaignStars<0 || campaignStars>3) return false;
            return campaignOutcome==PracticeOutcome.Running ? campaignStars==0 : campaignOutcome==PracticeOutcome.Victory ? campaignStars==3 : campaignStars<3;
        }

        public bool TryBeginCampaign(int mission, out string reason)
        {
            if (HasCampaignRun) { reason="Finish or dismiss your previous campaign result first."; return false; }
            if (!CampaignUnlocked(mission)) { reason="Clear the previous mission first."; return false; }
            if (!ArmyReady) { reason="Prepare at least one troop first."; return false; }
            campaignRunId=Guid.NewGuid().ToString("N");campaignMission=mission;campaignArmy=ArmyCount;campaignArchers=ArmyCountOf("Archer");
            campaignOutcome=PracticeOutcome.Running;campaignStars=0;army.Clear();
            reason="Prepared army committed. All assigned troops are spent when the attack starts.";
            return true;
        }

        // Re-simulate the command recording before accepting the local result. This is not server authority.
        public bool TryFinishCampaign(string runId, PracticeBattle.Recording recording, out string reason)
        {
            reason="This result does not match the active campaign attack.";
            if (!HasCampaignRun || runId!=campaignRunId || campaignOutcome!=PracticeOutcome.Running || recording==null ||
                recording.ArmyBudget!=campaignArmy || recording.ArcherBudget!=campaignArchers || recording.SealedEnclosure!=CampaignCatalog.Find(campaignMission).Sealed ||
                recording.EndTick<0 || recording.EndTick>PracticeBattle.TimeLimitTicks) return false;
            var replay=new PracticeReplay(recording);
            for(int i=0;i<=PracticeBattle.TimeLimitTicks && !replay.Finished;i++)replay.Step();
            if(!replay.Matches)return false;
            campaignOutcome=replay.Battle.Outcome;campaignStars=replay.Battle.Stars;
            reason="Campaign result saved. You can claim it after returning home or restarting.";
            return true;
        }

        public bool TryClaimCampaign(string runId, out string reason)
        {
            if (!HasCampaignRun || runId!=campaignRunId || campaignOutcome==PracticeOutcome.Running)
            { reason="There is no completed result to claim."; return false; }
            var mission=CampaignCatalog.Find(campaignMission);
            bool reward=campaignOutcome==PracticeOutcome.Victory && !CampaignCleared(campaignMission);
            if(reward && (GoldCapacity-gold<mission.Gold || ElixirCapacity-elixir<mission.Elixir))
            { reason="Make room for "+mission.Gold+" gold and "+mission.Elixir+" elixir. Your full reward remains saved."; return false; }
            if(reward){gold+=mission.Gold;elixir+=mission.Elixir;campaignCleared|=1<<campaignMission;}
            reason=reward ? "Claimed "+mission.Gold+" gold and "+mission.Elixir+" elixir. Mission cleared!" : "Result dismissed. No additional reward.";
            ResetCampaignRun();return true;
        }

        public bool TryAbandonCampaign(string runId, out string reason)
        {
            if(!HasCampaignRun || runId!=campaignRunId || campaignOutcome!=PracticeOutcome.Running)
            { reason="There is no interrupted attack to abandon.";return false; }
            ResetCampaignRun();reason="Interrupted attack abandoned. Prepare a new army for free.";return true;
        }

        void ResetCampaignRun()
        {campaignRunId="";campaignMission=-1;campaignArmy=0;campaignArchers=0;campaignStars=0;campaignOutcome=PracticeOutcome.Running;}
    }
}
