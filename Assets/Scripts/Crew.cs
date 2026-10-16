using System;
using UnityEngine;

namespace ScammerTown
{
    // Loyalty: paid, happy crew stay loyal. Disloyal crew talk to the detective,
    // and the Velvet Syndicate courts the unhappiest. Courting is announced first.
    public static class Crew
    {
        public const int RaiseAmount=10, MaxSalary=150;
        public static string Standing(Worker w) => w.courted?"Being courted by the Velvet Syndicate":w.loyalty>=70?"Loyal":w.loyalty>=45?"Steady":w.loyalty>=30?"Wavering":"Shifty · might talk";
        public static bool Raise(TycoonState s,Worker w)
        {
            if(s.paused || !s.Current.workers.Contains(w) || w.salary>=MaxSalary)return false;
            w.salary=Mathf.Min(MaxSalary,w.salary+RaiseAmount);w.loyalty=Mathf.Min(100,w.loyalty+12);
            if(w.loyalty>=20)w.courted=false;
            return true;
        }
        public static void Bonus(Worker w)
        {
            w.mood=Mathf.Min(100,w.mood+20);w.loyalty=Mathf.Min(100,w.loyalty+5);
            if(w.loyalty>=20)w.courted=false;
        }
        public static float DailyChange(Worker w,bool shortfall,bool held)
        {
            float delta=shortfall?-10:w.mood>=60?4:0;
            if(w.mood<40)delta-=6;
            if(held)delta-=8;
            delta+=(w.salary-35)/10f*2;
            if(w.skill>=4 && w.salary<60)delta-=3;
            return delta;
        }
        public static void Daily(TycoonState s,bool shortfall,Func<float> random,Action<string,float> report)
        {
            for(int i=0;i<s.companies.Length;i++)
            {
                if(!Management.Owned(s,i) || s.companies[i].workers==null)continue;
                var c=s.companies[i];
                foreach(var w in c.workers.ToArray())
                {
                    w.loyalty=Mathf.Clamp(w.loyalty+DailyChange(w,shortfall,w.Held(s)),0,100);
                    if(w.courted)
                    {
                        if(w.loyalty>=20)w.courted=false;
                        else if(c.workers.Count>1 && !w.onCall)
                        {
                            Management.RemoveWorker(c,w);
                            string left=w.name+" left to join the Velvet Syndicate. They took the good stapler.";
                            report(left,0);Underworld.Announce(s,left);continue;
                        }
                    }
                    else if(w.loyalty<20 && Syndicate.Active(s))
                    {
                        w.courted=true;
                        string courted="The Velvet Syndicate is courting "+w.name+". Win them back before payday.";
                        report(courted,0);Underworld.Announce(s,courted);
                    }
                    if(w.loyalty<30 && random()<(30-w.loyalty)/40f)
                    {
                        Heat.Add(s,12);w.loyalty=35;
                        string talk="Somebody's been chatting with "+Heat.Detective+". Case file +12.";
                        report(talk,0);Underworld.Announce(s,talk);
                    }
                }
            }
        }
    }
}
