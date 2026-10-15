using ScammerTown;
using UnityEditor;
using UnityEngine;

// A paused, save-free Play session for checking arrangement without spending player progress.
[InitializeOnLoad]
public static class FurniturePreview
{
    const string Key="ScammerTown.SafeFurniturePreview";
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
                if(game!=null)game.state.paused=true;
            }
            if(phase==PlayModeStateChange.EnteredEditMode)
            {SessionState.SetBool(Key,false);ScammerTownGame.SuppressDiskSaves=false;}
        };
    }
    [MenuItem("Scammer Town/Preview furniture without saving %#j")]
    public static void Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Key,true);ScammerTownGame.SuppressDiskSaves=true;
        EditorApplication.isPlaying=true;
    }
}
