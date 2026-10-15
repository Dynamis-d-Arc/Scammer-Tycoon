using System;
using System.Reflection;
using UnityEngine;
using ScammerTown;

public static class PlacementInteractionChecks
{
    public static int Run()
    {
        int count=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception("PLACEMENT INTERACTION FAILED: "+message);count++;};
        var go=new GameObject("Placement interaction verification");
        bool previousGuard=ScammerTownGame.SuppressDiskSaves;
        ScammerTownGame.SuppressDiskSaves=true;
        try
        {
            var game=go.AddComponent<ScammerTownGame>();
            game.state=new TycoonState {cash=10000,branchOwned=true};
            game.state.Current.level=3;
            Management.Ensure(game.state);
            var company=game.state.Current;
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            Action<string,object[]> call=(name,args)=>typeof(ScammerTownGame).GetMethod(name,flags).Invoke(game,args);
            Func<string,object> field=name=>typeof(ScammerTownGame).GetField(name,flags).GetValue(game);
            Action<string,object> set=(name,value)=>typeof(ScammerTownGame).GetField(name,flags).SetValue(game,value);
            Action<EventType,Vector2,Vector2,RoomItem,bool,int> pointer=(type,screen,tile,hover,inside,button)=>
            {
                var e=new Event {type=type,button=button,mousePosition=screen};
                // Batch mode reports native mouse-down as Ignore outside OnGUI.
                // Dispatch its intended type to the same handler used by the room.
                call("HandleRoomPointer",new object[]{e,type,inside,hover,tile});
            };
            call("SynchronizeRoom",null);
            var desk=company.layout.Find(i=>i.id=="desktop_0");
            string assigned=company.workers[0].station;
            var screen=new Vector2(500,350);
            var initial=new Vector2(desk.x+.25f,desk.y+.75f);
            pointer(EventType.MouseDown,screen,initial,desk,true,0);
            pointer(EventType.MouseDrag,screen+Vector2.one,initial,desk,true,0);
            check(!(bool)field("draggingFurniture"),"tiny pointer movement remains a click");
            pointer(EventType.MouseUp,screen+Vector2.one,initial,desk,true,0);
            check((bool)field("workerModal") && desk.x==1 && desk.y==1,"short click opens employee without moving the desk (modal="+field("workerModal")+", position="+desk.x+","+desk.y+", worker="+company.workers[0].station+", held="+field("heldItem")+")");
            call("Navigate",new object[]{"Operations"});
            pointer(EventType.MouseDown,screen,initial,desk,true,0);
            var target=new Vector2(6.25f,7.75f);
            pointer(EventType.MouseDrag,screen+new Vector2(80,40),target,null,true,0);
            check(!(bool)field("arrangingRoom") && (bool)field("draggingFurniture"),"drag starts directly from the office without Arrange");
            check((Vector2Int)field("candidate")==new Vector2Int(6,7),"drag keeps the grabbed point under the cursor");
            pointer(EventType.MouseUp,screen+new Vector2(80,40),target,null,true,0);
            check(desk.x==6 && desk.y==7 && field("heldItem")==null,"release places the desk at the chosen tile");
            check(!(bool)field("workerModal") && company.workers[0].station==assigned,"moving a staffed desk keeps employee assignment and does not open the profile");
            initial=new Vector2(6.25f,7.75f);
            pointer(EventType.MouseDown,screen,initial,desk,true,0);
            pointer(EventType.MouseDrag,screen+new Vector2(80,40),new Vector2(-2,-2),null,false,0);
            pointer(EventType.MouseUp,screen+new Vector2(80,40),new Vector2(-2,-2),null,false,0);
            check(desk.x==6 && desk.y==7 && field("heldItem")==null,"release outside the room restores original position and ends dragging");

            float cash=game.state.cash;int desktops=company.desktops,items=company.layout.Count;
            call("OpenShop",new object[]{0});call("BeginEquipmentPlacement",new object[]{"desktop"});
            var preview=(RoomItem)field("heldItem");
            check(preview!=null && !(bool)field("shopModal") && !(bool)field("cityView"),"shop choice closes catalog and starts an office preview");
            check(game.state.cash==cash && company.desktops==desktops && company.layout.Count==items && !company.layout.Contains(preview),"preview does not charge or create owned equipment");
            call("SynchronizeRoom",null);check(field("heldItem")==preview,"synchronizing the office preserves the pending preview");
            pointer(EventType.MouseMove,screen,new Vector2(6.5f,7.5f),desk,true,0);
            check((Vector2Int)field("candidate")==new Vector2Int(6,7),"preview follows cursor without holding the mouse button");
            pointer(EventType.MouseDown,screen,new Vector2(6.5f,7.5f),desk,true,0);
            check(field("heldItem")==preview && game.state.cash==cash && company.desktops==desktops,"clicking occupied tiles keeps the preview without charging");
            pointer(EventType.MouseDown,screen,new Vector2(15.5f,15.5f),null,true,0);
            check(field("heldItem")==preview && !preview.placed && game.state.cash==cash,"clicking beyond the floor cannot purchase");
            pointer(EventType.MouseDown,screen,new Vector2(8.5f,4.5f),null,true,1);
            check(field("heldItem")==null && game.state.cash==cash && company.layout.Count==items,"right-click cancels a purchase for free");
            call("BeginEquipmentPlacement",new object[]{"laptop"});call("CancelPlacement",null);
            check(game.state.cash==cash && company.laptops==0,"cancel leaves laptop inventory and money unchanged");
            call("BeginEquipmentPlacement",new object[]{"desktop"});
            call("RotateFurniture",new object[]{null});
            pointer(EventType.MouseDown,screen,new Vector2(8.5f,4.5f),null,true,0);
            var bought=company.layout.Find(i=>i.id=="desktop_"+desktops);
            check(bought!=null && bought.placed && bought.x==8 && bought.y==4 && bought.rotation==1,"valid click buys at the exact selected position and rotation");
            check(game.state.cash==cash-800 && company.desktops==desktops+1 && company.layout.Count==items+1,"valid placement charges once and adds one workstation");
            pointer(EventType.MouseUp,screen,new Vector2(8.5f,4.5f),null,true,0);call("CommitPlacement",new object[]{true});
            check(game.state.cash==cash-800 && company.desktops==desktops+1 && field("heldItem")==null,"mouse release or repeated commit cannot buy a second workstation");
            var loaded=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(game.state));Management.Ensure(loaded);
            var restored=loaded.Current.layout.Find(i=>i.id==bought.id);
            check(restored.x==8 && restored.y==4 && restored.rotation==1 && Rules.Valid(loaded),"chosen position and inventory survive save roundtrip");

