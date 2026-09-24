using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        [SerializeField] GameObject profileModal;
        [SerializeField] RectTransform profileWindow, profileContent;
        [SerializeField] Text chiefName, chiefTier;
        [SerializeField] System.Collections.Generic.List<Button> profileTabs = new System.Collections.Generic.List<Button>();

        public bool ProfileOpen => profileModal != null && profileModal.activeSelf;

        void BuildReferenceUI()
        {
            // The gameplay canvas owns the identity badge so modal windows cover it.
            var badge = Button("Open Profile",safe,"",new Vector2(0,1),new Vector2(0,1),
                new Color(.12f,.18f,.16f,.8f),new Vector2(335,74),new Vector2(188,-51));
            var tier = Panel("Chief Shield",badge.transform,new Vector2(0,0),new Vector2(.22f,1),new Color(.16f,.72f,.9f));
            chiefTier = Label("Chief Tier",tier.transform,"1",36,Color.white);
            chiefName = Label("Chief Name",badge.transform,"",25,Color.white,new Vector2(.25f,.34f),new Vector2(.98f,.94f));
            Label("Village Caption",badge.transform,"HOME VILLAGE",13,new Color(.84f,.9f,.77f),new Vector2(.25f,.07f),new Vector2(.98f,.35f));
            badge.onClick.AddListener(()=>OpenProfile(0));
            RailButton("Village Profile","PROFILE",0,()=>OpenProfile(0));
            RailButton("Village Progress","BUILDINGS",1,()=>OpenProfile(4));
            RailButton("Village Social","SOCIAL",2,()=>OpenProfile(3));
            var attack=Button("Attack Menu",safe,"Attack!",Vector2.zero,Vector2.zero,new Color(.94f,.63f,.2f),new Vector2(150,134),new Vector2(100,88));
            Icon("Map",attack.transform,new Vector2(.17f,.35f),new Vector2(.83f,.92f));
            attack.GetComponentInChildren<Text>().rectTransform.anchorMax=new Vector2(1,.35f);
            attack.onClick.AddListener(()=>OpenProfile(5));
            var settings=Button("Settings Menu",safe,"SETTINGS",new Vector2(1,0),new Vector2(1,0),new Color(.66f,.67f,.59f),new Vector2(100,72),new Vector2(-79,245));
            settings.GetComponentInChildren<Text>().resizeTextMaxSize=18;
            settings.onClick.AddListener(()=>OpenProfile(6));
            // New controls must stay underneath all modal and placement layers.
            shop.transform.SetAsLastSibling(); placementBar.transform.SetAsLastSibling(); detailModal.transform.SetAsLastSibling();
            profileModal=Box("Player Profile",safe,Vector2.zero,Vector2.one).gameObject;
            Panel("Profile Dimmer",profileModal.transform,Vector2.zero,Vector2.one,new Color(0,0,0,.65f));
            profileWindow=Box("Profile Window",profileModal.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1200,780));
            var frame=Panel("Profile Outer Stone",profileWindow,Vector2.zero,Vector2.one,new Color(.4f,.37f,.35f));
            var edge=frame.gameObject.AddComponent<Outline>();edge.effectDistance=new Vector2(3,-4);edge.effectColor=new Color(.12f,.1f,.09f);
            Panel("Profile Paper",profileWindow,new Vector2(.012f,.018f),new Vector2(.988f,.875f),new Color(.91f,.91f,.85f));
            string[] tabs={"My Profile","My Clan","Clans","Social"};
            for(int i=0;i<tabs.Length;i++)
            {
                int index=i;
                var tab=Button("Profile Tab "+i,profileWindow,tabs[i],new Vector2(.02f+i*.225f,.884f),new Vector2(.24f+i*.225f,.982f),new Color(.55f,.53f,.49f));
                tab.GetComponentInChildren<Text>().resizeTextMaxSize=29;
                tab.onClick.AddListener(()=>OpenProfile(index));profileTabs.Add(tab);
            }
            var close=Button("Close Profile",profileWindow,"X",new Vector2(.927f,.901f),new Vector2(.982f,.971f),new Color(.89f,.22f,.25f));
            close.onClick.AddListener(CloseProfile);
            profileContent=Box("Profile Content",profileWindow,new Vector2(.025f,.035f),new Vector2(.975f,.86f));
            profileModal.SetActive(false);
        }

        void RailButton(string name,string caption,int index,UnityEngine.Events.UnityAction action)
        {
            var b=Button(name,safe,caption,new Vector2(0,1),new Vector2(0,1),new Color(.84f,.63f,.32f),new Vector2(94,76),new Vector2(69,-155-index*92));
            b.GetComponentInChildren<Text>().resizeTextMaxSize=17;b.onClick.AddListener(action);
        }

        public void OpenProfile(int tab=0)
        {
            if(tab==5){OpenPracticeBattle();return;}
            if(State==null || !PlayerProfile.HasPlayerName || IsPlacing)return;
            CloseShop();CloseBuildingDetails();DeselectBuilding();

            profileModal.SetActive(true);if(tutorialEntry!=null)tutorialEntry.gameObject.SetActive(false);cameraController.InputBlocked=true;
            for(int i=0;i<profilePages.Count;i++) profilePages[i].gameObject.SetActive(i==tab);
            profileContent=profilePages[tab];
            RefreshProfileValues();
            for(int i=0;i<profileTabs.Count;i++)
            {
                var p=profileTabs[i].GetComponent<WelcomePanel>();
                p.topColor=i==tab ? new Color(.87f,.87f,.82f) : new Color(.54f,.52f,.48f);
                p.bottomColor=p.topColor*.9f;p.SetVerticesDirty();
            }
        }

        void BuildProfilePage(int tab)
        {
            if(tab==0) BuildPlayerProfile();
            else if(tab==4) BuildVillageInventory();
            else if(tab==6)
            {
                Label("Settings Title",profileContent,"Settings",42,Color.white,new Vector2(.05f,.78f),new Vector2(.95f,.94f));
                Label("Save Information",profileContent,"Your village saves on this device.\nCloud accounts are not connected.\n\nCamera: drag to pan, pinch or scroll to zoom.",28,new Color(.22f,.24f,.28f),new Vector2(.08f,.34f),new Vector2(.92f,.73f));
                var save=Button("Save Village",profileContent,"SAVE VILLAGE",new Vector2(.3f,.12f),new Vector2(.7f,.28f),new Color(.43f,.74f,.16f));
                save.onClick.AddListener(()=>{SaveProgress();CloseProfile();});
            }
            else
            {
                string heading=tab==1?"My Clan":tab==2?"Find a Clan":tab==3?"Social":tab==7?"Builder Base":tab==8?"Clan Capital":"Attack";
                Label("Section Title",profileContent,heading,45,new Color(.25f,.26f,.3f),new Vector2(.05f,.7f),new Vector2(.95f,.9f));
                Label("Section Availability",profileContent,tab>=7?"This village mode is under development. Your Home Village remains available.":tab==5?"Battles are under development.\nTroop training, deployment and enemy villages are not available yet.":"Multiplayer is under development.\nClan membership, search and friends require the online service.",30,new Color(.3f,.32f,.38f),new Vector2(.12f,.25f),new Vector2(.88f,.68f));
            }
        }

        void BuildPlayerProfile()
        {
            string[] villages={"Home Village","Builder Base","Clan Capital"};
            for(int i=0;i<3;i++)
            {
                int index=i;
                var b=Button("Village Tab "+i,profileContent,villages[i],new Vector2(i*.333f,.885f),new Vector2(i*.333f+.32f,.985f),new Color(.64f,.64f,.6f));
                b.GetComponentInChildren<Text>().resizeTextMaxSize=26;
                b.onClick.AddListener(()=>{if(index==0)OpenProfile(0);else OpenUnavailableVillage(villages[index]);});
            }
            var stats=Panel("Profile Statistics",profileContent,new Vector2(0,.34f),new Vector2(1,.86f),new Color(.53f,.59f,.73f));
            Label("Profile Player Name",stats.transform,"Chief",36,Color.white,new Vector2(.03f,.69f),new Vector2(.38f,.94f));
            Label("Profile Chief",stats.transform,"Chief â€¢ Local village\nTown Hall level "+State.TownHallLevel,24,Color.white,new Vector2(.03f,.39f),new Vector2(.38f,.67f));
            var inventory=Button("Profile Buildings",stats.transform,"MY BUILDINGS",new Vector2(.035f,.1f),new Vector2(.36f,.3f),new Color(.47f,.8f,.15f));
            inventory.GetComponentInChildren<Text>().resizeTextMaxSize=24;inventory.onClick.AddListener(()=>OpenProfile(4));
            Panel("Profile Divider",stats.transform,new Vector2(.405f,.07f),new Vector2(.407f,.95f),new Color(.71f,.75f,.84f));
            Label("Profile Clan",stats.transform,"No clan\n\nClan membership\nComing with multiplayer",27,Color.white,new Vector2(.43f,.16f),new Vector2(.69f,.92f));
            Panel("League Divider",stats.transform,new Vector2(.72f,.07f),new Vector2(.722f,.95f),new Color(.71f,.75f,.84f));
            Label("Profile League",stats.transform,"Unranked\n\nBattles and leagues\nUnder development",25,Color.white,new Vector2(.74f,.16f),new Vector2(.98f,.92f));
            var bottom=Panel("Village Collection",profileContent,Vector2.zero,new Vector2(1,.31f),new Color(.57f,.62f,.75f));
            Label("Village Collection Heading",bottom.transform,"Village buildings",26,Color.white,new Vector2(.02f,.72f),new Vector2(.98f,.98f));
            for(int i=0;i<BuildingCatalog.Shop.Count;i++)
            {
                var d=BuildingCatalog.Shop[i];float left=.02f+i*.245f;
                var card=Panel("Profile "+d.Id,bottom.transform,new Vector2(left,.06f),new Vector2(left+.23f,.7f),new Color(.76f,.79f,.87f));
                IconImage(card.transform,d.Id,new Vector2(.01f,.06f),new Vector2(.4f,.94f));
                Label("Owned",card.transform,d.Name+"\n"+State.Count(d.Id)+" built",19,new Color(.2f,.22f,.3f),new Vector2(.42f,.06f),new Vector2(.98f,.94f));
            }
        }

        void OpenUnavailableVillage(string village)
        {
            OpenProfile(village=="Builder Base" ? 7 : 8);
        }

        void IconImage(Transform parent,string id,Vector2 min,Vector2 max)
        {
            var holder=Box("Portrait Area",parent,min,max);
            var rect=Box("Building Portrait",holder,Vector2.zero,Vector2.one);
            var fit=rect.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
            var img=rect.gameObject.AddComponent<RawImage>();img.texture=Resources.Load<Texture2D>("BuildingIcons/"+id);img.raycastTarget=false;
        }

        void BuildVillageInventory()
        {
            Label("Inventory Title",profileContent,"My Buildings",40,new Color(.23f,.25f,.3f),new Vector2(.02f,.84f),new Vector2(.98f,.98f));
            string result="Town Hall level "+State.TownHallLevel+"\n\n";
            foreach(var d in BuildingCatalog.Shop)result+=d.Name+"    "+State.Count(d.Id)+" / "+State.BuildingLimit(d.Id)+"\n";
            result+="\nBuilders available: "+(2-State.BusyBuilders)+" / 2";
            Label("Inventory Counts",profileContent,result,30,new Color(.25f,.28f,.34f),new Vector2(.07f,.21f),new Vector2(.93f,.84f));
            var b=Button("Inventory Shop",profileContent,"OPEN SHOP",new Vector2(.32f,.04f),new Vector2(.68f,.19f),new Color(.45f,.75f,.17f));
            b.onClick.AddListener(()=>{CloseProfile();OpenShop();});
        }

        public void CloseProfile()
        {
            if(profileModal!=null)profileModal.SetActive(false);
            if(cameraController!=null)cameraController.InputBlocked=IsPlacing || (shop!=null && shop.activeSelf) || DetailsOpen;
        }

        void RefreshReferenceUI()
        {
            if(chiefName!=null)chiefName.text=PlayerProfile.PlayerName;
            if(chiefTier!=null)chiefTier.text=State.TownHallLevel.ToString();
            if(fitWindowsToScreen && profileWindow!=null)profileWindow.localScale=Vector3.one*Mathf.Min(1,Mathf.Min(safe.rect.width/1250f,safe.rect.height/810f));
        }
    }
}
