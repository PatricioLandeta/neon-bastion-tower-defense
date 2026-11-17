using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DefenseMusicValidation
{
    private const string Key="NeonBastionMusicValidation";
    private static DefenseGame game;
    private static AudioSource music;
    private static int stage,checks;
    private static float previousTime;
    private static double due,deadline;
    private static bool failed;
    static DefenseMusicValidation()
    {
        EditorApplication.playModeStateChanged+=Mode;
        if(SessionState.GetBool(Key,false)) EditorApplication.update+=Tick;
    }
    [MenuItem("Tools/Neon Bastion/Validate background music")]
    public static void Run()
    {
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene(DefenseSceneBuilder.ScenePath);EditorApplication.EnterPlaymode();
    }
    private static void Mode(PlayModeStateChange mode)
    {
        if(!SessionState.GetBool(Key,false)) return;
        if(mode==PlayModeStateChange.EnteredPlayMode)
        {
            stage=0;checks=0;failed=false;due=EditorApplication.timeSinceStartup+1;deadline=due+20;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        if(mode==PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            if(Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
    private static void Check(bool condition,string message)
    {
        if(!condition) throw new Exception(message);
        checks++;Debug.Log("MUSIC PASS: "+message);
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.timeSinceStartup<due) return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Music validation timed out");
            if(game==null) game=UnityEngine.Object.FindAnyObjectByType<DefenseGame>();
            if(game==null || game.Audio==null) return;
            switch(stage)
            {
                case 0:
                    music=game.Audio.Music;
                    Check(music.clip!=null && music.clip.name=="DearlyBeloved" && music.clip.length>60,"The supplied Dearly Beloved recording imports successfully");
                    Check(music.loop && music.isPlaying && music.time>0 && game.State==DefenseState.Title,"Music actually plays and loops at the title screen");
                    previousTime=music.time;game.StartGame();stage++;break;
                case 1:
                    Check(music==game.Audio.Music && music.isPlaying && music.time>previousTime,"Starting a run keeps the song progressing without restarting it");
                    game.LaunchWave();game.TogglePause();previousTime=music.time;stage++;break;
                case 2:
                    Check(game.Paused && Time.timeScale==0 && music.isPlaying && music.time>previousTime,"Pausing combat keeps the music playing");
                    game.TogglePause();game.ToggleSpeed();
                    Check(Time.timeScale==2 && music.pitch==1,"Double game speed preserves the song's normal pitch");
                    game.StartGame();previousTime=music.time;stage++;break;
                case 3:
                    Check(music==game.Audio.Music && music.isPlaying && music.time>previousTime,"Restarting a run preserves the same progressing music source");
                    game.HUD.OpenTutorial(false);previousTime=music.time;stage++;break;
                case 4:
                    Check(game.HUD.TutorialOpen && music.isPlaying && music.time>previousTime,"The tutorial keeps the background song playing");
                    game.HUD.CloseTutorial();game.LaunchWave();game.EnemyEscaped(game.SpawnEnemy(EnemyKind.Scout));previousTime=music.time;stage++;break;
                case 5:
                    Check(game.State==DefenseState.Lost && music.isPlaying && music.time>previousTime,"The ending screen keeps the background song playing");
                    Check(game.GetComponents<DefenseAudio>().Length==1 && game.GetComponentsInChildren<AudioSource>().Length==33,"Scene transitions do not duplicate music or laser sources");
                    Debug.Log("MUSIC VALIDATION COMPLETE: "+checks+" checks passed");EditorApplication.ExitPlaymode();stage++;return;
            }
            due=EditorApplication.timeSinceStartup+.4;
        }
        catch(Exception error)
        { failed=true;Debug.LogError("MUSIC VALIDATION FAILED: "+error);EditorApplication.ExitPlaymode(); }
    }
}
