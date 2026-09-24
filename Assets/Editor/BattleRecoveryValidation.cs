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
public static class BattleRecoveryValidation
{
    const string Pending="BattleRecovery.Pending";
    static BattleRecoveryValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static object Call(VillageGameplay game,string name,params object[] args)=>typeof(VillageGameplay).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    static VillageState Mixed(int mission)
    {
        var s=VillageState.Create(1000);s.buildings.Add(new PlacedBuilding{kind="Barracks",level=2,x=5,z=5});s.campaignCleared=(1<<mission)-1;
        Check(s.TrySetArmyCount("Raider",2,out _) && s.TrySetArmyCount("Archer",1,out _) && s.TrySetArmyCount("Tank",1,out _) && s.TryBeginCampaign(mission,out _),"Mixed commitment");return s;
    }
    static void Corrupt(VillageState original,Action<SavedBattleReplay> edit)
    {
        var s=original.Copy();var data=JsonUtility.FromJson<SavedBattleReplay>(s.campaignCheckpoint);edit(data);s.campaignCheckpoint=JsonUtility.ToJson(data);
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out s) && !SavedBattleReplay.TryRestoreCheckpoint(s,out _,out _) && s.TryAbandonCampaign(s.campaignRunId,out _),"Invalid checkpoint preserves load and abandonment");
    }
    static void Rules()
    {
        for(int mission=0;mission<4;mission++)
        {
            var s=Mixed(mission);Check(SavedBattleReplay.TryRestoreCheckpoint(s,out var battle,out _) && battle.Tick==0 && battle.Remaining==4,"Initial checkpoint");
            battle.DeployAt(-300,-1200,"Raider");battle.DeployAt(0,-1200,"Archer");battle.DeployAt(300,-1200,"Tank");
            for(int i=0;i<120;i++)battle.Step();
            Check(s.TryCheckpointCampaign(s.campaignRunId,battle,out _) && VillageSave.TryWrite(s,out _) && VillageSave.TryLoad(out s,out _),"Persist active battle");
            string original=JsonUtility.ToJson(s);
            Check(SavedBattleReplay.TryRestoreCheckpoint(s,out var restored,out _) && restored.StateHash()==battle.StateHash() && restored.Remaining==1 && JsonUtility.ToJson(s)==original,"Resume restores exact state without mutation");
            Check(!s.TryCheckpointCampaign("stale",battle,out _) && !s.TryCheckpointCampaign(s.campaignRunId,new PracticeBattle(false,1),out _),"Wrong run and army rejected");
            Check(battle.DeployAt(50,-1300) && restored.DeployAt(50,-1300),"Continue with remaining troop");
            while(battle.Outcome==PracticeOutcome.Running){battle.Step();restored.Step();Check(restored.StateHash()==battle.StateHash(),"Continued per-tick route/combat equality");}
            Check(s.TryFinishCampaign(s.campaignRunId,restored.Record(),out _) && string.IsNullOrEmpty(s.campaignCheckpoint) && s.battleHistory.Count==1,"Finish clears checkpoint and records once");
            Check(SavedBattleReplay.TryRestore(s.battleHistory[0],out var replay,out _) && replay.FinalHash==restored.StateHash(),"Recovered attack retains full replay");
        }
        var state=VillageState.Create(1000);state.TrySetArmyCount("Raider",8,out _);state.TryBeginCampaign(0,out _);
        SavedBattleReplay.TryRestoreCheckpoint(state,out var win,out _);for(int i=0;i<8;i++)win.Deploy(i%3);for(int i=0;i<10;i++)win.Step();
        Check(state.TryCheckpointCampaign(state.campaignRunId,win,out _),"Victory checkpoint");
        Corrupt(state,d=>d.runId=Guid.NewGuid().ToString("N"));Corrupt(state,d=>d.rules++);Corrupt(state,d=>d.layoutRevision++);Corrupt(state,d=>d.finalHash="0000000000000000");Corrupt(state,d=>d.commands[0].x=int.MaxValue);
        var legacy=state.Copy();legacy.campaignCheckpoint=null;
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(legacy),out legacy) && !SavedBattleReplay.TryRestoreCheckpoint(legacy,out _,out _) && legacy.TryAbandonCampaign(legacy.campaignRunId,out _),"Legacy active attempt has no invented checkpoint");
        var broken=state.Copy();broken.campaignCheckpoint="{";Check(VillageState.TryDeserialize(JsonUtility.ToJson(broken),out broken) && !SavedBattleReplay.TryRestoreCheckpoint(broken,out _,out _),"Malformed checkpoint does not discard village");
        Check(SavedBattleReplay.TryRestoreCheckpoint(state,out win,out _),"Restore future victory");while(win.Outcome==PracticeOutcome.Running)win.Step();
        string run=state.campaignRunId;Check(win.Outcome==PracticeOutcome.Victory && state.TryFinishCampaign(run,win.Record(),out _) && state.TryClaimCampaign(run,out _) && state.gold==1500 && !state.TryClaimCampaign(run,out _) && state.battleHistory.Count==1,"Recovered victory pays exactly once");
        Check(!SavedBattleReplay.TryRestoreCheckpoint(state,out _,out _),"Claimed attempt cannot resume");
        typeof(SavedReplayValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project.");
        try {
            Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");var seed=VillageState.Create(VillageState.Now);seed.TrySetArmyCount("Raider",8,out _);
            Check(VillageSave.TryWrite(seed,out _) && PlayerProfile.TrySaveName("RecoveryChief",out _),"Seed UI");
            SessionState.SetInt("BattleRecovery.Step",0);SessionState.SetString("BattleRecovery.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("BattleRecovery.Start","0")))).TotalSeconds<180,"Timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("BattleRecovery.Step",0);
            if(step==0)
            {
                game.OpenCampaignBattle(0);game.BeginPracticeAttack();game.DeployPracticeRaider(1);game.DeployPracticeRaider(1);
                Check(VillageSave.TryLoad(out var saved,out _) && SavedBattleReplay.TryRestoreCheckpoint(saved,out var restored,out _) && restored.StateHash()==game.CurrentPracticeBattle.StateHash(),"Deployment saves immediately");
                SessionState.SetInt("BattleRecovery.Step",1);return;
            }
            if(step==1)
            {
                if(game.CurrentPracticeBattle.Tick<30)return;
                Check(VillageSave.TryLoad(out var saved,out _) && SavedBattleReplay.TryRestoreCheckpoint(saved,out var restored,out _) && restored.Tick>=20 && game.CurrentPracticeBattle.Tick-restored.Tick<20,"Periodic checkpoint interval");
                string disk=PlayerPrefs.GetString(VillageSave.Key);game.State.gold=-1;game.DeployPracticeRaider(1);
                Check(PlayerPrefs.GetString(VillageSave.Key)==disk && GameObject.Find("Retry Battle Save")!=null,"Rejected save leaves disk unchanged and offers retry");
                int tick=game.CurrentPracticeBattle.Tick,remaining=game.CurrentPracticeBattle.Remaining;Call(game,"TickPracticeBattle");game.DeployPracticeRaider(1);game.SurrenderPracticeBattle();
                Check(game.CurrentPracticeBattle.Tick==tick && game.CurrentPracticeBattle.Remaining==remaining && game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running,"Failed checkpoint pauses combat and input");
                Capture("recovery-save-retry.png");game.State.gold=1000;Click("Retry Battle Save");Check(GameObject.Find("Retry Battle Save")==null,"Retry recovers save");
                Call(game,"OnApplicationPause",true);SessionState.SetString("BattleRecovery.Hash",game.CurrentPracticeBattle.StateHash().ToString());SessionState.SetString("BattleRecovery.Run",game.State.campaignRunId);
                Check(VillageSave.TryLoad(out saved,out _) && SavedBattleReplay.TryRestoreCheckpoint(saved,out restored,out _) && restored.StateHash()==game.CurrentPracticeBattle.StateHash(),"Pause checkpoints exact state");
                SessionState.SetInt("BattleRecovery.Step",2);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==2)
            {
                game.OpenCampaign();Check(game.State.campaignRunId==SessionState.GetString("BattleRecovery.Run","") && game.State.ArmyCount==0,"Reload retains commitment");Capture("recovery-campaign.png");Capture("recovery-campaign-16x9.png",1280,720);
                Click("Resume Campaign");Check(game.CampaignBattleOpen && !game.ScoutingPractice && !game.WatchingPracticeReplay && game.CurrentPracticeBattle.StateHash().ToString()==SessionState.GetString("BattleRecovery.Hash",""),"Resume exact state without another army commitment");
                Capture("recovery-resumed.png");game.DeployPracticeRaider(0);Check(game.CurrentPracticeBattle.Remaining==4,"Remaining deployments work after resume");
                string hash=game.CurrentPracticeBattle.StateHash().ToString();game.ClosePracticeBattle();Check(!game.PracticeOpen && game.State.campaignOutcome==PracticeOutcome.Running && game.State.battleHistory.Count==0,"Save and return keeps active attack");
                game.OpenCampaign();Click("Resume Campaign");Check(game.CurrentPracticeBattle.StateHash().ToString()==hash,"Return and resume exact state");
                game.SurrenderPracticeBattle();Check(game.State.campaignOutcome==PracticeOutcome.Surrendered && game.State.battleHistory.Count==1 && string.IsNullOrEmpty(game.State.campaignCheckpoint),"Explicit surrender records result and clears checkpoint");
                Click("Claim Campaign Battle");Check(!game.State.HasCampaignRun && game.State.gold==1000,"Dismiss pays nothing");game.ClosePracticeBattle();game.OpenCampaign();Check(GameObject.Find("Resume Campaign")==null,"Resolved attack has no Resume control");
                game.CloseProfile();game.OpenPracticeBattle();game.BeginPracticeAttack();game.DeployPracticeRaider(1);game.ClosePracticeBattle();Check(game.State.battleHistory.Count==1 && !game.State.HasCampaignRun,"Practice remains separate");
                Finish("PASS: initial and partial checkpoints; four layouts, mixed composition, exact hash and continued per-tick equality; full replay after recovery; wrong run/army and incompatible/corrupt data rejection without village loss; legacy active attempts remain abandonable; recovered victory and once-only claim; automatic deployment and two-second saves; pause/scene reload/resume; save-and-return/resume; save rejection leaves previous disk state, pauses combat/input and retries; explicit surrender clears checkpoint; resolved controls and practice isolation. Saved-replay, history, campaign, mixed army, Tank, tutorial and resource/progression rule regressions passed. Unity "+Application.unityVersion+". Simulated lifecycle events and two landscape screenshot sizes; physical-device suspension, forced termination and disk-full faults not tested. APK not rebuilt.",0);
            }
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"BattleRecoveryValidation.txt"),message);EditorApplication.Exit(code);}
}
