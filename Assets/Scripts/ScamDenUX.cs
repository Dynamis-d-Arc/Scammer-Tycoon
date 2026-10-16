using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    // Scam-den presentation: the live hook feed, the Gazette, the marks wall,
    // crew speech bubbles and town reactions. Nothing here changes the economy.
    public partial class ScammerTownGame
    {
        sealed class FeedEntry { public string text; public Color color; public float time; }
        readonly List<FeedEntry> hookFeed=new List<FeedEntry>();
        readonly Dictionary<Worker,Vector2Int> feedStages=new Dictionary<Worker,Vector2Int>();
        readonly Dictionary<int,Texture2D> portraits=new Dictionary<int,Texture2D>();
        TycoonState feedState;
        int feedOffice=-1,feedDay=-1;
        int[] feedBands;
        bool[] feedLockdowns;
        float feedStageReady;
        bool feedCollapsed;
        GUIStyle paperCard,paperTitle,paperBody,paperSmall,paperAlert,polaroidName,polaroidBelief,corkNote,ringText,gazetteMast,gazetteHead,bubbleText,cashFloat,feedText,moneyNote;
        Texture2D uiPaper;
        static readonly Color FeedStage=new Color(.78f,.85f,.95f),FeedWin=new Color(.36f,.98f,.62f),FeedLose=new Color(1,.74f,.38f),FeedTown=new Color(1,.5f,.46f);
        static readonly Color Cork=new Color(.52f,.36f,.22f),PaperInk=new Color(.17f,.12f,.1f),StringRed=new Color(.84f,.13f,.12f);
        static readonly Vector2[] BoardPins={new Vector2(187,120),new Vector2(261,94),new Vector2(276,160),new Vector2(205,205)};

        void DenStyles()
        {
            uiPaper=PixelSurface(new Color(.94f,.89f,.76f));
            paperCard=new GUIStyle(GUI.skin.box){normal={background=uiPaper},border=new RectOffset(8,8,8,8),padding=new RectOffset(14,14,14,12),margin=new RectOffset(0,0,10,4)};
            paperTitle=new GUIStyle(body){fontSize=16,fontStyle=FontStyle.Bold,normal={textColor=PaperInk}};
            paperBody=new GUIStyle(body){fontSize=13,normal={textColor=PaperInk}};
            paperSmall=new GUIStyle(body){fontSize=12,normal={textColor=new Color(.38f,.29f,.22f)}};
            gazetteMast=new GUIStyle(paperSmall){fontSize=10,fontStyle=FontStyle.Bold,wordWrap=false};
            gazetteHead=new GUIStyle(paperTitle){fontSize=14};
            bubbleText=new GUIStyle(paperBody){fontSize=12,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            cashFloat=new GUIStyle(title){fontSize=20,normal={textColor=FeedWin}};
            feedText=new GUIStyle(small){fontSize=12};
            paperAlert=new GUIStyle(paperBody){fontStyle=FontStyle.Bold,normal={textColor=new Color(.62f,.12f,.1f)}};
            polaroidName=new GUIStyle(paperTitle){fontSize=13,wordWrap=false,clipping=TextClipping.Clip};
            polaroidBelief=new GUIStyle(paperSmall){fontStyle=FontStyle.Italic};
            corkNote=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};
            ringText=new GUIStyle(bubbleText){normal={textColor=new Color(1,.86f,.42f)}};
            moneyNote=new GUIStyle(small);
        }
        void ReleaseDenArt()
        {
            foreach(var texture in portraits.Values)if(texture!=null)Destroy(texture);
            portraits.Clear();
            if(uiPaper!=null)Destroy(uiPaper);
        }

        // ---------- Live hook feed ----------
        void OnRunFinished(TycoonState s,int office,Worker w,int job,bool won,int pay)
        {
            if(s!=state)return;
            string text=Marks.Outcome(w,Marks.For(w.markSeed,job),won,pay);
            Feed(office==state.active?text:"["+OfficeTag(office)+"] "+text,won?FeedWin:FeedLose);
            // The next run starts fresh and announces its opening line.
            feedStages[w]=new Vector2Int(-1,-1);
        }
        static string OfficeTag(int office) => office==0?"Basement":office==1?"Neon":"Harbor";
        void Feed(string text,Color color)
        {
            hookFeed.Insert(0,new FeedEntry{text=text,color=color,time=Time.unscaledTime});
            if(hookFeed.Count>7)hookFeed.RemoveAt(hookFeed.Count-1);
        }
        void UpdateHookFeed()
        {
            if(feedState!=state || feedBands==null || feedBands.Length!=state.companies.Length)
            {
                feedState=state;feedStages.Clear();hookFeed.Clear();feedDay=-1;feedOffice=state.active;
                feedBands=new int[state.companies.Length];feedLockdowns=new bool[state.companies.Length];
                for(int i=0;i<state.companies.Length;i++){feedBands[i]=Band(state.companies[i]);feedLockdowns[i]=state.companies[i].lockdown>0;}
            }
            if(feedOffice!=state.active){feedStages.Clear();feedOffice=state.active;}
            if(feedDay!=state.day)
            {
                if(feedDay>=0)Feed("Gazette: "+Marks.Headline(state,out _),FeedTown);
                feedDay=state.day;
            }
            for(int i=0;i<state.companies.Length;i++)
            {
                if(!Management.Owned(state,i))continue;
                var c=state.companies[i];int band=Band(c);
                if(band>feedBands[i])Feed(band==2?"A patrol car slows down outside the "+OfficeTag(i)+"…":"Neighbors near the "+OfficeTag(i)+" report 'a lot of phone calls'.",FeedTown);
                if(c.lockdown>0 && !feedLockdowns[i])Feed(OfficeTag(i)+": lights off. The crew is laying low.",FeedTown);
                feedBands[i]=band;feedLockdowns[i]=c.lockdown>0;
            }
            if(state.paused)return;
            foreach(var w in C.workers)
            {
                int stage=Marks.Stage(w);
                if(stage<0)continue;
                // First sighting (load or office switch) syncs silently; no backlog burst.
                if(!feedStages.TryGetValue(w,out var seen)){feedStages[w]=new Vector2Int(w.markSeed,stage);continue;}
                if(seen.x!=w.markSeed)seen=new Vector2Int(w.markSeed,-1);
                // Throttle running commentary so big crews don't flood the feed.
                if(stage>seen.y && Time.unscaledTime>=feedStageReady)
                {
                    Feed(Marks.StageLine(w,Marks.Current(w),stage),FeedStage);
                    feedStageReady=Time.unscaledTime+1.4f;seen.y=stage;
                }
                feedStages[w]=seen;
            }
        }
        static int Band(Company c) => c.suspicion>=75?2:c.suspicion>=40?1:0;
        void DrawHookFeed()
        {
            const float x=1132,w=284;
            Rect header=new Rect(x,128,w,30);
            Panel(new Rect(x,128,w,feedCollapsed?30:30+FeedHeight(w)),new Color(.045f,.07f,.12f,.9f));
            GUI.Label(new Rect(x+12,132,200,22),"ON THE LINE",feedText);
            if(GUI.Button(new Rect(header.xMax-58,131,52,22),feedCollapsed?"Show":"Hide",feedText))feedCollapsed=!feedCollapsed;
            if(feedCollapsed)return;
            float y=160;
            if(hookFeed.Count==0){GUI.Label(new Rect(x+12,y,w-24,40),"Quiet line. Assign crew to a scheme.",feedText);return;}
            for(int i=0;i<hookFeed.Count && i<6;i++)
            {
                var entry=hookFeed[i];
                float h=feedText.CalcHeight(new GUIContent(entry.text),w-24);
                float age=Time.unscaledTime-entry.time,slide=Mathf.Clamp01(1-age/.25f)*18;
                Color old=feedText.normal.textColor;
                feedText.normal.textColor=new Color(entry.color.r,entry.color.g,entry.color.b,1-i*.12f);
                GUI.Label(new Rect(x+12+slide,y,w-24,h),entry.text,feedText);
                feedText.normal.textColor=old;
                y+=h+4;
            }
        }
        float FeedHeight(float w)
        {
            if(hookFeed.Count==0)return 40;
            float h=6;
            for(int i=0;i<hookFeed.Count && i<6;i++)h+=feedText.CalcHeight(new GUIContent(hookFeed[i].text),w-24)+4;
            return h;
        }

        // ---------- The Gazette ----------
        void DrawGazette(Rect r,bool compact)
        {
            string headline=Marks.Headline(state,out string kicker);
            float headHeight=gazetteHead.CalcHeight(new GUIContent(headline),r.width-24);
            if(compact)r.height=30+headHeight;
            Panel(r,new Color(.93f,.9f,.82f));
            GUI.Label(new Rect(r.x+12,r.y+6,r.width-24,16),"THE SCAMMER TOWN GAZETTE  ·  DAY "+state.day,gazetteMast);
            GUI.Label(new Rect(r.x+12,r.y+22,r.width-24,headHeight),headline,gazetteHead);
            if(!compact)GUI.Label(new Rect(r.x+12,r.y+24+headHeight,r.width-24,20),kicker,paperSmall);
        }

        // ---------- Marks wall on the Scam Board ----------
        Texture2D PortraitTexture(Mark mark)
        {
            int key=mark.seed*8+mark.job;
            if(portraits.TryGetValue(key,out var texture) && texture!=null)return texture;
            if(portraits.Count>48){foreach(var old in portraits.Values)if(old!=null)Destroy(old);portraits.Clear();}
            const int n=Marks.PortraitSize;
            var pixels=Marks.Portrait(mark);var flipped=new Color32[pixels.Length];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)flipped[(n-1-y)*n+x]=pixels[y*n+x];
            texture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(flipped);texture.Apply();portraits[key]=texture;return texture;
        }
        void DrawHookedMarks(Rect area)
        {
            Panel(area,Cork);
            var running=C.workers.FindAll(w=>w.job>=0 && Management.HasPlacedDesk(C,w));
            running.Sort((a,b)=>a.remaining.CompareTo(b.remaining));
            if(running.Count==0)
            {
                GUI.Label(new Rect(area.x+28,area.y+area.height*.5f-24,area.width-56,48),"Nobody's on the hook. Assign crew to a scheme below and the marks will start lining up.",corkNote);
                return;
            }
            int shown=Mathf.Min(5,running.Count);const float cardW=164,cardH=212;
            float gap=shown>1?Mathf.Clamp((area.width-28-shown*cardW)/(shown-1),12,40):0;
            float left=area.x+(area.width-(shown*cardW+(shown-1)*gap))*.5f;
            var pins=new List<Vector2>();
            for(int i=0;i<shown;i++)
            {
                var rect=new Rect(left+i*(cardW+gap),area.y+12+(i%2==0?0:7),cardW,cardH);
                DrawPolaroid(rect,running[i]);
                pins.Add(new Vector2(rect.center.x,rect.y+4));
            }
            for(int i=1;i<pins.Count;i++)DottedLine(pins[i-1],pins[i],StringRed,2);
            foreach(var pin in pins)DrawPin(pin);
            if(running.Count>shown)GUI.Label(new Rect(area.xMax-150,area.yMax-22,140,20),"+"+(running.Count-shown)+" more on the line",feedText);
        }
        void DrawPolaroid(Rect r,Worker w)
        {
            var mark=Marks.Current(w);
            Panel(r,new Color(.97f,.96f,.92f));
            var photo=new Rect(r.x+10,r.y+10,r.width-20,86);
            Panel(photo,Marks.TypeColors[mark.typeIndex]*.75f+new Color(0,0,0,.25f));
            // Whole-number scale keeps portrait pixels square.
            GUI.DrawTexture(new Rect(photo.center.x-32,photo.y+11,64,64),PortraitTexture(mark));
            GUI.Label(new Rect(r.x+10,r.y+99,r.width-20,20),mark.Name,polaroidName);
            GUI.Label(new Rect(r.x+10,r.y+117,r.width-20,18),mark.type+" · "+w.name+" calling",paperSmall);
            GUI.Label(new Rect(r.x+10,r.y+134,r.width-20,32),mark.belief,polaroidBelief);
            float progress=1-w.remaining/Mathf.Max(.01f,w.duration);
            Panel(new Rect(r.x+10,r.y+168,r.width-20,6),new Color(.78f,.74f,.66f));
            Panel(new Rect(r.x+10,r.y+168,(r.width-20)*Mathf.Clamp01(progress),6),C.lockdown>0?muted:new Color(.2f,.62f,.42f));
            if(Button(r.x+10,r.y+180,r.width-20,w.onCall?"On the line":"Take the call",TheCall.CanTake(state,state.active,w),26))OpenCall(w);
        }
        void DrawPin(Vector2 p)
        {
            Color old=GUI.color;
            GUI.color=new Color(0,0,0,.35f);GUI.DrawTexture(new Rect(p.x-4,p.y-2,9,9),Texture2D.whiteTexture);
            GUI.color=StringRed;GUI.DrawTexture(new Rect(p.x-5,p.y-5,9,9),Texture2D.whiteTexture);
            GUI.color=new Color(1,.7f,.66f);GUI.DrawTexture(new Rect(p.x-3,p.y-3,3,3),Texture2D.whiteTexture);
            GUI.color=old;
        }
        void DottedLine(Vector2 a,Vector2 b,Color color,float size)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Color old=GUI.color;GUI.color=color;
            int steps=Mathf.Max(1,Mathf.CeilToInt((b-a).magnitude/(size*.8f)));
            for(int i=0;i<=steps;i++)
            {
                var p=Vector2.Lerp(a,b,i/(float)steps);
                GUI.DrawTexture(new Rect(p.x-size*.5f,p.y-size*.5f,size,size),Texture2D.whiteTexture);
            }
            GUI.color=old;
        }
        // Red string between the pinned notes on the in-room marks board.
        void DrawBoardString(Rect sprite,bool mirror,float alpha)
        {
            var p=new Vector2[BoardPins.Length];
            for(int i=0;i<p.Length;i++)
            {
                float u=BoardPins[i].x/384f,v=BoardPins[i].y/341.33f;
                p[i]=new Vector2(sprite.x+(mirror?1-u:u)*sprite.width,sprite.y+v*sprite.height);
            }
            float size=Mathf.Max(1.2f,sprite.width/120f);var color=new Color(StringRed.r,StringRed.g,StringRed.b,alpha*.9f);
            DottedLine(p[0],p[1],color,size);DottedLine(p[1],p[2],color,size);DottedLine(p[2],p[3],color,size);
            DottedLine(p[3],p[0],color,size);DottedLine(p[0],p[2],color,size);
        }
        bool PhonesRinging() => C.workers.Exists(w=>w.job>=0 && w.duration>0 && Mathf.Abs(1-w.remaining/w.duration-.6f)<.04f);
        void DrawPhoneRing(Rect sprite)
        {
            Color old=GUI.color;
            float pulse=(Mathf.Sin(animationTime*18)+1)*.5f;
            GUI.color=new Color(1,.85f,.4f,.6f+.4f*pulse);
            GUI.Label(new Rect(sprite.center.x-30,sprite.y+sprite.height*.12f,60,20),"RING!",ringText);
            GUI.color=old;
        }

        // ---------- Crew speech bubbles ----------
        void DrawCrewChatter(Worker w,Rect info)
        {
            if(Event.current.type!=EventType.Repaint)return;
            string bark=Marks.Bark(C,w,C.workers.IndexOf(w),animationTime);
            float top=info.y;
            if(w.reaction>0 && w.status!=null && w.status.StartsWith("Scam landed"))
            {
                int dollar=w.status.IndexOf('$');
                string amount=dollar>=0?"+"+w.status.Substring(dollar):"";
                float rise=(4-w.reaction)*12;
                Color old=cashFloat.normal.textColor;
                cashFloat.normal.textColor=new Color(FeedWin.r,FeedWin.g,FeedWin.b,Mathf.Clamp01(w.reaction/2));
                GUI.Label(new Rect(info.xMax-70,info.y-30-rise,120,30),amount,cashFloat);
                cashFloat.normal.textColor=old;
            }
            if(string.IsNullOrEmpty(bark))return;
            var content=new GUIContent(bark);
            float width=Mathf.Min(200,bubbleText.CalcSize(content).x+20),height=bubbleText.CalcHeight(content,width-12)+10;
            var bubble=new Rect(Mathf.Clamp(info.center.x-width*.5f,24,1416-width),Mathf.Max(130,top-height-12),width,height);
            Panel(bubble,new Color(.99f,.98f,.95f,.97f));
            Color previous=GUI.color;GUI.color=new Color(.99f,.98f,.95f,.97f);
            for(int i=0;i<3;i++)GUI.DrawTexture(new Rect(bubble.center.x-12+i*3,bubble.yMax-1+i*3,8-i*2,4),Texture2D.whiteTexture);
            GUI.color=previous;
            GUI.Label(new Rect(bubble.x+6,bubble.y+5,bubble.width-12,bubble.height-10),bark,bubbleText);
        }

        // ---------- Town reactions in the office ----------
        // Drawn over the walls and floor only (the baked room is the mask), beneath
        // furniture and crew, so the room darkens while people and labels stay readable.
        void DrawSirenWash(Rect room)
        {
            if(Event.current.type!=EventType.Repaint || roomArtwork==null)return;
            Color old=GUI.color;
            if(C.lockdown>0){GUI.color=new Color(0,0,.04f,.45f);GUI.DrawTexture(room,roomArtwork);}
            if(C.suspicion>=75 || C.lockdown>0)
            {
                float t=Mathf.Repeat(animationTime,5);
                if(t<1.6f)
                {
                    bool red=(int)(t*5)%2==0;
                    GUI.color=red?new Color(1,.15f,.12f,.22f):new Color(.15f,.35f,1,.22f);
                    GUI.DrawTexture(room,roomArtwork);
                }
            }
            GUI.color=old;
        }
    }
}
