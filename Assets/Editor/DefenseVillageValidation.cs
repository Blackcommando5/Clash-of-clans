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
public static class DefenseVillageValidation
{
    const string Pending="DefenseVillage.Pending";
    static int checks;
    static string Root=>Path.GetDirectoryName(Application.dataPath);
    static DefenseVillageValidation(){EditorApplication.update+=Tick;}
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);checks++;}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use the isolated validation project and save identity.");
        try
        {
            StateChecks();
            ScreenshotReferenceArt.Build();
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            game.cannonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScreenshotReference/Cannon.prefab");
            game.archerTowerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScreenshotReference/ArcherTower.prefab");
            Check(game.cannonPrefab!=null && game.archerTowerPrefab!=null,"Authored defense prefabs");
            game.RefreshDefenseCatalogueArt();EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);
            var seed=VillageState.Create(VillageState.Now);seed.gold=10000;seed.elixir=10000;
            Check(VillageSave.TryWrite(seed,out _) && PlayerProfile.TrySaveName("DefenseChief",out _),"Isolated UI seed");
            SessionState.SetInt("DefenseVillage.Checks",checks);SessionState.SetInt("DefenseVillage.Step",0);
            SessionState.SetString("DefenseVillage.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void StateChecks()
    {
        typeof(ResourceMilestoneValidation).GetMethod("ProgressionChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        foreach(var d in new[]{BuildingCatalog.Cannon,BuildingCatalog.ArcherTower})
        {
            var s=VillageState.Create(1000);s.gold=10000;
            Check(s.BuildingLimit(d.Id)==d.Limit,"Defense tier-one limit");
            Check(!s.TryPlace(d.Id,-1,-1,1000,out _) && s.gold==10000,"Overlap cannot charge");
            Check(!s.TryPlace(d.Id,21,21,1000,out _) && s.gold==10000,"Bounds cannot charge");
            Check(s.TryPlace(d.Id,5,5,1000,out _) && s.gold==10000-d.Cost,"Defense gold price");
            Check(s.GoldCapacity==10000 && s.ElixirCapacity==10000,"Defenses do not add storage");
            s.Accrue(1010);Check(s.CollectableGold==0 && s.CollectableElixir==0,"Defenses do not produce resources");
            Check(s.TryMove(1,9,9,out _) && s.gold==10000-d.Cost,"Free defense movement");
            Check(s.TryStartUpgrade(1,1010,out _) && s.BusyBuilders==1,"Defense upgrades use builder");
            Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out s) && s.BusyBuilders==1,"Active defense save round trip");
            int balance=s.gold;Check(s.TryCancelUpgrade(1,1020,out _) && s.gold==balance+d.Cost && s.BusyBuilders==0,"Defense cancellation refund");
            Check(!s.TryCancelUpgrade(1,1020,out _),"Duplicate refund rejected");
            Check(s.TryStartUpgrade(1,1020,out _),"Defense upgrade restart");
            s.Accrue(1100);Check(s.buildings[1].level==2 && s.BusyBuilders==0 && s.IsValid(),"Defense offline completion");
            Check(!s.CanUpgrade(1,out _),"Town Hall gates defense level three");
            for(int i=1;i<d.Limit;i++)Check(s.TryPlace(d.Id,4+i*4,-8,1100,out _),"Fill defense limit");
            Check(!s.CanBuy(d.Id,out _),"Defense purchase limit enforced");
            s.buildings[0].level=2;Check(s.CanBuy(d.Id,out _),"Higher Town Hall unlocks additional defense");
            s.buildings[1].storedGold=1;Check(!s.IsValid(),"Defense cannot store producer gold");
            var poor=VillageState.Create(1000);poor.gold=d.Cost-1;Check(!poor.CanBuy(d.Id,out _),"Unaffordable defense rejected");
        }
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(VillageState.Create(1000)),out _),"Old version-three villages remain readable");
    }
    static void Capture(string name)
    {typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});}
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("DefenseVillage.Start","0")))).TotalSeconds<120,"Play timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null || GameObject.Find("Shop")==null)return;
            if(SessionState.GetInt("DefenseVillage.Step",0)==1)
            {
                Check(game.State.Count("Cannon")==1 && game.State.Count("ArcherTower")==1 && game.State.buildings[1].z==9 && game.State.buildings[2].level==2 && game.State.BusyBuilders==0,"Defense placement/move/cancel/upgrade survive reload");
                Capture("defense-village.png");
                Finish("PASS: defense rules, prices, overlap/bounds, limits and Town Hall unlocks, nonproduction, movement, active saves, refunds, offline upgrades; existing progression regressions; Play mode catalogue purchases, range display, upgrade/cancel buttons, portrait/stat panels, and scene reload. Unity "+Application.unityVersion+". Combat and physical-phone testing are not included.",0);return;
            }
            game.OpenShop();Click("Shop Category 2");Check(Button("Catalogue Buy Cannon").interactable && Button("Catalogue Buy ArcherTower").interactable,"Defense catalogue ready");Capture("defense-shop.png");
            int gold=game.State.gold;Click("Catalogue Buy Cannon");Check(game.IsPlacing,"Cannon shop callback");game.SetPreviewCell(5,5);game.ConfirmPlacement();Check(game.State.gold==gold-250,"Cannon UI purchase");
            game.OpenShop();Click("Catalogue Buy ArcherTower");Check(game.IsPlacing,"Tower shop callback");game.SetPreviewCell(9,5);game.ConfirmPlacement();Check(game.State.gold==gold-1250,"Tower UI purchase");
            game.SelectBuilding(1);Check(GameObject.Find("Defense Range").GetComponent<LineRenderer>().positionCount==96,"Defense range display");Capture("defense-range.png");
            game.BeginSelectedMove();game.SetPreviewCell(5,9);game.ConfirmPlacement();Check(game.State.buildings[1].z==9,"Defense UI movement");
            game.SelectBuilding(1);game.OpenBuildingDetails();Check(GameObject.Find("Details Stats").GetComponent<Text>().text.Contains("practice combat"),"Defense capability explained");Capture("defense-upgrade.png");
            Click("Confirm Upgrade");Check(game.State.BusyBuilders==1,"Cannon UI upgrade");
            game.SelectBuilding(2);game.OpenBuildingDetails();Click("Confirm Upgrade");Check(game.State.BusyBuilders==2,"Tower UI upgrade and second builder");
            game.SelectBuilding(1);game.OpenBuildingDetails();gold=game.State.gold;Click("Confirm Upgrade");Click("Confirm Upgrade");Check(game.State.gold==gold+250 && game.State.BusyBuilders==1,"Cannon UI cancellation");
            game.State.Accrue(game.State.buildings[2].upgradeFinishes);Check(VillageSave.TryWrite(game.State,out _),"Completed defense save");
            SessionState.SetInt("DefenseVillage.Step",1);SceneManager.LoadScene("Main Scene");
        }
        catch(Exception e){Debug.LogException(e);Finish("FAIL: "+e,1);}
    }
    static void Finish(string result,int code)
    {SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Root,"DefenseVillageValidation.txt"),result);EditorApplication.Exit(code);}
}
