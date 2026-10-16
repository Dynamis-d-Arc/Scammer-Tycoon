using System;
using System.Reflection;
using ScammerTown;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectSetup
{
    [MenuItem("Scammer Town/Create or rebuild main scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera"; camera.orthographic = true;
        camera.backgroundColor = new Color(.035f,.055f,.07f); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0,0,-10);
        camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Scammer Town • Game Controller").AddComponent<ScammerTownGame>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/ScammerTown.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/ScammerTown.unity",true) };
        PlayerSettings.companyName = "Dynamis d Arc";
        PlayerSettings.productName = "Scammer Town";
        PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        AssetDatabase.SaveAssets();
        Debug.Log("SCAMMER_TOWN_SCENE_READY");
    }

    [MenuItem("Scammer Town/Run gameplay checks")]
    public static void Verify()
    {
        int checks=0;
        Action<bool,string> check = (ok,message) => { if(!ok) throw new Exception("FAILED: " + message); checks++; };
        for(int tier=1;tier<=5;tier++) check(Rules.Chance(tier)==new[]{20,30,40,50,67}[tier-1],"tier rate");
        for(int level=1;level<=Rules.MaxOfficeLevel;level++) check(new Company{level=level}.Size==level*5,"building size");
        var state = new TycoonState();
        check(Rules.Valid(state),"new save valid");
        check(!Rules.Pay(state,5000) && state.cash==4820,"cannot overspend");
        check(Rules.Pay(state,820) && state.cash==4000,"purchase deducts money");
        check(!Rules.Pay(state,-1),"negative purchase rejected");
        check(Rules.Valid(JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state))),"save roundtrip");
        state.cash=float.NaN; check(!Rules.Valid(state),"non-finite save rejected");
        state=new TycoonState();state.companies[0].level=Rules.MaxOfficeLevel+1;check(!Rules.Valid(state),"invalid building rejected");
        check(Resources.Load<Texture2D>("ScamDen/scamden_basement_sheet")!=null,"basement atlas available");
        check(Resources.Load<Texture2D>("ScamDen/scamden_neon_sheet")!=null,"neon atlas available");
        var go = new GameObject("Temporary gameplay verification");
        var game=go.AddComponent<ScammerTownGame>();
        Action<string,object[]> call=(name,args)=>typeof(ScammerTownGame).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
        // Redirecting saves during checks is handled by the test-only guard in the game.
        ScammerTownGame.SuppressDiskSaves=true;
        try
        {
            game.state=new TycoonState{branchOwned=true};
            game.state.companies[0].laptops=3;game.state.companies[0].desktops=2;
            game.state.companies[1].desktops=4;game.state.companies[1].burners=0;
            call("Transfer",new object[0]);
            check(game.state.active==1 && game.state.Current.Stations==5 && game.state.companies[0].Stations==4,"transfer respects destination capacity");
            check(Rules.Valid(game.state),"post-transfer state valid");
            game.state=new TycoonState { branchOwned=true }; Rules.MigrateStaff(game.state);
            call("OpenCity",new object[0]);
            call("SelectCityApartment",new object[]{1});call("VisitSelectedApartment",new object[0]);
            check(game.state.active==1 && game.state.neon,"selected Neon marker opens Neon apartment");
            call("OpenCity",new object[0]);
            call("SelectCityApartment",new object[]{0});call("VisitSelectedApartment",new object[0]);
            check(game.state.active==0 && !game.state.neon,"selected Old Town marker returns from Neon to original apartment");
            call("OpenCity",new object[0]);call("SelectCityApartment",new object[]{1});call("VisitApartment",new object[]{0});
            check(game.state.active==0,"explicit Old Town card overrides selected Neon marker");
        }
        finally { ScammerTownGame.SuppressDiskSaves=false; UnityEngine.Object.DestroyImmediate(go); }
        checks += PlacementChecks.Run();
        checks += PlacementInteractionChecks.Run();
        checks += FurnishingChecks.Run();
        checks += CityChecks.Run();
        checks += ManagementChecks.Run();
        checks += IdleChecks.Run();
        checks += ReliabilityChecks.Run();
        checks += PixelArtImport.Verify();
        Debug.Log("SCAMMER_TOWN_CHECKS_PASSED: " + checks);
    }
    public static void SetupAndVerify() { CreateScene(); Verify(); }
    public static void VerifyAndBuild() { Verify(); BuildMac(); }
    public static void BuildMac()
    {
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/ScammerTown.unity"}, locationPathName="Builds/Scammer Town.app",
            target=BuildTarget.StandaloneOSX, options=BuildOptions.Development
        });
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: " + result.summary.result);
        Debug.Log("SCAMMER_TOWN_BUILD_PASSED");
    }
}
