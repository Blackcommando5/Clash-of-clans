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
public static class SavedReplayValidation
{
    const string Pending="SavedReplay.Pending";
    static SavedReplayValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static CampaignHistoryEntry Entry(PracticeBattle battle)=>new CampaignHistoryEntry {
        mission=battle.Layout.Id,raiders=battle.RaiderBudget,archers=battle.ArcherBudget,tanks=battle.TankBudget,
        stars=battle.Stars,destruction=battle.Destruction,durationTicks=battle.Tick,outcome=battle.Outcome,
        replayJson=SavedBattleReplay.Encode(battle.Record())
    };
    static void RoundTrip(PracticeBattle battle)
    {
        var entry=JsonUtility.FromJson<CampaignHistoryEntry>(JsonUtility.ToJson(Entry(battle)));
        Check(SavedBattleReplay.TryRestore(entry,out var restored,out _),"Restore "+battle.Layout.Name+" "+battle.Outcome);
        var expected=new PracticeReplay(battle.Record());var actual=new PracticeReplay(restored);
        Check(expected.Battle.StateHash()==actual.Battle.StateHash(),"Initial equality");
        while(!expected.Finished){expected.Step();actual.Step();Check(expected.Battle.StateHash()==actual.Battle.StateHash(),"Per-tick equality");}
        Check(actual.Matches && restored.FinalHash==battle.StateHash(),"Final verified hash");
    }
    static void Reject(CampaignHistoryEntry source,Action<SavedBattleReplay> edit)
    {
        var entry=JsonUtility.FromJson<CampaignHistoryEntry>(JsonUtility.ToJson(source));
        var data=JsonUtility.FromJson<SavedBattleReplay>(entry.replayJson);edit(data);entry.replayJson=JsonUtility.ToJson(data);
        Check(!SavedBattleReplay.TryRestore(entry,out _,out _),"Reject invalid replay");
    }
    static PracticeBattle Win()
    {
        var battle=new PracticeBattle();for(int i=0;i<8;i++)battle.Deploy(i%3);
        while(battle.Outcome==PracticeOutcome.Running)battle.Step();Check(battle.Outcome==PracticeOutcome.Victory,"Victory fixture");return battle;
    }
    static VillageState Seed()
    {
        var state=VillageState.Create(VillageState.Now);state.TrySetArmyCount("Raider",8,out _);state.TryBeginCampaign(0,out _);
        Check(state.TryFinishCampaign(state.campaignRunId,Win().Record(),out _) && state.TryClaimCampaign(state.campaignRunId,out _),"Recorded victory and claim");
        for(int i=0;i<2;i++){state.TrySetArmyCount("Raider",1,out _);state.TryBeginCampaign(0,out _);state.TryAbandonCampaign(state.campaignRunId,out _);}
        state.TrySetArmyCount("Raider",2,out _);state.TryBeginCampaign(1,out _);state.TrySetArmyCount("Raider",3,out _);
        Check(state.IsValid(),"Separate active attempt and prepared roster");return state;
    }
    static void Rules()
    {
        RoundTrip(Win());
        for(int layout=0;layout<4;layout++)
        {
            var battle=new PracticeBattle(EnemyLayoutCatalog.Find(layout),4,1,1);
            for(int i=0;i<7;i++)battle.Step();
            Check(battle.DeployAt(-300,-1200,"Raider") && battle.DeployAt(50,-1200,"Archer") && battle.DeployAt(300,-1200,"Tank"),"Exact mixed commands");
            for(int i=0;i<12;i++)battle.Step();battle.Surrender();RoundTrip(battle);
        }
        var empty=new PracticeBattle();empty.Surrender();RoundTrip(empty);
        var timeout=new PracticeBattle();while(timeout.Outcome==PracticeOutcome.Running)timeout.Step();RoundTrip(timeout);
        var defeat=new PracticeBattle(false,1);defeat.Deploy(1);while(defeat.Outcome==PracticeOutcome.Running)defeat.Step();Check(defeat.Outcome==PracticeOutcome.Defeat,"Defeat fixture");RoundTrip(defeat);
        var largest=new PracticeBattle(false,80);for(int i=0;i<80;i++)largest.Deploy(i%3);largest.Surrender();
        Check(Entry(largest).replayJson.Length<SavedBattleReplay.MaximumJsonLength,"Largest allowed command list fits storage bound");RoundTrip(largest);
        var fixture=Entry(Win());
        Reject(fixture,d=>d.format++);Reject(fixture,d=>d.rules++);Reject(fixture,d=>d.layoutRevision++);Reject(fixture,d=>d.layout=99);
        Reject(fixture,d=>d.finalHash="0000000000000000");Reject(fixture,d=>d.army=int.MaxValue);Reject(fixture,d=>d.commands=null);
        Reject(fixture,d=>d.commands[0]=null);Reject(fixture,d=>d.commands[0].troop="Unknown");Reject(fixture,d=>d.commands[0].tick=-1);
        Reject(fixture,d=>d.commands[0].x=int.MaxValue);Reject(fixture,d=>d.commands[0].tick=1);Reject(fixture,d=>d.endTick=1801);
        Reject(fixture,d=>d.commands[0].troop="Tank");Reject(fixture,d=>d.commands[0].x+=50);
        fixture.replayJson="{";Check(!SavedBattleReplay.TryRestore(fixture,out _,out _),"Malformed JSON");
        fixture.replayJson=new string('x',SavedBattleReplay.MaximumJsonLength+1);Check(!SavedBattleReplay.CanRead(fixture,out _),"Oversized recording");
        var state=Seed();Check(VillageSave.TryWrite(state,out _) && VillageSave.TryLoad(out state,out _),"Save and reload attachment");
        string before=JsonUtility.ToJson(state);
        Check(SavedBattleReplay.TryRestore(state.battleHistory[2],out _,out _) && JsonUtility.ToJson(state)==before,"Replay restore is read-only with unrelated active run");
        state.battleHistory[2].replayJson="broken";
        Check(VillageSave.TryWrite(state,out _) && VillageSave.TryLoad(out state,out _) && !SavedBattleReplay.TryRestore(state.battleHistory[2],out _,out _),"Bad optional recording does not block village load");
        state.battleHistory[2].replayJson=null;
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(state),out state) && state.battleHistory.Count==3 && !SavedBattleReplay.CanRead(state.battleHistory[2],out _),"Existing v9 summary without recording remains valid");
        typeof(BattleHistoryValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project.");
        try {
            Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            Check(VillageSave.TryWrite(Seed(),out _) && PlayerProfile.TrySaveName("ReplayChief",out _),"Seed UI");
            SessionState.SetInt("SavedReplay.Step",0);SessionState.SetString("SavedReplay.Start",DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void Unchanged(VillageGameplay game)
    {
        Check(game.State.gold==1500 && game.State.elixir==800 && game.State.ArmyCount==3 && game.State.campaignArmy==2 && game.State.campaignMission==1 &&
            game.State.campaignOutcome==PracticeOutcome.Running && game.State.battleHistory.Count==3 && game.State.battleHistory[2].goldAwarded==500,"Replay preserves economy, roster, active run and history");
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("SavedReplay.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("SavedReplay.Step",0);
            if(step==0)
            {
                game.OpenBattleHistory();Check(!GameObject.Find("History Replay 0").GetComponent<Button>().interactable,"Abandoned replay disabled");
                Click("History Older");Check(GameObject.Find("History Replay 0").GetComponent<Button>().interactable,"Saved replay available on older page");Capture("saved-replay-history.png");Capture("saved-replay-history-16x9.png",1280,720);
                Click("History Replay 0");Check(game.WatchingPracticeReplay && !game.CampaignBattleOpen && !game.ScoutingPractice,"History starts isolated playback");
                game.ResetPracticeBattle();Check(!game.SelectPracticeChallenge(true) && game.WatchingPracticeReplay,"Replay cannot enter practice retry or switch layout");
                game.SurrenderPracticeBattle();game.DeployPracticeRaider(0);Unchanged(game);
                Check(GameObject.Find("Claim Campaign Battle")==null && GameObject.Find("Begin Practice Attack")==null && GameObject.Find("Retry Practice")==null,"Mutating battle controls hidden");
                SessionState.SetInt("SavedReplay.Step",1);return;
            }
            if(step==1)
            {
                if(game.CurrentPracticeBattle.Tick<50)return;Capture("saved-replay-playing.png");SessionState.SetInt("SavedReplay.Step",2);return;
            }
            if(step==2)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches,"Playback result matches saved attack");Unchanged(game);Capture("saved-replay-complete.png");
                Click("Watch Practice Replay");Check(game.WatchingPracticeReplay && game.CurrentPracticeBattle.Tick==0,"Rewatch restarts saved playback");
                game.ClosePracticeBattle();Check(game.ProfileOpen && GameObject.Find("History Page Number").GetComponent<Text>().text=="2 / 2","Back returns to same history page");Unchanged(game);
                game.CloseProfile();Check(!game.cameraController.InputBlocked,"Camera restored after history close");SessionState.SetInt("SavedReplay.Step",3);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==3)
            {
                Unchanged(game);game.OpenBattleHistory();Click("History Older");Click("History Replay 0");Check(game.WatchingPracticeReplay,"Replay survives scene reload");game.ClosePracticeBattle();
                var entry=game.State.battleHistory[2];var data=JsonUtility.FromJson<SavedBattleReplay>(entry.replayJson);data.finalHash="0000000000000000";entry.replayJson=JsonUtility.ToJson(data);
                Click("History Replay 0");Check(!game.PracticeOpen && GameObject.Find("Battle History Help").GetComponent<Text>().text.Contains("could not be verified"),"Corrupt replay safely stays in history");Unchanged(game);
                Finish("PASS: persisted exact commands and mixed composition; four authored layouts; victory, defeat, surrender, timeout, zero-tick surrender and 80-command limit; per-tick and final hash equality; format/rules/layout expiry, bad hashes, malformed/oversized JSON and illegal commands; legacy v9 summaries and corrupt attachments preserve village loading; replay leaves active campaign, army, currency, claims and history unchanged; UI paging selection, disabled missing replay, live playback, rewatch, same-page return, scene reload and corrupt replay feedback. History, campaign, mixed army, Tank, tutorial and resource/progression rule regressions passed. Unity "+Application.unityVersion+". Screenshots 1600x702 and 1280x720. No APK rebuild or phone test.",0);
            }
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"SavedReplayValidation.txt"),message);EditorApplication.Exit(code);}
}
