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
    private static bool failed,captured,midpointChecked;
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
        SessionState.SetBool(Key,true);failed=false;stage=0;checks=0;captured=false;midpointChecked=false;
        EditorSceneManager.OpenScene(DefenseSceneBuilder.ScenePath);
        EditorApplication.EnterPlaymode();
    }
    private static void Mode(PlayModeStateChange mode)
    {
        if(!SessionState.GetBool(Key,false)) return;
        if(mode==PlayModeStateChange.EnteredPlayMode)
        {
            stage=0;checks=0;due=EditorApplication.timeSinceStartup+1;deadline=due+300;
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
            if(now>deadline) throw new Exception("Timed out at stage "+stage);
            if((stage==2 || stage==3 || stage==5) && Time.time<simulatedDue) return;
            switch(stage)
            {
                case 0:
                    game=UnityEngine.Object.FindAnyObjectByType<DefenseGame>();
                    Check(game!=null && game.State==DefenseState.Title,"Title starts with combat stopped");
                    Check(Resources.Load<Mesh>("DefenseMeshes/ArrowHull")!=null,"Custom non-basic mesh assets are available");
                    Check(Vector3.Dot(game.Route.Tangent(2),game.Route.Tangent(9))<.5f,"Enemy route contains predetermined bends");
                    bool solved=InterceptSolver.TrySolve(new Vector3(0,3,0),new Vector3(10,.7f,4),new Vector3(0,0,3),17,Physics.gravity,out Vector3 launch,out float time);
                    Vector3 hit=new Vector3(0,3,0)+launch*time+.5f*Physics.gravity*time*time;
                    Check(solved && Vector3.Distance(hit,new Vector3(10,.7f,4)+new Vector3(0,0,3)*time)<.002f && Mathf.Abs(launch.magnitude-17)<.01f,"Predictive ballistic solution intersects a moving target at the launch speed");
                    Check(!InterceptSolver.TrySolve(Vector3.zero,new Vector3(100,0,0),new Vector3(30,0,0),5,Physics.gravity,out _,out _),"Unreachable targets return no launch solution");
                    game.StartGame();
                    Check(game.Credits==110 && game.PlacementsRemaining==1 && game.Towers.Count==0,"New game starts with $110 and one wave-1 placement");
                    Check(!game.TryPlaceAt(new Vector3(-13,0,0)) && game.Credits==110 && game.PlacementsRemaining==1,"Invalid placement spends neither money nor allowance");
                    game.Choose(TowerKind.Pulse);Check(game.TryPlaceAt(game.Pads[1]),"Player can buy a Pulse tower on a pad");
                    testTower=game.Towers[0];
                    Check(game.Credits==35 && game.PlacementsRemaining==0,"Successful purchase spends its price and the wave's single placement");
                    Check(!game.TryPlaceAt(game.Pads[1]) && !game.TryPlaceAt(game.Pads[3]) && game.Credits==35,"Occupied pads and second placements are rejected without spending money");
                    game.SelectTower(testTower);game.UpgradeSelected();Check(testTower.Level==1 && game.Credits==35,"Cannot upgrade without enough kill earnings");
                    game.LaunchWave();game.enabled=false;
                    Check(game.PlacementsRemaining==0,"Launching a wave does not grant a second placement");
                    DefenseEnemy scout=game.SpawnEnemy(EnemyKind.Scout);scout.TakeDamage(1000);
                    Check(game.Credits==45 && game.KillEarnings==10 && game.Kills==1,"Scout kill pays exactly $10");
                    scout.TakeDamage(1000);Check(game.Credits==45 && game.Kills==1,"A defeated enemy cannot pay twice");
                    float oldHeight=testTower.Height,oldDamage=testTower.Damage,oldRate=testTower.FireRate,oldRange=testTower.Range;
                    game.SelectTower(testTower);game.UpgradeSelected();
                    Check(testTower.Level==2 && game.Credits==0,"Kill money purchases an upgrade for $45");
                    Check(Mathf.Abs(testTower.Height-oldHeight-.8f)<.001f && Mathf.Abs(testTower.Muzzle.parent.localPosition.y-testTower.Height)<.001f,"Upgrade visibly raises the turret and muzzle by 0.8 units");
                    Check(testTower.Damage>oldDamage && testTower.FireRate>oldRate && testTower.Range>oldRange,"Upgrade increases damage, fire rate and range");
                    DefenseEnemy runner=game.SpawnEnemy(EnemyKind.Runner),armored=game.SpawnEnemy(EnemyKind.Armored),boss=game.SpawnEnemy(EnemyKind.Siege);
                    Check(runner.Speed>scout.Speed && armored.Speed<scout.Speed && armored.MaxHealth>scout.MaxHealth,"Enemy types have different speed and HP");
                    runner.TakeDamage(1000);Check(game.Credits==15,"Runner kill pays $15");
                    armored.TakeDamage(1000);Check(game.Credits==40,"Armored kill pays $25");
                    boss.TakeDamage(1000);Check(game.Credits==140 && game.KillEarnings==150,"Siege kill pays $100 and total kill earnings are tracked");
                    Check(!game.TryPlaceAt(game.Pads[3]) && game.Credits==140,"Even ample kill money cannot bypass the one-tower limit during combat");
                    testTower.enabled=false;game.enabled=true;Time.timeScale=10;due=now+.05;stage++;break;
                case 1:
                    // Focused transition test only: clear scheduled enemies to exercise the wave boundary.
                    foreach(DefenseEnemy enemy in game.Enemies.ToArray()) enemy.TakeDamage(1000);
                    if(game.State!=DefenseState.Building) { due=now+.05;return; }
                    Time.timeScale=1;game.enabled=false;
                    Check(game.Wave==1 && game.PlacementWave==2 && game.PlacementsRemaining==1,"Clearing a wave unlocks exactly one placement for the next wave");
                    Check(game.Credits==260,"All twelve scheduled kills pay correctly and there is no free wave-clear money");
                    game.Choose(TowerKind.Siege);Check(game.TryPlaceAt(game.Pads[3]),"Next wave permits buying one additional tower");
                    DefenseTower siege=game.SelectedTower;
                    Check(testTower.FireRate!=siege.FireRate && testTower.ProjectileSpeed!=siege.ProjectileSpeed && testTower.Height!=siege.Height,"Tower types still differ in fire rate, projectile speed and height");
                    game.SellSelected();
                    Check(game.Credits==227 && game.Towers.Count==1 && game.PlacementsRemaining==0,"Selling refunds 70 percent but does not restore the wave allowance");
                    Check(!game.TryPlaceAt(game.Pads[3]) && game.Credits==227,"Sell-and-rebuild cannot bypass the one-per-wave rule");
                    game.SelectTower(testTower);game.SellSelected();Check(game.Towers.Count==1,"Cannot strand the player by selling their only tower after using the allowance");
                    game.UpgradeSelected();float maxHeight=testTower.Height;
                    Check(testTower.Level==3 && game.Credits==137 && maxHeight>2.9f,"A second upgrade spends $90 and makes the tower taller again");
                    game.UpgradeSelected();Check(testTower.Level==3 && game.Credits==137,"Maximum level cannot consume money or grow indefinitely");
                    game.LaunchWave();Check(game.PlacementsRemaining==0 && game.Wave==2,"Wave-2 launch preserves its already-used setup allowance");
                    game.StartGame();game.LaunchWave();
                    testEnemy=game.SpawnEnemy(EnemyKind.Scout);testEnemy.SetDistance(9);testEnemy.enabled=false;
                    Vector3 origin=testEnemy.Body.position+new Vector3(4,1.5f,0);
                    InterceptSolver.TrySolve(origin,testEnemy.Body.position,Vector3.zero,17,Physics.gravity,out Vector3 velocity,out _);
                    DefenseProjectile projectile=DefenseProjectile.Launch(game,origin,velocity,10,false);
                    Check(projectile.Body.useGravity && !projectile.Body.isKinematic,"Projectiles use dynamic Rigidbody gravity");
                    simulatedDue=Time.time+.8f;due=now+.1;stage++;break;
                case 2:
                    Check(game.Kills==1 && game.Credits==120,"An actual projectile collision kills an enemy and pays its money reward");
                    DefenseEnemy motorcycle=game.SpawnEnemy(EnemyKind.Motorcycle),truck=game.SpawnEnemy(EnemyKind.Truck);
                    Check(motorcycle.Speed>3.9f && motorcycle.MaxHealth==6 && truck.MaxHealth==160 && truck.Speed<1.25f,
                        "Motorcycles are fast and trucks have 160 base HP at a slower speed");
                    Check(truck.GetComponent<BoxCollider>().size.z>motorcycle.GetComponent<BoxCollider>().size.z &&
                        truck.transform.Find("Truck chassis")!=null && motorcycle.transform.Find("Motorcycle arrow fairing")!=null &&
                        Resources.Load<Mesh>("DefenseMeshes/Wheel")!=null,
                        "Vehicles have distinct models, fitted box hitboxes and authored wheel meshes");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureVehicles(motorcycle,truck);
                    motorcycle.TakeDamage(10000);Check(game.Credits==140,"Motorcycle kill pays $20");
                    truck.TakeDamage(10000);Check(game.Credits==260,"High-HP truck kill pays $120");
                    truck.TakeDamage(10000);Check(game.Credits==260,"Truck payout cannot be claimed twice");
                    int total=0,scouts=0;bool hasBike=false,hasTruck=false;
                    for(int wave=1;wave<=DefenseGame.TotalWaves;wave++)
                        foreach(EnemyKind kind in DefenseGame.WavePlan(wave))
                        { total++;if(kind==EnemyKind.Scout) scouts++;if(kind==EnemyKind.Motorcycle) hasBike=true;if(kind==EnemyKind.Truck) hasTruck=true; }
                    Check(DefenseGame.TotalWaves==10 && total==338 && scouts>150 && hasBike && hasTruck,
                        "Ten escalating waves contain 338 enemies with larger Scout groups and both new vehicle types");
                    game.StartGame();game.Choose(TowerKind.Pulse);game.TryPlaceAt(game.Pads[1]);testTower=game.Towers[0];
                    game.LaunchWave();testEnemy=game.SpawnEnemy(EnemyKind.Armored);testEnemy.SetDistance(7);
                    shotsAtStart=testTower.ShotsFired;simulatedDue=Time.time+2.1f;due=now+.1;stage++;break;
                case 3:
                    Check(testEnemy!=null && Vector3.Distance(testEnemy.Body.position,game.Route.Position(testEnemy.DistanceTravelled)+Vector3.up*.7f)<.15f,"Enemy body stays synchronized with curved-route progress");
                    Check(testEnemy.CurrentVelocity.magnitude<=testEnemy.Speed*1.1f,"Reported target velocity matches physical speed");
                    int count=testTower.ShotsFired-shotsAtStart;
                    Check(count>=4 && count<=6,"Tower continuously fires at its configured cadence");
                    Check(testEnemy.Health<testEnemy.MaxHealth,"Predictive tower shots hit a moving enemy");
                    testEnemy=game.SpawnEnemy(EnemyKind.Runner);testEnemy.SetDistance(16);game.TogglePause();pausedPosition=testEnemy.Body.position;
                    Check(Time.timeScale==0 && !game.IsCombat,"Pause stops combat time");due=now+.25;stage++;break;
                case 4:
                    Check(Vector3.Distance(testEnemy.Body.position,pausedPosition)<.001f,"Enemies remain still while paused");
                    game.TogglePause();game.ToggleSpeed();Check(Time.timeScale==2,"Speed control still enables 2x");game.ToggleSpeed();
                    testEnemy.SetDistance(game.Route.Length-.01f);simulatedDue=Time.time+.1f;due=now+.1;stage++;break;
                case 5:
                    Check(game.State==DefenseState.Lost,"Any enemy completing the path immediately ends the game");
                    game.StartGame();Check(game.Credits==110 && game.Towers.Count==0 && game.Wave==0 && game.Kills==0 && game.KillEarnings==0 && game.PlacementsRemaining==1,"Restart resets money, towers, rewards, waves and build allowance");
                    game.enabled=true;BuildCampaignDefense();game.LaunchWave();Time.timeScale=10;
                    Debug.Log("DEFENSE CAMPAIGN: one placement per wave, $110 start, kill money only");due=now+.05;stage++;break;
                case 6:
                    if(game.State==DefenseState.Lost) throw new Exception("Hard campaign lost in wave "+game.Wave+", money="+game.Credits+", kills="+game.Kills+", towers="+game.Towers.Count);
                    if(game.State==DefenseState.Won)
                    {
                        int expected=0;for(int wave=1;wave<=DefenseGame.TotalWaves;wave++) expected+=DefenseGame.WavePlan(wave).Count;
                        Check(game.Wave==10 && game.Kills==expected,"Expanded campaign is winnable: all ten waves and 338 enemies defeated");
                        Check(game.Towers.Count==10,"Winning strategy places exactly one tower per wave");
                        int spent=0;foreach(DefenseTower tower in game.Towers) spent+=tower.Investment;
                        Check(game.Credits==DefenseGame.StartingCredits+game.KillEarnings-spent,"Campaign money exactly equals starting cash plus kill rewards minus purchases and upgrades");
                        Check(game.Enemies.Count==0 && game.SpawnRemaining==0,"Victory waits for every active and scheduled enemy");
                        Debug.Log("DEFENSE VALIDATION COMPLETE: "+checks+" checks passed");
                        File.WriteAllText(Path.Combine(Application.dataPath,"../VALIDATION_RUN.txt"),checks+" checks passed; one-tower-per-wave campaign won; kills="+game.Kills+"; kill earnings="+game.KillEarnings);
                        EditorApplication.ExitPlaymode();stage++;return;
                    }
                    if(game.TowersPlacedThisWave>1) throw new Exception("Campaign exceeded the one-tower allowance");
                    if(!midpointChecked && game.State==DefenseState.Building && game.Wave==5)
                    { Check(game.PlacementsRemaining==1,"Wave five is now the midpoint and unlocks a sixth wave instead of victory");midpointChecked=true; }
                    BuildCampaignDefense();
                    if(game.State==DefenseState.Building) game.LaunchWave();
                    if(!captured && game.Wave>=5 && game.Enemies.Exists(e=>e.Kind==EnemyKind.Truck) && Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0)
                    { Capture();captured=true; }
                    due=now+.05;break;
            }
        }
        catch(Exception error)
        { failed=true;Debug.LogError("DEFENSE VALIDATION FAILED: "+error);Time.timeScale=1;EditorApplication.ExitPlaymode(); }
    }
    private static void BuildCampaignDefense()
    {
        int[] pads={1,3,5,7,0,6,2,8,4,9};
        TowerKind[] kinds={TowerKind.Pulse,TowerKind.Siege,TowerKind.Pulse,TowerKind.Siege,TowerKind.Pulse,
            TowerKind.Siege,TowerKind.Siege,TowerKind.Pulse,TowerKind.Pulse,TowerKind.Siege};
        if(game.PlacementsRemaining>0)
        {
            int index=game.PlacementWave-1;
            if(game.Credits<DefenseTower.Cost(kinds[index])) return;
            game.Choose(kinds[index]);game.TryPlaceAt(game.Pads[pads[index]]);
        }
        // Save for the next upgrade on the highest-priority non-maxed tower.
        foreach(int pad in new[]{3,7,1,5,0,6,2,8,4,9})
        {
            DefenseTower tower=game.Towers.Find(t=>t.PadIndex==pad);
            if(tower==null || tower.Level>=3) continue;
            if(game.Credits>=tower.UpgradeCost) { game.SelectTower(tower);game.UpgradeSelected(); }
            return;
        }
    }
    private static void CaptureVehicles(DefenseEnemy motorcycle,DefenseEnemy truck)
    {
        Camera view=game.View;
        Vector3 cameraPosition=view.transform.position;Quaternion cameraRotation=view.transform.rotation;
        float size=view.orthographicSize;
        Vector3 motorcyclePosition=motorcycle.Body.position,truckPosition=truck.Body.position;
        try
        {
            motorcycle.enabled=truck.enabled=false;
            motorcycle.Body.position=motorcycle.transform.position=new Vector3(-15,.7f,-3);
            truck.Body.position=truck.transform.position=new Vector3(-11,.7f,-3);
            Vector3 focus=new Vector3(-13,.7f,-3);
            view.transform.position=focus+new Vector3(8,10,12);view.transform.LookAt(focus);view.orthographicSize=5.3f;
            motorcycle.transform.Find("Health billboard").rotation=view.transform.rotation;
            truck.transform.Find("Health billboard").rotation=view.transform.rotation;
            game.HUD.gameObject.SetActive(false);
            CaptureTo("VEHICLES_PREVIEW.png");
        }
        finally
        {
            game.HUD.gameObject.SetActive(true);view.transform.SetPositionAndRotation(cameraPosition,cameraRotation);view.orthographicSize=size;
            motorcycle.Body.position=motorcycle.transform.position=motorcyclePosition;
            truck.Body.position=truck.transform.position=truckPosition;
            motorcycle.enabled=truck.enabled=true;
        }
    }
    [MenuItem("Tools/Neon Bastion/Capture preview during play")]
    public static void Capture() { CaptureTo("NEON_BASTION_PREVIEW.png"); }
    private static void CaptureTo(string filename)
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
            string path=Path.Combine(Application.dataPath,"../"+filename);File.WriteAllBytes(path,image.EncodeToPNG());Debug.Log("DEFENSE PREVIEW: "+path);
        }
        finally
        {
            camera.targetTexture=previous;RenderTexture.active=oldActive;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
            target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
