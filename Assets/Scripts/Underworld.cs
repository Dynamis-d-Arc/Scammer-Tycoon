using System;
using UnityEngine;

namespace ScammerTown
{
    // Shared pieces of the underworld systems: town news for the live feed,
    // save validation, recurring characters and the rank story beats.
    public static class Underworld
    {
        public static event Action<TycoonState,string> News;
        public static void Announce(TycoonState s,string text) => News?.Invoke(s,text);

        public static Mark DetectiveMark => new Mark{first="Dolores",last="Plodd",type="Detective",belief="is very close. She says.",seed=90210,typeIndex=3};
        public static Mark BossMark => new Mark{first="Vivienne",last="Velvet",type="Syndicate boss",belief="sends fruit baskets. All the fruit is fake.",seed=4040,typeIndex=4};

        public static readonly string[] StoryTitles={"","Street operator","Crew boss","District contender","Harbor magnate","Town legend"};
        public static readonly string[] Story={
            "",
            "Word gets around the laundromat: there's a new crew in the basement. Detective Plodd writes your name on a sticky note. Then she loses the sticky note.",
            "Vivienne Velvet sends a fruit basket to the office. Every single fruit is fake. It's a message. The crew eats the basket anyway.",
            "The Gazette runs a profile on 'the mysterious phone people'. Your mother calls to ask if it's you. You say no. She doesn't believe you either.",
            "You own a loft by the water and a pizza place nobody eats at. Plodd buys a bigger corkboard. The Syndicate starts copying your scripts.",
            "The town council names a pothole after you. Anonymously. Plodd retires to write a memoir. It is mostly about you."
        };

        static bool Number(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        public static bool Valid(TycoonState s)
        {
            if(s.underworldVersion<0 || s.underworldVersion>1)return false;
            if(s.underworldVersion==0)return true;
            if(!Number(s.caseFile) || s.caseFile<0 || s.caseFile>100 || !Number(s.raidSeconds))return false;
            if(s.raidOffice<-1 || s.raidOffice>=s.companies.Length || s.lastRaidOffice<-1 || s.lastRaidOffice>=s.companies.Length)return false;
            if(s.raidSeconds>=0 && (s.raidOffice<0 || !Management.Owned(s,s.raidOffice)))return false;
            if(!Number(s.dirty) || s.dirty<0 || s.dirty>s.cash+.5f || !Number(s.laundered) || s.laundered<0)return false;
            if(s.fronts<0 || s.fronts>=1<<Laundering.Fronts.Length || s.raidCount<0 || s.bribes<0 || s.offerAmount<0)return false;
            if(s.undercutJob<-1 || s.undercutJob>=Management.Jobs.Length || s.storyRank<0 || s.storyRank>5)return false;
            foreach(float f in new[]{s.lastRaidAt,s.rivalMoveAt,s.undercutUntil,s.truceUntil,s.offerUntil})if(!Number(f))return false;
            foreach(var c in s.companies)
            {
                if(c==null || c.workers==null)continue;
                foreach(var w in c.workers)
                    if(w==null || !Number(w.loyalty) || w.loyalty<0 || w.loyalty>100 || !Number(w.heldUntil) || w.heldUntil<0 || !Number(w.callReadyAt) || w.callReadyAt<0)return false;
            }
            return true;
        }
    }
}
