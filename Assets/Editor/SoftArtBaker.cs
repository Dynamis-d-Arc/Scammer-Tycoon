using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Editable rounded models are rendered into modular sprites, retaining the existing placement simulation.
public static class SoftArtBaker
{
    static Transform root;
    static Camera camera;
    static readonly Color Cream=new Color(.91f,.87f,.75f), Oak=new Color(.67f,.46f,.28f), Sage=new Color(.39f,.52f,.39f), Teal=new Color(.12f,.34f,.32f), Skin=new Color(.72f,.46f,.29f);
    static string Output="Assets/Resources/Soft3D";
    static Material Mat(Color c)
    {
        var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",.28f);return m;
    }
    static GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Color color)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=pos;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=Mat(color);return g;
    }
    // Rounded box surface, with shared smooth normals at the beveled edges.
    static GameObject Box(string name,Vector3 pos,Vector3 size,Color color,float bevel=.05f)
    {
        var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.localPosition=pos;
        int steps=8;var verts=new System.Collections.Generic.List<Vector3>();var normals=new System.Collections.Generic.List<Vector3>();var tri=new System.Collections.Generic.List<int>();
        var half=size*.5f;float r=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.8f);var core=half-Vector3.one*r;
        for(int face=0;face<6;face++)
        {
            int start=verts.Count;Vector3 normal=face==0?Vector3.right:face==1?Vector3.left:face==2?Vector3.up:face==3?Vector3.down:face==4?Vector3.forward:Vector3.back;
            Vector3 u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up,v=Vector3.Cross(normal,u);
            for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
            {
                Vector3 unit=normal+u*(x*2f/steps-1)+v*(y*2f/steps-1);
                Vector3 point=Vector3.Scale(unit,half),nearest=new Vector3(Mathf.Clamp(point.x,-core.x,core.x),Mathf.Clamp(point.y,-core.y,core.y),Mathf.Clamp(point.z,-core.z,core.z));
                Vector3 n=(point-nearest).normalized;verts.Add(nearest+n*r);normals.Add(n);
            }
            for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){int a=start+y*(steps+1)+x;tri.Add(a);tri.Add(a+1);tri.Add(a+steps+2);tri.Add(a);tri.Add(a+steps+2);tri.Add(a+steps+1);}
        }
        var mesh=new Mesh();mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetTriangles(tri,0);mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);return g;
    }
    static void Limb(Vector3 a,Vector3 b,float width,Color color)
    {
        var g=Shape("Limb",PrimitiveType.Capsule,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width),color);
        g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    static void Plant(float x,float z,float scale=1)
    {
        Shape("Ceramic planter",PrimitiveType.Cylinder,new Vector3(x,.16f*scale,z),new Vector3(.33f,.16f,.33f)*scale,Cream);
        for(int i=0;i<7;i++)
        {
            float a=i*2.4f;var leaf=Shape("Soft leaf",PrimitiveType.Sphere,new Vector3(x+Mathf.Sin(a)*.15f*scale,(.5f+(i%3)*.07f)*scale,z+Mathf.Cos(a)*.15f*scale),new Vector3(.16f,.46f,.18f)*scale,Sage*(.8f+i*.04f));
            leaf.transform.localRotation=Quaternion.Euler(Mathf.Cos(a)*32,0,Mathf.Sin(a)*32);
        }
    }
    static void Station(bool laptop,int frame)
    {
        Box("Oak tabletop",new Vector3(0,1.05f,0),new Vector3(1.7f,.12f,.92f),Oak,.08f);
        Box("Cream drawers",new Vector3(.61f,.51f,.04f),new Vector3(.4f,.94f,.75f),Cream,.07f);
        for(int i=0;i<3;i++)Box("Drawer handle",new Vector3(.61f,.25f+i*.27f,.43f),new Vector3(.16f,.035f,.035f),Sage,.015f);
        for(int z=-1;z<=1;z+=2)Box("Desk leg",new Vector3(-.65f,.51f,z*.32f),new Vector3(.08f,.96f,.08f),Cream,.03f);
        if(laptop)
        {
            Box("Laptop base",new Vector3(0,1.13f,0),new Vector3(.7f,.045f,.45f),Cream,.03f);
            Box("Laptop lid",new Vector3(0,1.39f,-.2f),new Vector3(.73f,.5f,.045f),Cream,.035f);
            Box("Laptop display",new Vector3(0,1.39f,-.171f),new Vector3(.63f,.4f,.012f),Teal,.02f);
        }
        else
        {
            Box("Monitor foot",new Vector3(0,1.14f,-.22f),new Vector3(.32f,.04f,.23f),Cream,.025f);
            Box("Monitor stem",new Vector3(0,1.35f,-.25f),new Vector3(.07f,.38f,.07f),Cream,.02f);
            Box("Rounded monitor",new Vector3(0,1.62f,-.25f),new Vector3(.96f,.67f,.1f),Cream,.07f);
            Box("Display",new Vector3(0,1.62f,-.191f),new Vector3(.83f,.54f,.015f),Teal,.035f);
        }
        float screenY=laptop?1.39f:1.62f,screenZ=laptop?-.16f:-.18f;
        for(int i=0;i<4;i++)Box("Screen content",new Vector3(-.13f,screenY+.12f-i*.065f,screenZ),new Vector3(i%2==0?.36f:.22f,.016f,.012f),frame==2?new Color(.7f,.88f,.74f):new Color(.4f,.7f,.61f),.005f);
        Box("Keyboard",new Vector3(0,1.14f,.23f),new Vector3(.68f,.045f,.25f),new Color(.76f,.78f,.66f),.025f);
        for(int row=0;row<3;row++)for(int k=0;k<10;k++)Box("Key",new Vector3(-.29f+k*.064f,1.169f,.15f+row*.07f),new Vector3(.046f,.018f,.047f),new Color(.98f,.96f,.87f),.008f);
        Shape("Mouse",PrimitiveType.Sphere,new Vector3(.48f,1.16f,.24f),new Vector3(.13f,.065f,.19f),Cream);
        Shape("Mug",PrimitiveType.Cylinder,new Vector3(-.63f,1.24f,.13f),new Vector3(.14f,.12f,.14f),Cream);
        Box("Chair seat",new Vector3(0,.6f,.8f),new Vector3(.59f,.16f,.57f),Sage,.11f);
        Box("Chair back",new Vector3(0,1,.99f),new Vector3(.63f,.75f,.15f),Sage,.12f);
        Shape("Chair stem",PrimitiveType.Cylinder,new Vector3(0,.28f,.8f),new Vector3(.07f,.25f,.07f),Teal);
        for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Limb(new Vector3(0,.13f,.8f),new Vector3(Mathf.Sin(a)*.32f,.1f,.8f+Mathf.Cos(a)*.32f),.05f,Teal);}
        if(frame==0)return;
        Box("Trousers",new Vector3(0,.68f,.69f),new Vector3(.42f,.22f,.38f),new Color(.22f,.29f,.28f),.09f);
        for(int side=-1;side<=1;side+=2)
        {
            Limb(new Vector3(side*.14f,.66f,.59f),new Vector3(side*.14f,.2f,.44f),.15f,new Color(.22f,.29f,.28f));
            Box("Shoes",new Vector3(side*.14f,.13f,.39f),new Vector3(.17f,.12f,.3f),Teal,.045f);
        }
        Box("Sweater",new Vector3(0,1.02f,.76f),new Vector3(.49f,.6f,.34f),new Color(.38f,.53f,.44f),.14f);
        Shape("Neck",PrimitiveType.Capsule,new Vector3(0,1.37f,.72f),new Vector3(.14f,.12f,.14f),Skin);
        Shape("Face",PrimitiveType.Sphere,new Vector3(0,1.57f,.7f),new Vector3(.43f,.49f,.4f),Skin);
        Shape("Hair",PrimitiveType.Sphere,new Vector3(0,1.72f,.74f),new Vector3(.46f,.28f,.43f),new Color(.24f,.16f,.11f));
        Shape("Nose",PrimitiveType.Sphere,new Vector3(0,1.56f,.49f),new Vector3(.1f,.11f,.11f),Skin);
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Eye",PrimitiveType.Sphere,new Vector3(side*.09f,1.62f,.51f),new Vector3(.028f,.04f,.028f),Teal);
            float tap=(frame==1 && side==1)||(frame==2 && side==-1)?.015f:0;
            Vector3 shoulder=new Vector3(side*.24f,1.24f,.72f),elbow=new Vector3(side*.28f,1.12f,.46f),hand=new Vector3(side*.19f,1.205f+tap,.22f);
            Limb(shoulder,elbow,.16f,new Color(.38f,.53f,.44f));Limb(elbow,hand,.12f,new Color(.38f,.53f,.44f));
            Shape("Hand on keys",PrimitiveType.Sphere,hand,new Vector3(.15f,.07f,.12f),Skin);
        }
    }
    static void Prop(string kind)
    {
        if(kind=="plant"){Plant(0,0,1.5f);return;}
        if(kind=="fridge")
        {
            Box("Water cooler",new Vector3(0,.6f,0),new Vector3(.6f,1.2f,.6f),Cream,.09f);
            Shape("Water bottle",PrimitiveType.Cylinder,new Vector3(0,1.44f,0),new Vector3(.43f,.25f,.43f),new Color(.48f,.68f,.71f));
            Box("Recess",new Vector3(0,.85f,.31f),new Vector3(.32f,.3f,.02f),Sage,.04f);return;
        }
        if(kind=="board")
        {
            Box("Standing board",new Vector3(0,1.14f,0),new Vector3(1.5f,1.12f,.12f),Oak,.08f);
            Box("Pin surface",new Vector3(0,1.14f,.07f),new Vector3(1.35f,.97f,.02f),Cream,.03f);
            for(int i=0;i<4;i++)Box("Note",new Vector3(-.46f+(i%2)*.57f,.95f+(i/2)*.4f,.09f),new Vector3(.35f,.26f,.015f),i%2==0?new Color(.91f,.73f,.42f):new Color(.64f,.74f,.59f),.015f);
            for(int i=-1;i<=1;i+=2)Box("Board feet",new Vector3(i*.52f,.35f,0),new Vector3(.06f,.7f,.1f),Oak);return;
        }
        if(kind=="stash")
        {
            Box("Cabinet",new Vector3(0,.4f,0),new Vector3(.75f,.8f,.65f),Oak,.06f);
            for(int i=0;i<2;i++)Box("Cabinet front",new Vector3(0,.21f+i*.37f,.34f),new Vector3(.66f,.3f,.04f),Cream,.025f);
            Plant(0,0,.65f);return;
        }
        Box("Side table",new Vector3(0,.6f,0),new Vector3(.6f,.08f,.48f),Oak,.05f);
        for(int i=-1;i<=1;i+=2)Box("Leg",new Vector3(i*.22f,.3f,0),new Vector3(.05f,.6f,.3f),Cream);
        for(int i=0;i<3;i++)Box("Phone",new Vector3(-.18f+i*.18f,.66f,0),new Vector3(.13f,.04f,.24f),Teal,.02f);
    }
    static void Clear(){if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);root=new GameObject("Soft 3D models").transform;}
    static void Save(string name,int size,float ortho,Vector3 target,int rotation=0,int height=0)
    {
        root.localRotation=Quaternion.Euler(0,rotation*90,0);
        camera.orthographicSize=ortho;camera.transform.position=target+new Vector3(10,8.164966f,10);camera.transform.LookAt(target);
        int h=height==0?size:height;
        var rt=new RenderTexture(size,h,24,RenderTextureFormat.ARGB32){antiAliasing=4};rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var t=new Texture2D(size,h,TextureFormat.RGBA32,false);t.ReadPixels(new Rect(0,0,size,h),0,0);var pixels=t.GetPixels32();for(int y=0;y<h;y++)System.Array.Reverse(pixels,y*size,size);t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(Output+"/"+name+".png",t.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(t);
    }
    public static void Bake()
    {
        Directory.CreateDirectory(Output);
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.53f,.49f);QualitySettings.antiAliasing=4;
        var sun=new GameObject("Warm afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.91f,.76f);sun.intensity=.95f;sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;
        camera=new GameObject("Baking camera").AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.1f;camera.farClipPlane=100;
        foreach(string kind in new[]{"desktop","laptop"})for(int rot=0;rot<4;rot++)for(int frame=0;frame<3;frame++)
        {Clear();Station(kind=="laptop",frame);Save(kind+"_"+rot+"_"+frame,512,2.25f,new Vector3(0,.75f,0),rot);}
        foreach(string kind in new[]{"board","fridge","stash","phones","plant"})for(int rot=0;rot<4;rot++)
        {Clear();Prop(kind);Save(kind+"_"+rot,512,2.25f,new Vector3(0,.75f,0),rot);}
        for(int level=1;level<=20;level++)BakeShell(level);BakeCity();
        Clear();UnityEngine.Object.DestroyImmediate(root.gameObject);AssetDatabase.Refresh();
        foreach(string path in Directory.GetFiles(Output,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.isReadable=true;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
        Debug.Log("SOFT_3D_ASSETS_BAKED");
    }
    static void BakeShell(int level)
    {
        Clear();
        Box("Foundation",new Vector3(0,-.14f,0),new Vector3(5,.25f,5),Oak,.1f);
        for(int z=0;z<20;z++)for(int x=0;x<5;x++)
            Box("Oak floorboard",new Vector3(-2+x,-.008f,-2.375f+z*.25f),new Vector3(.994f,.035f,.244f),new Color(.78f+(x+z)%3*.018f,.65f+(x+z)%3*.013f,.45f+(x+z)%3*.01f),.008f);
        Box("Warm plaster wall",new Vector3(0,1.05f,-2.5f),new Vector3(5,.0f+2.1f,.12f),Cream,.025f);
        Box("Warm plaster wall",new Vector3(-2.5f,1.05f,0),new Vector3(.12f,2.1f,5),Cream,.025f);
        Box("Sage door",new Vector3(-2.42f,.8f,.65f),new Vector3(.06f,1.6f,.85f),Sage,.035f);
        Shape("Door handle",PrimitiveType.Sphere,new Vector3(-2.36f,.8f,.91f),new Vector3(.06f,.06f,.06f),Oak);
        Box("Window frame",new Vector3(.65f,1.35f,-2.4f),new Vector3(1.8f,1.05f,.1f),Oak,.025f);
        Box("Sky glass",new Vector3(.65f,1.35f,-2.33f),new Vector3(1.65f,.9f,.04f),new Color(.68f,.81f,.8f),.02f);
        Box("Window sill",new Vector3(.65f,.87f,-2.22f),new Vector3(1.9f,.09f,.36f),Cream,.03f);
        Box("Window mullion",new Vector3(.65f,1.35f,-2.29f),new Vector3(.04f,.93f,.04f),Cream,.01f);
        root.localScale=new Vector3(1,1f/level,1);
        foreach(Transform part in root)
        {
            if((part.name.StartsWith("Window") || part.name=="Sky glass"))part.localScale=new Vector3(1f/level,1,1);
            if(part.name=="Sage door" || part.name=="Door handle")part.localScale=Vector3.Scale(part.localScale,new Vector3(1,1,1f/level));
        }
        foreach(Transform part in root)if(part.name=="Door handle")part.localPosition=new Vector3(-2.36f,.8f,.65f+.26f/level);
        Save(level==1?"room_shell":"room_shell_"+level,1024,5.2f,new Vector3(0,.75f,0));
    }
    static void Building(float x,float z,float w,float d,float h,Color c)
    {
        Box("Apartment building",new Vector3(x+w*.5f,h*.5f,z+d*.5f),new Vector3(w,h,d),c,.1f);
        Box("Roof",new Vector3(x+w*.5f,h+.05f,z+d*.5f),new Vector3(w+.08f,.1f,d+.08f),Cream,.06f);
        for(float floor=.25f;floor<h-.1f;floor+=.32f)
        {
            for(float col=.15f;col<w-.06f;col+=.3f)Box("Window",new Vector3(x+col,floor,z+d+.012f),new Vector3(.15f,.18f,.025f),new Color(.42f,.65f,.65f),.025f);
            for(float col=.15f;col<d-.06f;col+=.3f)Box("Window",new Vector3(x+w+.012f,floor,z+col),new Vector3(.025f,.18f,.15f),new Color(.42f,.65f,.65f),.025f);
        }
    }
    static void BakeCity()
    {
        Clear();
        Box("City base",new Vector3(7.5f,-.17f,7.5f),new Vector3(15,.3f,15),Cream,.18f);
        for(int z=0;z<15;z++)for(int x=0;x<15;x++)
        {
            bool water=x<3,road=x==3||x==8||x==13||z==4||z==9;
            Box("City tile",new Vector3(x+.5f,0,z+.5f),new Vector3(1,.05f,1),water?new Color(.38f,.67f,.7f):road?new Color(.48f,.54f,.51f):new Color(.77f,.81f,.68f),.012f);
            if(road&&!water)Box("Lane marking",new Vector3(x+.5f,.033f,z+.5f),x==3||x==8||x==13?new Vector3(.035f,.006f,.4f):new Vector3(.4f,.006f,.035f),Cream,.002f);
        }
        for(int block=0;block<3;block++)for(int row=0;row<3;row++)for(int col=0;col<2;col++)
        {
            float x=4.25f+block*5+col*1.7f,z=.4f+row*5;
            if(x>14.7f || (row==1 && col==1 && block<2))continue;
            float h=.8f+(block+row+col)%4*.4f;
            Building(x,z,Mathf.Min(1.25f,15-x),1.25f,h,(block+col)%2==0?Cream:new Color(.62f,.72f,.66f));
        }
        Building(5.5f,5.5f,1.6f,1.6f,88/67.882f,new Color(.7f,.74f,.52f));
        Building(10.6f,6.5f,1.6f,1.6f,146/67.882f,new Color(.67f,.62f,.74f));
        Building(4.3f,12.3f,1.25f,1.25f,65/67.882f,new Color(.8f,.65f,.49f));
        foreach(var pos in new[]{new Vector2(6.1f,10.6f),new Vector2(7.35f,11),new Vector2(7.35f,13),new Vector2(4.3f,6.3f),new Vector2(9.1f,7.7f),new Vector2(12.4f,5.2f)})Plant(pos.x,pos.y,1.3f);
        Save("city",1600,940/(2*67.882f),new Vector3(7.604166f,0,7.604166f),0,940);
        // Foreground-only render keeps moving vehicles behind buildings.
        foreach(Transform child in root)if(child.name=="City tile"||child.name=="City base"||child.name=="Lane marking")child.gameObject.SetActive(false);
        Save("city_foreground",1600,940/(2*67.882f),new Vector3(7.604166f,0,7.604166f),0,940);
        for(int kind=0;kind<3;kind++)
        {
            Clear();bool boat=kind==2;
            Box("Vehicle",new Vector3(0,.13f,0),new Vector3(boat?.55f:.38f,.22f,boat?1.1f:.7f),boat?Cream:kind==0?new Color(.82f,.55f,.36f):Sage,.09f);
            Box("Cabin",new Vector3(0,.29f,0),new Vector3(.29f,.22f,.36f),new Color(.68f,.82f,.82f),.06f);
            Save("vehicle_"+kind,256,1.5f,new Vector3(0,.2f,0),kind==1?1:0);
        }
    }
}
