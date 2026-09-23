using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class PracticeBattleValidation
{
    const string Pending="PracticeBattle.Pending";
    static string Root=>Path.GetDirectoryName(Application.dataPath);
    static PracticeBattleValidation(){EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use isolated ResourceValidation project and identity.");
        try
        {
            StateChecks();
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            Check(VillageSave.TryWrite(VillageState.Create(VillageState.Now),out _) && PlayerProfile.TrySaveName("PracticeChief",out _),"Isolated save setup");
            SessionState.SetInt("PracticeBattle.Step",0);SessionState.SetString("PracticeBattle.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static string Snapshot(PracticeBattle b)
    {
        var s=new StringBuilder();s.Append(b.Tick).Append(':').Append(b.Outcome).Append(':').Append(b.Remaining);
        foreach(var entity in b.Buildings)s.Append('|').Append(entity.Id).Append(',').Append(entity.HitPoints).Append(',').Append(entity.NextAttack);
        foreach(var entity in b.Raiders)s.Append('|').Append(entity.Id).Append(',').Append(entity.X).Append(',').Append(entity.Z).Append(',').Append(entity.HitPoints).Append(',').Append(entity.NextAttack);
        return s.ToString();
    }
    static void StateChecks()
    {
        NavigationChecks(false);NavigationChecks(true);
        ReplayChecks();
        ScoreChecks();
        var first=new PracticeBattle();var second=new PracticeBattle();
        Check(!first.Deploy(-1) && !first.Deploy(3) && first.Remaining==8,"Invalid deployment lanes");
        for(int i=0;i<8;i++){Check(first.Deploy(i%3) && second.Deploy(i%3),"Legal deployments");}
        Check(!first.Deploy(0) && first.Raiders.Count==8,"Army budget cannot be exceeded");
        bool defenseHit=false,raiderHit=false;
        for(int i=0;i<1800 && first.Outcome==PracticeOutcome.Running;i++)
        {
            first.Step();second.Step();Check(Snapshot(first)==Snapshot(second),"Repeat-run deterministic state");
            foreach(var strike in first.Strikes){if(strike.From<100)defenseHit=true;else raiderHit=true;}
        }
        Check(defenseHit && raiderHit,"Both sides attack");
        Check(first.Outcome==PracticeOutcome.Victory && first.Destruction==100 && first.Stars==3,"Practice encounter can be won with three stars");
        string before=Snapshot(first);first.Step();first.Surrender();Check(Snapshot(first)==before && !first.Deploy(1),"Terminal battle cannot mutate");
        var idle=new PracticeBattle();for(int i=0;i<1800;i++)idle.Step();Check(idle.Outcome==PracticeOutcome.Timeout && idle.Destruction==0,"Undeployed army times out");
        var surrender=new PracticeBattle();surrender.Deploy(1);surrender.Surrender();Check(surrender.Outcome==PracticeOutcome.Surrendered && !surrender.Deploy(1),"Surrender closes commands");
        var defeat=new PracticeBattle();for(int i=0;i<8;i++){defeat.Deploy(0);defeat.Raiders[i].HitPoints=1;}
        for(int i=0;i<1800 && defeat.Outcome==PracticeOutcome.Running;i++)defeat.Step();Check(defeat.Outcome==PracticeOutcome.Defeat,"Army elimination produces defeat");
        var range=new PracticeBattle();range.Deploy(0);range.Step();Check(range.Raiders[0].HitPoints==90,"Out-of-range raider is not damaged");
        range.Raiders[0].X=-450;range.Raiders[0].Z=-900;range.Step();Check(range.Raiders[0].HitPoints==81,"Cannon damage and range");
        range.Step();Check(range.Raiders[0].HitPoints==81,"Defense respects cooldown");
    }
    static void NavigationChecks(bool sealedWalls)
    {
        var battle=new PracticeBattle(sealedWalls);
        // Isolate navigation from defense balance: only the enclosed hall remains.
        battle.Buildings[1].HitPoints=0;battle.Buildings[2].HitPoints=0;
        battle.Deploy(1);bool wallHit=false,entered=false;
        for(int tick=0;tick<1800 && battle.Outcome==PracticeOutcome.Running;tick++)
        {
            battle.Step();var raider=battle.Raiders[0];
            if(raider.Z>0)entered=true;
            foreach(var b in battle.Buildings)
            {
                if(b.Kind=="Wall" && b.HitPoints<b.MaxHitPoints)wallHit=true;
                Check(!b.Alive || Math.Abs(raider.X-b.X)>=b.HalfSize || Math.Abs(raider.Z-b.Z)>=b.HalfSize,"Route never enters a live footprint");
            }
        }
        Check(battle.Outcome==PracticeOutcome.Victory && entered,"Route reaches enclosed hall and replans after breach");
        Check(wallHit==sealedWalls,"Open entrance is preferred; sealed walls require a breach");
        Check(battle.Destruction==100,"Surviving walls do not prevent full destruction score");
    }
    static void ScoreChecks()
    {
        int[] expected={0,1,0,2,0,2,1,3};
        for(int mask=0;mask<8;mask++)
        {
            var battle=new PracticeBattle();
            for(int i=0;i<3;i++)if((mask&(1<<i))!=0)battle.Buildings[i].HitPoints=0;
            Check(battle.Stars==expected[mask],"Independent Town Hall, half-destruction and full-destruction stars");
            foreach(var b in battle.Buildings)if(b.Kind=="Wall")b.HitPoints=0;
            Check(battle.Stars==expected[mask],"Wall destruction does not award stars");
            battle.Surrender();Check(battle.Stars==expected[mask],"Surrender retains earned score");
        }
        var army=new PracticeBattle();army.Deploy(0);army.Deploy(1);army.Raiders[0].HitPoints=0;
        Check(army.Survivors==1 && army.Raiders.Count==2 && army.Remaining==6,"Results distinguish deployed, survivors and reserves");
    }
    static void ReplayChecks()
    {
        foreach(bool sealedWalls in new[]{false,true})
        {
            var original=new PracticeBattle(sealedWalls);var hashes=new System.Collections.Generic.List<ulong>();
            original.Deploy(0);original.Deploy(2);hashes.Add(original.StateHash());
            while(original.Outcome==PracticeOutcome.Running)
            {
                original.Step();
                if(original.Tick==20){original.Deploy(1);original.Deploy(1);}
                if(original.Tick==45)for(int i=0;i<4;i++)original.Deploy(i%3);
                hashes.Add(original.StateHash());
            }
            var record=original.Record();Check(record.Deployments.Count==8,"Only accepted deployments recorded");
            var replay=new PracticeReplay(record);Check(replay.Battle.StateHash()==hashes[0],"Tick-zero replay commands");
            while(!replay.Finished){replay.Step();Check(replay.Battle.StateHash()==hashes[replay.Battle.Tick],"Replay matches every original tick");}
            Check(replay.Matches,"Replay final result and hash match");
            var final=replay.Battle.StateHash();replay.Step();Check(final==replay.Battle.StateHash(),"Finished replay is stable");
        }
        var stopped=new PracticeBattle();stopped.Deploy(1);for(int i=0;i<13;i++)stopped.Step();stopped.Surrender();
        var surrendered=new PracticeReplay(stopped.Record());while(!surrendered.Finished)surrendered.Step();Check(surrendered.Matches,"Replay timed surrender");
        var idle=new PracticeBattle();for(int i=0;i<1800;i++)idle.Step();
        var timeout=new PracticeReplay(idle.Record());while(!timeout.Finished)timeout.Step();Check(timeout.Matches,"Replay empty-army timeout");
        var changed=new PracticeBattle();changed.Deploy(0);changed.Raiders[0].HitPoints=1;changed.Surrender();
        Check(!new PracticeReplay(changed.Record()).Matches,"Hash detects unrecorded state modification");
    }
    static void Capture(string name)
    {typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});}
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("PracticeBattle.Start","0")))).TotalSeconds<180,"Play mode timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("PracticeBattle.Step",0);
            if(step==0)
            {
                if(GameObject.Find("Attack Menu")==null)return;
                SessionState.SetString("PracticeBattle.Home",JsonUtility.ToJson(game.State));SessionState.SetString("PracticeBattle.Save",PlayerPrefs.GetString(VillageSave.Key));
                Click("Attack Menu");Check(game.PracticeOpen && game.cameraController.InputBlocked && GameObject.Find("Shop")==null,"Attack opens isolated practice controls");
                Click("Switch Practice Challenge");Check(game.CurrentPracticeBattle.SealedEnclosure,"Select sealed challenge before deployment");
                Click("Switch Practice Challenge");Check(!game.CurrentPracticeBattle.SealedEnclosure,"Return to open challenge before deployment");
                Capture("practice-ready.png");
                for(int i=0;i<8;i++)Click("Deploy Raider "+(i%3));
                Check(game.CurrentPracticeBattle.Raiders.Count==8 && !GameObject.Find("Deploy Raider 0").GetComponent<Button>().interactable,"Deployment buttons exhaust army");
                Check(!game.SelectPracticeChallenge(true) && !game.CurrentPracticeBattle.SealedEnclosure,"Active attack rejects challenge switching");
                SessionState.SetInt("PracticeBattle.Step",1);return;
            }
            if(step==1)
            {
                if(game.CurrentPracticeBattle.Tick<70)return;
                bool damaged=false;foreach(var raider in game.CurrentPracticeBattle.Raiders)if(raider.HitPoints<raider.MaxHitPoints)damaged=true;
                Check(damaged,"Defenses fire during real-time playback");Capture("practice-combat.png");SessionState.SetInt("PracticeBattle.Step",2);return;
            }
            if(step==2)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Victory,"Real-time practice victory");Capture("practice-results.png");
                Check(GameObject.Find("Practice Results Text").GetComponent<Text>().text.Contains("3 / 3 STARS"),"Victory results display stars");
                Check(JsonUtility.ToJson(game.State)==SessionState.GetString("PracticeBattle.Home","") && PlayerPrefs.GetString(VillageSave.Key)==SessionState.GetString("PracticeBattle.Save",""),"Battle cannot mutate home or saved economy");
                Click("Watch Practice Replay");Check(game.WatchingPracticeReplay && !GameObject.Find("Deploy Raider 0").GetComponent<Button>().interactable,"Replay locks manual deployment");
                int count=game.CurrentPracticeBattle.Raiders.Count;game.DeployPracticeRaider(1);Check(count==game.CurrentPracticeBattle.Raiders.Count,"Direct deployment rejected during replay");
                game.SurrenderPracticeBattle();Check(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running,"Surrender cannot interrupt replay");
                SessionState.SetInt("PracticeBattle.Step",3);return;
            }
            if(step==3)
            {
                if(game.CurrentPracticeBattle.Tick<70)return;
                Capture("practice-replay.png");SessionState.SetInt("PracticeBattle.Step",4);return;
            }
            if(step==4)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches,"UI replay matches original battle");
                Click("Switch Practice Challenge");Check(!game.WatchingPracticeReplay && game.CurrentPracticeBattle.SealedEnclosure && game.CurrentPracticeBattle.Remaining==8,"Switch from replay results to wall challenge");
                Capture("practice-wall-challenge.png");
                for(int i=0;i<8;i++)Click("Deploy Raider "+(i%3));
                SessionState.SetInt("PracticeBattle.Step",5);return;
            }
            if(step==5)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Victory,"Sealed challenge can be won with eight raiders");
                bool breached=false;foreach(var b in game.CurrentPracticeBattle.Buildings)if(b.Kind=="Wall" && !b.Alive)breached=true;
                Check(breached,"Playable sealed challenge requires wall destruction");
                Capture("practice-wall-results.png");Click("Watch Practice Replay");
                Check(game.CurrentPracticeBattle.SealedEnclosure && !game.SelectPracticeChallenge(false),"Replay preserves challenge and blocks switching");
                SessionState.SetInt("PracticeBattle.Step",6);return;
            }
            if(step==6)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches,"Sealed challenge replay matches");
                Check(JsonUtility.ToJson(game.State)==SessionState.GetString("PracticeBattle.Home","") && PlayerPrefs.GetString(VillageSave.Key)==SessionState.GetString("PracticeBattle.Save",""),"Replay preserves home state and save");
                Click("Retry Practice");Check(game.PracticeOpen && !game.WatchingPracticeReplay && game.CurrentPracticeBattle.Remaining==8 && game.CurrentPracticeBattle.Tick==0,"Retry resets practice only");
                Check(game.CurrentPracticeBattle.SealedEnclosure,"Retry retains selected challenge");
                Click("Return From Practice");Check(!game.PracticeOpen && !game.cameraController.InputBlocked && GameObject.Find("Shop")!=null,"Return restores village and camera");
                Check(JsonUtility.ToJson(game.State)==SessionState.GetString("PracticeBattle.Home",""),"Returning does not change village state");
                Click("Attack Menu");Click("Deploy Raider 0");SessionState.SetInt("PracticeBattle.Step",7);return;
            }
            if(step==7)
            {
                if(game.CurrentPracticeBattle.Tick<15)return;
                Click("Surrender Practice");Check(game.PracticeOpen && game.CurrentPracticeBattle.Outcome==PracticeOutcome.Surrendered,"Surrender opens results without leaving battle");
                Check(GameObject.Find("Practice Results Text").GetComponent<Text>().text.Contains("1 deployed"),"Surrender results show deployed army");
                Capture("practice-surrender.png");Click("Watch Practice Replay");SessionState.SetInt("PracticeBattle.Step",8);return;
            }
            if(step==8)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches && game.CurrentPracticeBattle.Outcome==PracticeOutcome.Surrendered,"Surrendered UI replay matches original");
                Click("Retry Practice");Check(GameObject.Find("Practice Results")==null && GameObject.Find("Surrender Practice")!=null,"Retry hides results and restores surrender");
                Click("Deploy Raider 0");Click("Return From Practice");Check(!game.PracticeOpen,"Early return abandons encounter safely");
                Finish("PASS: all star combinations and wall exclusion; survivor counts; victory results; surrender/results/replay/retry UI; replay surrender guard; challenge selection and active-attack guard; sealed challenge victory and wall destruction; challenge-aware retry/replay; per-tick replay equality for open/sealed layouts; timed commands; surrender/timeout replay; state mismatch detection; replay UI and deployment lock; replay save preservation; entrance routing; sealed-wall breach and replanning; footprint collision checks; walls excluded from victory score; fixed-tick repeatability; deployment limits; defense range/damage/cooldown; both sides attack; victory/defeat/timeout/surrender; terminal-state guard; real-time Attack/deploy/results/retry/return UI; exact home-state and save preservation. Unity "+Application.unityVersion+". No rewards, multiplayer or phone validation.",0);
            }
        }
        catch(Exception e){Debug.LogException(e);Finish("FAIL: "+e,1);}
    }
    static void Finish(string text,int code)
    {SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Root,"PracticeBattleValidation.txt"),text);EditorApplication.Exit(code);}
}
