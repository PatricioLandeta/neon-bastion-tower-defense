using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DefenseValidation
{
    private const string Key="NeonBastionValidation";
    private static int stage,checks,shotsAtStart;
    private static bool failed,captured;
    private static double due,deadline;
    private static DefenseGame game;
    private static DefenseEnemy testEnemy;
    private static DefenseTower testTower;
    private static Vector3 pausedPosition;
    private static float simulatedDue;

    static DefenseValidation()
    {
        EditorApplication.playModeStateChanged+=Mode;
        if(SessionState.GetBool(Key,false)) EditorApplication.update+=Tick;
    }
    [MenuItem("Tools/Neon Bastion/Validate assignment and campaign")]
    public static void Run()
    {
        SessionState.SetBool(Key,true);failed=false;stage=0;checks=0;captured=false;
        EditorSceneManager.OpenScene(DefenseSceneBuilder.ScenePath);
        EditorApplication.EnterPlaymode();
    }
    private static void Mode(PlayModeStateChange mode)
    {
        if(!SessionState.GetBool(Key,false)) return;
        if(mode==PlayModeStateChange.EnteredPlayMode)
        {
            stage=0;checks=0;due=EditorApplication.timeSinceStartup+1;deadline=due+240;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        if(mode==PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            if(Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
    private static void Check(bool condition,string description)
    {
        if(!condition) throw new Exception(description);
        checks++;Debug.Log("DEFENSE PASS: "+description);
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.timeSinceStartup<due) return;
        double now=EditorApplication.timeSinceStartup;
        try
        {
            if((stage==1 || stage==2 || stage==4) && Time.time<simulatedDue) return;
            if(now>deadline) throw new Exception("Timed out at stage "+stage);
            switch(stage)
            {
                case 0:
                    game=UnityEngine.Object.FindAnyObjectByType<DefenseGame>();
                    Check(game!=null && game.State==DefenseState.Title,"Title screen starts with no active combat");
                    Check(Resources.Load<Mesh>("DefenseMeshes/ArrowHull")!=null && Resources.Load<Mesh>("DefenseMeshes/Spire")!=null,"Custom non-basic mesh assets are baked into the project");
                    Check(Vector3.Dot(game.Route.Tangent(2),game.Route.Tangent(9))<.5f && game.Route.Points.Count>100,"Predetermined path has bends and smoothly sampled corners");
                    bool solved=InterceptSolver.TrySolve(new Vector3(0,3,0),new Vector3(10,.7f,4),new Vector3(0,0,3),17,Physics.gravity,out Vector3 launch,out float time);
                    Vector3 hit=new Vector3(0,3,0)+launch*time+.5f*Physics.gravity*time*time;
                    Check(solved && Vector3.Distance(hit,new Vector3(10,.7f,4)+new Vector3(0,0,3)*time)<.002f && Mathf.Abs(launch.magnitude-17)<.01f,"Predictive ballistic solution intersects a moving target at the configured speed");
                    Check(!InterceptSolver.TrySolve(Vector3.zero,new Vector3(100,0,0),new Vector3(30,0,0),5,Physics.gravity,out _,out _),"Unreachable targets do not produce invalid launch vectors");
                    game.StartGame();
                    Check(game.Credits==320 && game.Towers.Count==0 && game.State==DefenseState.Building,"New game begins with 320 credits and player-placed towers only");
                    Check(!game.TryPlaceAt(new Vector3(-13,0,0)) && game.Credits==320,"Road and unmarked ground reject tower placement without spending credits");
                    game.Choose(TowerKind.Pulse);Check(game.TryPlaceAt(game.Pads[1]),"Player can place a Pulse tower on a marked pad");
                    Check(!game.TryPlaceAt(game.Pads[1]) && game.Towers.Count==1,"Occupied pads cannot receive duplicate towers");
                    game.Choose(TowerKind.Siege);Check(game.TryPlaceAt(game.Pads[3]),"Player can select and place a second tower type");
                    DefenseTower pulse=game.Towers[0],siege=game.Towers[1];
                    Check(pulse.FireRate!=siege.FireRate && pulse.ProjectileSpeed!=siege.ProjectileSpeed && pulse.Height!=siege.Height,"Tower types differ in fire rate, projectile speed and height");
                    game.SelectTower(siege);game.UpgradeSelected();
                    Check(siege.Level==2 && game.Credits==90,"Upgrade spends credits and increases tower level");
                    game.SellSelected();Check(game.Towers.Count==1 && game.Credits==198,"Selling refunds 70 percent of investment and frees the pad");
                    game.StartGame();game.LaunchWave();game.enabled=false;
                    DefenseEnemy scout=game.SpawnEnemy(EnemyKind.Scout),runner=game.SpawnEnemy(EnemyKind.Runner),armored=game.SpawnEnemy(EnemyKind.Armored);
                    Check(runner.Speed>scout.Speed && armored.Speed<scout.Speed && armored.MaxHealth>scout.MaxHealth,"Enemy types have distinct speed and HP statistics");
                    for(int i=0;i<45;i++) game.SpawnEnemy(EnemyKind.Scout).TakeDamage(100);
                    for(int i=0;i<6;i++){game.Choose(TowerKind.Pulse);game.TryPlaceAt(game.Pads[i]);}
                    int credits=game.Credits;game.Choose(TowerKind.Siege);
                    Check(game.Towers.Count==6 && !game.TryPlaceAt(game.Pads[7]) && game.Credits==credits,"Six-tower limit is enforced even with enough credits for more");
                    game.StartGame();game.LaunchWave();
                    testEnemy=game.SpawnEnemy(EnemyKind.Scout);testEnemy.SetDistance(9);testEnemy.enabled=false;
                    Vector3 origin=testEnemy.Body.position+new Vector3(4,1.5f,0);
                    InterceptSolver.TrySolve(origin,testEnemy.Body.position,Vector3.zero,17,Physics.gravity,out Vector3 velocity,out _);
                    DefenseProjectile projectile=DefenseProjectile.Launch(game,origin,velocity,10,false);
                    Check(projectile.Body.useGravity && !projectile.Body.isKinematic && projectile.GetComponent<Collider>()!=null,"Projectiles use dynamic 3D Rigidbody gravity and colliders");
                    simulatedDue=Time.time+.8f;due=now+.1;stage++;break;
                case 1:
                    Check(game.Kills==1 && game.Enemies.Count==0,"A real physics projectile collision damages and destroys an enemy");
                    game.StartGame();game.Choose(TowerKind.Pulse);game.TryPlaceAt(game.Pads[1]);testTower=game.Towers[0];
                    game.LaunchWave();testEnemy=game.SpawnEnemy(EnemyKind.Armored);testEnemy.SetDistance(7);
                    shotsAtStart=testTower.ShotsFired;simulatedDue=Time.time+2.1f;due=now+.1;stage++;break;
                case 2:
                    Check(testEnemy!=null && Vector3.Distance(testEnemy.Body.position,game.Route.Position(testEnemy.DistanceTravelled)+Vector3.up*.7f)<.15f,
                        "Enemy Rigidbody position matches its progress along the curved path");
                    Check(testEnemy.CurrentVelocity.magnitude<=testEnemy.Speed*1.1f,
                        "Enemy current velocity stays consistent with its configured speed");
                    int count=testTower.ShotsFired-shotsAtStart;
                    Check(count>=4 && count<=6,"Tower continuously fires at its configured cadence");
                    Check(testEnemy==null || testEnemy.Health<testEnemy.MaxHealth,"Predictive tower shots hit an actually moving enemy");
                    testEnemy=game.SpawnEnemy(EnemyKind.Runner);testEnemy.SetDistance(16);game.TogglePause();pausedPosition=testEnemy.Body.position;
                    Check(Time.timeScale==0 && !game.IsCombat,"Pause stops combat and simulation time");due=now+.25;stage++;break;
                case 3:
                    Check(Vector3.Distance(testEnemy.Body.position,pausedPosition)<.001f,"Enemies remain still while paused");
                    game.TogglePause();game.ToggleSpeed();Check(Time.timeScale==2,"Fast mode runs at 2x");game.ToggleSpeed();
                    testEnemy.SetDistance(game.Route.Length-.01f);simulatedDue=Time.time+.1f;due=now+.1;stage++;break;
                case 4:
                    Check(game.State==DefenseState.Lost,"An enemy completing its path immediately ends the game");
                    game.StartGame();Check(game.Credits==320 && game.Towers.Count==0 && game.Wave==0 && game.Kills==0,"Restart resets towers, credits, enemies and wave progress");
                    game.enabled=true;
                    BuildCampaignDefense();game.LaunchWave();Time.timeScale=10;
                    Debug.Log("DEFENSE CAMPAIGN: automated full run using only earned credits");
                    due=now+.1;stage++;break;
                case 5:
                    if(game.State==DefenseState.Lost) throw new Exception("Campaign strategy lost in wave "+game.Wave+" with "+game.Towers.Count+" towers, kills="+game.Kills+", shots="+string.Join(",",game.Towers.ConvertAll(t=>t.ShotsFired.ToString())));
                    BuildCampaignDefense();
                    if(game.State==DefenseState.Building) game.LaunchWave();
                    if(!captured && game.Wave>=3 && game.Enemies.Count>=4 && Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0)
                    { Capture();captured=true; }
                    if(game.State==DefenseState.Won)
                    {
                        Check(game.Wave==5 && game.Kills==68,"A full five-wave campaign is winnable using placement, upgrades and earned credits");
                        Check(game.SpawnRemaining==0 && game.Enemies.Count==0,"Victory waits for every spawned and scheduled enemy to be destroyed");
                        Debug.Log("DEFENSE VALIDATION COMPLETE: "+checks+" checks passed");
                        File.WriteAllText(Path.Combine(Application.dataPath,"../VALIDATION_RUN.txt"),checks+" scene checks passed; full campaign completed; kills="+game.Kills);
                        EditorApplication.ExitPlaymode();stage++;
                    }
                    due=now+.05;break;
            }
        }
        catch(Exception error)
        { failed=true;Debug.LogError("DEFENSE VALIDATION FAILED: "+error);Time.timeScale=1;EditorApplication.ExitPlaymode(); }
    }
    private static void BuildCampaignDefense()
    {
        int[] pads={1,3,5,7,0,6};TowerKind[] kinds={TowerKind.Pulse,TowerKind.Siege,TowerKind.Pulse,TowerKind.Siege,TowerKind.Pulse,TowerKind.Siege};
        for(int i=0;i<pads.Length;i++)
        {
            int pad=pads[i];if(game.Towers.Exists(t=>t.PadIndex==pad)) continue;
            if(game.Credits<DefenseTower.Cost(kinds[i])) return;
            game.Choose(kinds[i]);game.TryPlaceAt(game.Pads[pad]);
        }
        foreach(int pad in new[]{3,7,1,5,0,6})
        {
            DefenseTower tower=game.Towers.Find(t=>t.PadIndex==pad);
            if(tower!=null && tower.Level<3 && game.Credits>=tower.UpgradeCost) { game.SelectTower(tower);game.UpgradeSelected();return; }
        }
    }
    [MenuItem("Tools/Neon Bastion/Capture preview during play")]
    public static void Capture()
    {
        if(game==null) game=UnityEngine.Object.FindAnyObjectByType<DefenseGame>();
        if(game==null || game.View==null) return;
        Camera camera=game.View;Canvas canvas=game.HUD.GetComponent<Canvas>();
        RenderTexture previous=camera.targetTexture,oldActive=RenderTexture.active;
        RenderMode mode=canvas.renderMode;Camera oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
        RenderTexture target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;Texture2D image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
            string path=Path.Combine(Application.dataPath,"../NEON_BASTION_PREVIEW.png");File.WriteAllBytes(path,image.EncodeToPNG());Debug.Log("DEFENSE PREVIEW: "+path);
        }
        finally
        {
            camera.targetTexture=previous;RenderTexture.active=oldActive;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
