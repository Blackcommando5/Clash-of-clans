using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        Button campaignResume, checkpointRetry;
        bool checkpointSaveBlocked;
        int checkpointNextTick;

        void BuildCampaignRecovery(Transform page)
        {
            var prepare=page.Find("Campaign Prepare Army").GetComponent<RectTransform>();
            prepare.anchorMax=new Vector2(.33f,.19f);
            campaignResolve.GetComponent<RectTransform>().anchorMin=new Vector2(.67f,.04f);
            campaignResume=Button("Resume Campaign",page,"RESUME ATTACK",new Vector2(.36f,.04f),new Vector2(.64f,.19f),new Color(.35f,.60f,.25f));
            campaignResume.onClick.AddListener(ResumeCampaignAttack);
        }

        public void ResumeCampaignAttack()
        {
            if(State==null || PracticeOpen || IsPlacing)return;
            if(!SavedBattleReplay.TryRestoreCheckpoint(State,out var battle,out var reason))
            {campaignMessage=reason;OpenCampaign();return;}
            OpenBattle(State.campaignMission,null,battle);
        }

        void BuildCheckpointRetry()
        {
            checkpointRetry=Button("Retry Battle Save",practiceSafe,"RETRY SAVE",new Vector2(.81f,.03f),new Vector2(.98f,.12f),new Color(.65f,.35f,.16f));
            checkpointRetry.onClick.AddListener(()=>{SaveCampaignCheckpoint();RefreshPracticeBattle();});
            checkpointRetry.gameObject.SetActive(false);
        }

        bool SaveCampaignCheckpoint()
        {
            if(!CampaignBattleOpen || ScoutingPractice || WatchingPracticeReplay)return true;
            if(practiceBattle.Outcome!=PracticeOutcome.Running)return SaveCampaignResult();
            var candidate=State.Copy();
            bool saved=candidate.TryCheckpointCampaign(battleRunId,practiceBattle,out var reason);
            if(saved)saved=VillageSave.TryWrite(candidate,out reason);
            checkpointSaveBlocked=!saved;
            if(!saved){campaignMessage=reason+" Battle paused. Press Retry Save.";return false;}
            State=candidate;checkpointNextTick=practiceBattle.Tick+20;campaignMessage="";return true;
        }

        void RefreshCheckpointControls()
        {
            checkpointRetry.gameObject.SetActive(checkpointSaveBlocked);
            if(CampaignBattleOpen)
                practiceSafe.Find("Return From Practice").GetComponentInChildren<Text>().text=!ScoutingPractice && !WatchingPracticeReplay && practiceBattle.Outcome==PracticeOutcome.Running ? "SAVE & RETURN" : "RETURN HOME";
            if(!checkpointSaveBlocked)return;
            practiceInstructions.text=campaignMessage;
            foreach(var button in practiceDeployButtons)button.interactable=false;
            deployRaiders.interactable=false;deployArchers.interactable=false;deployTanks.interactable=false;practiceSurrender.interactable=false;
        }
    }
}
