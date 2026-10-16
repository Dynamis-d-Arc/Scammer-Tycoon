using System;
using System.IO;
using ScammerTown;
using UnityEditor;
using UnityEngine;

public static class RoomVisualChecks
{
    // Run with a graphics device (omit -nographics); writes preview artifacts only.
    [MenuItem("Scammer Town/Verify and render room proportions")]
    public static void VerifyAndRender()
    {
        ProjectSetup.Verify();
        var material=new Material(Resources.Load<Shader>("RoomSurface"));
        var shell=Resources.Load<Texture2D>("PixelOffice/room_empty");
        var floor=Resources.Load<Texture2D>("PixelOffice/floor");
        Directory.CreateDirectory("Logs/visuals");
        try
        {
            foreach(int size in new[]{5,10,25,100})
            {
                var rt=RoomArchitecture.Bake(size,shell,floor,material);
                var previous=RenderTexture.active;RenderTexture.active=rt;
                Texture2D capture=null;
                try
                {
                    float tile=RoomArchitecture.BakeTileSize(size);
                    Rect bounds=RoomLayout.FloorArtworkRect(size,tile,Vector2.zero);
                    Vector2 origin=-bounds.position;
                    GL.PushMatrix();GL.LoadPixelMatrix(0,bounds.width,bounds.height,0);
                    try
                    {
                        for(int i=0;i<(size==5?1:4);i++)
                        {
                            var crew=Resources.Load<Texture2D>("PixelOffice/"+CrewAppearance.Atlas(i+1));
                            float x=1+(i%2)*4,y=1+(i/2)*4;
                            Vector2 anchor=RoomLayout.Project(x+.5f,y+.5f,tile,origin)+new Vector2(0,tile*.25f);
                            Rect rect=ScammerTownGame.WorkstationSpriteRect(anchor,tile);
                            Graphics.DrawTexture(rect,crew,new Rect((i%2)*.25f,i<2?.75f:.25f,.25f,.25f),0,0,0,0,Color.white,material);
                        }
                    }
                    finally {GL.PopMatrix();}
                    capture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
                    capture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);capture.Apply();
                    if(capture.GetPixel(rt.width/2,rt.height/2).a<.5f)throw new Exception("Room render is empty");
                    for(int y=1;y<size;y++)for(int x=1;x<size;x++)
                    {
                        Vector2 p=RoomLayout.Project(x,y,tile,origin);
                        int px=Mathf.Clamp(Mathf.RoundToInt(p.x/bounds.width*rt.width),0,rt.width-1);
                        int py=Mathf.Clamp(rt.height-1-Mathf.RoundToInt(p.y/bounds.height*rt.height),0,rt.height-1);
                        if(capture.GetPixel(px,py).a<.9f)throw new Exception("Gap in modular floor: "+size+" / "+x+","+y);
                    }
                    File.WriteAllBytes("Logs/visuals/office-"+size+".png",capture.EncodeToPNG());
                }
                finally {RenderTexture.active=previous;if(capture!=null)UnityEngine.Object.DestroyImmediate(capture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(material);}
        Debug.Log("SCAMMER_TOWN_ROOM_RENDERS_PASSED");
    }
}