            call("BeginEquipmentPlacement",new object[]{"laptop"});game.state.cash=10;
            pointer(EventType.MouseDown,screen,new Vector2(10.5f,10.5f),null,true,0);
            check(company.laptops==0 && game.state.cash==10 && field("heldItem")!=null,"funds are rechecked when placement is confirmed");
            game.state.cash=10000;game.state.paused=true;
            pointer(EventType.MouseDown,screen,new Vector2(10.5f,10.5f),null,true,0);
            check(company.laptops==0 && game.state.cash==10000 && field("heldItem")!=null,"paused game cannot finish a purchase");
            game.state.paused=false;
            pointer(EventType.MouseDown,screen,new Vector2(10.5f,10.5f),null,true,0);
            check(company.laptops==1 && game.state.cash==9350 && company.layout.Exists(i=>i.id=="laptop_0" && i.x==10 && i.y==10),"laptop preview completes after resuming with sufficient funds");
            call("BeginEquipmentPlacement",new object[]{"desktop"});
            game.state.active=1;call("CommitPlacement",new object[]{true});
            check(field("heldItem")==null && game.state.cash==9350 && game.state.Current.desktops==1,"changing apartments cannot charge or place in another apartment");
            game.state.active=0;call("BeginEquipmentPlacement",new object[]{"desktop"});call("OpenCity",null);
            check(field("heldItem")==null && game.state.cash==9350,"navigation cancels an unfinished purchase");
            call("Navigate",new object[]{"Operations"});call("BeginEquipmentPlacement",new object[]{"desktop"});
            game.state.active=1;call("SynchronizeRoom",null);
            check(field("heldItem")==null,"room synchronization clears a preview from a different apartment");

            // Stored equipment uses the same click-to-place flow without charging.
            game.state.active=0;call("SynchronizeRoom",null);bought.placed=false;
            set("heldItem",bought);set("heldRotation",bought.rotation);set("placingFurniture",true);call("CenterPlacementGrip",null);
            pointer(EventType.MouseDown,screen,new Vector2(12.5f,12.5f),null,true,0);
            check(bought.placed && bought.x==12 && bought.y==12 && game.state.cash==9350,"placing a stored desk changes only its location");
        }
        finally {UnityEngine.Object.DestroyImmediate(go);ScammerTownGame.SuppressDiskSaves=previousGuard;}
        Debug.Log("SCAMMER_TOWN_PLACEMENT_INTERACTION_CHECKS_PASSED: "+count);
        return count;
    }
}
