using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    [Serializable]
    public class RoomItem
    {
        public string id, kind;
        public int x, y, rotation;
        public bool placed;
        public bool manuallyStored;
        public bool Station => kind == "desktop" || kind == "laptop";
        public bool GroundCover => kind == "rug";
        public string Name => FurnitureCatalog.Find(kind)!=null ? FurnitureCatalog.Find(kind).name : Station ? (kind == "desktop" ? "Desktop workstation" : "Laptop workstation") :
            kind == "board" ? "Crew board" : kind == "fridge" ? "Water cooler" : kind == "stash" ? "Stash" : "Burner phones";
    }

    public static class RoomLayout
    {
        public static bool HasEmployee(Company company,RoomItem item)
        {
            if(company.layout==null || item==null || !item.Station || !item.placed) return false;
            if(company.workers!=null) return Management.At(company,item)!=null;
            int staffed=0;
            foreach(var station in company.layout)
            {
                if(!station.Station || !station.placed) continue;
                if(station==item) return staffed<company.employeeCount;
                staffed++;
            }
            return false;
        }
        public static Vector2Int Footprint(RoomItem item, int rotation)
        {
            var furniture=FurnitureCatalog.Find(item.kind);
            var size = item.Station ? new Vector2Int(2, 2) : furniture!=null ? furniture.footprint : item.kind == "board" ? new Vector2Int(2, 1) : Vector2Int.one;
            return rotation % 2 == 0 ? size : new Vector2Int(size.y, size.x);
        }
        public static bool CanPlace(Company company, RoomItem item, int x, int y, int rotation)
        {
            if (rotation < 0 || rotation > 3) return false;
            var size = Footprint(item, rotation);
            if (x < 0 || y < 0 || x + size.x > company.Size || y + size.y > company.Size) return false;
            if (company.layout == null) return true;
            foreach (var other in company.layout)
            {
                if (other == null || other == item || !other.placed || other.GroundCover!=item.GroundCover) continue;
                var otherSize = Footprint(other, other.rotation);
                if (x < other.x + otherSize.x && x + size.x > other.x && y < other.y + otherSize.y && y + size.y > other.y) return false;
            }
            return true;
        }
        public static bool Move(Company company, RoomItem item, int x, int y, int rotation)
        {
            if (!CanPlace(company, item, x, y, rotation)) return false;
            item.x = x; item.y = y; item.rotation = rotation; item.placed = true; item.manuallyStored = false;
            return true;
        }
        public static bool HasStationSpace(Company company)
        {
            if(company.Stations>=company.Capacity)return false;
            var probe=new RoomItem {kind="desktop"};
            for(int y=0;y<company.Size-1;y++)for(int x=0;x<company.Size-1;x++)
                if(CanPlace(company,probe,x,y,0))return true;
            return false;
        }
        public static bool Expand(Company company)
        {
            if(company.level>=Rules.MaxOfficeLevel)return false;
            company.level++;
            Ensure(company);
            foreach(var item in company.layout)if(!item.placed && !item.manuallyStored)AutoPlace(company,item);
            return true;
        }
        public static bool AutoPlace(Company company, RoomItem item)
        {
            for (int y = 0; y < company.Size; y++)
                for (int x = 0; x < company.Size; x++)
                    if (Move(company, item, x, y, item.rotation)) return true;
            return false;
        }
        // Stable equipment IDs keep moved furniture in place when purchases or raids change counts.
        // Old saves have no layout: migrate them once without changing money or upgrades.
        public static void Ensure(Company company)
        {
            if (company.layout == null) company.layout = new List<RoomItem>();
            var desired = new Dictionary<string, string> { {"board","board"} };
            if (company.burners > 0) desired.Add("phones", "phones");
            for (int i = 0; i < company.desktops; i++) desired.Add("desktop_" + i, "desktop");
            for (int i = 0; i < company.laptops; i++) desired.Add("laptop_" + i, "laptop");
            FurnitureCatalog.AddOwned(desired,company);
            var seen = new HashSet<string>();
            company.layout.RemoveAll(item => item == null || item.id == null || !desired.ContainsKey(item.id) || !seen.Add(item.id));
            foreach (var item in company.layout)
            {
                item.kind = desired[item.id];
                item.rotation = ((item.rotation % 4) + 4) % 4;
                if (item.placed && !CanPlace(company,item,item.x,item.y,item.rotation)) item.placed = false;
            }
            foreach (var pair in desired)
            {
                if (seen.Contains(pair.Key)) continue;
                var item = new RoomItem { id = pair.Key, kind = pair.Value };
                company.layout.Add(item);
                int x = pair.Key == "fridge" || pair.Key == "phones" ? company.Size - 1 : pair.Key == "desktop_0" ? 1 : 0;
                int y = pair.Key == "stash" || pair.Key == "phones" ? company.Size - 1 : pair.Key == "desktop_0" ? 1 : 0;
                if(FurnitureCatalog.IsFurnishing(item))
                {
                    var preferred=FurnitureCatalog.InitialPosition(item,company);
                    x=preferred.x;y=preferred.y;
                    if(item.id=="shelf_0")item.rotation=1;
                }
                if (!Move(company,item,x,y,item.rotation)) AutoPlace(company,item);
            }
            // The full decorative collection overwhelms a five-tile starter office.
            // Keep workstations exactly where the player placed them, and retain all
            // excess furnishings in storage rather than deleting inventory.
            if(company.furnishingLayoutVersion<1)
            {
                company.furnishingLayoutVersion=1;
                if(company.Size==5)
                {
                    foreach(var item in company.layout)
                    {
                        if(item.Station || item.kind=="rug" || item.kind=="fridge" || item.id=="plant_0")continue;
                        item.placed=false;
                        item.manuallyStored=true;
                    }
                }
            }
        }
        public static Vector2 Project(float x,float y,float tile,Vector2 origin) => origin + new Vector2((x-y)*tile*.5f,(x+y)*tile*.25f);
        // Calibrated to the wood surface, excluding the walls and foundation.
        // The same tile pitch drives artwork, placement overlays and pointer picking.
        public static Rect FloorArtworkRect(int size,float tile,Vector2 origin)
        {
            float width=size*tile/.88f;
            float height=size*tile*.5f/.64f;
            return new Rect(origin.x-width*.5f,origin.y-height*.295f,width,height);
        }
        public static Vector2 Unproject(Vector2 point,float tile,Vector2 origin)
        {
            Vector2 p=point-origin;
            return new Vector2(p.x/tile + 2*p.y/tile, 2*p.y/tile - p.x/tile);
        }
    }
}
