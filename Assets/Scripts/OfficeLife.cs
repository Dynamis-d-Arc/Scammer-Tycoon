using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    public static class OfficeLife
    {
        static readonly Vector2Int[] Directions={Vector2Int.right,Vector2Int.up,Vector2Int.left,Vector2Int.down};
        public static bool Open(Company c,Vector2Int p)
        {
            if(p.x<0 || p.y<0 || p.x>=c.Size || p.y>=c.Size)return false;
            foreach(var item in c.layout)if(item.placed && !item.GroundCover)
            {
                var size=RoomLayout.Footprint(item,item.rotation);
                if(p.x>=item.x && p.x<item.x+size.x && p.y>=item.y && p.y<item.y+size.y)return false;
            }
            return true;
        }
        // Bounded breadth-first search: only walk through free tiles. A blocked
        // employee stays seated; movement never changes the furnishing layout.
        public static List<Vector2Int> Route(Company c,Worker w)
        {
            var empty=new List<Vector2Int>();var desk=c.layout.Find(it=>it.id==w.station && it.placed);
            if(desk==null)return empty;
            var size=RoomLayout.Footprint(desk,desk.rotation);Vector2Int start=new Vector2Int(-1,-1);
            for(int y=desk.y-1;y<=desk.y+size.y;y++)for(int x=desk.x-1;x<=desk.x+size.x;x++)
                if((x>=desk.x && x<desk.x+size.x) || (y>=desk.y && y<desk.y+size.y))
                    if(Open(c,new Vector2Int(x,y)))start=new Vector2Int(x,y);
            if(start.x<0)return empty;
            var queue=new Queue<Vector2Int>();var parents=new Dictionary<Vector2Int,Vector2Int>();
            var depths=new Dictionary<Vector2Int,int>();queue.Enqueue(start);parents[start]=start;depths[start]=0;
            var target=start;bool found=false;
            while(queue.Count>0 && parents.Count<256)
            {
                var p=queue.Dequeue();int depth=depths[p];
                bool coffee=c.layout.Exists(it=>it.placed && (it.kind=="fridge" || it.kind=="sofa" || it.kind=="coffeetable") && Mathf.Abs(it.x-p.x)+Mathf.Abs(it.y-p.y)<=2);
                if(coffee && depth>0){target=p;found=true;break;}
                if(depth<=4 && depth>depths[target])target=p;
                if(depth>=8)continue;
                foreach(var d in Directions)
                {
                    var next=p+d;if(parents.ContainsKey(next) || !Open(c,next))continue;
                    parents[next]=p;depths[next]=depth+1;queue.Enqueue(next);
                }
            }
            if(!found && target==start)return new List<Vector2Int>{start};
            var route=new List<Vector2Int>{target};while(target!=start){target=parents[target];route.Add(target);}route.Reverse();return route;
        }
        public static Vector2 Position(List<Vector2Int> route,float phase)
        {
            float along=Mathf.Clamp01(phase<.3f?phase/.3f:phase>.7f?(1-phase)/.3f:1)*(route.Count-1);
            int index=Mathf.Min(route.Count-1,Mathf.FloorToInt(along));
            return Vector2.Lerp(route[index],route[Mathf.Min(route.Count-1,index+1)],along-index);
        }
        public static Rect SpriteUV(Texture2D texture,int appearance,int frame)
        {
            int cw=texture.width/4,ch=texture.height/2,x0=(appearance-1)*cw,y0=(1-frame)*ch;
            int left=cw,right=0,bottom=ch,top=0;
            for(int y=0;y<ch;y++)for(int x=0;x<cw;x++)if(texture.GetPixel(x0+x,y0+y).a>.2f)
            {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            return new Rect((x0+left)/(float)texture.width,(y0+bottom)/(float)texture.height,(right-left+1)/(float)texture.width,(top-bottom+1)/(float)texture.height);
        }
    }
    public partial class ScammerTownGame
    {
        sealed class WalkingActor{public Worker worker;public Vector2 point;public bool returning;}
        readonly List<WalkingActor> walkingActors=new List<WalkingActor>();
        readonly Dictionary<Worker,List<Vector2Int>> walkingRoutes=new Dictionary<Worker,List<Vector2Int>>();
        readonly Dictionary<int,Rect> walkingUV=new Dictionary<int,Rect>();
        int walkingLayoutHash;
        int nextWalkingActor;
        void PrepareOfficeLife()
        {
            int hash=C.GetHashCode()+C.Size;unchecked{foreach(var it in C.layout)hash=hash*31+it.x*17+it.y*7+it.rotation+(it.placed?1:0);foreach(var w in C.workers)hash=hash*31+(w.station?.GetHashCode()??0);}
            if(hash!=walkingLayoutHash){walkingRoutes.Clear();walkingLayoutHash=hash;}
            walkingActors.Clear();nextWalkingActor=0;
            if(arrangingRoom || draggingFurniture || placingFurniture)return;
            foreach(var w in C.workers)
            {
                if(w.breakRemaining<=0 && C.lockdown<=0)continue;
                if(!walkingRoutes.TryGetValue(w,out var route)){route=OfficeLife.Route(C,w);walkingRoutes[w]=route;}
                if(route.Count==0)continue;
                float phase=w.breakRemaining>0?1-w.breakRemaining/12:Mathf.Repeat(state.elapsed*.06f,1);
                walkingActors.Add(new WalkingActor{worker=w,point=OfficeLife.Position(route,phase),returning=phase>.7f});
            }
            walkingActors.Sort((a,b)=>(a.point.x+a.point.y).CompareTo(b.point.x+b.point.y));
        }
        void DrawOfficeActors(float depth,float tile,Vector2 origin)
        {
            var texture=SoftAsset("crew_walking");if(texture==null)return;
            while(nextWalkingActor<walkingActors.Count)
            {
                var actor=walkingActors[nextWalkingActor];if(actor.point.x+actor.point.y+1>depth)break;
                nextWalkingActor++;
                int frame=(int)(animationTime*3)%2,key=actor.worker.appearance*2+frame;
                if(!walkingUV.TryGetValue(key,out var uv)){uv=OfficeLife.SpriteUV(texture,actor.worker.appearance,frame);walkingUV[key]=uv;}
                float height=tile*1.15f,width=height*uv.width*texture.width/(uv.height*texture.height);
                Vector2 anchor=RoomLayout.Project(actor.point.x,actor.point.y,tile,origin)+new Vector2(0,tile*.25f);
                if(actor.returning){uv.x+=uv.width;uv.width=-uv.width;}
                TextureSprite(texture,uv,new Rect(anchor.x-width*.5f,anchor.y-height,width,height),1);
            }
        }
        void DrawOfficeIdentity(float tile,Vector2 origin)
        {
            string identity=C.headquarters?"HARBOR HEADQUARTERS":state.active==0?"BASEMENT · AMBER":state.active==1?"NEON · NIGHT SHIFT":"HARBOR · BLUE HOUR";
            Color color=C.headquarters || state.empireRank==5?new Color(1,.76f,.36f):state.active==1?new Color(.8f,.55f,1):accent;
            Panel(new Rect(28,640,220,55),new Color(.045f,.075f,.12f,.9f));
            PixelIcon(C.headquarters || state.empireRank==5?7:0,new Rect(40,653,25,25),color);
            Label(76,645,164,45,identity,small);
            if(state.elapsed<C.deliveryUntil)
            {
                // Delivery sits outside the playable floor; it never blocks placement.
                SoftSprite("boxes_0",new Vector2(130,610),65);
                Label(28,612,220,24,"Delivery unpacked · "+Mathf.CeilToInt(C.deliveryUntil-state.elapsed)+"s",small);
            }
        }
    }
}
