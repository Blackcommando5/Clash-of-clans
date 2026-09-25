using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class CrowdSeparationValidation
{
    const string Pending="Separation.Pending";
    static CrowdSeparationValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [Serializable] public sealed class LegacyFixtures
    {
        public string[] checkpointVillages = new string[4];
        public CampaignHistoryEntry[] results = new CampaignHistoryEntry[4];
    }
    static LegacyFixtures Fixtures()=>JsonUtility.FromJson<LegacyFixtures>(File.ReadAllText(Path.Combine(Application.dataPath,"Editor/Fixtures/CombatRules6.json")));
    static void LegacyChecks()
    {
        var fixtures=Fixtures();
        for(int mission=0;mission<4;mission++)
        {
            Check(SavedBattleReplay.TryRestore(fixtures.results[mission],out var recording,out _) && recording.CombatRules==6,"Pre-change replay restores under rules 6");
            var replay=new PracticeReplay(recording);while(!replay.Finished)replay.Step();Check(replay.Matches,"Golden rules-6 final hash");
            Check(VillageState.TryDeserialize(fixtures.checkpointVillages[mission],out var state) && SavedBattleReplay.TryRestoreCheckpoint(state,out _,out _),"Pre-change village checkpoint loads");
            SavedBattleReplay.TryRestoreCheckpoint(state,out var battle,out _);
            Check(battle.CombatRules==6 && battle.Tick==70,"Legacy checkpoint retains engine and exact tick");
            battle.Step();Check(state.TryCheckpointCampaign(state.campaignRunId,battle,out _) && JsonUtility.FromJson<SavedBattleReplay>(state.campaignCheckpoint).rules==6,"Resaving old attack retains rules");
            while(battle.Outcome==PracticeOutcome.Running)battle.Step();
            Check(battle.StateHash()==recording.FinalHash && state.TryFinishCampaign(state.campaignRunId,battle.Record(),out _) && state.TryClaimCampaign(state.campaignRunId,out _),"Legacy continued combat and reward match original");
            Check(JsonUtility.FromJson<SavedBattleReplay>(state.battleHistory[0].replayJson).rules==6,"Finished legacy attack keeps replay version");
        }
    }
    static int ClosePairs(PracticeBattle b)
    {
        int pairs=0;
        for(int i=0;i<b.Raiders.Count;i++)for(int j=i+1;j<b.Raiders.Count;j++)
        {var a=b.Raiders[i];var c=b.Raiders[j];long x=a.X-c.X,z=a.Z-c.Z;if(a.Alive && c.Alive && x*x+z*z<900)pairs++;}
        return pairs;
    }
    static void Geometry(PracticeBattle b)
    {
        foreach(var troop in b.Raiders)
        {
            if(!troop.Alive)continue;
            Check(Math.Abs(troop.X)<=1800 && Math.Abs(troop.Z)<=1800,"Troop stays in navigation bounds");
            foreach(var building in b.Buildings)
                Check(!building.Alive || Math.Abs(troop.X-building.X)>=building.HalfSize+15 || Math.Abs(troop.Z-building.Z)>=building.HalfSize+15,"No troop pushed into a live wall/building");
        }
    }
    static void Rules()
    {
        LegacyChecks();
        var old=new PracticeBattle(false,8,0,0,6);var crowded=new PracticeBattle();
        for(int i=0;i<8;i++){old.DeployAt(0,-1200);crowded.DeployAt(0,-1200);}
        for(int i=0;i<40;i++){old.Step();crowded.Step();Geometry(crowded);}
        Check(ClosePairs(old)==28 && ClosePairs(crowded)<=6,"Coincident deployments should spread: close pairs "+ClosePairs(crowded));
        for(int mission=0;mission<4;mission++)
        {
            var b=new PracticeBattle(EnemyLayoutCatalog.Find(mission),16,4,4);
            for(int i=0;i<4;i++)b.DeployAt(-600+i*400,-1050,"Tank");
            for(int i=0;i<8;i++)b.Deploy(i%3);
            var hashes=new List<ulong>{b.StateHash()};
            VillageState recovered=null;
            while(b.Outcome==PracticeOutcome.Running)
            {
                b.Step();if(b.Tick==10)for(int i=0;i<4;i++)b.Deploy(i%3,"Archer");
                Geometry(b);hashes.Add(b.StateHash());
                if(b.Tick==70)
                {
                    VillageState.TryDeserialize(Fixtures().checkpointVillages[mission],out recovered);
                    Check(recovered.TryCheckpointCampaign(recovered.campaignRunId,b,out _) && SavedBattleReplay.TryRestoreCheckpoint(recovered,out var restored,out _) && restored.CombatRules==7 && restored.StateHash()==b.StateHash(),"Rules-7 checkpoint exact reconstruction");
                }
            }
            Check(b.Outcome==PracticeOutcome.Victory,"Existing 32-space mixed strategy still wins mission "+mission);
            var replay=new PracticeReplay(b.Record());Check(replay.Battle.StateHash()==hashes[0],"Initial hash");
            while(!replay.Finished){replay.Step();Check(replay.Battle.StateHash()==hashes[replay.Battle.Tick],"Separated replay matches each tick");}
            Check(replay.Matches,"Rules-7 replay final hash");
            Check(SavedBattleReplay.TryRestoreCheckpoint(recovered,out var continuation,out _),"Restore continuation");
            while(continuation.Outcome==PracticeOutcome.Running){continuation.Step();Check(continuation.StateHash()==hashes[continuation.Tick],"Resumed separation matches each tick");}
        }
        var maximum=new PracticeBattle(false,80);for(int i=0;i<80;i++)maximum.DeployAt(0,-1200);
        for(int i=0;i<60;i++){maximum.Step();Geometry(maximum);}Check(ClosePairs(maximum)<3160,"Maximum 80-unit crowd spreads");
        maximum.Surrender();var maxReplay=new PracticeReplay(maximum.Record());while(!maxReplay.Finished)maxReplay.Step();Check(maxReplay.Matches,"Maximum crowd deterministic replay");
        foreach(int rules in new[]{0,5,8,int.MaxValue}){bool rejected=false;try{new PracticeBattle(combatRules:rules);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"Unsupported rules rejected");}
        typeof(PracticeBattleValidation).GetMethod("StateChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(EnemyLayoutValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(BattleRecoveryValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project.");
        try
        {
            Rules();VillageState.TryDeserialize(Fixtures().checkpointVillages[0],out var seed);
            Check(VillageSave.TryWrite(seed,out _) && PlayerProfile.TrySaveName("CrowdChief",out _),"Seed legacy UI attack");
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");SessionState.SetInt("Separation.Step",0);SessionState.SetString("Separation.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static object Call(VillageGameplay game,string method,params object[] args)=>typeof(VillageGameplay).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("Separation.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("Separation.Step",0);
            if(step==0)
            {
                game.OpenCampaign();Click("Resume Campaign");Check(game.CurrentPracticeBattle.CombatRules==6 && game.CurrentPracticeBattle.Tick==70,"UI resumes legacy engine");
                game.ClosePracticeBattle();game.OpenCampaign();Click("Resume Campaign");Check(game.CurrentPracticeBattle.CombatRules==6,"Save and return preserves legacy engine");
                game.SurrenderPracticeBattle();game.WatchPracticeReplay();Check(game.CurrentPracticeBattle.CombatRules==6,"Live legacy replay keeps engine");game.ClosePracticeBattle();
                game.OpenBattleHistory();Click("History Replay 0");Check(game.CurrentPracticeBattle.CombatRules==6,"History replay keeps engine");game.ClosePracticeBattle();game.CloseProfile();
                game.OpenPracticeBattle();game.BeginPracticeAttack();Check(game.CurrentPracticeBattle.CombatRules==7,"New attack uses separation");
                for(int i=0;i<8;i++)game.CurrentPracticeBattle.DeployAt(0,-1200);
                SessionState.SetInt("Separation.Step",1);return;
            }
            if(step==1)
            {
                if(game.CurrentPracticeBattle.Tick<40)return;
                Check(ClosePairs(game.CurrentPracticeBattle)<=6,"Live rendered crowd spread");Capture("separated-troops.png");Capture("separated-troops-16x9.png",1280,720);
                game.SurrenderPracticeBattle();game.WatchPracticeReplay();Click("Replay Speed");Click("Replay Speed");SessionState.SetInt("Separation.Step",2);return;
            }
            if(step==2)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches,"Accelerated separated replay exact result");game.ClosePracticeBattle();
                game.OpenCampaign();Click("Resolve Campaign");game.OpenArmyPreparation();Click("Fill Army");game.OpenCampaign();Click("Campaign Gate");game.BeginPracticeAttack();game.DeployPracticeRaider(1);game.DeployPracticeRaider(1);
                Check(game.CurrentPracticeBattle.CombatRules==7,"New committed attack uses rules 7");Call(game,"OnApplicationPause",true);SessionState.SetString("Separation.Hash",game.CurrentPracticeBattle.StateHash().ToString());
                SessionState.SetInt("Separation.Step",3);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==3)
            {
                game.ResumeCampaignAttack();Check(game.CurrentPracticeBattle.CombatRules==7 && game.CurrentPracticeBattle.StateHash().ToString()==SessionState.GetString("Separation.Hash",""),"New engine checkpoint survives reload exactly");
                Finish("PASS: bounded deterministic crowd separation; coincident and maximum 80-unit deployment; all four layouts remain winnable with the existing 32-space mixed strategy; every-tick replay and checkpoint continuation equality; no intrusion into live walls/buildings or navigation bounds; unsupported rules rejected; pre-change golden rules-6 recordings and checkpoints on four layouts retain original final hashes and once-only rewards; old UI resume/save/replay/history plus new rules-7 live playback, 4x replay and scene-reload recovery. Practice navigation/scoring, authored layouts, recovery, saved replay, history, campaign, mixed army, Tank, tutorial and economy/progression rule regressions passed. Unity "+Application.unityVersion+". Soft spacing may overlap in crowds; physical-phone performance and APK rebuild not tested.",0);
            }
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"CrowdSeparationValidation.txt"),message);EditorApplication.Exit(code);}
}
