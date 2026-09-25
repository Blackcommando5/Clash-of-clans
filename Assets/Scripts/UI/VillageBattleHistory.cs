using System;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        const int HistoryPageSize = 2;
        int historyPageIndex, historyOffset;
        Text historyPageNumber;
        readonly Text[] historyRows = new Text[HistoryPageSize];
        readonly Button[] historyReplayButtons = new Button[HistoryPageSize];
        readonly Button[] historyArmyButtons = new Button[HistoryPageSize];
        Text historyHelp;
        Button historyNewer, historyOlder;

        void BuildBattleHistoryInterface()
        {
            var campaign = profilePages[campaignPageIndex];
            campaign.Find("Campaign Title").GetComponent<RectTransform>().anchorMax = new Vector2(.64f, .99f);
            var entry = Button("Open Battle History", campaign, "BATTLE HISTORY", new Vector2(.66f,.86f), new Vector2(.96f,.98f), new Color(.28f,.55f,.76f));
            entry.onClick.AddListener(OpenBattleHistory);
            historyPageIndex = profilePages.Count;
            var page = Box("Battle History Page", profilePages[0].parent, Vector2.zero, Vector2.one);
            profilePages.Add(page);
            Label("Battle History Title", page, "Battle History", 38, new Color(.23f,.25f,.3f), new Vector2(.05f,.87f), new Vector2(.95f,.99f));
            historyHelp=Label("Battle History Help", page, "Latest 20 results. Use Army replaces your prepared roster for free; rewards stay unchanged.", 21, new Color(.3f,.32f,.36f), new Vector2(.05f,.79f), new Vector2(.95f,.87f));
            for (int i=0; i<HistoryPageSize; i++)
            {
                float top = .78f - i*.31f;
                var row = Panel("Battle History Row " + i, page, new Vector2(.05f,top-.29f), new Vector2(.95f,top), new Color(.93f,.91f,.85f));
                historyRows[i] = Label("Battle History Text " + i, row.transform, "", 24, new Color(.23f,.25f,.3f), new Vector2(.025f,.04f), new Vector2(.76f,.96f));
                historyRows[i].alignment = TextAnchor.MiddleLeft;
                int index=i;
                historyReplayButtons[i]=Button("History Replay "+i,row.transform,"WATCH REPLAY",new Vector2(.78f,.54f),new Vector2(.98f,.94f),new Color(.28f,.55f,.76f));
                historyReplayButtons[i].GetComponentInChildren<Text>().resizeTextMaxSize=22;
                historyReplayButtons[i].onClick.AddListener(()=>WatchHistoryReplay(historyOffset+index));
                historyArmyButtons[i]=Button("History Army "+i,row.transform,"USE ARMY",new Vector2(.78f,.06f),new Vector2(.98f,.46f),new Color(.35f,.60f,.25f));
                historyArmyButtons[i].GetComponentInChildren<Text>().resizeTextMaxSize=22;
                historyArmyButtons[i].onClick.AddListener(()=>PrepareArmyFromHistory(historyOffset+index));
            }
            historyNewer = Button("History Newer", page, "NEWER", new Vector2(.05f,.02f), new Vector2(.25f,.13f), new Color(.28f,.55f,.76f));
            historyOlder = Button("History Older", page, "OLDER", new Vector2(.28f,.02f), new Vector2(.48f,.13f), new Color(.28f,.55f,.76f));
            historyNewer.onClick.AddListener(()=>{historyOffset-=HistoryPageSize;RefreshBattleHistory();});
            historyOlder.onClick.AddListener(()=>{historyOffset+=HistoryPageSize;RefreshBattleHistory();});
            historyPageNumber = Label("History Page Number", page, "", 22, new Color(.23f,.25f,.3f), new Vector2(.49f,.02f), new Vector2(.65f,.13f));
            var back = Button("History Back", page, "CAMPAIGN", new Vector2(.68f,.02f), new Vector2(.95f,.13f), new Color(.35f,.60f,.25f));
            back.onClick.AddListener(OpenCampaign);
            page.gameObject.SetActive(false);
        }

        public void OpenBattleHistory()
        {
            if (State==null || PracticeOpen || IsPlacing) return;
            historyOffset=0;OpenProfile(historyPageIndex);RefreshBattleHistory();
        }

        void RefreshBattleHistory()
        {
            int count=State.battleHistory.Count;
            historyOffset=Mathf.Clamp(historyOffset,0,Mathf.Max(0,(count-1)/HistoryPageSize*HistoryPageSize));
            for(int i=0;i<HistoryPageSize;i++)
            {
                bool exists=historyOffset+i<count;
                historyReplayButtons[i].gameObject.SetActive(exists);
                historyArmyButtons[i].gameObject.SetActive(exists);
                historyRows[i].transform.parent.gameObject.SetActive(exists || (count==0 && i==0));
                if(!exists){historyRows[i].text="No campaign results yet.\nPrepare an army and finish a campaign attack to begin your history.";continue;}
                var result=State.battleHistory[historyOffset+i];
                // Keep the action available so rejected capacity/unlock requests explain the requirement.
                historyArmyButtons[i].interactable=true;
                historyReplayButtons[i].interactable=SavedBattleReplay.CanRead(result,out var replayReason);
                historyReplayButtons[i].GetComponentInChildren<Text>().text=replayReason.ToUpperInvariant();
                string date=result.completedUtc==0 ? "Date unavailable (older save)" : DateTimeOffset.FromUnixTimeSeconds(result.completedUtc).ToLocalTime().ToString("dd MMM yyyy HH:mm");
                string stats=result.destruction<0 ? "Battle details unavailable" : result.destruction+"% destruction | "+(result.durationTicks*PracticeBattle.TickMilliseconds/1000f).ToString("0.0")+"s";
                string reward=!result.resolved ? (result.outcome==PracticeOutcome.Victory && !State.CampaignCleared(result.mission) ? "Reward pending - return to Campaign to claim" : "Result pending - return to Campaign to finish") :
                    result.goldAwarded>0 ? "Claimed: "+result.goldAwarded+" gold + "+result.elixirAwarded+" elixir" : "No reward";
                historyRows[i].text=CampaignCatalog.Find(result.mission).Name+" - "+(result.abandoned ? "Abandoned" : result.outcome+" | "+result.stars+" / 3 stars")+"\n"+
                    date+" | "+stats+"\n"+result.raiders+" Raiders + "+result.archers+" Archers + "+result.tanks+" Tanks committed\n"+reward;
            }
            historyNewer.interactable=historyOffset>0;historyOlder.interactable=historyOffset+HistoryPageSize<count;
            historyPageNumber.text=count==0 ? "0 results" : (historyOffset/HistoryPageSize+1)+" / "+((count+HistoryPageSize-1)/HistoryPageSize);
        }

        public void WatchHistoryReplay(int index)
        {
            if(State==null || PracticeOpen || IsPlacing || index<0 || index>=State.battleHistory.Count)return;
            if(!SavedBattleReplay.TryRestore(State.battleHistory[index],out var recording,out var reason))
            {historyHelp.text=reason;return;}
            OpenBattle(-1,recording);
        }
    }
}
