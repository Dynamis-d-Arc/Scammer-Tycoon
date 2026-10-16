using System;
using ScammerTown;
using UnityEngine;

public static class PlacementChecks
{
    public static int Run()
    {
        int count=0;
        Action<bool,string> check=(ok,description)=>{if(!ok)throw new Exception("PLACEMENT FAILED: "+description);count++;};
        var company=new Company();FurnishingChecks.Stock(company);
        RoomLayout.Ensure(company);
        check(company.layout.Count==18,"old save migrates equipment and individual furnishings");
        foreach(var item in company.layout)if(FurnitureCatalog.IsFurnishing(item))item.placed=false;
        check(company.layout.Exists(i=>i.Station && i.placed),"workstation remains placed with a full decoration inventory");
        var desk=company.layout.Find(i=>i.id=="desktop_0");
        var fridge=company.layout.Find(i=>i.id=="fridge");
        var board=company.layout.Find(i=>i.id=="board");
        check(RoomLayout.Move(company,fridge,3,4,1),"move and rotate into a free tile");
        check(fridge.x==3 && fridge.y==4 && fridge.rotation==1,"move persists coordinates and rotation");
        check(!RoomLayout.Move(company,fridge,1,1,0),"block occupied station footprint");
        check(fridge.x==3 && fridge.y==4 && fridge.rotation==1,"invalid drop leaves original transform intact");
        check(!RoomLayout.Move(company,desk,-1,1,0),"block negative floor coordinates");
        check(!RoomLayout.Move(company,desk,4,4,0),"block footprint crossing building boundary");
        check(RoomLayout.Move(company,desk,1,1,1),"rotation ignores own occupied cells");
        check(RoomLayout.Footprint(board,1)==new Vector2Int(1,2),"rotation swaps non-square footprint");
        check(!RoomLayout.Move(company,board,0,0,1) == false,"board rotation in clear area");
        var state=new TycoonState();state.companies[0]=company;
        var restored=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));
        RoomLayout.Ensure(restored.Current);
        var restoredFridge=restored.Current.layout.Find(i=>i.id=="fridge");
        check(restoredFridge.x==3 && restoredFridge.y==4 && restoredFridge.rotation==1,"save roundtrip preserves customized layout");
        company.desktops++;RoomLayout.Ensure(company);
        check(company.layout.Find(i=>i.id=="desktop_0")==desk && desk.rotation==1,"buying another station preserves existing layout");
        check(company.layout.Exists(i=>i.id=="desktop_1"),"new purchase gets a distinct item");
        company.desktops--;RoomLayout.Ensure(company);
        check(!company.layout.Exists(i=>i.id=="desktop_1"),"removed or transferred equipment disappears");
        company.burners=0;RoomLayout.Ensure(company);
        check(!company.layout.Exists(i=>i.id=="phones"),"last phone removes the phone item");
        company.burners=1;RoomLayout.Ensure(company);
        check(company.layout.Exists(i=>i.id=="phones"),"buying a phone restores it");
        fridge.placed=false;RoomLayout.Ensure(company);
        check(!fridge.placed,"stored furniture remains stored on synchronization");
        check(RoomLayout.Move(company,fridge,3,4,0),"stored furniture can be placed again");
        var other=new Company();FurnishingChecks.Stock(other);RoomLayout.Ensure(other);
        check(other.layout.Find(i=>i.id=="fridge")!=fridge,"company layouts are independent");
        company.level=10;RoomLayout.Ensure(company);
        check(fridge.x==3 && fridge.y==4,"expansion preserves existing placements");
        check(RoomLayout.Move(company,fridge,49,49,3),"full 50 by 50 room is placeable");
        foreach(float tile in new[]{6.2f,35f,70f,175f})
        {
            var origin=new Vector2(351,97);var grid=new Vector2(17,29);
            check((RoomLayout.Unproject(RoomLayout.Project(grid.x,grid.y,tile,origin),tile,origin)-grid).sqrMagnitude<.001f,"projection roundtrip at varied zoom");
        }
        check(Resources.Load<Texture2D>("ScamDen/scamden_basement_sheet").isReadable,"basement supports alpha picking");
        check(Resources.Load<Texture2D>("ScamDen/scamden_neon_sheet").isReadable,"neon supports alpha picking");
        var expanded=new Company();FurnishingChecks.Stock(expanded);RoomLayout.Ensure(expanded);
        var original=expanded.layout.Find(i=>i.Station);int oldX=original.x,oldY=original.y;
        expanded.desktops=5;RoomLayout.Ensure(expanded);
        check(!RoomLayout.HasStationSpace(expanded),"full starter office blocks further shop purchases");
        check(RoomLayout.Expand(expanded) && expanded.Size==10,"expand unlocks 10 by 10 floor");
        check(original.x==oldX && original.y==oldY,"expansion keeps placed furniture positions");
        check(expanded.layout.TrueForAll(i=>i.placed || i.manuallyStored),"expansion places overflow while respecting stored decor");
        check(RoomLayout.HasStationSpace(expanded),"expanded floor accepts new workstations");
        while(expanded.level<Rules.MaxOfficeLevel)RoomLayout.Expand(expanded);
        check(expanded.Size==100 && expanded.Capacity==2000,"large office reaches 100 by 100");
        check(!RoomLayout.Expand(expanded),"maximum expansion cannot exceed limit");
        var edge=expanded.layout.Find(i=>i.id=="fridge");
        check(RoomLayout.Move(expanded,edge,99,99,0),"far corner of largest office is usable");
        var large=new TycoonState();large.companies[0]=expanded;
        check(Rules.Valid(JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(large))),"large office save roundtrip remains valid");
        large.Current.level=21;check(!Rules.Valid(large),"oversized malformed office rejected");
        Debug.Log("SCAMMER_TOWN_PLACEMENT_CHECKS_PASSED: " + count);
        return count;
    }
}
