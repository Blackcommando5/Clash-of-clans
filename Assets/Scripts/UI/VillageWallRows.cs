using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        int wallRowLength=1, wallRowDirection;
        GameObject wallRowControls, wallRowWorld;
        Button wallRowShorter, wallRowLonger, wallRowRotate;
        Text wallRowCount;
        readonly List<GameObject> wallRowModels=new List<GameObject>();
        bool PlacingWallRow=>IsPlacing && placingKind=="Wall" && movingIndex<0;

        void BeginWallRowPreview()
        {
            wallRowLength=1;wallRowDirection=0;
            if(!PlacingWallRow)return;
            if(wallRowControls==null)
            {
                wallRowControls=Panel("Wall Row Controls",placementBar.transform,new Vector2(0,1.06f),new Vector2(1,1.85f),new Color(.14f,.12f,.09f,.97f)).gameObject;
                wallRowShorter=Button("Shorten Wall Row",wallRowControls.transform,"-",new Vector2(.02f,.12f),new Vector2(.14f,.88f),new Color(.45f,.55f,.23f));
                wallRowLonger=Button("Lengthen Wall Row",wallRowControls.transform,"+",new Vector2(.46f,.12f),new Vector2(.58f,.88f),new Color(.45f,.55f,.23f));
                wallRowCount=Label("Wall Row Count",wallRowControls.transform,"",24,Color.white,new Vector2(.15f,.12f),new Vector2(.45f,.88f));
                wallRowRotate=Button("Rotate Wall Row",wallRowControls.transform,"",new Vector2(.61f,.12f),new Vector2(.98f,.88f),new Color(.28f,.55f,.76f));
                wallRowShorter.onClick.AddListener(()=>SetWallRowLength(wallRowLength-1));
                wallRowLonger.onClick.AddListener(()=>SetWallRowLength(wallRowLength+1));
                wallRowRotate.onClick.AddListener(RotateWallRow);
            }
            wallRowControls.SetActive(true);
            wallRowWorld=new GameObject("Wall Row Preview");wallRowWorld.transform.SetParent(world,false);
            preview.transform.SetParent(wallRowWorld.transform,true);wallRowModels.Add(preview);
        }

        public void SetWallRowLength(int length)
        {
            if(!PlacingWallRow)return;
            wallRowLength=Mathf.Clamp(length,1,VillageState.MaximumWallRowLength);
            SetPreviewCell(cellX,cellZ);
        }

        public void RotateWallRow()
        {
            if(!PlacingWallRow)return;
            wallRowDirection=(wallRowDirection+1)%4;SetPreviewCell(cellX,cellZ);
        }

        void RefreshWallRowPreview()
        {
            while(wallRowModels.Count<wallRowLength)
            {
                var model=Instantiate(wallPrefab,wallRowWorld.transform);model.name="Wall Row Ghost";
                foreach(var collider in model.GetComponentsInChildren<Collider>())collider.enabled=false;
                wallRowModels.Add(model);
            }
            int dx=VillageState.WallRowDX(wallRowDirection),dz=VillageState.WallRowDZ(wallRowDirection);
            for(int i=0;i<wallRowModels.Count;i++)
            {
                wallRowModels[i].SetActive(i<wallRowLength);
                wallRowModels[i].transform.position=new Vector3(cellX+.5f+i*dx,0,cellZ+.5f+i*dz);
            }
            foreach(var segment in wallRowWorld.GetComponentsInChildren<WallSegment>())segment.RefreshConnections();
            footprint.transform.position=new Vector3(cellX+.5f+(wallRowLength-1)*dx*.5f,.035f,cellZ+.5f+(wallRowLength-1)*dz*.5f);
            footprint.transform.localScale=new Vector3((dx==0 ? 1 : wallRowLength)/10f,1,(dz==0 ? 1 : wallRowLength)/10f);
            bool valid=State.CanPlaceWallRow(cellX,cellZ,wallRowLength,wallRowDirection,out var reason);
            Color color=valid ? new Color(.30f,.92f,.18f) : new Color(.94f,.13f,.10f);
            previewMaterial.SetColor("_BaseColor",color);previewMaterial.SetColor("_Color",color);
            confirmButton.interactable=valid;placementInfo.text=reason;
            wallRowCount.text=wallRowLength+" WALL"+(wallRowLength==1 ? "" : "S")+" / "+VillageState.MaximumWallRowLength+"\n"+(wallRowLength*BuildingCatalog.Wall.Cost)+" GOLD";
            wallRowRotate.GetComponentInChildren<Text>().text="ROTATE: "+new[]{"EAST","NORTH","WEST","SOUTH"}[wallRowDirection];
            wallRowShorter.interactable=wallRowLength>1;wallRowLonger.interactable=wallRowLength<VillageState.MaximumWallRowLength;
        }

        void ClearWallRowPreview()
        {
            if(wallRowControls!=null)wallRowControls.SetActive(false);
            // The ordinary placement cleanup owns the first preview model.
            if(wallRowWorld!=null)
            {
                if(preview!=null)preview.transform.SetParent(world,true);
                wallRowWorld.SetActive(false);Destroy(wallRowWorld);
            }
            wallRowWorld=null;wallRowModels.Clear();wallRowLength=1;wallRowDirection=0;
        }
    }
}
