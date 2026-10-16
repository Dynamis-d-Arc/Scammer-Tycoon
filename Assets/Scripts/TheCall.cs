using System;
using UnityEngine;

namespace ScammerTown
{
    // One live call the player has taken over from a crew member.
    public sealed class CallSession
    {
        public int office, job, turn, lastApproach=-1, outcome, payout, seed;
        public Worker worker;
        public Mark mark;
        public float trust, doubt;
        public readonly int[] options=new int[3], lines=new int[3];
        public string markLine, reaction;
        public bool Over => outcome!=TheCall.Ongoing;
    }

    // The Call: talk a mark into it by choosing the right approach each turn.
    // Each mark type has tastes; tells hint at them. Repeating yourself costs trust.
    public static class TheCall
    {
        public const int Ongoing=0, Closed=1, HungUp=2, Stalled=3, Abandoned=4;
        public const int MaxTurns=6, Cooldown=45;
        public static readonly string[] Approaches={"Charm","Urgency","Proof","Sweeten"};
        public static readonly string[] ApproachHints={"Flatter them","Rush them","'Prove' it","Sweeten the deal"};
        // Taste per mark type (Greedy, Lonely, Gullible, Skeptical, Show-off, Bored) for
        // each approach: 3 favorite, 2 good, 1 neutral, 0 bad, -1 terrible.
        static readonly int[][] Taste={
            new[]{0,2,1,3},new[]{3,-1,1,1},new[]{2,3,0,2},new[]{0,-1,3,0},new[]{3,1,0,2},new[]{0,3,-1,1}
        };
        static readonly string[][] Lines={
            new[]{"You sound like a very smart person, {mark}.","Honestly? You're my favorite caller today.","I can tell you've got great taste.","My manager said to only call the special ones."},
            new[]{"This offer expires in four minutes.","Two other people are on hold for this.","I really shouldn't even be telling you this…","Act now, {mark}, before my boss finds out!"},
            new[]{"I'll read you my badge number: 0000001.","We're fully certified. By us.","Check our website. It has a padlock icon!","I'll fax you a very official certificate."},
            new[]{"For you? I'll throw in a second one.","Act today and we waive the fee.","VIP members get the gold version.","There's a mystery bonus. It's a good one."}
        };
        static readonly string[][] Tells={
            new[]{"So what's in it for me, exactly?","Can I get two?","Is there a bigger one?"},
            new[]{"It's nice to hear a voice. Go on…","Nobody calls me anymore.","Do you have a minute to chat?"},
            new[]{"Oh my goodness. Really?!","My horoscope said this would happen!","Is it always this easy?"},
            new[]{"How do I know this is real?","My nephew says these are all scams.","Can you put that in writing?"},
            new[]{"Will the neighbors see it?","Is it the premium version?","Do important people have this?"},
            new[]{"Is this going to take long?","I was about to watch paint dry.","Get to the exciting part."}
        };
        static readonly string[] Reactions={"{mark}: \"Wait. What?\"","{mark} goes quiet. The suspicious kind of quiet.","{mark} hums politely.","{mark} leans in.","{mark} is practically glowing."};

