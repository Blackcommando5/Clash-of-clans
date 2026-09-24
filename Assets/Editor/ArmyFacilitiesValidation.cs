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
public static class ArmyFacilitiesValidation
{
    const string Pending="ArmyFacilities.Pending";
    static ArmyFacilitiesValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use the isolated validation project and identity.");
        try
        {
            Rules(); ArmyBuildingAuthoring.Build();
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            var state=VillageState.Create(VillageState.Now);state.elixir=10000;
            Check(VillageSave.TryWrite(state,out _) && PlayerProfile.TrySaveName("CampChief",out _),"Seed isolated save");
            SessionState.SetInt("ArmyFacilities.Step",0);SessionState.SetString("ArmyFacilities.Start",DateTime.UtcNow.Ticks.ToString());
            SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Rules()
    {
        var s=VillageState.Create(1000);s.elixir=10000;
        Check(s.TrySetArmyCount("Raider",8,out _) && s.ArmyReady,"Starter roster preserved");
        Check(!s.TryPlace("ArmyCamp",5,5,1000,out _) && s.elixir==10000,"Camp requires Barracks without charge");
        Check(!s.TryPlace("Barracks",-1,-1,1000,out _) && s.elixir==10000,"Blocked placement does not charge");
        Check(s.TryPlace("Barracks",5,5,1000,out _) && s.elixir==9800,"Barracks price");
        Check(!s.CanBuy("Barracks",out _) && s.CanUpgrade(1,out _),"One Barracks and supported level-2 upgrade");
        Check(s.TryPlace("ArmyCamp",9,5,1000,out _) && s.elixir==9550 && s.ArmyCapacity==16,"Camp price and capacity");
        Check(s.TrySetArmyCount("Raider",16,out _) && !s.TrySetArmyCount("Raider",17,out _),"Expanded roster boundary");
        Check(!s.CanBuy("ArmyCamp",out _),"Town Hall one camp limit");
        Check(s.TryStartUpgrade(2,1000,out _) && s.ArmyCapacity==16 && s.ArmyReady,"Upgrade keeps army ready and old capacity");
        Check(s.TryCancelUpgrade(2,1001,out _) && s.ArmyCapacity==16 && s.ArmyHousing==16,"Cancel preserves army");
        Check(s.TryStartUpgrade(2,1001,out _),"Restart camp upgrade");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out s) && s.IsValid(),"Active camp upgrade save");
        s.Accrue(1031);Check(s.ArmyCapacity==24 && s.buildings[2].level==2 && s.BusyBuilders==0,"Offline upgrade expands capacity");
        s.Accrue(1032);Check(s.ArmyCapacity==24,"Completion is not repeated");
        Check(!s.CanUpgrade(2,out _),"Town Hall gates level three");
        s.buildings[0].level=2;
        Check(s.TryPlace("ArmyCamp",13,5,1032,out _) && s.ArmyCapacity==32,"Town Hall two unlocks second camp");
        Check(s.TryMove(2,9,9,out _) && s.ArmyCapacity==32,"Moving camp retains capacity");
        Check(s.TrySetArmyCount("Raider",32,out _) && s.IsValid(),"Larger roster valid");
        var invalid=s.Copy();invalid.buildings.RemoveAt(1);Check(!invalid.IsValid(),"Saved camp requires Barracks");
        invalid=s.Copy();invalid.buildings[1].level=3;Check(!invalid.IsValid(),"Unsupported Barracks level rejected");
        invalid=s.Copy();invalid.army[0].count=33;Check(!VillageState.TryDeserialize(JsonUtility.ToJson(invalid),out _),"Over-capacity save rejected");
        Check(s.GoldCapacity==10000 && s.ElixirCapacity==10000 && s.CollectableGold==0 && s.CollectableElixir==0,"Facilities do not produce or store currency");
    }
    static void Capture(string name)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("ArmyFacilities.Start","0")))).TotalSeconds<120,"Play mode timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null || GameObject.Find("Shop")==null)return;
            if(SessionState.GetInt("ArmyFacilities.Step",0)==1)
            {
                Check(game.State.ArmyCapacity==24 && game.State.ArmyHousing==24 && game.State.buildings[2].z==9,"Facilities and roster survive scene reload");
                game.OpenArmyPreparation();Capture("army-facilities-preparation.png");
                game.CloseProfile();game.OpenShop();Click("Shop Category 0");Capture("army-facilities-shop.png");
                Finish("PASS: Barracks/camp prices and prerequisites; placement/limits; preserved starter roster; expanded capacity boundaries; upgrade/cancel/offline completion; Town Hall gates and second camp; invalid facility and roster saves; no currency production; authored prefab/icon loading; real shop buy/place/move/details/upgrade controls; Barracks level-2 upgrade and Tank selection UI; expanded free preparation; persistence and scene reload. Unity "+Application.unityVersion+". No campaign integration, APK or phone validation.",0);return;
            }
            game.OpenShop();Click("Shop Category 0");Check(!Button("Catalogue Buy ArmyCamp").interactable && Button("Catalogue Buy Barracks").interactable,"Shop prerequisite");
            Click("Catalogue Buy Barracks");Check(game.IsPlacing,"Barracks model loads");game.SetPreviewCell(5,5);game.ConfirmPlacement();
            game.OpenShop();Click("Shop Category 0");Check(Button("Catalogue Buy ArmyCamp").interactable,"Camp unlocked");
            Click("Catalogue Buy ArmyCamp");Check(game.IsPlacing,"Camp model loads");game.SetPreviewCell(9,5);game.ConfirmPlacement();
            Check(game.State.elixir==9550 && game.State.ArmyCapacity==16,"UI purchase balances/capacity");
            game.SelectBuilding(2);game.BeginSelectedMove();game.SetPreviewCell(9,9);game.ConfirmPlacement();
            game.SelectBuilding(2);game.OpenBuildingDetails();Check(GameObject.Find("Details Stats").GetComponent<Text>().text.Contains("Army capacity"),"Camp details");
            Click("Confirm Upgrade");Check(game.State.BusyBuilders==1 && game.State.ArmyCapacity==16,"UI upgrade keeps capacity");
            game.State.Accrue(game.State.buildings[2].upgradeFinishes);game.CloseBuildingDetails();game.OpenArmyPreparation();Click("Fill Army");
            Check(game.State.ArmyHousing==24 && game.State.ArmyReady,"Fill expanded army");
            Check(VillageSave.TryLoad(out var saved,out _) && saved.ArmyHousing==24 && saved.ArmyCapacity==24,"Expanded roster saved");
            Click("Close Profile");game.SelectBuilding(1);game.OpenBuildingDetails();
            Check(GameObject.Find("Details Stats").GetComponent<Text>().text.Contains("Level 2 unlocks Tanks"),"Barracks unlock details");
            Click("Confirm Upgrade");Check(game.State.buildings[1].upgradeFinishes>0 && !game.State.TroopUnlocked("Tank"),"Barracks UI starts upgrade");
            game.State.Accrue(game.State.buildings[1].upgradeFinishes);game.CloseBuildingDetails();game.OpenArmyPreparation();
            Click("Prepare Tanks");Check(GameObject.Find("Add Raider").GetComponentInChildren<Text>().text=="ADD TANK","Completed Barracks UI unlock");
            Check(Resources.Load<Texture2D>("BuildingIcons/ArmyCamp")!=null && Resources.Load<Texture2D>("BuildingIcons/Barracks")!=null,"Portraits load");
            SessionState.SetInt("ArmyFacilities.Step",1);SceneManager.LoadScene("Main Scene");
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string result,int code)
    {SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"ArmyFacilitiesValidation.txt"),result);EditorApplication.Exit(code);}
}
