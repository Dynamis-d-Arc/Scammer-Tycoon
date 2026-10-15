using System;
using System.Reflection;
using UnityEngine;
using ScammerTown;

public static class FurnishingChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("FURNISHING CHECK FAILED: "+msg);count++;};
        // Legacy layouts keep every owned workstation and its exact position.
        var company=new Company {level=3};
        company.layout=new System.Collections.Generic.List<RoomItem>{new RoomItem {id="desktop_0",kind="desktop",x=6,y=6,rotation=2,placed=true}};
        var desk=company.layout[0];
        company.layout.Add(new RoomItem {id="fridge",kind="fridge",x=3,y=4,rotation=1,placed=true});
        RoomLayout.Ensure(company);
        var cooler=company.layout.Find(i=>i.id=="fridge");
        check(cooler.x==3 && cooler.y==4 && cooler.rotation==1 && cooler.placed,"migration preserves previously moved equipment too");
        check(desk.x==6 && desk.y==6 && desk.rotation==2 && desk.placed,"migration preserves the existing workstation");
        foreach(string id in new[]{"sofa_0","shelf_0","shelf_1","coffee_0","sideboard_0","rug_0","lamp_0","plant_0","plant_1","plant_2","boxes_0","bin_0","radiator_0"})
            check(company.layout.Exists(i=>i.id==id && i.placed),id+" is an independently placed item");
        int total=company.layout.Count;RoomLayout.Ensure(company);
        check(company.layout.Count==total,"repeated migration never duplicates furnishings");
        var sofa=company.layout.Find(i=>i.kind=="sofa");var shelf=company.layout.Find(i=>i.id=="shelf_0");
        check(RoomLayout.Move(company,sofa,10,10,1),"sofa moves and rotates to a free area");
        check(RoomLayout.Footprint(sofa,1)==new Vector2Int(2,3),"sofa footprint rotates with the item");
        check(!RoomLayout.Move(company,sofa,6,6,0) && sofa.x==10 && sofa.y==10,"sofa cannot overlap a workstation");
        check(RoomLayout.Move(company,shelf,12,10,0),"shelf moves independently");
        check(!RoomLayout.Move(company,shelf,15,10,0),"furniture stays within the owned floor");
        shelf.placed=false;RoomLayout.Ensure(company);check(!shelf.placed,"stored shelf stays stored across synchronization");
        var rug=company.layout.Find(i=>i.GroundCover);
        check(RoomLayout.Move(company,rug,6,6,0),"rug can be placed beneath a workstation");
        check(RoomLayout.CanPlace(company,desk,6,6,2),"rug does not block furniture on top");
        check(FurnitureCatalog.CompareDepth(rug,desk)<0,"rug renders and picks behind furniture");
        var saved=new TycoonState();saved.companies[0]=company;
        var loaded=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(saved));RoomLayout.Ensure(loaded.Current);
        var restored=loaded.Current.layout.Find(i=>i.id==sofa.id);
        check(restored.x==10 && restored.y==10 && restored.rotation==1 && restored.placed,"sofa position and rotation survive a save roundtrip");
        check(!loaded.Current.layout.Find(i=>i.id==shelf.id).placed,"shelf storage state survives save roundtrip");
        check(loaded.Current.layout.Count==total,"loading does not regenerate duplicate decorative items");
        var another=new Company {level=3};RoomLayout.Ensure(another);
        check(another.layout.Find(i=>i.id==sofa.id).x!=sofa.x,"apartment layouts are independent");
        company.decorUpgrade=2;RoomLayout.Ensure(company);
        check(company.layout.Exists(i=>i.id=="decorPlant_0") && company.layout.Exists(i=>i.id=="decorPlant_1"),"upgrade plants are movable room items too");
        var plant=company.layout.Find(i=>i.id=="decorPlant_1");check(RoomLayout.Move(company,plant,13,12,2),"upgrade plant accepts a custom position");
        RoomLayout.Ensure(company);check(plant.x==13 && plant.y==12 && plant.rotation==2,"upgrade plant stays at the chosen position");
        var small=new Company {desktops=4};RoomLayout.Ensure(small);
        check(small.layout.FindAll(i=>FurnitureCatalog.IsFurnishing(i)).Count==13,"small offices retain the full furniture inventory");
        check(small.layout.Exists(i=>FurnitureCatalog.IsFurnishing(i) && !i.placed),"overflow furnishings go into storage");
        foreach(var item in small.layout)if(item.placed)
            check(RoomLayout.CanPlace(small,item,item.x,item.y,item.rotation),"migration never leaves overlapping solid objects");
        var atlas=Resources.Load<Texture2D>("PixelOffice/furniture");
        foreach(var def in FurnitureCatalog.All)
        {
            check(def.pixels.x>=0 && def.pixels.y>=0 && def.pixels.xMax<=atlas.width && def.pixels.yMax<=atlas.height,def.kind+" sprite region stays in bounds");
            int opaque=0,clear=0;
            for(int y=0;y<20;y++)for(int x=0;x<20;x++)
            {
                int px=(int)(def.pixels.x+(x+.5f)/20*def.pixels.width);
                int py=atlas.height-1-(int)(def.pixels.y+(y+.5f)/20*def.pixels.height);
                if(atlas.GetPixel(px,py).a>.35f)opaque++;else clear++;
            }
            check(opaque>35 && clear>10,def.kind+" has visible artwork and transparent click-through margins");
        }
        bool oldGuard=ScammerTownGame.SuppressDiskSaves;ScammerTownGame.SuppressDiskSaves=true;
        var go=new GameObject("Furnishing interaction verification");
        try
        {
            var game=go.AddComponent<ScammerTownGame>();game.state=saved;Management.Ensure(saved);
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            Action<string,object[]> call=(name,args)=>typeof(ScammerTownGame).GetMethod(name,flags).Invoke(game,args);
            Func<string,object> field=name=>typeof(ScammerTownGame).GetField(name,flags).GetValue(game);
            call("SynchronizeRoom",null);
            float cash=saved.cash;
            call("StoreFurniture",new object[]{sofa});call("OpenFurnitureStorage",null);
            check(!sofa.placed && (bool)field("furnitureStorageOpen"),"sofa stores and the inventory opens");
            call("BeginStoredFurniturePlacement",new object[]{shelf});
            check(field("heldItem")==shelf && !(bool)field("furnitureStorageOpen"),"storage can select the shelf rather than the first item in the list");
            var e=new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(600,500)};
            call("HandleRoomPointer",new object[]{e,EventType.MouseDown,true,null,new Vector2(12,11)});
            check(shelf.placed && shelf.x==12 && shelf.y==11 && !sofa.placed && saved.cash==cash,"selected shelf places for free while sofa remains stored");
            call("BeginStoredFurniturePlacement",new object[]{sofa});call("CancelPlacement",null);
            check(!sofa.placed && saved.cash==cash,"cancelling a stored sofa preview keeps ownership without charging");
            call("OpenFurnitureStorage",null);call("OpenCity",null);
            check(!(bool)field("furnitureStorageOpen"),"navigation closes the furniture storage panel");
            call("Navigate",new object[]{"Operations"});
            var start=new Vector2(500,350);var from=new Vector2(shelf.x,shelf.y);
            e=new Event{type=EventType.MouseDown,button=0,mousePosition=start};
            call("HandleRoomPointer",new object[]{e,EventType.MouseDown,true,shelf,from});
            e=new Event{type=EventType.MouseDrag,button=0,mousePosition=start+new Vector2(80,40)};
            call("HandleRoomPointer",new object[]{e,EventType.MouseDrag,true,null,new Vector2(11,12)});
            e=new Event{type=EventType.MouseUp,button=0,mousePosition=start+new Vector2(80,40)};
            call("HandleRoomPointer",new object[]{e,EventType.MouseUp,true,null,new Vector2(11,12)});
            check(shelf.x==11 && shelf.y==12 && !(bool)field("workerModal"),"bookshelf drags directly without opening an employee profile");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);ScammerTownGame.SuppressDiskSaves=oldGuard;}
        Debug.Log("SCAMMER_TOWN_FURNISHING_CHECKS_PASSED: "+count);return count;
    }
}
