using System;
using UnityEngine;

namespace ScammerTown
{
    // The Velvet Syndicate makes a move every two days once you matter (25 runs):
    // undercut a scheme, tip off the detective, offer a shady laundering deal, or
    // court your crew. A truce or a race win buys quiet.
    public static class Syndicate
    {
        public const string Boss="Vivienne Velvet";
        public const float MoveEvery=240, OfferSeconds=60, OfferCut=.35f;
        public const int TruceCost=3000;
        public static readonly string[] Moves={"Undercut","Tip-off","Shady offer","Poach"};
        public static bool Active(TycoonState s) => Management.ScamProgress(s)>=25;
        public static float UndercutFactor(TycoonState s,int job) => job==s.undercutJob && s.elapsed<s.undercutUntil?.8f:1;
        public static bool OfferPending(TycoonState s) => s.offerAmount>0 && s.elapsed<s.offerUntil;
        public static int OfferReturn(TycoonState s) => Mathf.RoundToInt(Mathf.Min(s.offerAmount,s.dirty)*(1-OfferCut));
        public static bool TruceActive(TycoonState s) => s.elapsed<s.truceUntil;

        public static void Advance(TycoonState s,Func<float> random,Action<string,float> report)
        {
            if(s.offerAmount>0 && s.elapsed>=s.offerUntil)s.offerAmount=0;
            if(!Active(s))return;
            if(s.rivalMoveAt<=0){s.rivalMoveAt=s.elapsed+MoveEvery;return;}
            if(s.elapsed<s.rivalMoveAt)return;
            s.rivalMoveAt=s.elapsed+MoveEvery;
            if(TruceActive(s))return;
            int move=Mathf.Clamp((int)(random()*Moves.Length),0,Moves.Length-1);
            if(move==2 && s.dirty<1000)move=1;
            Worker target=null;
            if(move==3)
            {
                for(int i=0;i<s.companies.Length;i++)if(Management.Owned(s,i))foreach(var w in s.companies[i].workers)
                    if(!w.courted && (target==null || w.loyalty<target.loyalty))target=w;
                if(target==null)move=0;
            }
            string text;
            switch(move)
            {
                case 0:
                    s.undercutJob=MostUsedScheme(s);s.undercutUntil=s.elapsed+Rules.DaySeconds;
                    text=Boss+" is flooding the market with knock-off "+Management.Jobs[s.undercutJob].name+". Takes −20% for a day.";
                    break;
                case 1:
                    Heat.Add(s,8);
                    text="An anonymous tip lands on "+Heat.Detective+"'s desk. It's on velvet stationery. Case file +8.";
                    break;
                case 2:
                    s.offerAmount=Mathf.Min(5000,Mathf.FloorToInt(s.dirty/100)*100);s.offerUntil=s.elapsed+OfferSeconds;
                    text=Boss+" offers to wash $"+s.offerAmount.ToString("N0")+" of your dirty cash right now. She keeps 35%.";
                    break;
                default:
                    target.loyalty=Mathf.Max(0,target.loyalty-15);
                    if(target.loyalty<20)target.courted=true;
                    text="The Syndicate slipped "+target.name+" a business card. Loyalty −15.";
                    break;
            }
            report(text,0);Underworld.Announce(s,text);
        }
        static int MostUsedScheme(TycoonState s)
        {
            var counts=new int[Management.Jobs.Length];
            for(int i=0;i<s.companies.Length;i++)if(Management.Owned(s,i))foreach(var w in s.companies[i].workers)if(w.assignment>=0 && w.assignment<counts.Length)counts[w.assignment]++;
            int best=0;for(int j=1;j<counts.Length;j++)if(counts[j]>counts[best])best=j;
            return best;
        }
        public static bool AcceptOffer(TycoonState s)
        {
            if(s.paused || !OfferPending(s))return false;
            float amount=Mathf.Min(s.offerAmount,s.dirty),cut=amount*OfferCut;
            s.dirty-=amount;s.cash-=cut;s.dirty=Mathf.Min(s.dirty,s.cash);s.laundered+=amount-cut;s.offerAmount=0;
            return true;
        }
        public static bool DeclineOffer(TycoonState s)
        {
            if(!OfferPending(s))return false;
            s.offerAmount=0;return true;
        }
        public static bool Truce(TycoonState s)
        {
            if(s.paused || !Active(s) || TruceActive(s) || !Rules.Pay(s,TruceCost))return false;
            s.truceUntil=s.elapsed+3*Rules.DaySeconds;s.undercutUntil=0;return true;
        }
        public static void OnRaceWon(TycoonState s) => s.truceUntil=Mathf.Max(s.truceUntil,s.elapsed+2*Rules.DaySeconds);
    }
}
