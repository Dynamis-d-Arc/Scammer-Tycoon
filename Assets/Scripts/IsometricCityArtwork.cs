using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    // A cached, code-drawn diorama: the same orthographic 2:1 grid and flat shaded faces as the room kit.
    public static class IsometricCityArtwork
    {
        public const int Width=1600, Height=940;
        public static Vector2 Project(float x,float y,float z=0) => new Vector2(800+(x-y)*48,105+(x+y)*24-z);
        public static Vector2 ApartmentPin(int index) => index==0 ? Project(6.3f,6.3f,93) : index==1 ? Project(11.4f,7.3f,151) : Project(4.9f,12.9f,70);
        struct Building
        {
            public float x,y,w,d,h; public Color color; public int style;
            public Building(float x,float y,float w,float d,float h,Color color,int style=0)
            {this.x=x;this.y=y;this.w=w;this.d=d;this.h=h;this.color=color;this.style=style;}
        }
        sealed class Painter
        {
            public Color32[] pixels=new Color32[Width*Height];
            public void Polygon(Color color,params Vector2[] points)
            {
                float min=Height,max=0;
                foreach(var p in points) {min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}
                var intersections=new float[points.Length];color.a=1;Color32 fill=color;
                for(int y=Mathf.Max(0,Mathf.FloorToInt(min));y<=Mathf.Min(Height-1,Mathf.CeilToInt(max));y++)
                {
                    int count=0;float scan=y+.5f;
                    for(int i=0;i<points.Length;i++)
                    {
                        var a=points[i];var b=points[(i+1)%points.Length];
                        if((a.y<=scan && b.y>scan)||(b.y<=scan && a.y>scan))
                            intersections[count++]=a.x+(scan-a.y)/(b.y-a.y)*(b.x-a.x);
                    }
                    System.Array.Sort(intersections,0,count);
                    for(int i=0;i+1<count;i+=2)
                        for(int x=Mathf.Max(0,Mathf.CeilToInt(intersections[i]));x<=Mathf.Min(Width-1,Mathf.FloorToInt(intersections[i+1]));x++)
                            pixels[(Height-1-y)*Width+x]=fill;
                }
            }
            public void Tile(float x,float y,float w,float d,Color color,float z=0)
                =>Polygon(color,Project(x,y,z),Project(x+w,y,z),Project(x+w,y+d,z),Project(x,y+d,z));
            public void Line(Vector2 a,Vector2 b,float width,Color color)
            {
                Vector2 v=(b-a).normalized;var n=new Vector2(-v.y,v.x)*width*.5f;
                Polygon(color,a+n,b+n,b-n,a-n);
            }
            public void Box(float x,float y,float w,float d,float height,Color color,float baseHeight=0)
            {
                var a=Project(x,y,height+baseHeight);var b=Project(x+w,y,height+baseHeight);
                var c=Project(x+w,y+d,height+baseHeight);var e=Project(x,y+d,height+baseHeight);
                Polygon(color*.66f,b,c,Project(x+w,y+d,baseHeight),Project(x+w,y,baseHeight));
                Polygon(color*.85f,e,c,Project(x+w,y+d,baseHeight),Project(x,y+d,baseHeight));
                Polygon(color*1.16f,a,b,c,e);
                Line(e,c,1.4f,color*1.35f);Line(c,b,1.4f,color*.92f);
            }
            public void Tree(float x,float y)
            {
                Box(x,y,.12f,.12f,16,new Color(.3f,.27f,.19f));
                Box(x-.17f,y-.17f,.46f,.46f,22,new Color(.27f,.4f,.29f),13);
                Box(x-.1f,y-.1f,.32f,.32f,12,new Color(.34f,.46f,.31f),35);
            }
            public void Building(Building b)
            {
                Box(b.x,b.y,b.w,b.d,b.h,b.color);
                Box(b.x-.04f,b.y-.04f,b.w+.08f,b.d+.08f,5,b.color*.93f,b.h);
                int floors=Mathf.Max(1,(int)(b.h/22));
                for(int f=0;f<floors;f++)
                {
                    float z=12+f*21;
                    int cols=Mathf.Max(2,(int)(b.w/.28f));
                    for(int c=0;c<cols;c++)
                    {
                        float xx=b.x+.12f+c*(b.w-.15f)/cols;
                        Color light=(f+c+b.style)%4==0 ? new Color(.24f,.34f,.35f) : b.style==2 ? new Color(.45f,.78f,.78f) : new Color(.72f,.73f,.48f);
                        Polygon(light,Project(xx,b.y+b.d+.003f,z),Project(xx+.13f,b.y+b.d+.003f,z),Project(xx+.13f,b.y+b.d+.003f,z+9),Project(xx,b.y+b.d+.003f,z+9));
                    }
                    for(int c=0;c<Mathf.Max(2,(int)(b.d/.3f));c++)
                    {
                        float yy=b.y+.13f+c*.29f;
                        if(yy+.13f>b.y+b.d)continue;
                        Color light=(f+c)%3==0 ? new Color(.17f,.25f,.28f) : new Color(.44f,.53f,.46f);
                        Polygon(light,Project(b.x+b.w+.003f,yy,z),Project(b.x+b.w+.003f,yy+.13f,z),Project(b.x+b.w+.003f,yy+.13f,z+9),Project(b.x+b.w+.003f,yy,z+9));
                    }
                }
                // Roof equipment, setbacks and vents echo the furniture kit's chunky geometry.
                Box(b.x+.18f,b.y+.18f,.32f,.3f,12,b.color*.7f,b.h+5);
                if(b.style==3) Box(b.x+b.w*.25f,b.y+b.d*.3f,b.w*.5f,b.d*.4f,32,b.color,b.h+5);
                if(b.style==2)
                {
                    Line(Project(b.x,b.y+b.d+.01f,b.h-8),Project(b.x+b.w,b.y+b.d+.01f,b.h-8),4,new Color(.48f,.84f,.79f));
                    Line(Project(b.x+b.w,b.y,b.h-8),Project(b.x+b.w,b.y+b.d,b.h-8),4,new Color(.6f,.42f,.73f));
                }
            }
        }
        public static Texture2D CreateVehicle(bool boat,bool horizontal)
        {
            var p=new Painter();
            float w=boat ? .65f : horizontal ? .72f : .38f;
            float d=boat ? 1.6f : horizontal ? .38f : .72f;
            Color color=boat ? new Color(.39f,.5f,.49f) : horizontal ? new Color(.37f,.54f,.55f) : new Color(.65f,.62f,.4f);
            if(boat)
                for(int i=0;i<3;i++) p.Line(Project(-.1f,d+.15f+i*.18f),Project(w+.1f,d+.15f+i*.18f),2,new Color(.25f,.45f,.47f));
            p.Box(0,0,w,d,boat ? 8 : 10,color);
            p.Box(w*.2f,d*.3f,w*.6f,d*.4f,boat ? 13 : 5,boat ? new Color(.67f,.65f,.48f) : new Color(.2f,.31f,.34f),boat ? 8 : 10);
            var pixels=new Color32[160*200];
            for(int y=0;y<200;y++) for(int x=0;x<160;x++) pixels[(199-y)*160+x]=p.pixels[(Height-1-(35+y))*Width+760+x];
            var texture=new Texture2D(160,200,TextureFormat.RGBA32,false){name=boat ? "Moving boat" : "Moving car",filterMode=FilterMode.Bilinear};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
        public static Texture2D Create(bool foregroundOnly=false)
        {
            var p=new Painter();
            var asphalt=new Color(.18f,.23f,.25f);var pavement=new Color(.34f,.38f,.34f);
            // Visible island thickness and a waterfront along the west side.
            p.Box(0,0,15,15,15,new Color(.12f,.17f,.18f),-15);
            for(int y=0;y<15;y++)for(int x=0;x<15;x++)
            {
                bool water=x<3;
                bool road=x==3 || x==8 || x==13 || y==4 || y==9;
                Color c=water ? ((x+y)%2==0 ? new Color(.1f,.23f,.28f) : new Color(.1f,.22f,.27f)) : road ? asphalt :
                    ((x+y)%2==0 ? new Color(.29f,.33f,.28f) : new Color(.28f,.32f,.27f));
                p.Tile(x,y,1,1,c);
                if(water && y%2==0)p.Line(Project(x+.2f,y+.25f),Project(x+.75f,y+.25f),2,new Color(.18f,.32f,.35f));
                if(!water && !road)p.Tile(x+.035f,y+.035f,.93f,.93f,pavement);
                if(!water && road && x!=8 && x!=13 && x!=3)
                    p.Line(Project(x+.2f,y+.5f),Project(x+.65f,y+.5f),2,new Color(.62f,.61f,.43f));
                if(!water && (x==3||x==8||x==13) && y!=4 && y!=9)
                    p.Line(Project(x+.5f,y+.2f),Project(x+.5f,y+.65f),2,new Color(.62f,.61f,.43f));
            }
            // Quays, bridges, cargo and boats.
            p.Box(2.65f,0,.3f,15,7,new Color(.36f,.4f,.37f));
            foreach(int y in new[]{4,9})
            {
                p.Box(.15f,y,2.85f,1,8,asphalt);
                p.Line(Project(.15f,y,9),Project(3,y,9),3,new Color(.5f,.56f,.51f));
                p.Line(Project(.15f,y+1,9),Project(3,y+1,9),3,new Color(.5f,.56f,.51f));
            }
            var blocks=new List<Building>();
            var stone=new Color(.46f,.48f,.4f);var blue=new Color(.33f,.43f,.47f);var purple=new Color(.4f,.34f,.47f);
            blocks.Add(new Building(4.25f,.3f,1.3f,1.15f,95,stone));
            blocks.Add(new Building(6,.35f,1.5f,1.3f,170,blue,3));
            blocks.Add(new Building(4.3f,2.1f,1.1f,1.3f,67,new Color(.43f,.4f,.33f)));
            blocks.Add(new Building(6.1f,2.2f,1.3f,1.2f,116,blue));
            blocks.Add(new Building(9.3f,.35f,1.25f,1.25f,168,purple,3));
            blocks.Add(new Building(11.2f,.5f,1.2f,1.4f,126,blue,2));
            blocks.Add(new Building(9.3f,2.4f,1.1f,1.05f,85,stone));
            blocks.Add(new Building(11,2.5f,1.3f,1,105,blue));
            blocks.Add(new Building(14.15f,.5f,.65f,1.1f,61,stone));
            blocks.Add(new Building(14.1f,2.3f,.7f,1.1f,90,purple));
            // The two recognizable destinations: olive Old Town and purple/cyan Neon.
            blocks.Add(new Building(5.5f,5.5f,1.6f,1.6f,88,new Color(.47f,.48f,.31f),1));
            blocks.Add(new Building(4.2f,7.5f,1,.85f,42,stone));
            blocks.Add(new Building(6.4f,7.5f,1.05f,.85f,57,new Color(.46f,.37f,.3f)));
            blocks.Add(new Building(9.3f,5.35f,1.1f,1.1f,83,blue));
            blocks.Add(new Building(10.6f,6.5f,1.6f,1.6f,146,purple,2));
            blocks.Add(new Building(14.1f,5.2f,.7f,1.2f,66,stone));
            blocks.Add(new Building(14.1f,7.3f,.7f,1.15f,93,blue));
            blocks.Add(new Building(4.25f,10.3f,1.1f,1.1f,51,stone));
            blocks.Add(new Building(4.3f,12.3f,1.25f,1.25f,65,new Color(.44f,.38f,.32f)));
            blocks.Add(new Building(9.3f,10.3f,1.15f,1.15f,78,stone));
            blocks.Add(new Building(11.1f,10.5f,1.2f,1,55,blue));
            blocks.Add(new Building(9.4f,12.3f,1.1f,1.4f,65,new Color(.45f,.38f,.31f)));
            blocks.Add(new Building(11.3f,12.4f,1.05f,1.15f,92,purple));
            blocks.Add(new Building(14.1f,10.4f,.7f,1.1f,46,stone));
            blocks.Add(new Building(14.1f,12.4f,.7f,1.3f,70,blue));
            // Park and geometric fountain in the foreground.
            p.Tile(6,10.4f,1.7f,3.3f,new Color(.23f,.34f,.25f));
            p.Tile(6.45f,10.4f,.35f,3.3f,new Color(.47f,.48f,.38f));
            p.Box(6.1f,11.4f,1.3f,1.1f,6,new Color(.48f,.5f,.42f));
            p.Tile(6.22f,11.52f,1.06f,.86f,new Color(.2f,.5f,.54f),7);
            p.Box(6.65f,11.85f,.15f,.15f,24,new Color(.47f,.79f,.75f),7);
            if(foregroundOnly) System.Array.Clear(p.pixels,0,p.pixels.Length);
            foreach(var b in blocks)p.Tile(b.x+.24f,b.y+.16f,b.w+.15f,b.d+.16f,new Color(.2f,.25f,.23f));
            blocks.Sort((a,b)=>(a.x+a.y+a.w+a.d).CompareTo(b.x+b.y+b.w+b.d));
            foreach(var b in blocks)p.Building(b);
            foreach(var pos in new[]{new Vector2(6.1f,10.6f),new Vector2(7.35f,11),new Vector2(7.35f,13),new Vector2(6.1f,13.2f),new Vector2(4.3f,6.3f),new Vector2(9.1f,7.7f),new Vector2(12.4f,5.2f)})p.Tree(pos.x,pos.y);
            var texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false){name="Scammer Town isometric city",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(p.pixels);texture.Apply(false,true);return texture;
        }
    }
}
