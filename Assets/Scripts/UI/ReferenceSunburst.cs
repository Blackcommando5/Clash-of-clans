using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ReferenceSunburst : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var c=r.center;
            for(int i=0;i<18;i++)
            {
                float a=i*Mathf.PI*2/18;int n=vh.currentVertCount;
                vh.AddVert(c,new Color(1,1,1,.42f),Vector2.zero);
                vh.AddVert(c+new Vector2(Mathf.Cos(a)*r.width*.48f,Mathf.Sin(a)*r.height*.48f),new Color(1,1,1,0),Vector2.zero);
                vh.AddVert(c+new Vector2(Mathf.Cos(a+.16f)*r.width*.48f,Mathf.Sin(a+.16f)*r.height*.48f),new Color(1,1,1,0),Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);
            }
        }
    }
}
