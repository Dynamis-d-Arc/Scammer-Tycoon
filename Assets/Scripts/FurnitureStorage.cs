using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        bool furnitureStorageOpen;
        Vector2 furnitureStorageScroll;
        void StoreFurniture(RoomItem item)
        {
            if(item==null || !C.layout.Contains(item))return;
            CancelPlacement();item.placed=false;item.manuallyStored=true;selectedItemId=item.id;
            Management.Ensure(state);Save();Notify("Stored "+item.Name+". Find it in Storage when you want it back.");
        }
        void OpenFurnitureStorage()
        {
            CancelPlacement();SynchronizeRoom();
            shopModal=workerModal=managementPanel=cityView=false;
            furnitureStorageScroll=Vector2.zero;furnitureStorageOpen=true;
        }
        void BeginStoredFurniturePlacement(RoomItem item)
        {
            if(item==null || item.placed || !C.layout.Contains(item))return;
            CancelPlacement();furnitureStorageOpen=false;
            heldItem=item;selectedItemId=item.id;heldRotation=item.rotation;
            placingFurniture=true;CenterPlacementGrip();GUIUtility.keyboardControl=0;
            Notify("Click a clear floor area to place "+item.Name+". R rotates; Esc cancels.");
        }
        void DrawFurnitureStorage()
        {
            Panel(new Rect(0,0,1440,900),new Color(.015f,.025f,.05f,.82f));
            Panel(new Rect(340,125,760,635),new Color(.065f,.095f,.16f));
            Label(372,149,530,40,"FURNITURE STORAGE",heading);
            Label(372,194,650,40,"Choose an item, then click where you want it in this office.",small);
            if(Button(966,150,102,"Close",true,35))furnitureStorageOpen=false;
            var stored=C.layout.FindAll(item=>!item.placed);
            if(stored.Count==0){Label(372,266,650,80,"Storage is empty. Select any furnishing in the room and click Store.",body);return;}
            GUILayout.BeginArea(new Rect(372,244,696,480));
            furnitureStorageScroll=GUILayout.BeginScrollView(furnitureStorageScroll);
            foreach(var item in stored)
            {
                Rect row=GUILayoutUtility.GetRect(650,105);
                Panel(new Rect(row.x,row.y,row.width,97),new Color(.1f,.14f,.22f));
                GUI.BeginGroup(new Rect(row.x+8,row.y+4,120,88));
                SoftSprite(item.kind+"_"+item.rotation+"_0",new Vector2(60,80),item.kind=="shelf"?30:38);
                GUI.EndGroup();
                GUI.Label(new Rect(row.x+145,row.y+18,300,28),item.Name,body);
                var size=RoomLayout.Footprint(item,item.rotation);
                GUI.Label(new Rect(row.x+145,row.y+49,300,28),size.x+" × "+size.y+" tiles"+(item.GroundCover ? " · fits beneath furniture" : ""),small);
                if(GUI.Button(new Rect(row.xMax-134,row.y+29,118,38),"Place",button))BeginStoredFurniturePlacement(item);
            }
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
    }
}
