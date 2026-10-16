using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    public static class EquipmentSprites
    {
        static readonly Dictionary<Texture2D,Rect[]> rows=new Dictionary<Texture2D,Rect[]>();
        // Generated sheets have transparent gutters of varying height. Read those
        // gutters once so row slicing never clips monitors, heads or chair feet.
        public static Rect[] Rows(Texture2D texture)
        {
            if(rows.TryGetValue(texture,out var cached))return cached;
            var pixels=texture.GetPixels32();var bands=new List<Vector2Int>();int start=-1;
            for(int y=0;y<=texture.height;y++)
            {
                bool occupied=false;
                if(y<texture.height)for(int x=0;x<texture.width;x++)
                    if(pixels[(texture.height-1-y)*texture.width+x].a>32){occupied=true;break;}
                if(occupied && start<0)start=y;
                if(!occupied && start>=0){bands.Add(new Vector2Int(start,y));start=-1;}
            }
            var result=new Rect[bands.Count];
            for(int i=0;i<bands.Count;i++)
                result[i]=new Rect(0,1-bands[i].y/(float)texture.height,.25f,(bands[i].y-bands[i].x)/(float)texture.height);
            rows[texture]=result;return result;
        }
        public static Rect UV(Texture2D texture,int column,int row)
        {
            var bands=Rows(texture);
            Rect uv=bands.Length==6?bands[row]:new Rect(0,1-(row+1)/6f,.25f,1/6f);
            uv.x=column*.25f;return uv;
        }
        public static Rect DrawRect(Texture2D texture,Rect uv,Rect cell)
        {
            float height=cell.width*(uv.height*texture.height)/(texture.width*.25f);
            return new Rect(cell.x,cell.y+cell.height*.95f-height,cell.width,height);
        }
    }
}
