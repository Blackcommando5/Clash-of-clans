using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        public GameObject wallPrefab;
        [SerializeField] Transform starterWallLayout;
        [SerializeField] Button wallShopButton;
        [SerializeField] Text wallStockLabel;

        void BindWalls()
        {
            if(wallShopButton!=null)wallShopButton.onClick.AddListener(()=>BeginPlacement("Wall"));
        }

        void RefreshWallsHUD()
        {
            if(wallShopButton==null || State==null)return;
            wallShopButton.interactable=State.CanBuy("Wall",out _);
            wallStockLabel.text="WALL  |  25 GOLD\n"+State.Count("Wall")+" / "+State.BuildingLimit("Wall")+" built";
        }

        void AddStarterWalls(VillageState target)
        {
            if(starterWallLayout==null)return;
            foreach(var segment in starterWallLayout.GetComponentsInChildren<WallSegment>())
            {
                var p=segment.transform.position;
                target.buildings.Add(new PlacedBuilding{kind="Wall",x=Mathf.RoundToInt(p.x-.5f),z=Mathf.RoundToInt(p.z-.5f)});
            }
        }

#if UNITY_EDITOR
        public void AddEditableWalls(GameObject prefab)
        {
            if(wallPrefab==null)wallPrefab=prefab;
            if(wallShopButton==null)
            {
                wallShopButton=Button("Buy Wall",shopPanel,"WALL | 25 GOLD",new Vector2(.72f,.86f),new Vector2(.97f,.98f),new Color(.46f,.72f,.18f));
                wallStockLabel=wallShopButton.GetComponentInChildren<Text>();
                wallStockLabel.resizeTextMaxSize=23;
                // Extend the authored shop without recreating its existing cards.
                var title=shopPanel.Find("Shop Title");
                if(title!=null)((RectTransform)title).anchorMax=new Vector2(.7f,.97f);
            }
            if(starterWallLayout==null)
            {
                starterWallLayout=new GameObject("Starter Wall Layout").transform;
                starterWallLayout.SetParent(editorVillage.transform,false);
                for(int x=-3;x<=2;x++)
                    for(int z=-3;z<=2;z++)
                    {
                        if(x!=-3 && x!=2 && z!=-3 && z!=2)continue;
                        if(z==-3 && (x==-1 || x==0))continue;
                        AddEditorWall(x,z);
                    }
            }
            RefreshEditorWalls();
        }

        public void AddEditorWall(int x,int z)
        {
            if(starterWallLayout==null || wallPrefab==null)return;
            foreach(Transform item in starterWallLayout)
                if(Vector3.Distance(item.position,new Vector3(x+.5f,0,z+.5f))<.1f)return;
            var design=VillageState.Create(VillageState.Now);
            design.buildings[0].x=Mathf.RoundToInt(townHallPrefab.transform.position.x-2);
            design.buildings[0].z=Mathf.RoundToInt(townHallPrefab.transform.position.z-2);
            AddStarterWalls(design);
            if(!design.CanPlace("Wall",x,z,out string reason)){Debug.LogWarning("Wall layout: "+reason);return;}
            var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(wallPrefab,starterWallLayout);
            UnityEditor.Undo.RegisterCreatedObjectUndo(instance,"Place wall");
            instance.transform.position=new Vector3(x+.5f,0,z+.5f);
            instance.name="Wall ("+x+", "+z+")";
            RefreshEditorWalls();
        }

        public void RefreshEditorWalls()
        {
            if(starterWallLayout==null)return;
            foreach(var wall in starterWallLayout.GetComponentsInChildren<WallSegment>())wall.RefreshConnections();
        }
#endif
    }
}


