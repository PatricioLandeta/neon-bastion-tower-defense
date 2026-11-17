using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Opening balance regression: normal purchases, no upgrades, no injected damage or cash.
[InitializeOnLoad]
public static class DefenseOpeningValidation
{
    private const string Key="NeonBastionOpeningCheck";
    private static DefenseGame game;
    private static int scenario,passed;
    private static bool running;
    private static double deadline;
    private static readonly int[] Pads={0,1,3,0,1,3};
    static DefenseOpeningValidation() { EditorApplication.playModeStateChanged+=Mode; }
    [MenuItem("Tools/Neon Bastion/Validate opening without upgrades")]
    public static void Run()
    {
        scenario=0;passed=0;running=false;SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene(DefenseSceneBuilder.ScenePath);EditorApplication.EnterPlaymode();
    }
    private static void Mode(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        { deadline=EditorApplication.timeSinceStartup+150;EditorApplication.update+=Tick; }
        if(state==PlayModeStateChange.EnteredEditMode)
        { SessionState.SetBool(Key,false);EditorApplication.update-=Tick;if(Application.isBatchMode) EditorApplication.Exit(passed==6 ? 0 : 1); }
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying) return;
        try
        {
            if(game==null) game=UnityEngine.Object.FindAnyObjectByType<DefenseGame>();
            if(game==null || game.HUD==null) return;
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Opening validation timed out");
            if(!running)
            {
                if(scenario>=6)
                { Debug.Log("OPENING SUMMARY: "+passed+" / 6 no-upgrade openings won");EditorApplication.ExitPlaymode();return; }
                game.StartGame();TowerKind kind=scenario<3 ? TowerKind.Pulse : TowerKind.Siege;
                game.Choose(kind);
                if(scenario==0 && Array.IndexOf(Environment.GetCommandLineArgs(),"-captureOpening")>=0) DefenseValidation.CaptureTo("OPENING_GUIDE_PREVIEW.png");
                if(!game.TryPlaceAt(game.Pads[Pads[scenario]])) throw new Exception("Could not buy opening tower");
                game.LaunchWave();Time.timeScale=10;running=true;
                Debug.Log("OPENING START: "+kind+" pad "+Pads[scenario]+"; no upgrades");
            }
            else if(game.State==DefenseState.Lost || (game.State==DefenseState.Building && game.Wave==1))
            {
                bool won=game.State==DefenseState.Building;
                if(won) passed++;
                Debug.Log("OPENING RESULT: "+(scenario<3 ? "Pulse" : "Siege")+" pad "+Pads[scenario]+" => "+(won ? "PASS" : "FAIL")+"; kills="+game.Kills);
                scenario++;running=false;
            }
        }
        catch(Exception error) { Debug.LogError("OPENING ERROR: "+error);EditorApplication.ExitPlaymode(); }
    }
}
