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