        public static string Blocker(TycoonState s,int office,Worker w)
        {
            if(s.paused)return "Resume the game first.";
            if(!Management.Owned(s,office) || !s.companies[office].workers.Contains(w))return "Not in this office.";
            if(w.Held(s))return w.name+" is in custody.";
            if(w.job<0)return w.name+" isn't on a call right now. Assign a scheme first.";
            if(w.onCall)return "You're already on this call.";
            if(w.breakRemaining>0)return w.name+" is on a coffee break.";
            if(s.companies[office].lockdown>0)return "The office is laying low.";
            if(!Management.HasPlacedDesk(s.companies[office],w))return w.name+" needs a placed desk.";
            if(s.elapsed<w.callReadyAt)return "Catch your breath · "+Mathf.CeilToInt(w.callReadyAt-s.elapsed)+"s";
            return null;
        }
        public static bool CanTake(TycoonState s,int office,Worker w) => Blocker(s,office,w)==null;
        public static CallSession Begin(TycoonState s,int office,Worker w,int seed)
        {
            if(!CanTake(s,office,w))return null;
            var c=s.companies[office];
            var call=new CallSession{office=office,worker=w,job=w.job,seed=seed&0x3fffffff,mark=Marks.For(w.markSeed,w.job)};
            call.trust=Mathf.Clamp(15+Marks.Stage(w)*8+(w.skill-1)*3,0,60);
            call.doubt=Mathf.Clamp(10+LivingTown.Resistance(c,w.job)*.3f,0,60);
            call.reaction=call.mark.first+" picks up.";
            w.onCall=true;Deal(call);
            return call;
        }
        static void Deal(CallSession call)
        {
            var random=new System.Random(call.seed*31+call.turn);
            int skip=random.Next(Approaches.Length),k=0;
            for(int a=0;a<Approaches.Length;a++)if(a!=skip){call.options[k]=a;call.lines[k]=random.Next(Lines[a].Length);k++;}
            call.markLine=Tells[call.mark.typeIndex][random.Next(Tells[call.mark.typeIndex].Length)];
        }
        public static int Effect(Mark mark,int approach) => Taste[mark.typeIndex][approach];
        public static string Line(CallSession call,int option) => Lines[call.options[option]][call.lines[option]].Replace("{mark}",call.mark.first);
        public static float CloseMultiplier(CallSession call) => 1.5f+.5f*(1-call.doubt/100f);
        public static bool Choose(TycoonState s,CallSession call,int option,Action<string,float> report)
        {
            if(call==null || call.Over || s.paused || option<0 || option>=call.options.Length)return false;
            var w=call.worker;
            if(!w.onCall || w.job!=call.job){call.outcome=Abandoned;w.onCall=false;return false;}
            int approach=call.options[option],taste=Effect(call.mark,approach);
            float trust=taste>=3?34:taste==2?24:taste==1?12:0,doubt=taste>=3?0:taste==2?4:taste==1?10:taste==0?22:36;
            bool repeat=approach==call.lastApproach;
            if(repeat){trust*=.5f;doubt+=14;}
            if(trust>0)trust+=(w.skill-1)*2;
            call.trust=Mathf.Min(100,call.trust+trust);call.doubt=Mathf.Min(100,call.doubt+doubt);
            call.lastApproach=approach;call.turn++;
            call.reaction=(repeat?"{mark}: \"You already said that.\"":Reactions[taste+1]).Replace("{mark}",call.mark.first);
            if(call.trust>=100)Finish(s,call,Closed,report);
            else if(call.doubt>=100)Finish(s,call,HungUp,report);
            else if(call.turn>=MaxTurns)Finish(s,call,Stalled,report);
            else Deal(call);
            return true;
        }
        static void Finish(TycoonState s,CallSession call,int outcome,Action<string,float> report)
        {
            var w=call.worker;var c=s.companies[call.office];
            call.outcome=outcome;w.callReadyAt=s.elapsed+(outcome==Stalled?20:Cooldown);
            if(outcome==Closed)
            {
                float multiplier=CloseMultiplier(call);
                call.payout=Mathf.RoundToInt((w.quoteVersion>0?w.quotedPay:EmpireProgression.Pay(c,call.office,call.job,true))*multiplier);
                // A smooth close draws half the usual suspicion and calms the town.
                Management.ResolveRun(s,call.office,w,true,multiplier,.5f,s.elapsed,report);
                c.townResistance[call.job]=Mathf.Max(0,c.townResistance[call.job]-15);
                call.markLine="Where do I send the money?";
            }
            else if(outcome==HungUp)
            {
                call.payout=w.quoteVersion>0?w.quotedPartial:EmpireProgression.Pay(c,call.office,call.job,false);
                // A blown call gets reported: extra suspicion and wariness.
                Management.ResolveRun(s,call.office,w,false,1,1.5f,s.elapsed,report);
                c.townResistance[call.job]=Mathf.Min(100,c.townResistance[call.job]+5);
                call.markLine="*click*";
            }
            else
            {
                w.onCall=false;
                call.markLine="I need to think about it. Call me later.";
            }
        }
        public static void Abandon(TycoonState s,CallSession call)
        {
            if(call==null || call.Over)return;
            call.outcome=Abandoned;call.worker.onCall=false;call.worker.callReadyAt=s.elapsed+20;
        }
    }
}
