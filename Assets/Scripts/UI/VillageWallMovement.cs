using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        Button moveConnectedWalls;
        readonly List<int> movingWalls=new List<int>();
        readonly List<WallSegment> movingWallModels=new List<WallSegment>();
        readonly List<Transform> movingWallFootprints=new List<Transform>();
        readonly List<int> movingWallLevels=new List<int>();
        bool MovingConnectedWalls=>movingWalls.Count>0;

        void RefreshWallMoveAction()
        {
            bool show=DetailsOpen && !viewingBuilderQueue && !confirmingUpgradeCancellation && selectedIndex>=0 && State.buildings[selectedIndex].kind=="Wall";
            if(show && moveConnectedWalls==null)
            {
                moveConnectedWalls=Button("Move Connected Walls",detailPanel,"",new Vector2(.05f,.045f),new Vector2(.42f,.165f),new Color(.28f,.55f,.76f));
                moveConnectedWalls.GetComponentInChildren<Text>().resizeTextMaxSize=26;
                moveConnectedWalls.onClick.AddListener(BeginConnectedWallMove);
            }
            if(moveConnectedWalls==null)return;
            moveConnectedWalls.gameObject.SetActive(show);
            if(show)moveConnectedWalls.GetComponentInChildren<Text>().text="MOVE CONNECTED\n"+State.ConnectedWalls(selectedIndex).Count+" WALLS";
        }

        public void BeginConnectedWallMove()
        {
            if(State==null || !PlayerProfile.HasPlayerName || IsPlacing || PracticeOpen || ProfileOpen || shop.activeSelf || !DetailsOpen || viewingBuilderQueue || confirmingUpgradeCancellation)return;
            var connected=State.ConnectedWalls(selectedIndex);if(connected.Count==0 || wallPrefab==null)return;
            int anchor=selectedIndex;CloseBuildingDetails();movingIndex=anchor;placingKind="Wall";
            movingWalls.AddRange(connected);
            preview=new GameObject("Connected Wall Preview");preview.transform.SetParent(world,false);
            footprint=new GameObject("Connected Wall Footprints");footprint.transform.SetParent(world,false);
            previewMaterial=new Material(footprintMaterial);
            foreach(int index in movingWalls)
            {
                var model=Instantiate(wallPrefab,preview.transform);model.name="Moving Wall "+index;
                foreach(var collider in model.GetComponentsInChildren<Collider>())collider.enabled=false;
                movingWallModels.Add(model.GetComponent<WallSegment>());movingWallLevels.Add(0);
                var cell=GameObject.CreatePrimitive(PrimitiveType.Plane);cell.name="Wall Cell "+index;cell.transform.SetParent(footprint.transform,false);
                cell.transform.localScale=new Vector3(.1f,1,.1f);cell.GetComponent<Collider>().enabled=false;Destroy(cell.GetComponent<Collider>());
                var renderer=cell.GetComponent<Renderer>();renderer.sharedMaterial=previewMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                movingWallFootprints.Add(cell.transform);buildingInstances[index].SetActive(false);
            }
            var wall=State.buildings[anchor];placementBar.SetActive(true);cameraController.InputBlocked=true;dragging=false;
            DeselectBuilding();SetPreviewCell(wall.x,wall.z);RefreshHUD();
        }

        void RefreshConnectedWallPreview()
        {
            var anchor=State.buildings[movingIndex];
            float dx=(float)cellX-anchor.x,dz=(float)cellZ-anchor.z;
            for(int i=0;i<movingWalls.Count;i++)
            {
                var wall=State.buildings[movingWalls[i]];
                var position=new Vector3(wall.x+dx+.5f,0,wall.z+dz+.5f);
                movingWallModels[i].transform.position=position;
                if(movingWallLevels[i]!=wall.level){movingWallModels[i].ApplyLevel(wall.level);movingWallLevels[i]=wall.level;}
                movingWallFootprints[i].position=position+Vector3.up*.035f;
            }
            foreach(var wall in movingWallModels)wall.RefreshConnections();
            bool valid=State.CanMoveConnectedWalls(movingIndex,cellX,cellZ,out var reason);
            var color=valid ? new Color(.30f,.92f,.18f) : new Color(.94f,.13f,.10f);
            previewMaterial.SetColor("_BaseColor",color);previewMaterial.SetColor("_Color",color);
            confirmButton.interactable=valid;placementInfo.text=reason;
        }

        void ApplyConnectedWallPositions()
        {
            foreach(int index in movingWalls)
            {
                var wall=State.buildings[index];var instance=buildingInstances[index];
                instance.transform.position=new Vector3(wall.x+.5f,0,wall.z+.5f);
                instance.name="Wall ("+wall.x+", "+wall.z+")";
            }
        }

        void ClearConnectedWallPreview()
        {
            foreach(int index in movingWalls)
                if(index<buildingInstances.Count && buildingInstances[index]!=null)buildingInstances[index].SetActive(true);
            movingWalls.Clear();movingWallModels.Clear();movingWallFootprints.Clear();movingWallLevels.Clear();
        }
    }
}
