using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        Texture2D[] keyboardArt, employeeArt, typingArmArt;
        bool arrangingRoom;
        string selectedItemId;
        RoomItem heldItem;
        Company viewedCompany, purchaseCompany;
        int purchasePrice;
        int layoutDesktops = -1, layoutLaptops = -1, layoutBurners = -1;
        int heldRotation, roomControl;
        Vector2 roomPan, grabOffset, pressPosition;
        Vector2Int candidate;
        bool draggingFurniture, placingFurniture, panningRoom, cancelForFocusLoss;
        readonly List<RoomItem> roomOrder = new List<RoomItem>();
        readonly Rect roomViewport = new Rect(0,0,1440,900);

        void CancelPlacement()
        {
            heldItem = null;
            purchaseCompany = null; purchasePrice = 0;
            draggingFurniture = placingFurniture = panningRoom = false;
            if (roomControl != 0 && GUIUtility.hotControl == roomControl) GUIUtility.hotControl = 0;
        }
        void SynchronizeRoom()
        {
            if (viewedCompany != C)
            {
                CancelPlacement(); furnitureStorageOpen=false; selectedItemId = null; roomPan = Vector2.zero;
                viewedCompany = C; layoutDesktops = -1;
            }
            if (layoutDesktops != C.desktops || layoutLaptops != C.laptops || layoutBurners != C.burners)
            {
                RoomLayout.Ensure(C);
                layoutDesktops = C.desktops; layoutLaptops = C.laptops; layoutBurners = C.burners;
                if (heldItem != null && purchaseCompany == null && !C.layout.Contains(heldItem)) CancelPlacement();
            }
        }
        void DrawRoom()
        {
            if(cancelForFocusLoss) { CancelPlacement(); cancelForFocusLoss=false; }
            SynchronizeRoom();
            if (!GUI.enabled) CancelPlacement();
            Management.Ensure(state);
            PixelCamera();
            RoomItem selected = C.layout.Find(item => item.id == selectedItemId);
            Panel(new Rect(0,70,1440,758),new Color(.035f,.05f,.09f));
            var e = Event.current;
            roomControl = GUIUtility.GetControlID("ScammerTownRoom".GetHashCode(),FocusType.Passive);
            if (GUI.enabled && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape) { CancelPlacement(); e.Use(); }
                else if (e.keyCode == KeyCode.R && (heldItem != null || selected != null))
                { RotateFurniture(selected); e.Use(); }
            }
            GUI.BeginGroup(roomViewport);
            int n = C.Size;
            float tile = Mathf.Min(90,1100f/n,560f/(n*.5f+2.5f)) * zoom;
            Vector2 origin = new Vector2(720, 330) + roomPan;
            bool inside = new Rect(0,126,roomViewport.width,610).Contains(e.mousePosition);
            Vector2 pointerTile = RoomLayout.Unproject(e.mousePosition-new Vector2(0,tile*.25f),tile,origin);
            UpdatePlacementCandidate(pointerTile);
            DrawSoftRoom(tile,origin);
            if (arrangingRoom || placingFurniture || draggingFurniture) DrawPlacementGrid(tile,origin);
            if (selected != null && selected.placed && heldItem == null)
                DrawFootprint(selected,selected.x,selected.y,selected.rotation,tile,origin,new Color(.45f,.65f,.28f));
            roomOrder.Clear();
            foreach (var item in C.layout) if (item.placed) roomOrder.Add(item);
            roomOrder.Sort(FurnitureCatalog.CompareDepth);
            RoomItem hover = null;
            foreach (var item in roomOrder)
            {
                bool moving = item == heldItem && draggingFurniture;
                bool hit = PaintFurniture(item,item.x,item.y,item.rotation,tile,origin,moving ? .25f : 1f);
                if (inside && hit && !moving) hover=item;
            }
            if (heldItem != null && (draggingFurniture || placingFurniture))
            {
                bool valid = inside && RoomLayout.CanPlace(C,heldItem,candidate.x,candidate.y,heldRotation);
                DrawFootprint(heldItem,candidate.x,candidate.y,heldRotation,tile,origin,valid ? new Color(.48f,.82f,.32f) : new Color(.9f,.24f,.23f));
                PaintFurniture(heldItem,candidate.x,candidate.y,heldRotation,tile,origin,.75f);
                Label(24,118,780,25,valid ? (placingFurniture ? "Click to place" : "Release to place") + "  ·  R rotates  ·  Esc cancels" : "Blocked: choose an empty space inside the floor",body);
            }
            else if (hover != null) Label(24,118,780,25,hover.Name + " — drag to move · click an employee to manage",body);

            if (GUI.enabled) HandleRoomPointer(e,e.type,inside,hover,pointerTile);
            GUI.color=Color.white;
            GUI.EndGroup();
            Label(24,88,650,24,placingFurniture ? heldItem.Name + (purchaseCompany != null ? " · " + Money(purchasePrice) + " on placement" : " · from storage") : arrangingRoom ? "Drag furniture · R rotates · Esc finishes" : "Drag furniture to move · Click to select · R rotates",small);
            if(Button(1040,86,160,"Focus team",true,30)){pixelCameraCompany=-1;PixelCamera();}
            zoom=GUI.HorizontalSlider(new Rect(1215,97,190,20),zoom,.5f,Mathf.Max(3f,C.Size/5f));

            int stored=C.layout.FindAll(item=>!item.placed).Count;
            selected=C.layout.Find(item=>item.id==selectedItemId);
            Label(24,742,1050,25,selected == null ? "" :
                selected.Name + "  •  " + (selected.placed ? "Tile " + (selected.x+1) + ", " + (selected.y+1) : "In storage") + "  •  " + selected.rotation*90 + "°",small);
            if(!arrangingRoom)
            {
                Label(24,785,440,24,"Scroll to zoom · Drag empty floor to pan",small);
                if(selected!=null)
                {
                    if(Button(490,775,120,"Rotate (R)",heldItem==null,34))RotateFurniture(selected);
                    if(Button(620,775,120,"Store",selected.placed && heldItem==null,34))StoreFurniture(selected);
                }
                if(Button(760,775,210,"Storage ("+stored+")",true,34))OpenFurnitureStorage();
                if(Button(1050,775,170,"Expand floor",true,34))Navigate("Properties");
                if(Button(1232,775,184,"Fit office",true,34)){zoom=1;roomPan=new Vector2(0,30-C.Size*Mathf.Min(90,1100f/C.Size,560f/(C.Size*.5f+2.5f))*.25f);}
            }
            else
            {
                if(Button(250,775,180,"Rotate (R)",selected!=null,34))RotateFurniture(selected);
                if(Button(442,775,180,"Store selected",selected!=null && selected.placed && heldItem==null,34))
                StoreFurniture(selected);
                if(Button(634,775,230,"Storage ("+stored+")",true,34))OpenFurnitureStorage();
                if(Button(876,775,160,"Fit office",heldItem==null || placingFurniture,34)){zoom=1;roomPan=new Vector2(0,30-C.Size*Mathf.Min(90,1100f/C.Size,560f/(C.Size*.5f+2.5f))*.25f);}
                if(Button(1048,775,180,"Done arranging",true,34)){arrangingRoom=false;CancelPlacement();}
            }
        }

        // A shop preview is transient: it is neither owned nor charged until a valid click.
        void BeginEquipmentPlacement(string kind)
        {
            if (kind != "desktop" && kind != "laptop") return;
            int price = kind == "desktop" ? 800 : 650;
            if (!CanAct) { Notify("Resume the game to buy equipment."); return; }
            Management.Ensure(state);
            if (state.cash < price) { Notify("Need " + Money(price) + " for this workstation."); return; }
            if (!RoomLayout.HasStationSpace(C)) { Notify("Clear a 2 × 2 space or expand your floor first."); return; }
            CancelPlacement(); SynchronizeRoom();
            shopModal=workerModal=managementPanel=cityView=arrangingRoom=furnitureStorageOpen=false;
            selectedItemId=null;
            heldItem=new RoomItem { id="purchase-preview", kind=kind, placed=false };
            purchaseCompany=C; purchasePrice=price; heldRotation=0; placingFurniture=true;
            CenterPlacementGrip(); GUIUtility.keyboardControl=0;
            Notify("Choose a spot, then click to buy and place. Esc or right-click cancels for free.");
        }
        void CenterPlacementGrip()
        {
            var size=RoomLayout.Footprint(heldItem,heldRotation);
            grabOffset=new Vector2((size.x-1)*.5f,(size.y-1)*.5f);
        }
        void UpdatePlacementCandidate(Vector2 pointerTile)
        {
            if (heldItem != null && (draggingFurniture || placingFurniture))
                candidate=new Vector2Int(Mathf.RoundToInt(pointerTile.x-grabOffset.x),Mathf.RoundToInt(pointerTile.y-grabOffset.y));
        }
        void HandleRoomPointer(Event e,EventType inputType,bool inside,RoomItem hover,Vector2 pointerTile)
        {
            UpdatePlacementCandidate(pointerTile);
            if (inside && inputType==EventType.ScrollWheel && (heldItem==null || placingFurniture))
            {
                float next=Mathf.Clamp(zoom-e.delta.y*.06f,.5f,Mathf.Max(3f,C.Size/5f));
                float ratio=next/zoom;
                roomPan=roomPan*ratio+(e.mousePosition-new Vector2(720,360))*(1-ratio);
                zoom=next;e.Use();
            }
            if (inputType==EventType.MouseDown && e.button==1 && heldItem!=null)
            { CancelPlacement();e.Use(); }
            if (inside && inputType==EventType.MouseDown && e.button==2 && (heldItem==null || placingFurniture))
            { panningRoom=true;GUIUtility.hotControl=roomControl;e.Use(); }
            if (panningRoom && inputType==EventType.MouseDrag)
            { roomPan+=e.delta;e.Use(); }
            if (inside && inputType==EventType.MouseDown && e.button==0)
            {
                if (placingFurniture && heldItem!=null) { CommitPlacement(inside);e.Use(); }
                else if (hover!=null)
                {
                    heldItem=hover;selectedItemId=hover.id;heldRotation=hover.rotation;
                    candidate=new Vector2Int(hover.x,hover.y);
                    grabOffset=pointerTile-new Vector2(hover.x,hover.y);pressPosition=e.mousePosition;
                    GUIUtility.hotControl=roomControl;GUIUtility.keyboardControl=0;
                    selection=hover.Name;
                    tab=hover.kind=="board" ? "Employees" : hover.kind=="stash" ? "Properties" : "Operations";
                    e.Use();
                }
                else {selectedItemId=null;panningRoom=true;GUIUtility.hotControl=roomControl;}
            }
            // A short click still opens the employee; dragging works without entering Arrange.
            if (heldItem!=null && !placingFurniture && inputType==EventType.MouseDrag && e.button==0)
            {
                if ((e.mousePosition-pressPosition).sqrMagnitude>16) draggingFurniture=true;
                UpdatePlacementCandidate(pointerTile);e.Use();
            }
            // rawType catches releases outside the room, so a drag cannot get stuck.
            if (e.rawType==EventType.MouseUp && e.button==0 && heldItem!=null && !placingFurniture)
            {
                if(draggingFurniture) CommitPlacement(inside);
                else {var worker=Management.At(C,heldItem);if(worker!=null)OpenWorkerProfile(C.workers.IndexOf(worker));CancelPlacement();}
                e.Use();
            }
            if (e.rawType==EventType.MouseUp && (e.button==2 || e.button==0) && panningRoom)
            {panningRoom=false;GUIUtility.hotControl=0;e.Use();}
        }
        void CommitPlacement(bool inside)
        {
            var item=heldItem;
            if (item==null) return;
            if (purchaseCompany!=null && purchaseCompany!=C) {CancelPlacement();return;}
            if (!inside || !RoomLayout.CanPlace(C,item,candidate.x,candidate.y,heldRotation))
            {
                if (placingFurniture) Notify("That space is blocked. Choose a clear floor area, or Esc to cancel.");
                else {Notify("Can't place there. Returned to the original position.");CancelPlacement();}
                return;
            }
            if (purchaseCompany!=null)
            {
                if (!CanAct) {Notify("Resume the game to finish placing this purchase.");return;}
                if (C.Stations>=C.Capacity) {Notify("Expand your floor before adding another workstation.");return;}
                if (!Rules.Pay(state,purchasePrice)) {Notify("Need " + Money(purchasePrice) + " to place this workstation.");return;}
                item.id=item.kind+"_"+(item.kind=="desktop" ? C.desktops++ : C.laptops++);
                C.layout.Add(item);
                Log("Bought " + item.Name + " · −" + Money(purchasePrice));
                PlayFeedback(false);
            }
            RoomLayout.Move(C,item,candidate.x,candidate.y,heldRotation);
            selectedItemId=item.id;
            Management.Ensure(state);
            Log("Placed " + item.Name + " at " + (item.x+1) + ", " + (item.y+1) + ".");
            Save();CancelPlacement();Notify("Placed " + item.Name + ". Drag it any time to move it.");
        }
        void DrawPlacementGrid(float tile,Vector2 origin)
        {
            for(int y=0;y<C.Size;y++)for(int x=0;x<C.Size;x++)
            {
                var p=RoomLayout.Project(x,y,tile,origin);
                if(p.x+tile*.5f<0 || p.x-tile*.5f>1440 || p.y+tile*.5f<126 || p.y>736)continue;
                FloorCell(x,y,tile,origin,new Color(.3f,.7f,.8f,(x+y)%2==0 ? .08f : .16f));
            }
        }
        void RotateFurniture(RoomItem selected)
        {
            if(heldItem!=null) {heldRotation=(heldRotation+1)%4;draggingFurniture=!placingFurniture;if(placingFurniture)CenterPlacementGrip();return;}
            if(selected==null) return;
            int next=(selected.rotation+1)%4;
            if(!selected.placed) {selected.rotation=next;Save();}
            else if(RoomLayout.Move(C,selected,selected.x,selected.y,next)) Save();
            else Notify("Not enough room to rotate. Move it to a clear area first.");
        }
        void FloorCell(int x,int y,float tile,Vector2 origin,Color color)
        {
            Vector2 p=RoomLayout.Project(x,y,tile,origin);
            GUI.color=color;GUI.DrawTexture(new Rect(p.x-tile/2,p.y,tile,tile/2),diamond);GUI.color=Color.white;
        }
        void DrawFootprint(RoomItem item,int x,int y,int rotation,float tile,Vector2 origin,Color color)
        {
            Vector2Int size=RoomLayout.Footprint(item,rotation);
            for(int ix=0;ix<size.x;ix++) for(int iy=0;iy<size.y;iy++) FloorCell(x+ix,y+iy,tile,origin,color);
        }
        bool PaintFurniture(RoomItem item,int x,int y,int rotation,float tile,Vector2 origin,float alpha)
        {
            return PaintSoftFurniture(item,x,y,rotation,tile,origin,alpha);
        }
        void DrawDesktopKeyboard(int rotation,Vector2 anchor,float tile,float alpha)
        {
            EnsureWorkerArtwork();
            // Put the keyboard on the near edge of the desk, in front of the monitor.
            DrawDetail(keyboardArt[rotation],anchor+tile*WorkstationArtwork.KeyboardOffset(rotation),tile,alpha);
        }
        void EnsureWorkerArtwork()
        {
            if(keyboardArt!=null) return;
            keyboardArt=new Texture2D[4];employeeArt=new Texture2D[8];typingArmArt=new Texture2D[8];
            for(int i=0;i<4;i++)
            {
                keyboardArt[i]=WorkstationArtwork.Keyboard(i);
                for(int frame=0;frame<2;frame++)
                {
                    employeeArt[i*2+frame]=WorkstationArtwork.Employee((6-i)%4,frame);
                    typingArmArt[i*2+frame]=WorkstationArtwork.TypingArms(i,frame);
                }
            }
        }
        void DrawSeatedWorker(int rotation,Vector2 p,float tile,float alpha,Worker worker)
        {
            int frame=worker!=null && worker.job>=0 && C.lockdown<=0 ? (int)(animationTime*4)%2 : 0;
            if(C.comfortUpgrade>0)Panel(new Rect(p.x-tile*.2f,p.y-tile*.5f,tile*.4f,tile*.14f),new Color(.35f,.46f,.62f,alpha));
            DrawDetail(employeeArt[rotation*2+frame],p,tile,alpha);
        }
        void DrawDetailPlant(Vector2 p,float tile)
        {
            SoftSprite("plant_0",p,tile);
        }
        void DrawDetail(Texture2D texture,Vector2 anchor,float tile,float alpha)
        {
            float scale=tile/70;
            GUI.color=new Color(1,1,1,alpha);
            GUI.DrawTexture(new Rect(anchor.x-96*scale,anchor.y-210*scale,192*scale,256*scale),texture);
            GUI.color=Color.white;
        }
        bool AtlasSprite(int row,int rotation,Vector2 p,float size,float alpha)
        {
            Texture2D sheet=state.neon ? neon : basement;
            if(sheet==null) return false;
            Rect rect=new Rect(p.x-size*.5f,p.y-size*.65f,size,size);
            // Monitor/laptop luminance animates independently of placement and hit testing.
            float brightness=1;
            if(row==1 || row==2)
            {
                float phase=animationTime*2.4f+rotation*1.7f+p.x*.013f;
                // Clearly visible slow brightness sweep with brief terminal refresh dips.
                brightness=.48f+.52f*(.5f+.5f*Mathf.Sin(phase));
                if(Mathf.Repeat(animationTime+p.x*.001f,3.5f)<.16f) brightness=.3f;
            }
            if(row==0 && C.computerUpgrade>0) GUI.color=new Color(.8f,1,1,alpha);
            else GUI.color=new Color(brightness,brightness,brightness,alpha);
            GUI.DrawTextureWithTexCoords(rect,sheet,new Rect(rotation*.25f,1f-(row+1)/16f,.25f,1f/16));
            GUI.color=Color.white;
            Vector2 pointer=Event.current.mousePosition;
            if(!rect.Contains(pointer)) return false;
            if(!sheet.isReadable) return new Rect(p.x-size*.23f,p.y-size*.4f,size*.46f,size*.45f).Contains(pointer);
            // Transparent atlas padding must not intercept clicks on the object behind it.
            int px=rotation*320+Mathf.Clamp((int)((pointer.x-rect.x)/size*320),0,319);
            int py=(15-row)*320+Mathf.Clamp((int)((1-(pointer.y-rect.y)/size)*320),0,319);
            return sheet.GetPixel(px,py).a>.15f;
        }
        void OnApplicationFocus(bool focused)
        {
            if(!focused) cancelForFocusLoss=true;
        }
    }
}
