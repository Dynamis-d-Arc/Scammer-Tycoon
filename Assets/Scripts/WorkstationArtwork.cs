using UnityEngine;

namespace ScammerTown
{
    // Small cached sprites use the room kit's flat faces and 2:1 isometric projection.
    public static class WorkstationArtwork
    {
        const int W=192,H=256;
        sealed class Painter
        {
            public Color[] pixels=new Color[W*H];
            public int rotation;
            public Vector2 P(float x,float y,float z)
            {
                for(int i=0;i<rotation;i++){float old=x;x=-y;y=old;}
                return new Vector2(96+(x-y)*45,210+(x+y)*22.5f-z*70);
            }
            void Poly(Color color,params Vector2[] v)
            {
                float min=H,max=0;foreach(var p in v){min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);}
                for(int y=Mathf.Max(0,(int)min);y<Mathf.Min(H,Mathf.CeilToInt(max));y++)
                {
                    float left=W,right=0;
                    for(int i=0;i<v.Length;i++)
                    {
                        var a=v[i];var b=v[(i+1)%v.Length];float scan=y+.5f;
                        if((a.y<=scan && b.y>scan)||(b.y<=scan && a.y>scan))
                        {float x=a.x+(scan-a.y)*(b.x-a.x)/(b.y-a.y);left=Mathf.Min(left,x);right=Mathf.Max(right,x);}
                    }
                    for(int x=Mathf.Max(0,Mathf.CeilToInt(left));x<Mathf.Min(W,Mathf.CeilToInt(right));x++) pixels[(H-1-y)*W+x]=color;
                }
            }
            public void Box(float x,float y,float w,float d,float z,float h,Color c)
            {
                // Sort side faces by screen depth after rotation.
                var corners=new[]{P(x,y,z),P(x+w,y,z),P(x+w,y+d,z),P(x,y+d,z)};
                var tops=new[]{P(x,y,z+h),P(x+w,y,z+h),P(x+w,y+d,z+h),P(x,y+d,z+h)};
                for(int i=0;i<4;i++)
                {
                    int j=(i+1)%4;
                    if(corners[j].x<corners[i].x) {Color shade=c*(i%2==0 ? .72f : .88f);shade.a=1;Poly(shade,corners[i],corners[j],tops[j],tops[i]);}
                }
                Poly(c,tops);
            }
            public void Limb(Vector2 from,Vector2 to,float width,Color color)
            {
                Vector2 normal=new Vector2(-(to-from).y,(to-from).x).normalized*width*.5f;
                Poly(color,from+normal,to+normal,to-normal,from-normal);
            }
            public Texture2D Finish(string name)
            {
                var t=new Texture2D(W,H,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear};
                t.SetPixels(pixels);t.Apply(false,true);return t;
            }
        }
        public static Texture2D Keyboard(int rotation)
        {
            var p=new Painter{rotation=rotation};
            var dark=new Color(.12f,.15f,.18f);var key=new Color(.9f,.92f,.85f);
            p.Box(-.43f,-.17f,.86f,.36f,0,.045f,dark);
            for(int row=0;row<3;row++)for(int col=0;col<9;col++)
                p.Box(-.39f+col*.086f,-.14f+row*.085f,.064f,.06f,.046f,.012f,key);
            p.Box(-.23f,.12f,.46f,.045f,.046f,.012f,key);
            return p.Finish("Workstation keyboard");
        }
        public static Vector2 ChairOffset(int rotation)
        {
            return rotation==0 ? new Vector2(-.3f,.15f) : rotation==1 ? new Vector2(.3f,.15f) :
                rotation==2 ? new Vector2(.3f,-.15f) : new Vector2(-.3f,-.15f);
        }
        public static Vector2 KeyboardOffset(int rotation) => ChairOffset(rotation)*.85f-new Vector2(0,.83f);

        public static Texture2D TypingArms(int rotation,int frame)
        {
            var p=new Painter {rotation=rotation};
            var body=new Painter {rotation=(6-rotation)%4};
            Vector2 bodyOffset=(ChairOffset(rotation)-KeyboardOffset(rotation))*70;
            var shirt=new Color(.3f,.53f,.57f);var skin=new Color(.72f,.52f,.36f);
            // Both hands are defined in keyboard space so rotation and scaling cannot separate them.
            for(int hand=0;hand<2;hand++)
            {
                float x=hand==0 ? -.24f : .14f;
                Vector2 wrist=p.P(x+.05f,.05f,.075f);
                Vector2 shoulder=body.P(hand==0 ? -.2f : .2f,0,.67f)+bodyOffset;
                Vector2 elbow=Vector2.Lerp(shoulder,wrist,.52f)+new Vector2(0,5);
                p.Limb(shoulder,elbow,8,shirt);
                p.Limb(elbow,wrist,6,shirt);
                float tap=(hand==frame ? .008f : .022f);
                p.Box(x,-.01f,.1f,.09f,.065f+tap,.035f,skin);
                for(int finger=0;finger<3;finger++)
                    p.Box(x+finger*.033f,-.075f,.024f,.07f,.06f+tap,.025f,skin);
            }
            return p.Finish("Hands on keyboard");
        }
        public static Texture2D Employee(int rotation,int frame)
        {
            var p=new Painter{rotation=rotation};
            var shirt=new Color(.3f,.53f,.57f);var trousers=new Color(.18f,.24f,.3f);var skin=new Color(.72f,.52f,.36f);
            // Seated legs and shoes, torso, then head and hair.
            p.Box(-.17f,-.12f,.13f,.39f,.15f,.16f,trousers);
            p.Box(.04f,-.12f,.13f,.39f,.15f,.16f,trousers);
            p.Box(-.17f,.15f,.13f,.12f,0,.2f,trousers);
            p.Box(.04f,.15f,.13f,.12f,0,.2f,trousers);
            p.Box(-.18f,.13f,.15f,.21f,0,.07f,new Color(.13f,.16f,.17f));
            p.Box(.03f,.13f,.15f,.21f,0,.07f,new Color(.13f,.16f,.17f));
            p.Box(-.21f,-.17f,.42f,.3f,.32f,.45f,shirt);
            p.Box(-.12f,-.11f,.24f,.23f,.78f,.28f,skin);
            p.Box(-.135f,-.125f,.27f,.25f,1.01f,.1f,new Color(.23f,.18f,.15f));
            return p.Finish("Seated employee typing");
        }
    }
}
