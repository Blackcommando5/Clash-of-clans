using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class ReplayControlsValidation
{
    const string Pending="ReplayControls.Pending";
    static ReplayControlsValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Button FindButton(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>FindButton(name).onClick.Invoke();
    static object Call(VillageGameplay game,string name,params object[] args)=>typeof(VillageGameplay).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    static void Advance(VillageGameplay game,float elapsed){Call(game,"AdvancePracticeSimulation",elapsed);Call(game,"RefreshPracticeBattle");}
    static void Start(VillageGameplay game,PracticeBattle.Recording recording)=>Call(game,"StartPracticeBattle",recording,null);
    // The normal application-pause save advances the production clock. This fixture has no producers.
    static string Snapshot(string json){var state=JsonUtility.FromJson<VillageState>(json);state.lastProduction=0;return JsonUtility.ToJson(state);}
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});

    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            typeof(BattleRecoveryValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            var seed=(VillageState)typeof(SavedReplayValidation).GetMethod("Seed",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Check(VillageSave.TryWrite(seed,out _) && PlayerProfile.TrySaveName("PlaybackChief",out _),"Seed saved history and separate active run");
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            SessionState.SetInt("ReplayControls.Step",0);SessionState.SetString("ReplayControls.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }

    static void VerifySpeeds(VillageGameplay game)
    {
        for(int layout=0;layout<4;layout++)
        {
            var battle=new PracticeBattle(EnemyLayoutCatalog.Find(layout),4,1,1);
            battle.DeployAt(-300,-1200,"Tank");
            for(int i=0;i<7;i++)battle.Step();battle.DeployAt(0,-1200,"Archer");
            for(int i=0;i<12;i++)battle.Step();battle.DeployAt(300,-1200,"Raider");
            for(int i=0;i<100;i++)battle.Step();battle.Surrender();var recording=battle.Record();
            foreach(int speed in new[]{1,2,4})
            {
                Start(game,recording);if(speed>=2)Click("Replay Speed");if(speed==4)Click("Replay Speed");
                var reference=new PracticeReplay(recording);
                while(!reference.Finished)
                {
                    Advance(game,.5f);
                    for(int i=0;i<5*speed && !reference.Finished;i++)reference.Step();
                    Check(game.CurrentPracticeBattle.StateHash()==reference.Battle.StateHash(),"Speed preserves exact command timing and state, layout "+layout+" speed "+speed);
                }
                Check(game.PracticeReplayMatches && !FindButton("Pause Replay").interactable && !FindButton("Replay Speed").interactable,"Verified finish disables pause/speed");
                Check(GameObject.Find("Replay Progress").GetComponent<Text>().text.Contains("COMPLETE"),"Completion progress");
                ulong hash=game.CurrentPracticeBattle.StateHash();Advance(game,.5f);Check(game.CurrentPracticeBattle.StateHash()==hash,"Finished playback stays fixed");
            }
        }
        var zero=new PracticeBattle();zero.Surrender();Start(game,zero.Record());
        Check(game.PracticeReplayMatches && GameObject.Find("Replay Progress").GetComponent<Text>().text=="COMPLETE  |  0:00.0 / 0:00.0","Zero-duration recording");
        Click("Restart Replay");Check(game.PracticeReplayMatches,"Zero-duration restart");
    }

    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("ReplayControls.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("ReplayControls.Step",0);
            if(step==0)
            {
                game.OpenBattleHistory();Click("History Older");Click("History Replay 0");
                string state=Snapshot(JsonUtility.ToJson(game.State)),disk=Snapshot(PlayerPrefs.GetString(VillageSave.Key));
                Check(GameObject.Find("Replay Controls")!=null && GameObject.Find("Deploy Raider 0")==null,"Replay controls replace deployment controls");
                Advance(game,.5f);Check(game.CurrentPracticeBattle.Tick==5,"Normal speed advances five ticks per half second");
                Click("Replay Speed");Advance(game,.5f);Check(game.CurrentPracticeBattle.Tick==15,"Double speed");
                Click("Replay Speed");Advance(game,.5f);Check(game.CurrentPracticeBattle.Tick==35,"Quadruple speed");
                Click("Pause Replay");ulong hash=game.CurrentPracticeBattle.StateHash();Advance(game,.5f);
                Call(game,"OnApplicationPause",true);Call(game,"OnApplicationPause",false);Advance(game,.5f);
                Check(game.CurrentPracticeBattle.StateHash()==hash && FindButton("Pause Replay").GetComponentInChildren<Text>().text=="RESUME","User pause survives application resume");
                Capture("replay-controls-paused.png");Capture("replay-controls-paused-16x9.png",1280,720);
                Click("Replay Speed");Check(FindButton("Replay Speed").GetComponentInChildren<Text>().text=="SPEED: 1x","Speed wraps while paused");
                Click("Pause Replay");Call(game,"OnApplicationPause",true);Advance(game,.5f);Check(game.CurrentPracticeBattle.StateHash()==hash,"Application suspension stops playing replay");
                Call(game,"OnApplicationPause",false);Advance(game,.5f);Check(game.CurrentPracticeBattle.Tick==40,"Resume continues without backlog");
                Click("Replay Speed");Click("Pause Replay");Click("Restart Replay");
                Check(game.CurrentPracticeBattle.Tick==0 && FindButton("Pause Replay").GetComponentInChildren<Text>().text=="PAUSE" && FindButton("Replay Speed").GetComponentInChildren<Text>().text=="SPEED: 1x","Mid-playback restart resets time, pause and speed");
                Click("Replay Speed");Click("Replay Speed");
                while(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)Advance(game,.5f);
                Check(game.PracticeReplayMatches,"Restart retains full recording and original result");Capture("replay-controls-complete.png");
                Click("Restart Replay");Check(game.CurrentPracticeBattle.Tick==0,"Restart completed recording");
                Check(Snapshot(JsonUtility.ToJson(game.State))==state && Snapshot(PlayerPrefs.GetString(VillageSave.Key))==disk,"Playback leaves village and saved active checkpoint unchanged");
                VerifySpeeds(game);
                Check(Snapshot(JsonUtility.ToJson(game.State))==state && Snapshot(PlayerPrefs.GetString(VillageSave.Key))==disk,"All speed/layout checks leave village unchanged");
                game.ClosePracticeBattle();Check(GameObject.Find("History Page Number").GetComponent<Text>().text=="2 / 2","Returns to same history page");
                game.CloseProfile();game.OpenPracticeBattle();
                Check(GameObject.Find("Replay Controls")==null,"No controls in scouting");
                game.ToggleReplayPause();game.CycleReplaySpeed();game.RestartReplay();Advance(game,.5f);Check(game.ScoutingPractice && game.CurrentPracticeBattle.Tick==0,"Replay methods cannot alter scouting");
                game.BeginPracticeAttack();game.CycleReplaySpeed();Advance(game,.5f);Check(game.CurrentPracticeBattle.Tick==5,"Live battle stays normal speed");
                game.SurrenderPracticeBattle();game.WatchPracticeReplay();Check(GameObject.Find("Replay Controls")!=null,"Practice replay has controls");
                Click("Replay Speed");Click("Pause Replay");game.ClosePracticeBattle();
                SessionState.SetInt("ReplayControls.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==1)
            {
                game.OpenBattleHistory();Click("History Older");Click("History Replay 0");
                Check(FindButton("Replay Speed").GetComponentInChildren<Text>().text=="SPEED: 1x" && FindButton("Pause Replay").GetComponentInChildren<Text>().text=="PAUSE","Reload starts fresh playback preferences");
                Click("Replay Speed");Click("Replay Speed");SessionState.SetInt("ReplayControls.Step",2);return;
            }
            if(step==2)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches,"Real Update loop completes verified accelerated playback after reload");
                Finish("PASS: saved and practice replay controls; pause/resume; pause retained through application suspension; 1x/2x/4x scheduling and wrap; restart during playback, pause and completion; four layouts with staggered mixed troop commands and exact state equality at each playback batch; original final hashes; zero-duration replay; progress and disabled finished controls; unchanged village, currency, roster, active checkpoint and history; scouting/live battle isolation; same-page history return; scene reload and real-frame accelerated playback. Recovery, saved replay, history, campaign, mixed army, Tank, tutorial and resource/progression rule regressions passed. Unity "+Application.unityVersion+". Screenshots at 1600x702 and 1280x720. No APK rebuild or physical-phone test.",0);
            }
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"ReplayControlsValidation.txt"),message);EditorApplication.Exit(code);}
}
