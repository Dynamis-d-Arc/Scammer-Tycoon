using ScammerTown;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A paused, save-free Play session for checking arrangement without spending player progress.
[InitializeOnLoad]
public static class FurniturePreview
{
    const string Key="ScammerTown.SafeFurniturePreview";
    const string EmpireKey="ScammerTown.EmpirePreview";
    const string TownKey="ScammerTown.LivingTownPreview";
    static FurniturePreview()
    {
        if(SessionState.GetBool(Key,false))ScammerTownGame.SuppressDiskSaves=true;
        EditorApplication.playModeStateChanged+=phase=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(phase==PlayModeStateChange.EnteredPlayMode)
            {
                ScammerTownGame.SuppressDiskSaves=true;
                var game=Object.FindFirstObjectByType<ScammerTownGame>();
                if(game!=null)
                {
                    if(SessionState.GetBool(EmpireKey,false))
                    {
                        game.state=new TycoonState{cash=50000,scamsCompleted=40,scamVersion=3};
                        game.state.Current.level=2;game.state.Current.desktops=4;game.state.Current.laptops=2;game.state.Current.employeeCount=6;
                        Management.Ensure(game.state);
                        if(SessionState.GetBool(TownKey,false))
                        {
                            var s=game.state;s.cash=100000;s.scamsCompleted=150;s.empireRank=3;s.rivalWins=1;s.branchOwned=s.harborOwned=true;
                            s.Current.computerUpgrade=3;s.Current.moonshotPot=1750;s.Current.suspicion=72;s.Current.deliveryUntil=10;
                            s.Current.workers[0].breakRemaining=7;s.Current.workers[1].schemeSteps[3]=2;
                            Management.Ensure(s);s.paused=true;
                        }
                    }
                    else game.state.paused=true;
                }
            }
            if(phase==PlayModeStateChange.EnteredEditMode)
            {SessionState.SetBool(Key,false);SessionState.SetBool(EmpireKey,false);SessionState.SetBool(TownKey,false);ScammerTownGame.SuppressDiskSaves=false;}
        };
    }
    [MenuItem("Scammer Town/Preview furniture without saving %#j")]
    public static void Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Key,true);ScammerTownGame.SuppressDiskSaves=true;
        EditorApplication.isPlaying=true;
    }
    [MenuItem("Scammer Town/Preview upgrades without saving %#u")]
    public static void BeginEmpire()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/ScammerTown.unity")
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Scenes/ScammerTown.unity");
        }
        SessionState.SetBool(EmpireKey,true);Begin();
    }
    [MenuItem("Scammer Town/Preview living town without saving %#l")]
    public static void BeginLivingTown()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(TownKey,true);BeginEmpire();
    }
}
