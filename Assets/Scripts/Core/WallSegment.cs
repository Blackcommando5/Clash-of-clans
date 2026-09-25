using UnityEngine;

namespace Kingdoms
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class WallSegment : MonoBehaviour
    {
        public GameObject north, east, south, west;
        float nextRefresh;
        public static string LevelName(int level) => level>=3 ? "Fortified stone" : level==2 ? "Dressed stone" : "Stone";

        // Runtime instances only: preserve shared prefab materials and the one-cell footprint.
        public void ApplyLevel(int level)
        {
            level=Mathf.Clamp(level,1,3);
            transform.localScale=new Vector3(1,1+(level-1)*.2f,1);
            var properties=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.transform.parent!=transform)continue;
                properties.Clear();
                if(level>1)
                {
                    bool trim=renderer.name=="Coping" || renderer.name.StartsWith("Stone Course");
                    Color color=level==2 ? (trim ? new Color(.88f,.83f,.66f) : new Color(.69f,.72f,.74f))
                        : (trim ? new Color(.9f,.66f,.22f) : new Color(.32f,.39f,.47f));
                    properties.SetColor("_BaseColor",color);properties.SetColor("_Color",color);
                }
                renderer.SetPropertyBlock(properties);
            }
        }
        void OnEnable(){RefreshConnections();}
        void Update()
        {
            if(Time.realtimeSinceStartup<nextRefresh)return;
            nextRefresh=Time.realtimeSinceStartup+.15f;
            RefreshConnections();
        }
        public void RefreshConnections()
        {
            if(transform.parent==null)return;
            bool n=false,e=false,s=false,w=false;
            foreach(Transform other in transform.parent)
            {
                if(other==transform || !other.gameObject.activeSelf || other.GetComponent<WallSegment>()==null)continue;
                Vector3 d=other.position-transform.position;
                if(Mathf.Abs(d.y)>.1f)continue;
                n|=Mathf.Abs(d.x)<.05f && Mathf.Abs(d.z-1)<.05f;
                s|=Mathf.Abs(d.x)<.05f && Mathf.Abs(d.z+1)<.05f;
                e|=Mathf.Abs(d.z)<.05f && Mathf.Abs(d.x-1)<.05f;
                w|=Mathf.Abs(d.z)<.05f && Mathf.Abs(d.x+1)<.05f;
            }
            Set(north,n);Set(east,e);Set(south,s);Set(west,w);
        }
        static void Set(GameObject part,bool active){if(part!=null && part.activeSelf!=active)part.SetActive(active);}
    }
}
