using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        void DrawTownDecisions()
        {
            DrawCaseFile();
            int today=LivingTown.EventAt(state.elapsed);
            Text("DAY "+state.day+" · "+LivingTown.Events[today]);
            Text(LivingTown.EventEffects[today],true);
            Text("Changes in "+Mathf.CeilToInt(Rules.DaySeconds-state.elapsed%Rules.DaySeconds)+"s. Events repeat every five days. Current runs keep their original terms.",true);
            if(state.empireRank>=1)Text("NEXT · "+LivingTown.Events[(today+1)%5]+" · "+LivingTown.EventEffects[(today+1)%5],true);
            else Text("Reach Street operator in Goals to unlock the event forecast.",true);
            Space();Text("SUSPICION · "+Mathf.CeilToInt(C.suspicion)+" / 100");
            Text("Repeated schemes build resistance: lower success and more suspicion. Resistance fades over time. Shelving a scheme for 60s removes 20 resistance immediately; active runs finish.",true);
            Text("At 100 suspicion: automatic office cooldown and loss of "+(C.potProtected?"half":"all")+" the unbanked Moonshot pot. Your cash balance is safe.",true);
            Action(C.lockdown>0?"Office cooldown · "+Mathf.CeilToInt(C.lockdown)+"s":"Lay low · pause 30s, reduce suspicion by 20",()=>{if(OfficeProgression.LayLow(state))Save();},CanAct && C.lockdown<=0);
            Action("Use cautious pace · 20% slower, 30% less suspicion",()=>{if(EmpireProgression.SetPace(state,1))Save();},CanAct && C.operationPace!=1);
            Action("Use balanced pace",()=>{if(EmpireProgression.SetPace(state,0))Save();},CanAct && C.operationPace!=0);
            Text("Push your luck: new jobs started in the next 30s pay +25% on success but create 75% more suspicion. 90s between pushes.",true);
            Action(state.elapsed<C.pushUntil?"Push active · "+Mathf.CeilToInt(C.pushUntil-state.elapsed)+"s":state.elapsed<C.pushCooldown?"Push cooling down · "+Mathf.CeilToInt(C.pushCooldown-state.elapsed)+"s":"Push your luck for 30s",()=>{if(LivingTown.Push(state))Save();},CanAct && C.lockdown<=0 && state.elapsed>=C.pushCooldown);
            Space();Text("MOONSHOT POT · "+Money(C.moonshotPot));
            Text("Successful Moonshot runs add 20% extra here. Banking is free and does not count as another completed scam.",true);
            PrimaryAction("Bank "+Money(C.moonshotPot),()=>{int reward=LivingTown.CashOut(state);if(reward>0){RewardFeedback(reward);Save();}},CanAct && C.moonshotPot>0);
            Action("Return to schemes",()=>Navigate("Scams"));
        }
        void DrawWorkerPersonality(Worker w)
        {
            Text(LivingTown.Traits[w.trait]+" · "+LivingTown.TraitEffects[w.trait]);
            if(w.job>=0)
            {
                Text(LivingTown.WorkerMechanic(w,w.job),true);
                if(w.quoteVersion>0)Text("CURRENT RUN · "+Money(w.quotedPay)+" success / "+Money(w.quotedPartial)+" partial · "+w.quotedChance.ToString("0.0")+"% success · +"+w.quotedSuspicion.ToString("0.0")+" suspicion on success",true);
            }
            Text("A 12s coffee break restores at least 18 mood. Break-room upgrades improve recovery and automatically send unhappy crew on breaks.",true);
            Action(w.breakRemaining>0?"On break · "+Mathf.CeilToInt(w.breakRemaining)+"s":w.breakQueued?"Break queued after this run":"Give a coffee break after this run",()=>{if(LivingTown.RequestBreak(state,w))Save();},CanAct && w.breakRemaining<=0 && !w.breakQueued && !w.onCall && !w.Held(state));
        }
        void DrawEmpireGoals()
        {
            Text("EMPIRE · "+(state.empireRank==0?"New crew":LivingTown.RankNames[state.empireRank-1]));
            if(state.empireRank<5)
            {
                int rank=state.empireRank;
                Text("Next: "+LivingTown.RankNames[rank]+" · "+Management.ScamProgress(state)+" / "+LivingTown.RankRuns[rank]+" scam runs"+(rank>=2?" · "+state.rivalWins+" / "+(rank-1)+" rival wins":""),true);
                Text(LivingTown.RankRewards[rank],true);
                PrimaryAction("Claim empire rank",()=>{if(LivingTown.ClaimRank(state)){state.storyRank=state.empireRank;storyModal=true;Save();Notify("Empire rank unlocked!");}},CanAct && LivingTown.RankReady(state));
            }
            else Text("Town legend reached. Keep building your offices and taking tougher rival challenges.",true);
            Space();Text("RIVAL · VELVET SYNDICATE");
            Text("A simulated race across all your offices. Finish the target number of scam runs within 180s. The rival's progress is shown below. No entry fee or loss penalty.",true);
            if(state.rivalActive)
            {
                Text("Your crew: "+(state.scamsCompleted-state.rivalStartRuns)+" / "+state.rivalTarget+" · Rival: "+LivingTown.RivalProgress(state)+" / "+state.rivalTarget);
                Text(Mathf.CeilToInt(state.rivalDeadline-state.elapsed)+"s left · Reward "+Money(LivingTown.RivalReward(state)),true);
            }
            else
            {
                if(state.rivalResult>0)Text(state.rivalResult==1?"You won the last race!":"The rival won the last race. Grow your crew and try again.",true);
                Action("Challenge rival · "+(12+6*Mathf.Min(state.rivalWins,10))+" runs / 180s",()=>{if(LivingTown.StartRival(state))Save();},CanAct && Management.ScamProgress(state)>=25 && state.elapsed>=state.rivalCooldown);
                if(Management.ScamProgress(state)<25)Text("Unlocks at 25 scam runs.",true);
                else if(state.elapsed<state.rivalCooldown)Text("Next challenge in "+Mathf.CeilToInt(state.rivalCooldown-state.elapsed)+"s",true);
            }
            DrawSyndicate();
            DrawHeadquarters();Space();Text("STARTER GOALS");
        }
        void DrawHeadquarters()
        {
            Space();Text("HARBOR HEADQUARTERS");
            Text("District contender rank + Harbor ownership unlocks a $15,000 renovation (clean money): teal-and-gold office styling and 10% faster jobs in Harbor.",true);
            Action(state.companies[2].headquarters?"Headquarters complete":"Renovate Harbor · $15,000",()=>{if(LivingTown.Renovate(state)){Save();Notify("Harbor headquarters ready. Visit it in City.");}},CanAct && state.empireRank>=3 && state.harborOwned && !state.companies[2].headquarters && Laundering.Clean(state)>=15000);
        }
    }
}
