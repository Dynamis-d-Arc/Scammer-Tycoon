using System;
using System.Reflection;
using ScammerTown;
using UnityEditor;
using UnityEngine;
public class PixelArtImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.Contains("/PixelOffice/"))return;
        var t=(TextureImporter)assetImporter;
        t.textureShape=TextureImporterShape.Texture2D;
        t.textureCompression=TextureImporterCompression.Uncompressed;
        t.mipmapEnabled=false;t.isReadable=true;t.alphaIsTransparency=true;
        t.filterMode=FilterMode.Point;t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;
    }
    public static int Verify()
    {
        int count=0;
        foreach(string name in new[]{"stations","typing","laptop_seated","props","room_empty","furniture","city","floor"})
        {
            var t=Resources.Load<Texture2D>("PixelOffice/"+name);
            if(t==null || !t.isReadable || t.filterMode!=FilterMode.Point)throw new Exception("Pixel art missing/incorrect import: "+name+" (texture="+(t==null?"missing":t.width+"x"+t.height+", readable="+t.isReadable+", filter="+t.filterMode)+")");
            var corners=new[]{t.GetPixel(0,0).a,t.GetPixel(t.width-1,0).a,t.GetPixel(0,t.height-1).a,t.GetPixel(t.width-1,t.height-1).a};
            foreach(float a in corners)if(a>.05f)throw new Exception("Pixel art background must be transparent: "+name);
            count++;
        }
        var atex=Resources.Load<Texture2D>("PixelOffice/stations");var btex=Resources.Load<Texture2D>("PixelOffice/typing");
        if(atex.width!=btex.width || atex.height!=btex.height)throw new Exception("Animation atlas sizes differ");
        count++;
        var seated=Resources.Load<Texture2D>("PixelOffice/laptop_seated");
        if(seated.width!=seated.height)throw new Exception("Laptop atlas must be a square 4x4 sheet");
        count++;
        foreach(int row in new[]{0,2,3})for(int col=0;col<4;col++)
        {
            int opaque=0;
            for(int y=2;y<28;y++)for(int x=2;x<28;x++)
                if(seated.GetPixel((int)((col+x/30f)*seated.width/4),(int)((3-row+y/30f)*seated.height/4)).a>.5f)opaque++;
            if(opaque<30)throw new Exception("Empty laptop seating cell: "+col+","+row);
            count++;
        }
        // Every workstation cell must contain a usable sprite.
        for(int row=0;row<4;row++)for(int col=0;col<4;col++)
        {
            int opaque=0;
            for(int y=2;y<28;y++)for(int x=2;x<28;x++)
                if(atex.GetPixel((int)((col+x/30f)*atex.width/4),(int)((row+y/30f)*atex.height/4)).a>.5f)opaque++;
            if(opaque<30)throw new Exception("Empty workstation atlas cell");
            count++;
        }
        var go=new GameObject("Pixel camera checks");
        var game=go.AddComponent<ScammerTownGame>();
        try
        {
            foreach(int level in new[]{1,5,20})
            {
                game.state=new TycoonState();
                game.state.Current.level=level;RoomLayout.Ensure(game.state.Current);
                var station=game.state.Current.layout.Find(i=>i.Station);
                station.x=game.state.Current.Size-2;station.y=game.state.Current.Size-2;
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(ScammerTownGame).GetField("pixelCameraCompany",flags).SetValue(game,-1);
                string before=JsonUtility.ToJson(game.state);
                typeof(ScammerTownGame).GetMethod("PixelCamera",flags).Invoke(game,null);
                float zoom=(float)typeof(ScammerTownGame).GetField("zoom",flags).GetValue(game);
                Vector2 pan=(Vector2)typeof(ScammerTownGame).GetField("roomPan",flags).GetValue(game);
                float n=game.state.Current.Size,tile=Mathf.Min(90,1100/n,560/(n*.5f+2.5f))*zoom;
                Rect room=RoomLayout.FloorArtworkRect((int)n,tile,new Vector2(720,330)+pan);
                if(room.x<0 || room.xMax>1440 || room.y<126 || room.yMax>760)throw new Exception("Camera must frame the full room at every expansion size");
                if(before!=JsonUtility.ToJson(game.state))throw new Exception("Camera must not mutate save");
                count+=2;
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(go);}
        return count;
    }
}
