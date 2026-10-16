using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        // Scale the complete desk/employee sprite together, keeping its floor anchor.
        const float WorkstationVisualScale = .65f;
        readonly Dictionary<string,Texture2D> softAssets=new Dictionary<string,Texture2D>();
        int pixelCameraCompany=-1;
        Texture2D SoftAsset(string name)
        {
            if(!softAssets.TryGetValue(name,out var texture)){texture=Resources.Load<Texture2D>("PixelOffice/"+name);softAssets[name]=texture;}
            return texture;
        }
        void PixelCamera()
        {
            if(pixelCameraCompany==state.active)return;
            pixelCameraCompany=state.active;
            float fit=Mathf.Min(90,1100f/C.Size,560f/(C.Size*.5f+2.5f));
            float tile=Mathf.Min(180,1000f/C.Size,600f/(C.Size*.78125f));
            zoom=tile/fit;
            // Frame the entire usable floor and walls rather than an oversized backdrop.
            var room=RoomLayout.FloorArtworkRect(C.Size,tile,Vector2.zero);
            roomPan=new Vector2(0,140-room.y-330);
        }
        bool AtlasSprite(Texture2D texture,int col,int row,int columns,int rows,Rect rect,float alpha=1,bool mirror=false)
        {
            if(texture==null)return false;
            Rect uv=new Rect(col/(float)columns,1-(row+1)/(float)rows,1f/columns,1f/rows);
            if(mirror){uv.x+=uv.width;uv.width=-uv.width;}
            GUI.color=new Color(1,1,1,alpha);
            GUI.DrawTextureWithTexCoords(rect,texture,uv,true);GUI.color=Color.white;
            Vector2 p=Event.current.mousePosition;if(!rect.Contains(p))return false;
            int px=Mathf.Clamp((int)((uv.x+(p.x-rect.x)/rect.width*uv.width)*texture.width),0,texture.width-1);
            int py=Mathf.Clamp((int)((uv.y+(1-(p.y-rect.y)/rect.height)*uv.height)*texture.height),0,texture.height-1);
            return texture.GetPixel(px,py).a>.35f;
        }
        bool SoftSprite(string name,Vector2 anchor,float tile,float alpha=1)
        {
            string[] bits=name.Split('_');string kind=bits[0];int rotation=bits.Length>1?int.Parse(bits[1]):0;
            bool station=kind=="desktop"||kind=="laptop";
            int frame=bits.Length>2?int.Parse(bits[2]):0;
            if(station)
            {
                int row=(kind=="laptop"?2:0)+(frame>0?1:0);
                float size=tile*2.45f*WorkstationVisualScale;
                var rect=new Rect(anchor.x-size*.5f,anchor.y-size*.88f,size,size);
                // The second sheet holds actual alternate hand/screen artwork.
                return AtlasSprite(SoftAsset(frame==2?"typing":"stations"),rotation,row,4,4,rect,alpha);
            }
            var furniture=FurnitureCatalog.Find(kind);
            if(furniture!=null)return FurnitureSprite(furniture,rotation,anchor,tile,alpha);
            int index=kind=="board"?0:kind=="fridge"?1:kind=="stash"?2:kind=="phones"?3:4;
            float width=tile*(kind=="board"?1.65f:1.25f),height=width*(1024f/3)/(1536f/4);
            return AtlasSprite(SoftAsset("props"),index%4,index/4,4,3,new Rect(anchor.x-width*.5f,anchor.y-height*.87f,width,height),alpha,rotation%2==1);
        }
        bool FurnitureSprite(FurnitureDefinition furniture,int rotation,Vector2 anchor,float tile,float alpha)
        {
            var texture=SoftAsset("furniture");if(texture==null)return false;
            Rect pixels=furniture.pixels;
            // Leave visual breathing room around props; their atlas includes tall
            // silhouettes that otherwise conceal adjacent workstations.
            float width=tile*furniture.width*(furniture.kind=="rug" ? 1f : .78f),height=width*pixels.height/pixels.width;
            Rect rect=new Rect(anchor.x-width*.5f,anchor.y-height+tile*.06f,width,height);
            if(furniture.kind=="rug")rect.y=anchor.y-height*.5f;
            Rect uv=new Rect(pixels.x/texture.width,1-pixels.yMax/texture.height,pixels.width/texture.width,pixels.height/texture.height);
            if(rotation%2==1){uv.x+=uv.width;uv.width=-uv.width;}
            GUI.color=new Color(1,1,1,alpha);GUI.DrawTextureWithTexCoords(rect,texture,uv,true);GUI.color=Color.white;
            Vector2 p=Event.current.mousePosition;if(!rect.Contains(p))return false;
            int px=Mathf.Clamp((int)((uv.x+(p.x-rect.x)/rect.width*uv.width)*texture.width),0,texture.width-1);
            int py=Mathf.Clamp((int)((uv.y+(1-(p.y-rect.y)/rect.height)*uv.height)*texture.height),0,texture.height-1);
            return texture.GetPixel(px,py).a>.35f;
        }
        void DrawSoftRoom(float tile,Vector2 origin)
        {
            var texture=SoftAsset("room_empty");if(texture==null)return;
            Rect room=RoomLayout.FloorArtworkRect(C.Size,tile,origin);
            GUI.color=state.neon?new Color(.86f,.87f,1):Color.white;
            GUI.DrawTexture(room,texture);GUI.color=Color.white;
        }
        bool PaintSoftFurniture(RoomItem item,int x,int y,int rotation,float tile,Vector2 origin,float alpha)
        {
            Vector2Int footprint=RoomLayout.Footprint(item,rotation);
            Vector2 anchor=RoomLayout.Project(x+(footprint.x-1)*.5f,y+(footprint.y-1)*.5f,tile,origin)+new Vector2(0,tile*.25f);
            if(!item.Station)return SoftSprite(item.kind+"_"+rotation,anchor,tile,alpha);
            var worker=Management.At(C,item);
            bool staffed=RoomLayout.HasEmployee(C,item);
            int frame=staffed ? 1+(worker!=null && worker.job>=0 && C.lockdown<=0 ? (int)(animationTime*2.4f)%2 : 0) : 0;
            bool hit=SoftSprite(item.kind+"_"+rotation+"_"+frame,anchor,tile,alpha);
            if(staffed && alpha>.9f)DrawWorkerBubble(item,anchor,tile);
            return hit;
        }
        static Texture2D PixelSurface(Color color)
        {
            const int size=32;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int edge=Mathf.Min(x,y,size-1-x,size-1-y);
                bool cut=(x<3||x>28)&&(y<3||y>28);
                Color c=color;
                if(edge==0)c=new Color(.015f,.025f,.05f,color.a);
                else if(edge==1)c=new Color(.3f,.48f,.67f,color.a);
                else if(edge==2)c=new Color(.13f,.25f,.39f,color.a);
                if(cut)c=Color.clear;
                texture.SetPixel(x,y,c);
            }
            texture.Apply();texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;return texture;
        }
        void PixelIcon(int kind,Rect rect,Color color)
        {
            string[] grids={
                "00111100/01111110/11000011/11111111/11011011/11011011/11111111/00000000",
                "00111000/00111000/00000011/01111111/11111100/11111110/11000110/11000110",
                "11000000/01111111/01111110/00111100/00111100/00000000/00100100/00100100",
                "00111000/00101000/11111011/10101011/11111011/10101011/11111111/11111111",
                "11100111/10100101/11100111/00000000/00000000/11100111/10100101/11100111",
                "00111000/01111100/11010110/11101110/11010110/01111100/00111000/00000000"
            };
            string[] rows=grids[Mathf.Clamp(kind,0,5)].Split('/');
            Color old=GUI.color;GUI.color=color;
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)if(rows[y][x]=='1')
                GUI.DrawTexture(new Rect(rect.x+x*rect.width/8,rect.y+y*rect.height/8,rect.width/8,rect.height/8),Texture2D.whiteTexture);
            GUI.color=old;
        }
    }
}
