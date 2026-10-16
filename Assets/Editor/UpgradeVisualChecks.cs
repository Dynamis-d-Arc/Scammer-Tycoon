using System;
using System.IO;
using ScammerTown;
using UnityEditor;
using UnityEngine;

public static class UpgradeVisualChecks
{
    public static int VerifyAssets()
    {
        int count=0;
        foreach(string name in new[]{"equipment_crew","equipment_vest","equipment_beanie","equipment_leather","equipment_empty"})
        {
            var texture=Resources.Load<Texture2D>("PixelOffice/"+name);
            if(texture==null || !texture.isReadable || texture.filterMode!=FilterMode.Point)throw new Exception("Upgrade sprite unavailable: "+name);
            if(EquipmentSprites.Rows(texture).Length!=6)throw new Exception("Expected six separated equipment rows: "+name+" found "+EquipmentSprites.Rows(texture).Length);
            if(texture.GetPixel(0,0).a>.05f || texture.GetPixel(texture.width-1,texture.height-1).a>.05f)throw new Exception("Equipment background is not transparent: "+name);
            for(int row=0;row<6;row++)for(int column=0;column<4;column++)
            {
                Rect uv=EquipmentSprites.UV(texture,column,row);int opaque=0;
                for(int y=0;y<24;y++)for(int x=0;x<24;x++)
                    if(texture.GetPixelBilinear(uv.x+(x+.5f)/24*uv.width,uv.y+(y+.5f)/24*uv.height).a>.5f)opaque++;
                if(opaque<80 || opaque>500)throw new Exception("Equipment cell empty or background opaque: "+name+" "+column+","+row+" count "+opaque);
                Rect destination=EquipmentSprites.DrawRect(texture,uv,new Rect(0,0,256,256));
                if(destination.y< -8 || destination.height<100 || destination.yMax>256)throw new Exception("Equipment sprite doesn't fit: "+name);
                count++;
            }
        }
        return count;
    }
    [MenuItem("Scammer Town/Verify and render upgrades")]
    public static void VerifyAndRender()
    {
        RoomVisualChecks.VerifyAndRender();
        var material=new Material(Resources.Load<Shader>("RoomSurface"));
        try
        {
            foreach(string kind in new[]{"desktop","laptop"})
            {
                var rt=new RenderTexture(1280,1024,0,RenderTextureFormat.ARGB32);rt.Create();
                var previous=RenderTexture.active;Texture2D capture=null;
                try
                {
                    RenderTexture.active=rt;GL.Clear(true,true,new Color(.045f,.065f,.09f,1));
                    GL.PushMatrix();GL.LoadPixelMatrix(0,1280,1024,0);
                    try
                    {
                        for(int tier=0;tier<=3;tier++)for(int look=0;look<=4;look++)
                        {
                            bool staffed=look>0;Texture2D texture;Rect uv;
                            Rect cell=new Rect(look*256+8,tier*256+8,240,240),destination=cell;
                            if(tier==0)
                            {
                                texture=Resources.Load<Texture2D>("PixelOffice/"+(staffed?CrewAppearance.Atlas(look):kind=="laptop"?"laptop_seated":"stations"));
                                int row=staffed && kind=="laptop"?2:0;
                                uv=new Rect(0,1-(row+1)*.25f,.25f,.25f);
                            }
                            else
                            {
                                texture=Resources.Load<Texture2D>("PixelOffice/"+EmpireProgression.EquipmentAtlas(look,staffed));
                                uv=EquipmentSprites.UV(texture,0,EmpireProgression.EquipmentRow(kind,tier));
                                destination=EquipmentSprites.DrawRect(texture,uv,cell);
                            }
                            Graphics.DrawTexture(destination,texture,uv,0,0,0,0,Color.white,material);
                        }
                    }
                    finally {GL.PopMatrix();}
                    capture=new Texture2D(1280,1024,TextureFormat.RGBA32,false);
                    capture.ReadPixels(new Rect(0,0,1280,1024),0,0);capture.Apply();
                    Directory.CreateDirectory("Logs/visuals");File.WriteAllBytes("Logs/visuals/upgrades-"+kind+".png",capture.EncodeToPNG());
                }
                finally {RenderTexture.active=previous;if(capture!=null)UnityEngine.Object.DestroyImmediate(capture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(material);}
        Debug.Log("SCAMMER_TOWN_UPGRADE_RENDERS_PASSED");
    }
}
