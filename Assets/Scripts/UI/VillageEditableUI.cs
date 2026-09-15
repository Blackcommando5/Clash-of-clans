using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        [SerializeField] List<RectTransform> profilePages = new List<RectTransform>();
        [SerializeField] GameObject editorVillage;
        [SerializeField] Button collectionBadgeTemplate;
        [System.Serializable] class SceneAction { public string id; public Button button; }
        [SerializeField] List<SceneAction> sceneActions = new List<SceneAction>();
        [SerializeField] Text profilePlayerValue, profileChiefValue, inventoryValue;
        [SerializeField] List<Text> profileOwnedValues = new List<Text>();
        [Tooltip("Fit large windows inside the device safe area. Disable to use their authored scales.")]
        public bool fitWindowsToScreen = true;
        public bool HasEditableInterface => hud != null;

        // Called once by the editor authoring tool, or as a fallback for older scenes.
        public void BuildEditableInterface()
        {
            if(hud!=null)return;
            if(titleFont==null)titleFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(bodyFont==null)bodyFont=titleFont;
            var previous=State;
            State=VillageState.Create(VillageState.Now);
            BuildUI();BuildInteractionUI();BuildReferenceUI();
            var host=profileContent;
            string[] names={"My Profile","My Clan","Clans","Social","My Buildings","Attack","Settings","Builder Base","Clan Capital"};
            for(int i=0;i<names.Length;i++)
            {
                profileContent=Box(names[i]+" Page",host,Vector2.zero,Vector2.one);
                profilePages.Add(profileContent);BuildProfilePage(i);
                profileContent.gameObject.SetActive(i==0);
            }
            profileContent=profilePages[0];
            goldLabel.text="1,000";elixirLabel.text="500";gemsLabel.text="50";
            chiefName.text="Chief";chiefTier.text="1";builderLabel.text="2 / 2";townLevelLabel.text="TOWN HALL 1";
            guide.text="Build your village. Select buildings to move or upgrade.";
            selectedTitle.text="Town Hall (Level 1)";detailTitle.text="Town Hall (Level 1)";
            detailStats.text="Select a building in Play mode to see live upgrade information.";
            collectionBadgeTemplate=Button("Collection Badge Template",safe,"TAP TO COLLECT",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Color(.63f,.43f,.08f),new Vector2(180,54));
            collectionBadgeTemplate.GetComponentInChildren<Text>().resizeTextMaxSize=21;
            collectionBadgeTemplate.gameObject.SetActive(false);
            profilePlayerValue=profilePages[0].Find("Profile Statistics/Profile Player Name").GetComponent<Text>();
            profileChiefValue=profilePages[0].Find("Profile Statistics/Profile Chief").GetComponent<Text>();
            inventoryValue=profilePages[4].Find("Inventory Counts").GetComponent<Text>();
            foreach(var d in BuildingCatalog.Shop)profileOwnedValues.Add(profilePages[0].Find("Village Collection/Profile "+d.Id+"/Owned").GetComponent<Text>());
            foreach(var b in hud.GetComponentsInChildren<Button>(true))sceneActions.Add(new SceneAction{id=b.name,button=b});
            State=previous;
            HideEditorScreens();
        }

        void Hook(string name,UnityAction action)
        {
            foreach(var binding in sceneActions)
                if(binding.id==name && binding.button!=null){binding.button.onClick.AddListener(action);return;}
            Debug.LogError("Missing editable UI button: "+name,this);
        }

        void BindEditableInterface()
        {
            Hook("Collect Resources",Collect);Hook("Shop",OpenShop);Hook("Close Shop",CloseShop);
            Hook("Cancel Placement",CancelPlacement);Hook("Confirm Placement",ConfirmPlacement);
            foreach(var d in BuildingCatalog.Shop){string id=d.Id;Hook("Buy "+id,()=>BeginPlacement(id));}
            Hook("Builders",OpenBuilders);Hook("Building Info",OpenBuildingDetails);
            Hook("Move Building",BeginSelectedMove);Hook("Deselect Building",()=>{DeselectBuilding();RefreshHUD();});
            Hook("Confirm Upgrade",StartSelectedUpgrade);Hook("Close Details",CloseBuildingDetails);
            Hook("Open Profile",()=>OpenProfile(0));Hook("Village Profile",()=>OpenProfile(0));
            Hook("Village Progress",()=>OpenProfile(4));Hook("Village Social",()=>OpenProfile(3));
            Hook("Attack Menu",()=>OpenProfile(5));Hook("Settings Menu",()=>OpenProfile(6));
            Hook("Close Profile",CloseProfile);Hook("Profile Buildings",()=>OpenProfile(4));
            Hook("Inventory Shop",()=>{CloseProfile();OpenShop();});
            Hook("Save Village",()=>{SaveProgress();CloseProfile();});
            for(int i=0;i<4;i++){int tab=i;Hook("Profile Tab "+i,()=>OpenProfile(tab));}
            Hook("Village Tab 0",()=>OpenProfile(0));
            Hook("Village Tab 1",()=>OpenUnavailableVillage("Builder Base"));
            Hook("Village Tab 2",()=>OpenUnavailableVillage("Clan Capital"));
        }

        void SetPageValue(string name,string value)
        {
            foreach(var text in profilePages[0].GetComponentsInChildren<Text>(true))
                if(text.name==name){text.text=value;return;}
        }

        void RefreshProfileValues()
        {
            if(State==null || profilePages.Count==0)return;
            profilePlayerValue.text=PlayerProfile.PlayerName;
            profileChiefValue.text="Chief • Local village\nTown Hall level "+State.TownHallLevel;
            for(int i=0;i<BuildingCatalog.Shop.Count;i++)
            {
                var d=BuildingCatalog.Shop[i];
                profileOwnedValues[i].text=d.Name+"\n"+State.Count(d.Id)+" built";
            }
            string result="Town Hall level "+State.TownHallLevel+"\n\n";
            foreach(var d in BuildingCatalog.Shop)result+=d.Name+"    "+State.Count(d.Id)+" / "+State.BuildingLimit(d.Id)+"\n";
            result+="\nBuilders available: "+(VillageState.BuilderCount-State.BusyBuilders)+" / "+VillageState.BuilderCount;
            inventoryValue.text=result;
        }

        public void HideEditorScreens()
        {
            if(shop!=null)shop.SetActive(false);
            if(detailModal!=null)detailModal.SetActive(false);
            if(profileModal!=null)profileModal.SetActive(false);
            if(selectedBar!=null)selectedBar.SetActive(false);
            if(placementBar!=null)placementBar.SetActive(false);
        }

        public void PreviewEditorScreen(int screen)
        {
            HideEditorScreens();
            if(hud==null)return;
            hud.SetActive(true);
            if(screen==1)shop.SetActive(true);
            else if(screen==2)detailModal.SetActive(true);
            else if(screen==3)placementBar.SetActive(true);
            else if(screen>=4)
            {
                profileModal.SetActive(true);
                for(int i=0;i<profilePages.Count;i++)profilePages[i].gameObject.SetActive(i==screen-4);
            }
        }

#if UNITY_EDITOR
        public void CreateEditorVillage()
        {
            if(editorVillage==null)
            {
                editorVillage=new GameObject("Starter Village Preview");
                editorVillage.transform.SetParent(transform,false);
                var hall=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(townHallPrefab,editorVillage.transform);
                hall.name="Town Hall";hall.transform.position=Vector3.zero;
                townHallPrefab=hall;
                var templates=new GameObject("Building Model Templates");templates.transform.SetParent(editorVillage.transform,false);
                goldMinePrefab=SceneModel(goldMinePrefab,templates.transform,0);
                elixirCollectorPrefab=SceneModel(elixirCollectorPrefab,templates.transform,1);
                goldStoragePrefab=SceneModel(goldStoragePrefab,templates.transform,2);
                elixirStoragePrefab=SceneModel(elixirStoragePrefab,templates.transform,3);
                templates.SetActive(false);
            }
            if(transform.Find("Village Woodland")==null)CreateForest();
        }
        GameObject SceneModel(GameObject prefab,Transform parent,int index)
        {
            var model=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,parent);
            model.transform.localPosition=new Vector3(index*5,0,8);
            return model;
        }
#endif
    }
}
