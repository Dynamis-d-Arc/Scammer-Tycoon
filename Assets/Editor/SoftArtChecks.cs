using System;
using UnityEngine;
public static class SoftArtChecks
{
    public static int Run()
    {
        int count=0;
        Action<string> check=name=>{
            var t=Resources.Load<Texture2D>("Soft3D/"+name);
            if(t==null || !t.isReadable || t.width<256)throw new Exception("Missing or unreadable Soft 3D asset: "+name);
            count++;
        };
        foreach(string kind in new[]{"desktop","laptop"})
            for(int r=0;r<4;r++)for(int f=0;f<3;f++)check(kind+"_"+r+"_"+f);
        foreach(string kind in new[]{"board","fridge","stash","phones","plant"})
            for(int r=0;r<4;r++)check(kind+"_"+r);
        foreach(string kind in new[]{"room_shell","city","city_foreground","vehicle_0","vehicle_1","vehicle_2"})check(kind);
        for(int level=2;level<=20;level++)check("room_shell_"+level);
        var a=Resources.Load<Texture2D>("Soft3D/desktop_0_1").GetPixels32();
        var b=Resources.Load<Texture2D>("Soft3D/desktop_0_2").GetPixels32();
        bool different=false;for(int i=0;i<a.Length;i++)if(!a[i].Equals(b[i])){different=true;break;}
        if(!different)throw new Exception("Typing/screen animation frames must differ");
        return count+1;
    }
}
