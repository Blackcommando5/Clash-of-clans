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
public static class RepeatArmyValidation
{
    const string Pending="RepeatArmy.Pending";
    static RepeatArmyValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    static CampaignHistoryEntry Entry(int raiders,int archers=0,int tanks=0)=>new CampaignHistoryEntry {
        runId=Guid.NewGuid().ToString("N"),mission=0,raiders=raiders,archers=archers,tanks=tanks,
        outcome=PracticeOutcome.Surrendered,resolved=true,completedUtc=VillageState.Now,durationTicks=10,destruction=0
    };
    static VillageState Seed()
    {
        var s=VillageState.Create(VillageState.Now);
        s.buildings.Add(new PlacedBuilding{kind="Barracks",level=2,x=5,z=5});
        s.buildings.Add(new PlacedBuilding{kind="ArmyCamp",level=1,x=9,z=5});
        s.battleHistory.Add(Entry(2,1,1));s.battleHistory.Add(Entry(4,2,2));s.battleHistory.Add(Entry(8));
        Check(s.TrySetArmyCount("Raider",1,out _) && s.TryBeginCampaign(0,out _) && s.TrySetArmyCount("Archer",8,out _),"Seed separate active run and full roster");
        Check(s.IsValid(),"Valid fixture");return s;
    }
    static string WithoutArmy(VillageState state){var copy=state.Copy();copy.army.Clear();return JsonUtility.ToJson(copy);}
    static void Reject(VillageState state,int index)
    {string before=JsonUtility.ToJson(state);Check(!state.TryPrepareHistoryArmy(index,out _) && JsonUtility.ToJson(state)==before,"Rejected replacement is atomic");}
    static void Rules()
    {
        var s=Seed();string other=WithoutArmy(s),history=JsonUtility.ToJson(s.battleHistory[0]);
        Check(s.CanPrepareHistoryArmy(0,out _) && s.TryPrepareHistoryArmy(0,out _) && s.ArmyCountOf("Raider")==2 && s.ArmyCountOf("Archer")==1 && s.ArmyCountOf("Tank")==1 && s.ArmyHousing==8,"Exact mixed replacement");
        Check(WithoutArmy(s)==other && JsonUtility.ToJson(s.battleHistory[0])==history,"No campaign, checkpoint, economy, history or guide mutation");
        Check(s.TryPrepareHistoryArmy(1,out _) && s.ArmyHousing==16,"Capacity checked against replacement, not existing roster plus replacement");
        Check(s.TryPrepareHistoryArmy(2,out _) && s.army.Count==1 && s.ArmyCountOf("Raider")==8,"Absent troop types removed");
        s.army[0].count=1;Check(s.battleHistory[2].raiders==8,"Roster does not alias history");
        Check(s.TryPrepareHistoryArmy(2,out _) && VillageSave.TryWrite(s,out _) && VillageSave.TryLoad(out var loaded,out _) && loaded.ArmyCountOf("Raider")==8,"Persist replacement");
        Reject(s,-1);Reject(s,3);Reject(VillageState.Create(1000),0);
        s=Seed();s.army.Clear();s.buildings.RemoveAll(b=>b.kind=="ArmyCamp");Reject(s,1);
        s=Seed();s.army.Clear();s.buildings.Find(b=>b.kind=="Barracks").level=1;Reject(s,0);
        s=Seed();s.army.Clear();s.buildings.RemoveAll(b=>b.kind=="Barracks");Reject(s,1);
        s=Seed();s.battleHistory[0].raiders=int.MaxValue;Reject(s,0);
        s=Seed();s.battleHistory[0]=null;Reject(s,0);
        s=Seed();s.battleHistory=null;Reject(s,0);
        s=Seed();s.army=null;Reject(s,0);
        // Any saved outcome can supply its composition, even without a readable recording.
        foreach(var outcome in new[]{PracticeOutcome.Victory,PracticeOutcome.Defeat,PracticeOutcome.Surrendered,PracticeOutcome.Timeout,PracticeOutcome.Running})
        {
            s=Seed();var entry=s.battleHistory[0];entry.outcome=outcome;entry.replayJson="expired or corrupt";
            if(outcome==PracticeOutcome.Victory){entry.stars=3;entry.destruction=100;}
            if(outcome==PracticeOutcome.Running){entry.abandoned=true;entry.durationTicks=-1;entry.destruction=-1;}
            Check(s.TryPrepareHistoryArmy(0,out _) && s.IsValid(),"Summary reusable regardless of outcome or optional replay");
        }
        s=Seed();s.battleHistory[0].durationTicks=s.battleHistory[0].destruction=-1;s.battleHistory[0].completedUtc=0;
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out s) && s.TryPrepareHistoryArmy(0,out _),"Older summary with missing battle details works");
        var pending=VillageState.Create(VillageState.Now);pending.TrySetArmyCount("Raider",8,out _);pending.TryBeginCampaign(0,out _);
        var battle=new PracticeBattle();for(int i=0;i<8;i++)battle.Deploy(i%3);while(battle.Outcome==PracticeOutcome.Running)battle.Step();
        Check(pending.TryFinishCampaign(pending.campaignRunId,battle.Record(),out _),"Pending victory fixture");other=WithoutArmy(pending);
        Check(pending.TryPrepareHistoryArmy(0,out _) && WithoutArmy(pending)==other && !pending.battleHistory[0].resolved,"Reprepare pending victory without claim");
        Check(pending.TryClaimCampaign(pending.campaignRunId,out _) && pending.ArmyCountOf("Raider")==8 && pending.gold==1500,"Normal once-only claim preserves prepared army");
        typeof(BattleRecoveryValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            Rules();Check(VillageSave.TryWrite(Seed(),out _) && PlayerProfile.TrySaveName("ArmyReuseChief",out _),"Seed UI");EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            SessionState.SetInt("RepeatArmy.Step",0);SessionState.SetString("RepeatArmy.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("RepeatArmy.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("RepeatArmy.Step",0);
            if(step==0)
            {
                string checkpoint=game.State.campaignCheckpoint;game.OpenBattleHistory();Capture("reuse-army-history.png");Capture("reuse-army-history-16x9.png",1280,720);
                Check(!Button("History Replay 0").interactable && Button("History Army 0").interactable,"Summary without replay supports army reuse");
                Click("History Army 0");Check(game.State.ArmyHousing==8 && GameObject.Find("Army Summary")!=null && game.State.campaignCheckpoint==checkpoint,"History action opens prepared mixed army, preserves committed battle");
                Click("Clear Army");Click("Prepare Last Army");Check(game.State.ArmyCountOf("Tank")==1 && game.State.ArmyCountOf("Archer")==1,"Last Army rebuilds exact latest roster");
                Capture("reuse-army-preparation.png");Capture("reuse-army-preparation-16x9.png",1280,720);
                game.OpenBattleHistory();Click("History Older");Click("History Army 0");Check(game.State.army.Count==1 && game.State.ArmyCountOf("Raider")==8,"Paged history uses selected entry");
                game.OpenBattleHistory();string disk=PlayerPrefs.GetString(VillageSave.Key);game.State.gold=-1;Click("History Army 0");
                Check(game.State.ArmyCountOf("Tank")==0 && PlayerPrefs.GetString(VillageSave.Key)==disk && GameObject.Find("Battle History Help").GetComponent<Text>().text.Length>0,"Rejected save keeps old roster and disk");game.State.gold=1000;
                game.State.buildings.RemoveAll(b=>b.kind=="ArmyCamp");Click("History Army 1");
                Check(game.State.ArmyCountOf("Raider")==8 && GameObject.Find("Battle History Help").GetComponent<Text>().text.Contains("16 spaces"),"Oversized history reports capacity without replacing");
                game.State.buildings.Add(new PlacedBuilding{kind="ArmyCamp",level=1,x=9,z=5});Click("History Army 1");
                Check(game.State.ArmyHousing==16 && game.State.ArmyCountOf("Tank")==2,"Retry after capacity restored works");
                SessionState.SetString("RepeatArmy.Checkpoint",checkpoint);SessionState.SetInt("RepeatArmy.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==1)
            {
                Check(game.State.ArmyHousing==16 && game.State.ArmyCountOf("Tank")==2 && game.State.campaignCheckpoint==SessionState.GetString("RepeatArmy.Checkpoint","") && game.State.campaignArmy==1,"Reload preserves new roster and separate attack");
                game.OpenArmyPreparation();Click("Prepare Last Army");Check(game.State.ArmyHousing==8,"Shortcut still works after restart");
                game.CloseProfile();game.ResumeCampaignAttack();string before=JsonUtility.ToJson(game.State);game.PrepareArmyFromHistory(1);Check(JsonUtility.ToJson(game.State)==before && game.CurrentPracticeBattle.ArmyBudget==1,"Reuse cannot change active battlefield");game.ClosePracticeBattle();
                game.State.battleHistory.Clear();game.OpenArmyPreparation();Check(!Button("Prepare Last Army").interactable && GameObject.Find("Army Rules").GetComponent<Text>().text.Contains("Finish a campaign"),"Empty history explains disabled shortcut");
                game.OpenBattleHistory();Check(GameObject.Find("History Army 0")==null,"Empty history hides reuse actions");
                Finish("PASS: atomic whole-roster replacement; mixed counts, weighted capacity, removal of absent types and independent history data; current Barracks unlocks; capacity, invalid-index, null and malformed-data rejection; every outcome, legacy summaries and unreadable optional replays; pending victory remains unclaimed; ordinary once-only payout; unchanged currency, active campaign/checkpoint and history; history paging and Use Army controls; Last Army shortcut; candidate-save failure keeps prior roster/disk; capacity feedback/retry; scene reload and battle input guard; empty history; 1600x702 and 1280x720 screenshots. Recovery, saved replay, history, campaign, mixed army, Tank, tutorial and resource/progression rule regressions passed in Unity "+Application.unityVersion+". No APK rebuild or physical-phone test.",0);
            }
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"RepeatArmyValidation.txt"),message);EditorApplication.Exit(code);}
}
