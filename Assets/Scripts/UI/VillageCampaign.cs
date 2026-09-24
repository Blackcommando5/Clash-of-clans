using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        int campaignPageIndex, battleMission=-1;
        string battleRunId="", campaignMessage="";
        bool campaignResultSaved, campaignSaveAttempted;
        Text campaignSummary;
        Button campaignGate, campaignKeep, campaignResolve, campaignBattleClaim;
        public bool CampaignBattleOpen => PracticeOpen && battleMission>=0;

        void BuildCampaignInterface()
        {
            var armyPage=profilePages[armyPageIndex];
            armyPage.Find("Army Title").GetComponent<RectTransform>().anchorMax=new Vector2(.72f,.98f);
            var entry=Button("Open Campaign",armyPage,"CAMPAIGN",new Vector2(.73f,.85f),new Vector2(.97f,.98f),new Color(.28f,.55f,.76f));
            entry.onClick.AddListener(OpenCampaign);
            campaignPageIndex=profilePages.Count;
            var page=Box("Campaign Page",profilePages[0].parent,Vector2.zero,Vector2.one);profilePages.Add(page);
            Label("Campaign Title",page,"Border Campaign",40,new Color(.23f,.25f,.3f),new Vector2(.05f,.86f),new Vector2(.95f,.99f));
            campaignSummary=Label("Campaign Summary",page,"",24,new Color(.23f,.25f,.3f),new Vector2(.06f,.40f),new Vector2(.94f,.84f));
            campaignGate=Button("Campaign Gate",page,"",new Vector2(.04f,.23f),new Vector2(.48f,.38f),new Color(.35f,.60f,.25f));
            campaignKeep=Button("Campaign Keep",page,"",new Vector2(.52f,.23f),new Vector2(.96f,.38f),new Color(.35f,.60f,.25f));
            campaignGate.onClick.AddListener(()=>OpenCampaignBattle(0));campaignKeep.onClick.AddListener(()=>OpenCampaignBattle(1));
            campaignResolve=Button("Resolve Campaign",page,"",new Vector2(.52f,.04f),new Vector2(.96f,.19f),new Color(.55f,.44f,.20f));
            campaignResolve.onClick.AddListener(ResolveCampaignFromMenu);
            var prepare=Button("Campaign Prepare Army",page,"PREPARE ARMY",new Vector2(.04f,.04f),new Vector2(.48f,.19f),new Color(.28f,.55f,.76f));
            prepare.onClick.AddListener(OpenArmyPreparation);page.gameObject.SetActive(false);
        }

        public void OpenCampaign()
        {
            if(State==null || PracticeOpen || IsPlacing)return;
            OpenProfile(campaignPageIndex);RefreshCampaignPage();
        }

        void RefreshCampaignPage()
        {
            campaignGate.GetComponentInChildren<Text>().text="GATE OUTPOST"+(State.CampaignCleared(0) ? " - CLEARED" : " - SCOUT");
            campaignKeep.GetComponentInChildren<Text>().text="SEALED KEEP"+(State.CampaignCleared(1) ? " - CLEARED" : State.CampaignUnlocked(1) ? " - SCOUT" : " - LOCKED");
            campaignGate.interactable=State.ArmyReady && !State.HasCampaignRun;
            campaignKeep.interactable=campaignGate.interactable && State.CampaignUnlocked(1);
            campaignResolve.gameObject.SetActive(State.HasCampaignRun);
            string pending="";
            if(State.HasCampaignRun)
            {
                bool interrupted=State.campaignOutcome==PracticeOutcome.Running;
                campaignResolve.GetComponentInChildren<Text>().text=interrupted ? "ABANDON ATTACK" : State.campaignOutcome==PracticeOutcome.Victory ? "CLAIM / FINISH" : "DISMISS RESULT";
                pending="\n"+CampaignCatalog.Find(State.campaignMission).Name+": "+(interrupted ? "Interrupted attack. Assigned troops are spent." : State.campaignOutcome+" - "+State.campaignStars+" / 3 stars. Result saved.");
            }
            campaignSummary.text="Prepared: "+State.ArmyCountOf("Raider")+" Raiders + "+State.ArmyCountOf("Archer")+" Archers + "+State.ArmyCountOf("Tank")+" Tanks | "+State.ArmyHousing+" / "+State.ArmyCapacity+" spaces"+
                "\nStarting commits the entire roster, including undeployed troops.\nPrepare again for free after each attack. Scouting costs nothing."+
                "\nFirst victories: Outpost 500 gold + 300 elixir; Keep 1,000 gold + 600 elixir.\nRepeat victories and losses give no resources."+pending+
                (string.IsNullOrEmpty(campaignMessage) ? "" : "\n"+campaignMessage);
        }

        public void OpenCampaignBattle(int mission)
        {
            if(State==null || PracticeOpen || IsPlacing)return;
            if(State.HasCampaignRun || !State.ArmyReady || !State.CampaignUnlocked(mission))
            {campaignMessage="Prepare an army, finish pending results and unlock the mission first.";OpenCampaign();return;}
            OpenBattle(mission);
        }

        bool CommitCampaignArmy()
        {
            var candidate=State.Copy();
            if(!candidate.TryBeginCampaign(battleMission,out var reason)){campaignMessage=reason;return false;}
            if(candidate.campaignArmy!=practiceBattle.ArmyBudget || candidate.campaignArchers!=practiceBattle.ArcherBudget || candidate.campaignTanks!=practiceBattle.TankBudget){campaignMessage="Your roster changed. Return home and scout again.";return false;}
            if(!VillageSave.TryWrite(candidate,out var error)){campaignMessage=error;return false;}
            State=candidate;battleRunId=State.campaignRunId;campaignMessage="";return true;
        }

        bool SaveCampaignResult()
        {
            if(battleMission<0 || ScoutingPractice || WatchingPracticeReplay || campaignResultSaved || string.IsNullOrEmpty(battleRunId))return true;
            if(practiceBattle.Outcome==PracticeOutcome.Running)return false;
            campaignSaveAttempted=true;
            var candidate=State.Copy();
            if(!candidate.TryFinishCampaign(battleRunId,practiceBattle.Record(),out var reason)){campaignMessage=reason;return false;}
            if(!VillageSave.TryWrite(candidate,out var error)){campaignMessage=error;return false;}
            State=candidate;campaignResultSaved=true;campaignMessage=reason;return true;
        }

        void ClaimCampaignBattle()
        {
            if(!CampaignBattleOpen || WatchingPracticeReplay || practiceBattle.Outcome==PracticeOutcome.Running)return;
            if(!SaveCampaignResult()){RefreshPracticeBattle();return;}
            ResolveCampaign(false);RefreshPracticeBattle();
        }

        void ResolveCampaignFromMenu(){ResolveCampaign(true);RefreshCampaignPage();}
        void ResolveCampaign(bool allowAbandon)
        {
            var candidate=State.Copy();
            bool abandoned=allowAbandon && candidate.campaignOutcome==PracticeOutcome.Running;
            string reason;
            bool ok=abandoned ? candidate.TryAbandonCampaign(candidate.campaignRunId,out reason) : candidate.TryClaimCampaign(candidate.campaignRunId,out reason);
            if(!ok){campaignMessage=reason;return;}
            if(!VillageSave.TryWrite(candidate,out var error)){campaignMessage=error;return;}
            State=candidate;campaignMessage=reason;RefreshHUD();
        }

        void RefreshCampaignBattle()
        {
            var mission=CampaignCatalog.Find(battleMission);
            bool running=practiceBattle.Outcome==PracticeOutcome.Running;
            if(!running && !WatchingPracticeReplay && !campaignSaveAttempted)SaveCampaignResult();
            practiceChallenge.gameObject.SetActive(false);practiceRetry.gameObject.SetActive(false);
            practiceWatch.interactable=!running && campaignResultSaved;
            campaignBattleClaim.gameObject.SetActive(!running && !WatchingPracticeReplay);
            campaignBattleClaim.interactable=State.HasCampaignRun;
            campaignBattleClaim.GetComponentInChildren<Text>().text=!campaignResultSaved ? "SAVE RESULT" : practiceBattle.Outcome==PracticeOutcome.Victory ? "CLAIM" : "FINISH";
            if(ScoutingPractice)
            {
                practiceStatus.text="CAMPAIGN SCOUTING | "+mission.Name+"\n"+practiceBattle.ArmyBudget+" prepared troops | 3-minute attack";
                practiceInstructions.text=string.IsNullOrEmpty(campaignMessage) ? "Start Attack spends the whole roster. Return Home from scouting keeps it." : campaignMessage;
            }
            else if(running && !WatchingPracticeReplay)
            {
                practiceStatus.text=practiceStatus.text.Replace("PRACTICE BATTLE",mission.Name.ToUpperInvariant());
                practiceInstructions.text="Campaign attack: destroy every building to win. Assigned troops are spent; prepare again for free.";
            }
            else if(!running && !WatchingPracticeReplay)
                practiceInstructions.text=campaignMessage;
            if(!running)practiceResultsText.text=practiceResultsText.text.Replace(practiceSealedChallenge ? "WALL BREACH" : "OPEN GATE",mission.Name.ToUpperInvariant());
        }
    }
}
