using UnityEngine;

namespace Kingdoms
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class WallSegment : MonoBehaviour
    {
        public GameObject north, east, south, west;
        float nextRefresh;
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
