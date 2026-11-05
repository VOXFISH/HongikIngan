using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    // Resolution-independent placeholder art; size and colors remain Inspector editable.
    public sealed class BattleOrbGraphic : MaskableGraphic
    {
        public Color borderColor = new Color(.18f,.24f,.34f,1);
        [Min(0)] public float borderWidth = 3;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            Vector2 center = rect.center, outer = rect.size / 2;
            Vector2 inner = new Vector2(Mathf.Max(0,outer.x-borderWidth),Mathf.Max(0,outer.y-borderWidth));
            for (int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96, b=(i+1)*Mathf.PI*2/96;
                Vector2 va=new Vector2(Mathf.Cos(a),Mathf.Sin(a)), vb=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                int n=mesh.currentVertCount;
                mesh.AddVert(center,color,Vector2.zero);
                mesh.AddVert(center+Vector2.Scale(va,inner),color,Vector2.zero);
                mesh.AddVert(center+Vector2.Scale(vb,inner),color,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);
                n=mesh.currentVertCount;
                mesh.AddVert(center+Vector2.Scale(va,outer),borderColor,Vector2.zero);
                mesh.AddVert(center+Vector2.Scale(vb,outer),borderColor,Vector2.zero);
                mesh.AddVert(center+Vector2.Scale(vb,inner),borderColor,Vector2.zero);
                mesh.AddVert(center+Vector2.Scale(va,inner),borderColor,Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
