using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

// Explicit batch entry point. Refuses to run against the player's project/preferences.
[InitializeOnLoad]
public static class ResourceMilestoneValidation
{
    const string Pending="Resources.Validation.Pending";
    static string Root => Path.GetDirectoryName(Application.dataPath);
    static int assertions;
    static ResourceMilestoneValidation()
    {
        if(SessionState.GetBool(Pending,false)) EditorApplication.update+=Tick;
    }
    static void Check(bool value,string message)
    {
        if(!value) throw new Exception(message);
        assertions++;
    }

    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") ||
            PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use the isolated ResourceValidation project and test save identity.");
        try
        {
            StateChecks();
            ProgressionChecks();
            ResourceBuildingFactory.Build();
            HomeVillageArtFactory.Build();
            PlayerPrefs.DeleteKey(VillageSave.Key);PlayerPrefs.DeleteKey(VillageSave.LegacyBackupKey);PlayerPrefs.DeleteKey(PlayerProfile.NameKey);PlayerPrefs.Save();
            SessionState.SetInt("Resources.Assertions",assertions);
            SessionState.SetInt("Resources.Step",0);
            SessionState.SetString("Resources.Start",DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Pending,true);
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            EditorApplication.EnterPlaymode();
        }
        catch(Exception e) { Fail(e); }
    }

    static void StateChecks()
    {
        var s=VillageState.Create(1000);
        Check(s.IsValid() && s.GoldCapacity==10000 && s.ElixirCapacity==10000,"Initial state and preserved capacity");
        Check(!s.TryPlace("ElixirCollector",-1,-1,1100,out _) && s.gold==1000,"Overlap cannot charge gold");
        Check(!s.CanPlace("GoldStorage",int.MaxValue,0,out _) && !s.CanPlace("ElixirCollector",20,0,out _),"Bounds checks");
        Check(!s.CanBuy("Bogus",out _) && !s.CanBuy("TownHall",out _),"Unknown and Town Hall purchases");
        Check(s.TryPlace("GoldMine",5,-1,1100,out _) && s.elixir==350,"Mine purchase currency");
        Check(s.TryPlace("ElixirCollector",9,-1,1160,out _) && s.gold==850,"Collector purchase currency");
        Check(s.CollectableGold==60 && s.CollectableElixir==0,"No retroactive collector production");
        s.Accrue(1220);Check(s.CollectableGold==120 && s.CollectableElixir==60,"Both production rates");
        s.Accrue(1200);Check(s.lastProduction==1220 && s.CollectableElixir==60,"Backward clock watermark");
        s.Accrue(1220);Check(s.CollectableElixir==60,"Repeated timestamp");
        s.Accrue(long.MaxValue);Check(s.CollectableGold==500 && s.CollectableElixir==500,"Long offline production capped");
        s.gold=9990;s.elixir=9995;
        Check(s.Collect(ResourceKind.Gold,long.MaxValue)==10 && s.CollectableGold==490,"Gold overflow retained");
        Check(s.Collect(ResourceKind.Elixir,long.MaxValue)==5 && s.CollectableElixir==495,"Elixir overflow retained");
        Check(s.CollectGold(long.MaxValue)==0 && s.CollectableGold==490,"Full storage loses nothing");
        Check(s.TryPlace("GoldStorage",13,-1,long.MaxValue,out _) && s.GoldCapacity==15000 && s.ElixirCapacity==10000,"Gold storage capacity");
        Check(s.TryPlace("ElixirStorage",17,-1,long.MaxValue,out _) && s.ElixirCapacity==15000,"Elixir storage capacity");
        Check(s.CollectGold(long.MaxValue)==490 && s.Collect(ResourceKind.Elixir,long.MaxValue)==495,"Collection after storage expansion");
        Check(s.IsValid(),"Expanded valid state");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out var restored) && restored.GoldCapacity==15000 && restored.elixir==s.elixir,"Version 2 round trip");
        restored.buildings[0].storedElixir=1;Check(!restored.IsValid(),"Town Hall cannot contain producer elixir");
        foreach(var definition in BuildingCatalog.Shop)
        {
            var limit=VillageState.Create(1000);limit.gold=10000;limit.elixir=10000;
            for(int i=0;i<definition.Limit;i++) Check(limit.TryPlace(definition.Id,4+i*4,5,1000,out _),definition.Id+" within limit");
            int beforeGold=limit.gold,beforeElixir=limit.elixir;
            Check(!limit.TryPlace(definition.Id,4,10,1000,out _) && beforeGold==limit.gold && beforeElixir==limit.elixir,definition.Id+" count limit");
            var poor=VillageState.Create(1000);poor.gold=0;poor.elixir=0;
            Check(!poor.TryPlace(definition.Id,5,5,1000,out _),definition.Id+" insufficient funds");
        }
        var individual=VillageState.Create(1000);
        individual.TryPlace("ElixirCollector",5,5,1000,out _);individual.TryPlace("ElixirCollector",9,5,1000,out _);
        Check(individual.Collect(ResourceKind.Elixir,1060,individual.buildings[1])==60 && individual.CollectableElixir==60,"Individual collection leaves other producer untouched");
        string legacy="{\"version\":1,\"gold\":9999,\"elixir\":50,\"gems\":50,\"collectedFirstGold\":true,\"lastProduction\":1000,\"buildings\":[{\"kind\":\"TownHall\",\"x\":-2,\"z\":-2,\"storedGold\":0},{\"kind\":\"GoldMine\",\"x\":5,\"z\":-1,\"storedGold\":321}]}";
        Check(VillageState.TryDeserialize(legacy,out var migrated) && migrated.version==3 && migrated.gold==9999 && migrated.elixir==50 && migrated.CollectableGold==321,"Legacy progress preserved");
        Check(migrated.TryPlace("ElixirCollector",9,-1,1000,out _),"Legacy low-elixir village can buy collector with gold");
        PlayerPrefs.DeleteKey(VillageSave.LegacyBackupKey);PlayerPrefs.SetString(VillageSave.Key,legacy);
        Check(VillageSave.TryLoad(out _,out _) && PlayerPrefs.GetString(VillageSave.LegacyBackupKey)==legacy && PlayerPrefs.GetString(VillageSave.Key)==legacy,"Migration backup preserves original payload");
        PlayerPrefs.SetString(VillageSave.Key,"{bad data");
        Check(!VillageSave.TryLoad(out _,out _) && PlayerPrefs.GetString(VillageSave.Key)=="{bad data","Corrupt save preserved");
        Check(!VillageState.TryDeserialize(legacy.Replace("\"version\":1","\"version\":99"),out _),"Unknown version rejected");
    }

    public static void RunCancellation()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") ||
            PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use the isolated ResourceValidation project and test save identity.");
        try
        {
            ProgressionChecks();
            var s=VillageState.Create(1000);s.elixir=10000;
            Check(s.TryPlace("GoldMine",5,5,1000,out _),"Cancellation mine setup");
            s.Accrue(1010);
            Check(s.TryStartUpgrade(1,1010,out _),"Cancellation upgrade setup");
            int paidBalance=s.elixir;
            Check(s.TryCancelUpgrade(1,1020,out _) && s.elixir==paidBalance+150 && s.BusyBuilders==0,"Half refund and builder release");
            Check(s.buildings[1].level==1 && s.buildings[1].upgradeStarted==0 && s.CollectableGold==10,"Cancellation retains level and pre-upgrade production");
            Check(!s.TryCancelUpgrade(1,1020,out _) && s.elixir==paidBalance+150,"Duplicate cancellation cannot refund twice");
            s.Accrue(1030);Check(s.CollectableGold==20,"Production resumes only after cancellation");
            Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out var restored) && restored.BusyBuilders==0 && restored.elixir==s.elixir,"Cancelled upgrade round trip");
            Check(restored.TryStartUpgrade(1,1030,out _),"Released builder can restart upgrade");
            Check(VillageState.TryDeserialize(JsonUtility.ToJson(restored),out restored),"Active job round trip");
            restored.elixir=9990;
            Check(restored.UpgradeCancellationRefund(1)==10 && restored.TryCancelUpgrade(1,1040,out _) && restored.elixir==10000 && restored.IsValid(),"Refund capped at storage after reload");
            Check(restored.TryStartUpgrade(1,1040,out _),"Completion boundary setup");
            int before=restored.elixir;
            Check(!restored.TryCancelUpgrade(1,1070,out _) && restored.buildings[1].level==2 && restored.elixir==before,"Completion at exact deadline wins over cancellation");
            Check(!restored.TryCancelUpgrade(-1,1070,out _) && !restored.TryCancelUpgrade(99,1070,out _),"Invalid cancellation indexes");
            var hall=VillageState.Create(1000);
            Check(hall.TryStartUpgrade(0,1000,out _) && hall.TryCancelUpgrade(0,1010,out _) && hall.gold==500 && hall.TownHallLevel==1,"Town Hall gold refund");
            hall.gold=1000;Check(hall.TryStartUpgrade(0,1010,out _),"Backward clock setup");
            hall.gold=hall.GoldCapacity;
            Check(hall.TryCancelUpgrade(0,1005,out _) && hall.gold==hall.GoldCapacity && hall.lastProduction==1010 && hall.IsValid(),"Full storage and backward clock cancellation");
            File.WriteAllText(Path.Combine(Root,"UpgradeCancellationValidation.txt"),"PASS: "+assertions+" progression and cancellation assertions. Unity "+Application.unityVersion+".\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }

    static void ProgressionChecks()
    {
        var s=VillageState.Create(1000);s.gold=10000;s.elixir=10000;
        s.TryPlace("GoldMine",5,5,1000,out _);s.TryPlace("ElixirCollector",9,5,1000,out _);
        int gold=s.gold,elixir=s.elixir;
        Check(!s.TryMove(1,-1,-1,out _) && s.buildings[1].x==5,"Invalid move retains position");
        Check(s.TryMove(1,5,9,out _) && s.gold==gold && s.elixir==elixir,"Moving has no cost");
        Check(s.TryMove(0,-8,-8,out _) && s.IsValid(),"Town Hall can move");
        Check(s.TryStartUpgrade(1,1000,out _) && s.elixir==elixir-300 && s.BusyBuilders==1,"Upgrade costs and builder assignment");
        Check(!s.TryStartUpgrade(1,1000,out _) && s.BusyBuilders==1,"Repeated upgrade cannot double-charge");
        Check(s.TryStartUpgrade(2,1000,out _) && s.BusyBuilders==2,"Second builder assignment");
        Check(!s.TryStartUpgrade(0,1000,out _),"Third job rejected while builders busy");
        s.Accrue(1020);Check(s.buildings[1].level==1 && s.CollectableGold==0,"No production while upgrading");
        s.Accrue(1040);Check(s.buildings[1].level==2 && s.CollectableGold==20 && s.CollectableElixir==20 && s.BusyBuilders==0,"Upgrade completion and post-finish production interval");
        s.Accrue(1040);Check(s.buildings[1].level==2 && s.CollectableGold==20,"Upgrade completion is idempotent");
        Check(!s.TryStartUpgrade(1,1040,out _),"Resource tier gated by Town Hall");
        Check(s.TryStartUpgrade(0,1040,out _) && s.buildings[0].upgradeFinishes==1100,"Town Hall upgrade job");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out var restored) && restored.BusyBuilders==1,"Active upgrade persists");
        restored.Accrue(1160);Check(restored.TownHallLevel==2 && restored.BuildingLimit("GoldMine")==4,"Town Hall unlocks additional producers");
        Check(restored.TryStartUpgrade(1,1160,out _),"Town Hall unlocks next resource level");
        Check(restored.TryMove(1,10,10,out _) && restored.buildings[1].upgradeFinishes>0,"Moving active upgrade preserves job");
        restored.Accrue(1400);Check(restored.buildings[1].level==3 && restored.IsValid(),"Tier three resource completion");
        Check(!restored.TryStartUpgrade(1,1400,out _),"Maximum level enforced");
        var poor=VillageState.Create(1000);poor.gold=0;
        Check(!poor.TryStartUpgrade(0,1000,out _) && poor.BusyBuilders==0,"Unaffordable upgrade assigns no builder");
        var storage=VillageState.Create(1000);storage.elixir=10000;storage.TryPlace("GoldStorage",5,5,1000,out _);
        Check(storage.TryStartUpgrade(1,1000,out _) && storage.GoldCapacity==15000,"Old storage capacity retained during upgrade");
        storage.Accrue(1030);Check(storage.GoldCapacity==20000 && storage.IsValid(),"Storage upgrade increases capacity");
        var old=VillageState.Create(1000);old.version=2;old.buildings[0].level=0;
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(old),out var upgraded) && upgraded.version==3 && upgraded.buildings[0].level==1,"Version 2 save gains valid levels");
    }

    static void Tick()
    {
        try
        {
            if(!SessionState.GetBool(Pending,false)) return;
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("Resources.Start","0")))).TotalSeconds<180,"Play mode validation timeout");
            if(EditorApplication.isPaused) EditorApplication.isPaused=false;
            if(!EditorApplication.isPlaying) { if(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling) EditorApplication.EnterPlaymode(); return; }
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            if(game==null || game.State==null) return;
            int step=SessionState.GetInt("Resources.Step",0);
            if(step==0)
            {
                var naming=UnityEngine.Object.FindFirstObjectByType<NewPlayerOnboarding>();
                if(naming==null) return;
                Check(naming.IsAskingForName,"Fresh test save must show onboarding; saved name="+PlayerProfile.HasPlayerName);
                UnityEngine.Object.FindFirstObjectByType<InputField>().text="ResourceChief";naming.Continue();naming.Continue();
                Check(PlayerProfile.HasPlayerName,"Onboarding still works");
                SessionState.SetInt("Resources.Step",1);return;
            }
            if(step==1)
            {
                Check(UnityEngine.EventSystems.EventSystem.current!=null,"EventSystem exists");
                foreach(var icon in UnityEngine.Object.FindObjectsByType<VillageIcon>(FindObjectsSortMode.None))Check(icon.GetComponent<CanvasRenderer>()!=null,"HUD icon renderer");
                game.OpenShop();Check(game.cameraController.InputBlocked,"Shop blocks camera");
                GameObject.Find("Buy ElixirCollector").GetComponent<Button>().onClick.Invoke();
                Check(game.IsPlacing,"Collector shop button opens placement");
                game.SetPreviewCell(-1,-1);game.ConfirmPlacement();
                Check(game.State.gold==1000 && game.State.Count("ElixirCollector")==0,"Invalid preview does not charge");
                game.CancelPlacement();Check(!game.IsPlacing && !game.cameraController.InputBlocked && game.State.gold==1000,"Cancel preserves resources and releases camera");
                string[] kinds={"GoldMine","ElixirCollector","GoldStorage","ElixirStorage"};
                for(int i=0;i<kinds.Length;i++)
                {
                    game.OpenShop();GameObject.Find("Buy "+kinds[i]).GetComponent<Button>().onClick.Invoke();
                    Check(game.IsPlacing,kinds[i]+" shop button");game.SetPreviewCell(4+i*4,-1);game.ConfirmPlacement();game.ConfirmPlacement();
                    Check(game.State.Count(kinds[i])==1 && GameObject.Find(kinds[i]+" ("+(4+i*4)+", -1)")!=null,kinds[i]+" spawn and double-confirm protection");
                }
                Check(game.State.gold==550 && game.State.elixir==50,"All four purchase costs");
                game.State.lastProduction=VillageState.Now-60;game.Collect();
                Check(game.State.gold>=610 && game.State.elixir>=110 && game.State.GoldCapacity==15000 && game.State.ElixirCapacity==15000,"Collect-all and storage capacity");
                game.State.lastProduction=VillageState.Now-60;game.CollectBuilding(2);
                Check(game.State.CollectableGold>=60 && game.State.CollectableElixir==0,"Runtime individual elixir collection");
                SessionState.SetInt("Resources.Step",2);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==2)
            {
                if(GameObject.Find("Player Name")==null) return;
                Check(game.State.buildings.Count==5 && game.State.GoldCapacity==15000 && game.State.elixir>=170,"Reload preserves buildings/resources");
                Check(game.State.version==3 && game.State.IsValid(),"Reloaded save is valid");
                game.SelectBuilding(1);Check(game.SelectedBuildingIndex==1,"Select building");
                game.BeginSelectedMove();game.SetPreviewCell(-1,-1);game.ConfirmPlacement();
                Check(game.IsPlacing && game.State.buildings[1].x==4,"Occupied move refused");
                game.CancelPlacement();Check(GameObject.Find("GoldMine (4, -1)").activeSelf,"Move cancellation restores model");
                game.SelectBuilding(1);game.BeginSelectedMove();game.SetPreviewCell(4,4);game.ConfirmPlacement();
                Check(game.State.buildings[1].z==4 && !game.IsPlacing && !game.cameraController.InputBlocked,"Move commits and unlocks camera");
                game.SelectBuilding(2);game.OpenBuildingDetails();Check(game.DetailsOpen && game.cameraController.InputBlocked,"Building details modal");
                GameObject.Find("Confirm Upgrade").GetComponent<Button>().onClick.Invoke();
                Check(game.State.BusyBuilders==1 && !game.DetailsOpen,"Upgrade confirmation button");
                SceneManager.LoadScene("Main Scene");
                SessionState.SetInt("Resources.Step",3);return;
            }
            if(step==3)
            {
                Check(game.State.buildings[1].z==4 && game.State.BusyBuilders==1,"Move and upgrade survive scene reload");
                game.SelectBuilding(2);game.OpenBuildingDetails();Capture("building-details.png",1920,1080);game.CloseBuildingDetails();game.DeselectBuilding();
                Capture("resource-village.png",1920,1080);game.OpenShop();Capture("resource-shop.png",1920,1080);Capture("resource-shop-wide.png",2340,1080);
                game.CloseShop();
                game.OpenProfile();Check(game.ProfileOpen && game.cameraController.InputBlocked,"Profile locks village camera");
                Check(GameObject.Find("Profile Player Name").GetComponent<Text>().text==PlayerProfile.PlayerName,"Profile displays saved name");
                Capture("reference-profile.png",1600,702);
                GameObject.Find("Profile Tab 1").GetComponent<Button>().onClick.Invoke();
                Check(GameObject.Find("Section Title").GetComponent<Text>().text=="My Clan","Profile tab navigation");
                game.CloseProfile();Check(!game.ProfileOpen && !game.cameraController.InputBlocked,"Profile close restores camera");
                Capture("reference-village.png",1600,702);
                game.OpenProfile(4);GameObject.Find("Inventory Shop").GetComponent<Button>().onClick.Invoke();
                Check(!game.ProfileOpen && game.cameraController.InputBlocked,"Inventory opens shop");
                game.CloseShop();SessionState.SetBool(Pending,false);
                File.WriteAllText(Path.Combine(Root,"resource-result.txt"),"PASS: economy/progression state checks ("+SessionState.GetInt("Resources.Assertions",0)+" assertions), four shop buttons, placement/overlap/cancellation, double-confirm protection, both currencies, individual and collect-all, storage expansion, v1/v2 migration, limits, offline cap, clock rollback, building selection/moving, timed upgrades, two-builder limit, Town Hall unlocks, upgrade persistence, onboarding, camera lock, scene reload, and URP screenshots. Tested in isolated Unity Editor, not a phone build.");
                EditorApplication.Exit(0);
            }
        }
        catch(Exception e) { Fail(e); }
    }

    static void Capture(string name,int width,int height)
    {
        var camera=Camera.main;var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.aspect=(float)width/height;
        var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;}
        Canvas.ForceUpdateCanvases();
        UnityEngine.Object.FindFirstObjectByType<VillageGameplay>().SendMessage("RefreshLayouts");
        UnityEngine.Object.FindFirstObjectByType<VillageGameplay>().SendMessage("LateUpdate");
        Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(Root,name),texture.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);
        foreach(var canvas in canvases) canvas.renderMode=RenderMode.ScreenSpaceOverlay;
    }

    static void Fail(Exception exception)
    {
        SessionState.SetBool(Pending,false);Debug.LogException(exception);
        File.WriteAllText(Path.Combine(Root,"resource-result.txt"),"FAIL: "+exception);EditorApplication.Exit(1);
    }
}
