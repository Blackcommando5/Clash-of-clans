using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        [Serializable] sealed class ReferenceCard
        {
            public string id;
            public int category;
            public GameObject root;
            public WelcomePanel background;
            public Button buy,info;
            public Text stock,cost;
            public RawImage portrait;
        }
        [SerializeField] RectTransform referenceShopRoot;
        [SerializeField] List<ReferenceCard> referenceCards=new List<ReferenceCard>();
        [SerializeField] List<Button> referenceCategoryTabs=new List<Button>();
        [SerializeField] Text referenceShopGold,referenceShopElixir,referenceShopGems;
        [SerializeField] Text referenceShopNotice;
        [SerializeField] GameObject referenceDetailArt,referenceDetailPaper;
        [SerializeField] RawImage referenceDetailPortrait;
        [SerializeField] Text referenceDetailGain,referenceDetailTime;
        [SerializeField] Button referenceShopClose,referenceUpgradeSelection;
        [SerializeField] int referenceCategory=1;
        readonly List<Button> referenceSuggestions=new List<Button>();
        readonly List<string> referenceSuggestionKinds=new List<string>();
        readonly List<int> referenceSuggestionIndices=new List<int>();
        Text referenceSuggestionsTitle;
        readonly Color referenceGreen=new Color(.61f,.84f,.29f);

        static void FitReference(RectTransform rect,Vector2 min,Vector2 max)
        { rect.anchorMin=min;rect.anchorMax=max;rect.sizeDelta=Vector2.zero;rect.anchoredPosition=Vector2.zero; }

        static void ReferenceColor(WelcomePanel panel,Color color)
        { panel.topColor=color;panel.bottomColor=new Color(color.r*.77f,color.g*.77f,color.b*.77f,color.a);panel.SetVerticesDirty(); }

        void ReferenceText(Text text,int size,Color color,bool outlined=true)
        {
            var font=Resources.Load<Font>("Fonts/VillageBold");if(font!=null)text.font=font;
            text.fontSize=size;text.resizeTextMaxSize=size;text.color=color;
            var edge=text.GetComponent<Outline>();
            if(outlined){if(edge==null)edge=text.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.06f,.06f,.045f);edge.effectDistance=new Vector2(1.6f,-1.8f);}
            else if(edge!=null)edge.enabled=false;
        }

        void ReferenceButton(Button button,string icon,Vector2 min,Vector2 max,string caption)
        {
            FitReference((RectTransform)button.transform,min,max);
            var label=button.GetComponentInChildren<Text>();label.text=caption;ReferenceText(label,24,Color.white);
            FitReference(label.rectTransform,new Vector2(.03f,.015f),new Vector2(.97f,.3f));
            foreach(var old in button.GetComponentsInChildren<VillageIcon>())old.gameObject.SetActive(false);
            Icon(icon,button.transform,new Vector2(.17f,.29f),new Vector2(.83f,.92f));
            var shine=Panel("Button highlight",button.transform,new Vector2(.05f,.54f),new Vector2(.95f,.95f),new Color(1,1,1,.16f));
            shine.raycastTarget=false;shine.transform.SetAsFirstSibling();
        }

        Button ReferenceExistingButton(string name)
        { foreach(var action in sceneActions)if(action.id==name)return action.button;return null; }

        // One-time additive conversion. It also runs for old scenes, without changing village saves.
        public void BuildScreenshotReferenceInterface()
        {
            if(referenceShopRoot!=null)return;
            if(hud==null)BuildEditableInterface();
            var scaler=hud.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1600,702);scaler.matchWidthOrHeight=.5f;
            foreach(var t in hud.GetComponentsInChildren<Text>(true))
                if(t.color.r>.8f && t.color.g>.8f)ReferenceText(t,t.resizeTextMaxSize, t.color);
            BuildScreenshotHUD();BuildScreenshotShop();BuildScreenshotDetails();
        }

        void BuildScreenshotHUD()
        {
            var profile=ReferenceExistingButton("Open Profile");
            if(profile!=null)
            {
                FitReference((RectTransform)profile.transform,new Vector2(.047f,.89f),new Vector2(.215f,.984f));
                ReferenceColor(profile.GetComponent<WelcomePanel>(),new Color(0,0,0,.12f));
                FitReference(chiefName.rectTransform,new Vector2(.29f,.53f),new Vector2(.98f,1));chiefName.alignment=TextAnchor.MiddleLeft;
                var caption=profile.transform.Find("Village Caption");if(caption!=null)caption.gameObject.SetActive(false);
                var bar=Panel("Chief Progress Track",profile.transform,new Vector2(.26f,.24f),new Vector2(.99f,.57f),new Color(.03f,.12f,.15f,.9f));
                var progress=Panel("Chief Progress",bar.transform,Vector2.zero,new Vector2(.68f,1),new Color(.1f,.72f,.9f));progress.raycastTarget=false;bar.raycastTarget=false;
                ReferenceText(chiefName,18,Color.white);ReferenceText(chiefTier,36,Color.white);
            }
            var resources=safe.Find("Resources") as RectTransform;
            if(resources!=null)FitReference(resources,new Vector2(.8f,.712f),new Vector2(.955f,.99f));
            foreach(var label in new[]{goldLabel,elixirLabel,gemsLabel}){ReferenceText(label,23,Color.white);label.alignment=TextAnchor.MiddleRight;}
            if(resourceFills.Count>=3)
            { ReferenceColor(resourceFills[0].GetComponent<WelcomePanel>(),new Color(1,.84f,.16f));ReferenceColor(resourceFills[1].GetComponent<WelcomePanel>(),new Color(.86f,.18f,.87f)); }
            var builders=ReferenceExistingButton("Builders");
            if(builders!=null){FitReference((RectTransform)builders.transform,new Vector2(.448f,.91f),new Vector2(.547f,.995f));ReferenceText(builderLabel,26,Color.white);}
            FitReference(townLevelLabel.transform.parent as RectTransform,new Vector2(.56f,.925f),new Vector2(.66f,.985f));ReferenceText(townLevelLabel,17,Color.white);
            string[] names={"Village Profile","Village Progress","Village Social","Attack Menu","Settings Menu","Shop","Collect Resources"};
            string[] icons={"Stats","Tasks","Social","Map","Settings","Shop","Tasks"};
            string[] captions={"","","Social","Attack!","","Shop","Collect"};
            Vector2[] mins={new Vector2(.048f,.78f),new Vector2(.048f,.67f),new Vector2(.048f,.49f),new Vector2(.048f,.028f),new Vector2(.912f,.23f),new Vector2(.876f,.028f),new Vector2(.138f,.03f)};
            Vector2[] maxs={new Vector2(.089f,.875f),new Vector2(.089f,.765f),new Vector2(.091f,.613f),new Vector2(.128f,.2f),new Vector2(.955f,.32f),new Vector2(.956f,.2f),new Vector2(.18f,.12f)};
            for(int i=0;i<names.Length;i++)
            {
                var b=ReferenceExistingButton(names[i]);if(b==null)continue;
                ReferenceButton(b,icons[i],mins[i],maxs[i],captions[i]);
                ReferenceColor(b.GetComponent<WelcomePanel>(),i==4 || i==5 ? new Color(.88f,.91f,.74f) : new Color(.94f,.66f,.31f));
            }
            FitReference((RectTransform)guide.transform.parent,new Vector2(.25f,.018f),new Vector2(.75f,.08f));ReferenceText(guide,18,Color.white);
            FitReference((RectTransform)selectedBar.transform,new Vector2(.34f,.135f),new Vector2(.66f,.36f));
            var backing=selectedBar.GetComponent<WelcomePanel>();if(backing!=null)backing.enabled=false;
            FitReference(selectedTitle.rectTransform,new Vector2(-.2f,.78f),new Vector2(1.2f,1));ReferenceText(selectedTitle,32,Color.white);
            var info=ReferenceExistingButton("Building Info");var move=ReferenceExistingButton("Move Building");var dismiss=ReferenceExistingButton("Deselect Building");
            if(info!=null){ReferenceButton(info,"Info",new Vector2(.04f,0),new Vector2(.31f,.77f),"Info");ReferenceColor(info.GetComponent<WelcomePanel>(),new Color(.9f,.94f,.71f));}
            if(move!=null){ReferenceButton(move,"Move",new Vector2(.66f,0),new Vector2(.93f,.77f),"Move");ReferenceColor(move.GetComponent<WelcomePanel>(),new Color(.9f,.94f,.71f));}
            if(dismiss!=null){FitReference((RectTransform)dismiss.transform,new Vector2(.96f,.43f),new Vector2(1.04f,.69f));}
            referenceUpgradeSelection=Button("Selected Upgrade",selectedBar.transform,"Upgrade",new Vector2(.35f,0),new Vector2(.62f,.77f),new Color(.9f,.94f,.71f));
            ReferenceButton(referenceUpgradeSelection,"Builder",new Vector2(.35f,0),new Vector2(.62f,.77f),"Upgrade");
        }

        void BuildScreenshotShop()
        {
            // Preserve authored legacy elements and references for rollback and existing saves.
            foreach(Transform child in shop.transform)child.gameObject.SetActive(false);
            referenceShopRoot=Box("Reference Building Shop",shop.transform,Vector2.zero,Vector2.one);
            Panel("Shop Black Header",referenceShopRoot,Vector2.zero,Vector2.one,Color.black);
            Panel("Shop Paper",referenceShopRoot,Vector2.zero,new Vector2(1,.845f),new Color(.93f,.93f,.89f));
            var title=Label("Reference Shop Title",referenceShopRoot,"Buildings & Traps",44,Color.white,new Vector2(.18f,.764f),new Vector2(.82f,.845f));ReferenceText(title,44,Color.white);
            referenceShopNotice=Label("Catalogue Notice",referenceShopRoot,"",18,new Color(.2f,.24f,.15f),new Vector2(.1f,.729f),new Vector2(.9f,.766f));
            string[] menuIcons={"Builder","Map","Gems","Social","Stats"};
            for(int i=0;i<5;i++)
            {
                var tab=Panel("Shop Section "+i,referenceShopRoot,new Vector2(.337f+i*.067f,.846f),new Vector2(.4f+i*.067f,i==0 ? .984f : .96f),i==0 ? new Color(.94f,.94f,.9f) : new Color(.52f,.52f,.47f));
                Icon(menuIcons[i],tab.transform,new Vector2(.17f,.1f),new Vector2(.83f,.9f));
                if(i>0){var label=Label("Section Availability",tab.transform,"SOON",11,Color.white,new Vector2(.1f,0),new Vector2(.9f,.19f));ReferenceText(label,11,Color.white);}
            }
            referenceShopClose=Button("Reference Close Shop",referenceShopRoot,"X",new Vector2(.915f,.883f),new Vector2(.951f,.964f),new Color(.94f,.18f,.23f));
            string[] categories={"Army","Resources","Defenses","Traps"};
            for(int i=0;i<4;i++)
            {
                var tab=Button("Shop Category "+i,referenceShopRoot,categories[i],new Vector2(.235f+i*.134f,.66f),new Vector2(.365f+i*.134f,.728f),referenceGreen);
                ReferenceText(tab.GetComponentInChildren<Text>(),25,Color.white);referenceCategoryTabs.Add(tab);
            }
            Panel("Shop Card Tray",referenceShopRoot,new Vector2(.043f,.092f),new Vector2(.957f,.67f),new Color(.19f,.19f,.16f));
            // Visual catalogue of the supplied references. Unimplemented systems cannot be purchased.
            string[][] ids={new[]{"ArmyCamp","Barracks","Laboratory","HeroHall","SpellFactory"},new[]{"BuilderHut","ElixirCollector","GoldMine","ElixirStorage","GoldStorage"},new[]{"Cannon","ArcherTower","Wall","Mortar","AirDefense"},new[]{"Bomb","SpringTrap","AirBomb","GiantBomb","SeekingAirMine"}};
            string[][] titles={new[]{"Army Camp","Barracks","Laboratory","Hero Hall","Spell Factory"},new[]{"Builder's Hut","Elixir Collector","Gold Mine","Elixir Storage","Gold Storage"},new[]{"Cannon","Archer Tower","Wall","Mortar","Air Defense"},new[]{"Bomb","Spring Trap","Air Bomb","Giant Bomb","Seeking Air Mine"}};
            for(int category=0;category<4;category++)for(int i=0;i<5;i++)BuildReferenceCard(category,i,ids[category][i],titles[category][i]);
            var footer=Panel("Shop Resource Footer",referenceShopRoot,new Vector2(.045f,.012f),new Vector2(.955f,.096f),new Color(.65f,.84f,.46f));
            referenceShopGold=ReferenceFooterValue(footer.transform,"Gold",.3f);
            referenceShopElixir=ReferenceFooterValue(footer.transform,"Elixir",.445f);
            referenceShopGems=ReferenceFooterValue(footer.transform,"Gems",.59f);
            SetReferenceCategory(referenceCategory);
        }

        Text ReferenceFooterValue(Transform parent,string kind,float x)
        {
            var bar=Panel(kind+" Footer",parent,new Vector2(x,.17f),new Vector2(x+.13f,.7f),new Color(.28f,.43f,.19f));
            Icon(kind,bar.transform,new Vector2(.8f,-.13f),new Vector2(1.03f,1.2f));
            var text=Label(kind+" Footer Value",bar.transform,"0",22,Color.white,new Vector2(.02f,0),new Vector2(.79f,1));ReferenceText(text,22,Color.white);return text;
        }

        void BuildReferenceCard(int category,int column,string id,string title)
        {
            float x=.053f+column*.182f;
            var panel=Panel("Catalogue "+id,referenceShopRoot,new Vector2(x,.12f),new Vector2(x+.171f,.642f),new Color(.28f,.72f,.82f));
            var edge=panel.gameObject.AddComponent<Outline>();edge.effectColor=Color.black;edge.effectDistance=new Vector2(2,-3);
            var heading=Label("Card Name",panel.transform,title,27,Color.white,new Vector2(.04f,.86f),new Vector2(.84f,.98f));heading.alignment=TextAnchor.UpperLeft;ReferenceText(heading,27,Color.white);
            var info=Button("Catalogue Info "+id,panel.transform,"i",new Vector2(.86f,.88f),new Vector2(.97f,.976f),new Color(.65f,.71f,.72f));
            var art=Box("Catalogue Portrait",panel.transform,new Vector2(.025f,.3f),new Vector2(.975f,.87f));
            var burst=art.gameObject.AddComponent<ReferenceSunburst>();burst.raycastTarget=false;
            var image=Box("Model",art,new Vector2(.02f,.02f),new Vector2(.98f,.98f)).gameObject.AddComponent<RawImage>();
            image.texture=Resources.Load<Texture2D>("BuildingIcons/"+id);image.raycastTarget=false;
            var aspect=image.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            if(image.texture==null){image.enabled=false;var silhouette=Box("Building Illustration",art,new Vector2(.17f,.08f),new Vector2(.83f,.88f)).gameObject.AddComponent<VillageIcon>();silhouette.kind=id;silhouette.raycastTarget=false;}
            var stock=Label("Card Stock",panel.transform,"",21,Color.white,new Vector2(.045f,.22f),new Vector2(.955f,.36f));ReferenceText(stock,21,Color.white);
            var buy=Button("Catalogue Buy "+id,panel.transform,"",new Vector2(.03f,.035f),new Vector2(.97f,.205f),new Color(.22f,.47f,.52f));
            var cost=buy.GetComponentInChildren<Text>();ReferenceText(cost,29,Color.white);
            var definition=BuildingCatalog.Find(id);
            if(definition!=null)
            {
                Icon(definition.CostResource==ResourceKind.Gold ? "Gold" : "Elixir",buy.transform,new Vector2(.7f,.14f),new Vector2(.89f,.86f));
                FitReference(cost.rectTransform,new Vector2(.1f,.02f),new Vector2(.69f,.98f));cost.alignment=TextAnchor.MiddleRight;
            }
            referenceCards.Add(new ReferenceCard{id=id,category=category,root=panel.gameObject,background=panel,buy=buy,info=info,stock=stock,cost=cost,portrait=image});
        }

        void BuildScreenshotDetails()
        {
            detailPanel.sizeDelta=new Vector2(1200,750);
            ReferenceColor(detailPanel.Find("Details Frame").GetComponent<WelcomePanel>(),new Color(.48f,.42f,.39f));
            FitReference(detailTitle.rectTransform,new Vector2(.03f,.9f),new Vector2(.93f,.99f));ReferenceText(detailTitle,35,Color.white);
            FitReference(detailPanel.Find("Close Details") as RectTransform,new Vector2(.94f,.91f),new Vector2(.988f,.984f));
            var art=Panel("Upgrade Model Landscape",detailPanel,new Vector2(.018f,.026f),new Vector2(.452f,.855f),new Color(.59f,.75f,.24f));referenceDetailArt=art.gameObject;
            var rim=art.gameObject.AddComponent<Outline>();rim.effectColor=new Color(.9f,.9f,.85f);rim.effectDistance=new Vector2(5,-5);
            var portrait=Box("Upgrade Model",art.transform,new Vector2(.06f,.19f),new Vector2(.94f,.81f));
            referenceDetailPortrait=portrait.gameObject.AddComponent<RawImage>();referenceDetailPortrait.raycastTarget=false;
            var fit=portrait.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
            var paper=Panel("Upgrade Description Paper",detailPanel,new Vector2(.468f,.196f),new Vector2(.981f,.855f),Color.white);referenceDetailPaper=paper.gameObject;
            var paperEdge=paper.gameObject.AddComponent<Outline>();paperEdge.effectDistance=new Vector2(4,-4);paperEdge.effectColor=new Color(.8f,.8f,.8f);
            var gain=Panel("Upgrade Stat Gain",paper.transform,new Vector2(.025f,.81f),new Vector2(.975f,.98f),new Color(.61f,.86f,.13f));
            referenceDetailGain=Label("Upgrade Gain",gain.transform,"",27,new Color(.15f,.23f,.05f));ReferenceText(referenceDetailGain,27,new Color(.15f,.23f,.05f),false);
            paper.transform.SetSiblingIndex(1);art.transform.SetSiblingIndex(1);
            FitReference(detailStats.rectTransform,new Vector2(.49f,.23f),new Vector2(.96f,.704f));ReferenceText(detailStats,25,new Color(.12f,.13f,.11f),false);
            FitReference((RectTransform)upgradeAction.transform,new Vector2(.655f,.035f),new Vector2(.85f,.167f));ReferenceColor(upgradeAction.GetComponent<WelcomePanel>(),new Color(.66f,.92f,.23f));
            ReferenceText(upgradeCaption,27,Color.white);
            referenceDetailTime=Label("Upgrade Duration",detailPanel,"",24,Color.white,new Vector2(.855f,.035f),new Vector2(.985f,.167f));ReferenceText(referenceDetailTime,24,Color.white);
        }

        void BindScreenshotReferenceInterface()
        {
            RefreshDefenseCatalogueArt();
            referenceShopClose.onClick.RemoveAllListeners();referenceShopClose.onClick.AddListener(CloseShop);
            referenceUpgradeSelection.onClick.RemoveAllListeners();referenceUpgradeSelection.onClick.AddListener(OpenBuildingDetails);
            for(int i=0;i<referenceCategoryTabs.Count;i++){int tab=i;referenceCategoryTabs[i].onClick.RemoveAllListeners();referenceCategoryTabs[i].onClick.AddListener(()=>SetReferenceCategory(tab));}
            foreach(var card in referenceCards)
            {
                var item=card;card.buy.onClick.RemoveAllListeners();card.info.onClick.RemoveAllListeners();
                card.buy.onClick.AddListener(()=>BeginPlacement(item.id));
                card.info.onClick.AddListener(()=>ShowReferenceCardInfo(item.id));
            }
        }

        public void RefreshDefenseCatalogueArt()
        {
            foreach(var card in referenceCards)
            {
                var definition=BuildingCatalog.Find(card.id);if(definition==null || (!definition.IsDefense && card.id!="ArmyCamp" && card.id!="Barracks"))continue;
                card.portrait.texture=Resources.Load<Texture2D>("BuildingIcons/"+card.id);
                card.portrait.enabled=card.portrait.texture!=null;
                var illustration=card.portrait.transform.parent.Find("Building Illustration");
                if(illustration!=null)illustration.gameObject.SetActive(!card.portrait.enabled);
                string currency=definition.CostResource==ResourceKind.Gold ? "Gold" : "Elixir";
                if(card.buy.transform.Find(currency+" Icon")==null)Icon(currency,card.buy.transform,new Vector2(.7f,.14f),new Vector2(.89f,.86f));
                FitReference(card.cost.rectTransform,new Vector2(.1f,.02f),new Vector2(.69f,.98f));card.cost.alignment=TextAnchor.MiddleRight;
            }
        }

        public void SetReferenceCategory(int category)
        {
            referenceCategory=Mathf.Clamp(category,0,3);
            if(referenceShopNotice!=null)referenceShopNotice.text="";
            foreach(var card in referenceCards)card.root.SetActive(card.category==referenceCategory);
            for(int i=0;i<referenceCategoryTabs.Count;i++)ReferenceColor(referenceCategoryTabs[i].GetComponent<WelcomePanel>(),i==referenceCategory ? referenceGreen : new Color(.4f,.45f,.39f));
            if(State!=null)RefreshScreenshotReference();
        }

        void ShowReferenceCardInfo(string id)
        {
            var d=BuildingCatalog.Find(id);
            if(d==null){referenceShopNotice.text="This building is not available in Kingdoms yet.";return;}
            int index=State.buildings.FindIndex(b=>b.kind==id);
            if(index>=0){CloseShop();SelectBuilding(index);OpenBuildingDetails();}
            else referenceShopNotice.text=d.Name+": "+(id=="Wall" ? "Connects to adjacent walls." : d.Description.Replace('\n',' '));
        }

        void RefreshScreenshotReference()
        {
            if(referenceShopRoot==null || State==null)return;
            Shader.SetGlobalFloat("_KingdomsPlacementGrid",IsPlacing ? 1 : 0);
            referenceShopGold.text=State.gold.ToString("N0");referenceShopElixir.text=State.elixir.ToString("N0");referenceShopGems.text=State.gems.ToString("N0");
            foreach(var card in referenceCards)
            {
                var d=BuildingCatalog.Find(card.id);bool available=d!=null && State.Count(d.Id)<State.BuildingLimit(d.Id);
                card.buy.interactable=d!=null && State.CanBuy(d.Id,out _);
                ReferenceColor(card.background,available ? new Color(.28f,.74f,.85f) : new Color(.62f,.63f,.62f));
                card.portrait.color=available ? Color.white : new Color(.65f,.65f,.65f);
                card.stock.text=d==null ? "COMING TO KINGDOMS" : "Build: Instant       Built:\n"+State.Count(d.Id)+" / "+State.BuildingLimit(d.Id);
                if(card.id=="ArmyCamp" && State.Count("Barracks")==0)card.stock.text="Requires Barracks";
                card.cost.text=d==null ? "UNAVAILABLE" : d.Cost.ToString("N0");
                card.cost.color=d!=null && State.Balance(d.CostResource)<d.Cost ? new Color(1,.48f,.48f) : Color.white;
            }
            collectLabel.text="Collect";
            foreach(var badge in producerButtons)
            {
                ((RectTransform)badge.transform).sizeDelta=new Vector2(105,30);
                var caption=badge.GetComponentInChildren<Text>();ReferenceText(caption,15,Color.white);
                caption.text=badge.interactable ? "Collect" : "Producing";
                ReferenceColor(badge.GetComponent<WelcomePanel>(),new Color(.22f,.31f,.09f,.8f));
            }
            guide.transform.parent.gameObject.SetActive(IsPlacing || Time.unscaledTime<messageUntil || TutorialHintsVisible);
            referenceDetailArt.SetActive(!viewingBuilderQueue);referenceDetailPaper.SetActive(!viewingBuilderQueue);referenceDetailTime.gameObject.SetActive(!viewingBuilderQueue);
            LayoutReferenceDetails();
            if(viewingBuilderQueue)return;
            if(selectedIndex<0 || selectedIndex>=State.buildings.Count)return;
            var b=State.buildings[selectedIndex];var definition=BuildingCatalog.Find(b.kind);
            referenceDetailPortrait.texture=Resources.Load<Texture2D>("BuildingIcons/"+b.kind);
            referenceDetailPortrait.enabled=referenceDetailPortrait.texture!=null;
            string gain=definition.StorageBonus>0 ? "Storage Capacity\n"+(definition.StorageBonus*b.level).ToString("N0")+"  + "+definition.StorageBonus.ToString("N0") : definition.ProductionPerSecond>0 ? "Production / minute\n"+(definition.ProductionPerSecond*b.level*60)+"  + "+(definition.ProductionPerSecond*60) : b.kind=="TownHall" ? "Town Hall Level\n"+b.level+"  →  "+Mathf.Min(3,b.level+1) : "Connected village walls";
            referenceDetailGain.text=b.level>=3 ? "Maximum available level" : gain;
            if(b.kind=="ArmyCamp")referenceDetailGain.text="Army spaces: "+(VillageState.ArmySpacesPerCampLevel*b.level)+(b.level<3 ? " + 8" : " (maximum)");
            if(b.kind=="Barracks")referenceDetailGain.text=b.level>=2 ? "Tanks unlocked (maximum level)" : "Level 2 unlocks Tanks";
            if(definition.IsDefense)referenceDetailGain.text=b.level>=3 ? "Maximum available level" : "Hit points: "+(definition.HitPoints*b.level)+" + "+definition.HitPoints+"\nDamage / second: "+(definition.DamagePerSecond*b.level)+" + "+definition.DamagePerSecond;
            if(b.kind=="Wall")referenceDetailGain.text=b.level>=3 ? "Fortified stone (maximum level)" : WallSegment.LevelName(b.level)+" to "+WallSegment.LevelName(b.level+1);
            referenceDetailTime.text=(b.level>=3 || (b.kind=="Barracks" && b.level>=2)) ? "" : "Upgrade time\n"+Duration(b.upgradeFinishes>0 ? b.upgradeFinishes-VillageState.Now : VillageState.UpgradeSeconds(b));
            if(b.upgradeFinishes==0 && b.level<3 && (b.kind!="Barracks" || b.level<2))detailTitle.text="Upgrade "+definition.Name+" to Level "+(b.level+1)+"?";
        }

        void LayoutReferenceDetails()
        {
            var frame=detailPanel.Find("Details Frame").GetComponent<WelcomePanel>();
            var dimmer=detailModal.transform.Find("Details Dimmer").GetComponent<WelcomePanel>();
            foreach(var button in referenceSuggestions)button.gameObject.SetActive(viewingBuilderQueue);
            if(referenceSuggestionsTitle!=null)referenceSuggestionsTitle.gameObject.SetActive(viewingBuilderQueue);
            if(!viewingBuilderQueue)
            {
                detailPanel.anchorMin=detailPanel.anchorMax=new Vector2(.5f,.5f);detailPanel.anchoredPosition=Vector2.zero;detailPanel.sizeDelta=new Vector2(1200,750);
                ReferenceColor(frame,new Color(.48f,.42f,.39f));ReferenceColor(dimmer,new Color(0,0,0,.65f));
                FitReference(detailTitle.rectTransform,new Vector2(.03f,.9f),new Vector2(.93f,.99f));ReferenceText(detailTitle,35,Color.white);
                FitReference(detailPanel.Find("Close Details") as RectTransform,new Vector2(.94f,.91f),new Vector2(.988f,.984f));
                return;
            }
            detailPanel.anchorMin=detailPanel.anchorMax=new Vector2(.5f,1);detailPanel.sizeDelta=new Vector2(400,550);detailPanel.anchoredPosition=new Vector2(0,-365);
            ReferenceColor(frame,new Color(.2f,.27f,.065f,.94f));ReferenceColor(dimmer,Color.clear);
            FitReference(detailTitle.rectTransform,new Vector2(.025f,.9f),new Vector2(.87f,.98f));ReferenceText(detailTitle,23,Color.white);
            FitReference(detailPanel.Find("Close Details") as RectTransform,new Vector2(.9f,.91f),new Vector2(.98f,.975f));
            for(int i=0;i<builderRows.Count;i++)
            {
                FitReference((RectTransform)builderRows[i].transform,new Vector2(.035f,.745f-i*.145f),new Vector2(.965f,.877f-i*.145f));
                ReferenceText(builderRows[i].GetComponentInChildren<Text>(),19,Color.white);
            }
            if(referenceSuggestions.Count==0)
            {
                referenceSuggestionsTitle=Label("Suggested Upgrades",detailPanel,"Suggested upgrades",22,new Color(.8f,1,.45f),new Vector2(.03f,.52f),new Vector2(.97f,.59f));
                for(int i=0;i<5;i++)
                {
                    int row=i;
                    var suggestion=Button("Suggested Upgrade "+i,detailPanel,"",new Vector2(.03f,.423f-i*.084f),new Vector2(.97f,.5f-i*.084f),new Color(.24f,.32f,.1f,.1f));
                    ReferenceText(suggestion.GetComponentInChildren<Text>(),20,Color.white);
                    suggestion.onClick.AddListener(()=>OpenReferenceSuggestion(row));referenceSuggestions.Add(suggestion);
                }
            }
            referenceSuggestionKinds.Clear();referenceSuggestionIndices.Clear();
            foreach(var d in BuildingCatalog.Purchasable)
                if(State.CanBuy(d.Id,out _)){referenceSuggestionKinds.Add(d.Id);referenceSuggestionIndices.Add(-1);}
            for(int i=0;i<State.buildings.Count;i++)
                if(State.CanUpgrade(i,out _)){referenceSuggestionKinds.Add(State.buildings[i].kind);referenceSuggestionIndices.Add(i);}
            for(int i=0;i<referenceSuggestions.Count;i++)
            {
                bool present=i<referenceSuggestionKinds.Count;referenceSuggestions[i].gameObject.SetActive(present);
                if(!present)continue;
                var d=BuildingCatalog.Find(referenceSuggestionKinds[i]);int index=referenceSuggestionIndices[i];
                int cost=index<0 ? d.Cost : VillageState.UpgradeCost(State.buildings[index]);
                referenceSuggestions[i].GetComponentInChildren<Text>().text=(index<0 ? "New " : "")+d.Name+"   "+cost.ToString("N0");
            }
            referenceSuggestionsTitle.text=referenceSuggestionKinds.Count>0 ? "Suggested upgrades" : "Collect resources to keep building";
        }

        void OpenReferenceSuggestion(int row)
        {
            if(!viewingBuilderQueue || row<0 || row>=referenceSuggestionKinds.Count)return;
            string kind=referenceSuggestionKinds[row];int index=referenceSuggestionIndices[row];
            CloseBuildingDetails();
            if(index<0)BeginPlacement(kind);else{SelectBuilding(index);OpenBuildingDetails();}
        }
    }

}
