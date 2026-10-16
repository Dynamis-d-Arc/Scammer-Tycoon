using System;
using System.IO;
using ScammerTown;
using UnityEngine;

public static class ReliabilityChecks
{
    public static int Run()
    {
        int count=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception("RELIABILITY: "+message);count++;};
        for(int i=0;i<7;i++)
        {
            Rect r=ScammerTownGame.DockButtonRect(i);
            check(r.x>=220 && r.xMax<=1220 && r.y>=816 && r.yMax<=888,"navigation stays inside centered dock");
            if(i>0)check(r.x-ScammerTownGame.DockButtonRect(i-1).xMax>=4,"navigation has consistent non-overlapping gaps");
        }
        for(int i=0;i<6;i++)
        {
            Rect r=ScammerTownGame.RoomToolRect(i);
            check(r.x>=220 && r.xMax<=1220 && r.y>=750 && r.yMax<=806,"room controls stay inside their toolbar");
            if(i>0)check(r.x-ScammerTownGame.RoomToolRect(i-1).xMax>=8,"room controls have consistent non-overlapping gaps");
            check(r.yMax<ScammerTownGame.DockButtonRect(0).y,"room controls never overlap navigation");
        }
        foreach(int size in new[]{5,10,25,100})foreach(float tile in new[]{20f,90f,180f})
        {
            var origin=new Vector2(720,320);var art=RoomLayout.FloorArtworkRect(size,tile,origin);
            check(Mathf.Abs(art.width*.88f-size*tile)<.01f,"artwork floor width matches full grid at every office size");
            check(Mathf.Abs(art.y+art.height*.295f-origin.y)<.01f,"back floor corner follows grid origin");
            check(Mathf.Abs(art.y+art.height*.935f-(origin.y+size*tile*.5f))<.01f,"front floor corner follows last grid row");
        }
        for(int y=0;y<5;y++)for(int x=0;x<5;x++)
        {
            var origin=new Vector2(720,320);float tile=150;
            var center=RoomLayout.Project(x,y,tile,origin)+new Vector2(0,tile*.25f);
            var picked=RoomLayout.Unproject(center-new Vector2(0,tile*.25f),tile,origin);
            check(Mathf.RoundToInt(picked.x)==x && Mathf.RoundToInt(picked.y)==y,"every starter cell picks correctly across the floor");
        }
        var starter=new Company();RoomLayout.Ensure(starter);
        check(starter.decorations.Count==0,"starter room has no free decoration inventory");
        check(!starter.layout.Exists(i=>FurnitureCatalog.Find(i.kind)!=null),"starter does not receive free decorative furniture");
        var starterDesk=starter.layout.Find(i=>i.Station);
        check(starterDesk.placed && starterDesk.x==1 && starterDesk.y==1,"decluttering preserves the original desk");
        RoomLayout.Expand(starter);
        var purchaseState=new TycoonState();purchaseState.companies[0]=starter;
        var sofa=FurnitureCatalog.BuyDecoration(purchaseState,"sofa");
        check(purchaseState.cash==4820-FurnitureCatalog.Price("sofa") && sofa.manuallyStored && !sofa.placed,"decoration purchase charges once and goes to storage");
        RoomLayout.AutoPlace(starter,sofa);RoomLayout.Ensure(starter);
        check(sofa.placed,"player can restore stored decor without cleanup running again");
        var secondSofa=FurnitureCatalog.BuyDecoration(purchaseState,"sofa");
        check(secondSofa.id!=sofa.id && starter.decorations.Count==2,"repeat furniture purchases have unique ownership");
        var purchaseReload=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(purchaseState));Management.Ensure(purchaseReload);
        check(purchaseReload.Current.decorations.Count==2 && purchaseReload.Current.layout.Exists(i=>i.id==secondSofa.id && !i.placed),"purchased storage survives save reload");
        RoomLayout.Expand(purchaseReload.Current);
        check(!purchaseReload.Current.layout.Find(i=>i.id==secondSofa.id).placed,"expansion never auto-places purchased storage");
        purchaseReload.cash=0;
        check(FurnitureCatalog.BuyDecoration(purchaseReload,"plant")==null && purchaseReload.Current.decorations.Count==2,"insufficient funds grant no furniture");
        purchaseReload.cash=1000;purchaseReload.paused=true;
        check(FurnitureCatalog.BuyDecoration(purchaseReload,"plant")==null && purchaseReload.cash==1000,"paused purchase cannot charge");
        purchaseReload.paused=false;
        check(FurnitureCatalog.BuyDecoration(purchaseReload,"unknown")==null && purchaseReload.cash==1000,"unknown furniture cannot charge");
        var legacy=new Company{level=3,layout=new System.Collections.Generic.List<RoomItem>{
            new RoomItem{id="plant_0",kind="plant",placed=true,x=8,y=8},
            new RoomItem{id="sofa_0",kind="sofa",placed=false,manuallyStored=true}}};
        RoomLayout.Ensure(legacy);
        check(legacy.decorations.Count==1 && legacy.layout.Exists(i=>i.id=="plant_0" && i.x==8 && i.y==8),"migration preserves placed furniture without granting unused starter stock");
        RoomLayout.Ensure(legacy);check(legacy.decorations.Count==1,"ownership migration is idempotent");
        var s=new TycoonState();s.Current.level=3;s.Current.desktops=2;
        Management.Ensure(s);
        var w=s.Current.workers[0];var desk=s.Current.layout.Find(i=>i.id==w.station);
        Management.SetAssignment(s,0,w,0);
        desk.placed=false;desk.manuallyStored=true;
        Management.Ensure(s);
        check(w.station==desk.id,"stored desk retains worker despite another free desk");
        float remaining=w.remaining;
        Management.Tick(s,5,()=>0,(m,a)=>{});
        check(w.remaining==remaining,"stored assigned desk pauses progress");
        RoomLayout.Expand(s.Current);
        check(!desk.placed,"expansion does not restore intentionally stored desk");
        var loaded=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(s));
        Management.Ensure(loaded);RoomLayout.Expand(loaded.Current);
        check(!loaded.Current.layout.Find(i=>i.id==desk.id).placed,"storage preference survives reload and expansion");
        check(RoomLayout.AutoPlace(s.Current,desk) && !desk.manuallyStored,"placing clears manual storage flag");
        Management.Tick(s,1,()=>0,(m,a)=>{});
        check(w.remaining<remaining,"same worker resumes when desk returns");

        var coarse=new TycoonState();Management.Ensure(coarse);
        Management.SetAssignment(coarse,0,coarse.Current.workers[0],3);
        var fine=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(coarse));
        Management.Tick(coarse,35,()=>0,(m,a)=>{});
        for(int i=0;i<140;i++)Management.Tick(fine,.25f,()=>0,(m,a)=>{});
        check(coarse.jobsCompleted==3 && coarse.cash==fine.cash,"large and small time steps pay equally");
        check(Mathf.Abs(coarse.Current.workers[0].remaining-fine.Current.workers[0].remaining)<.001f,"leftover time advances next job");
        coarse.Current.workers[0].assignment=-1;
        Management.Tick(coarse,20,()=>0,(m,a)=>{});
        check(coarse.jobsCompleted==4 && coarse.Current.workers[0].job==-1,"stop finishes only current job");

        string directory=Path.Combine(Path.GetTempPath(),"ScamDenSaveCheck-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"save.json");
        try
        {
            AtomicSave.Write(path,"first");AtomicSave.Write(path,"second");
            check(File.ReadAllText(path)=="second" && File.ReadAllText(path+".bak")=="first","atomic replacement preserves previous save");
            check(!File.Exists(path+".tmp"),"successful replacement consumes staging file");
            File.WriteAllText(path+".tmp","interrupted staging");
            check(File.ReadAllText(path)=="second","interrupted staging leaves live save intact");
            AtomicSave.Write(path,"third");
            check(File.ReadAllText(path)=="third" && File.ReadAllText(path+".bak")=="second","next save recovers from stale staging");
        }
        finally
        {
            foreach(string file in Directory.GetFiles(directory))File.Delete(file);
            Directory.Delete(directory);
        }
        return count;
    }
}
