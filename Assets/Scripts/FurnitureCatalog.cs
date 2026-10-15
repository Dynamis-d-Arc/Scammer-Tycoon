using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    public sealed class FurnitureDefinition
    {
        public readonly string kind, name;
        public readonly Vector2Int footprint;
        public readonly Rect pixels;
        public readonly float width;
        public FurnitureDefinition(string kind,string name,int x,int y,Rect pixels,float width)
        {this.kind=kind;this.name=name;footprint=new Vector2Int(x,y);this.pixels=pixels;this.width=width;}
    }
    public static class FurnitureCatalog
    {
        // Explicit sprite rectangles: the source atlas has variable-height rows.
        public static readonly FurnitureDefinition[] All={
            new FurnitureDefinition("sofa","Sofa",3,2,new Rect(18,199,322,269),2.45f),
            new FurnitureDefinition("shelf","Bookshelf",2,1,new Rect(378,70,234,397),1.4f),
            new FurnitureDefinition("lamp","Floor lamp",1,1,new Rect(752,112,128,342),.65f),
            new FurnitureDefinition("plant","Potted plant",1,1,new Rect(977,131,252,323),1.05f),
            new FurnitureDefinition("coffee","Coffee table",2,1,new Rect(27,599,288,215),1.55f),
            new FurnitureDefinition("sideboard","Sideboard",2,1,new Rect(336,561,295,269),1.8f),
            new FurnitureDefinition("rug","Rug",3,2,new Rect(626,621,382,192),2.5f),
            new FurnitureDefinition("boxes","Moving boxes",1,1,new Rect(1018,596,188,224),.9f),
            new FurnitureDefinition("stash","Filing cabinet",1,1,new Rect(60,913,203,290),1f),
            new FurnitureDefinition("fridge","Water cooler",1,1,new Rect(400,856,211,353),1f),
            new FurnitureDefinition("bin","Waste bin",1,1,new Rect(730,1023,135,178),.6f),
            new FurnitureDefinition("radiator","Radiator",1,1,new Rect(988,949,202,249),1.05f)
        };
        public static FurnitureDefinition Find(string kind)
        {foreach(var entry in All)if(entry.kind==kind)return entry;return null;}
        public static bool IsFurnishing(RoomItem item) => item!=null && Find(item.kind)!=null && item.kind!="stash" && item.kind!="fridge";
        public static void AddOwned(Dictionary<string,string> desired,Company company)
        {
            desired.Add("sofa_0","sofa");
            desired.Add("shelf_0","shelf");desired.Add("shelf_1","shelf");
            desired.Add("lamp_0","lamp");
            desired.Add("plant_0","plant");desired.Add("plant_1","plant");desired.Add("plant_2","plant");
            desired.Add("coffee_0","coffee");desired.Add("sideboard_0","sideboard");
            desired.Add("rug_0","rug");desired.Add("boxes_0","boxes");
            desired.Add("bin_0","bin");desired.Add("radiator_0","radiator");
            for(int i=0;i<company.decorUpgrade;i++)desired.Add("decorPlant_"+i,"plant");
        }
        public static Vector2Int InitialPosition(RoomItem item,Company company)
        {
            int edge=company.Size-1;
            switch(item.kind)
            {
                case "sofa":return new Vector2Int(0,Mathf.Min(5,edge));
                case "shelf":return item.id=="shelf_0" ? new Vector2Int(0,3) : new Vector2Int(Mathf.Min(5,edge),0);
                case "lamp":return new Vector2Int(0,Mathf.Min(4,edge));
                case "plant":return item.id=="plant_0" ? new Vector2Int(0,edge) : item.id=="plant_1" ? new Vector2Int(edge,0) : new Vector2Int(edge,edge);
                case "coffee":return new Vector2Int(3,Mathf.Min(5,edge));
                case "sideboard":return new Vector2Int(2,0);
                case "rug":return new Vector2Int(0,Mathf.Min(4,company.Size-2));
                case "boxes":return new Vector2Int(Mathf.Max(0,edge-1),1);
                case "bin":return new Vector2Int(3,3);
                case "radiator":return new Vector2Int(Mathf.Max(0,edge-1),0);
                default:return Vector2Int.zero;
            }
        }
        public static int CompareDepth(RoomItem a,RoomItem b)
        {
            if(a.GroundCover!=b.GroundCover)return a.GroundCover ? -1 : 1;
            var sa=RoomLayout.Footprint(a,a.rotation);var sb=RoomLayout.Footprint(b,b.rotation);
            int depth=(a.x+a.y+(sa.x+sa.y)*.5f).CompareTo(b.x+b.y+(sb.x+sb.y)*.5f);
            return depth!=0 ? depth : string.CompareOrdinal(a.id,b.id);
        }
    }
}
