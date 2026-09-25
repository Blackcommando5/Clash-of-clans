using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        PracticeBattle.Recording replayRecording;
        GameObject replayControls;
        Button replayPause, replaySpeedButton;
        Text replayProgress;
        bool replayUserPaused;
        int replaySpeed=1;

        void BuildReplayControls()
        {
            replayControls=Box("Replay Controls",practiceSafe,new Vector2(.25f,.025f),new Vector2(.75f,.16f)).gameObject;
            replayProgress=Label("Replay Progress",replayControls.transform,"",20,Color.white,new Vector2(0,.67f),Vector2.one);
            ReferenceText(replayProgress,20,Color.white);
            replayPause=Button("Pause Replay",replayControls.transform,"PAUSE",Vector2.zero,new Vector2(.31f,.62f),new Color(.22f,.45f,.65f));
            replayPause.onClick.AddListener(ToggleReplayPause);
            replaySpeedButton=Button("Replay Speed",replayControls.transform,"SPEED: 1x",new Vector2(.345f,0),new Vector2(.655f,.62f),new Color(.22f,.45f,.65f));
            replaySpeedButton.onClick.AddListener(CycleReplaySpeed);
            var restart=Button("Restart Replay",replayControls.transform,"RESTART",new Vector2(.69f,0),new Vector2(1,.62f),new Color(.45f,.64f,.24f));
            restart.onClick.AddListener(RestartReplay);
            foreach(var button in new[]{replayPause,replaySpeedButton,restart})ReferenceText(button.GetComponentInChildren<Text>(),22,Color.white);
            replayControls.SetActive(false);
        }

        public void ToggleReplayPause()
        {
            if(!WatchingPracticeReplay || practiceReplay.Finished)return;
            replayUserPaused=!replayUserPaused;
            RefreshPracticeBattle();
        }

        public void CycleReplaySpeed()
        {
            if(!WatchingPracticeReplay || practiceReplay.Finished)return;
            replaySpeed=replaySpeed==4 ? 1 : replaySpeed*2;
            RefreshPracticeBattle();
        }

        public void RestartReplay()
        {
            if(!WatchingPracticeReplay || replayRecording==null)return;
            // Keep the original full recording even when restarting partway through playback.
            StartPracticeBattle(replayRecording);
        }

        void AdvancePracticeSimulation(float elapsed)
        {
            if(!PracticeOpen || ScoutingPractice || practicePaused || checkpointSaveBlocked ||
                (WatchingPracticeReplay && replayUserPaused) || practiceBattle.Outcome!=PracticeOutcome.Running)return;
            // Speed changes only tick scheduling, never combat rules or the global Unity clock.
            practiceAccumulator+=Mathf.Clamp(elapsed,0,.5f)*(WatchingPracticeReplay ? replaySpeed : 1);
            while(practiceAccumulator+1e-9>=.1 && practiceBattle.Outcome==PracticeOutcome.Running)
            {
                practiceAccumulator-=.1;
                if(practiceReplay==null)practiceBattle.Step();else practiceReplay.Step();
                foreach(var raider in practiceBattle.Raiders)if(!practiceModels.ContainsKey(raider.Id))CreatePracticeRaider(raider);
                foreach(var strike in practiceBattle.Strikes)ShowPracticeStrike(strike);
            }
            if(practiceBattle.Outcome!=PracticeOutcome.Running)practiceAccumulator=0;
        }

        void RefreshReplayControls()
        {
            replayControls.SetActive(WatchingPracticeReplay);
            if(!WatchingPracticeReplay)return;
            bool finished=practiceReplay.Finished;
            replayPause.interactable=!finished;replaySpeedButton.interactable=!finished;
            replayPause.GetComponentInChildren<Text>().text=replayUserPaused && !finished ? "RESUME" : "PAUSE";
            replaySpeedButton.GetComponentInChildren<Text>().text="SPEED: "+replaySpeed+"x";
            replayProgress.text=(finished ? "COMPLETE" : replayUserPaused ? "PAUSED" : "PLAYING")+"  |  "+ReplayTime(practiceBattle.Tick)+" / "+ReplayTime(replayRecording.EndTick);
            if(!finished)practiceInstructions.text=replayUserPaused ? "Replay paused. Resume or restart to continue watching." : "Watching your recorded attack at "+replaySpeed+"x speed. Deployments play automatically.";
        }

        static string ReplayTime(int ticks)=>$"{ticks/600}:{ticks/10%60:00}.{ticks%10}";
    }
}
