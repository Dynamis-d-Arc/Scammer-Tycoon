using UnityEngine;

namespace ScammerTown
{
    // Assemble the existing art as world-sized surfaces, instead of stretching a
    // complete room photograph. The cached render target respects IMGUI clipping.
    public static class RoomArchitecture
    {
        public const int ModuleSize=5;
        public static float BakeTileSize(int size) => Mathf.Min(160,2048f/(size+2*RoomLayout.WallBorderTiles));
        static Vector2 UV(float x,float y) => new Vector2(x/1774f,1-y/887f);
        static void Vertex(Vector2 p,Vector2 uv) { GL.TexCoord2(uv.x,uv.y);GL.Vertex3(p.x,p.y,0); }
        static void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Vector2 ua,Vector2 ub,Vector2 uc,Vector2 ud)
        {
            Vertex(a,ua);Vertex(b,ub);Vertex(c,uc);
            Vertex(a,ua);Vertex(c,uc);Vertex(d,ud);
        }
        static void Wall(bool right,float start,float length,float sourceStart,float tile,Vector2 origin)
        {
            Vector2 a=RoomLayout.Project(right?start:0,right?0:start,tile,origin);
            Vector2 b=RoomLayout.Project(right?start+length:0,right?0:start+length,tile,origin);
            var up=new Vector2(0,-RoomLayout.WallHeightTiles*tile);
            Vector2 top0=UV(887,12),bottom0=UV(887,260);
            Vector2 top1=right?UV(1720,284):UV(55,274),bottom1=right?UV(1720,527):UV(55,498);
            float from=sourceStart/ModuleSize,to=(sourceStart+length)/ModuleSize;
            Quad(a+up,b+up,b,a,Vector2.Lerp(top0,top1,from),Vector2.Lerp(top0,top1,to),Vector2.Lerp(bottom0,bottom1,to),Vector2.Lerp(bottom0,bottom1,from));
        }
        public static RenderTexture Bake(int size,Texture2D shell,Texture2D floor,Material material)
        {
            float tile=BakeTileSize(size);
            Rect bounds=RoomLayout.FloorArtworkRect(size,tile,Vector2.zero);
            Vector2 origin=-bounds.position;
            var texture=new RenderTexture(Mathf.CeilToInt(bounds.width),Mathf.CeilToInt(bounds.height),0,RenderTextureFormat.ARGB32)
            {name="Modular office "+size,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.Create();
            var previous=RenderTexture.active;
            RenderTexture.active=texture;GL.PushMatrix();
            try
            {
                GL.LoadPixelMatrix(0,bounds.width,bounds.height,0);GL.Clear(true,true,Color.clear);
                // An opaque wood base prevents subpixel cracks at shared tile
                // edges on small render targets / very large offices.
                material.mainTexture=Texture2D.whiteTexture;material.SetPass(0);GL.Begin(GL.TRIANGLES);
                GL.Color(new Color(.43f,.23f,.12f));
                Quad(origin,RoomLayout.Project(size,0,tile,origin),RoomLayout.Project(size,size,tile,origin),RoomLayout.Project(0,size,tile,origin),Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);
                GL.End();
                material.mainTexture=floor;material.SetPass(0);GL.Begin(GL.TRIANGLES);GL.Color(Color.white);
                for(int y=0;y<size;y+=ModuleSize)for(int x=0;x<size;x+=ModuleSize)
                {
                    Vector2 p=RoomLayout.Project(x,y,tile,origin);
                    float width=ModuleSize*tile,height=width*.5f;
                    // Sample the actual wood diamond, not the transparent canvas
                    // around it. Shared geometric edges prevent expansion seams.
                    Quad(p,p+new Vector2(width*.5f,height*.5f),p+new Vector2(0,height),p+new Vector2(-width*.5f,height*.5f),UV(879,24),UV(1726,427),UV(887,838),UV(48,425));
                }
                GL.End();
                material.mainTexture=shell;material.SetPass(0);GL.Begin(GL.TRIANGLES);GL.Color(Color.white);
                // Interior left-wall modules omit the entrance; the outermost
                // five-tile module contains the single door at a constant scale.
                float leftInterior=size-ModuleSize;
                for(float start=0;start<leftInterior;start+=3.5f)
                    Wall(false,start,Mathf.Min(3.5f,leftInterior-start),0,tile,origin);
                Wall(false,leftInterior,ModuleSize,0,tile,origin);
                for(int start=0;start<size;start+=ModuleSize)Wall(true,start,ModuleSize,0,tile,origin);
                var down=new Vector2(0,RoomLayout.FoundationDepthTiles*tile);
                for(int start=0;start<size;start+=ModuleSize)
                {
                    Vector2 a=RoomLayout.Project(start,size,tile,origin),b=RoomLayout.Project(start+ModuleSize,size,tile,origin);
                    Quad(a,b,b+down,a+down,UV(30,503),UV(866,842),UV(866,883),UV(30,562));
                    a=RoomLayout.Project(size,start,tile,origin);b=RoomLayout.Project(size,start+ModuleSize,tile,origin);
                    Quad(a,b,b+down,a+down,UV(1745,518),UV(886,834),UV(886,884),UV(1745,568));
                }
                GL.End();
            }
            finally { GL.PopMatrix();RenderTexture.active=previous; }
            return texture;
        }
    }
}
