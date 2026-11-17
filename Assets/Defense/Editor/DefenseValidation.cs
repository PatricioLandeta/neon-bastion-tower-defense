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
    private static int stage,checks,shotsAtStart,audioShotsAtStart;
    private static bool failed,captured,midpointChecked;
    private static double due,deadline;
    private static readonly System.Collections.Generic.HashSet<int> campaignPlans=new System.Collections.Generic.HashSet<int>();
    private static DefenseGame game;
    private static DefenseEnemy testEnemy;
    private static DefenseTower testTower,mergedFireTower;
    private static DefenseEnemy mergedFireEnemy;
    private static Vector3 pausedPosition;
    private static float simulatedDue,demoTimeBeforePause;

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
            campaignPlans.Clear();stage=0;checks=0;due=EditorApplication.timeSinceStartup+1;deadline=due+300;
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
                    Check(game.Audio!=null && game.Audio.PulseLaser!=null && game.Audio.SiegeLaser!=null && game.Audio.PulseLaser!=game.Audio.SiegeLaser,"Both tower kinds have distinct imported laser sound assets");
                    Check(game.Audio.Music.loop && game.Audio.Music.pitch==1 && game.Audio.Music.spatialBlend==0,"Background music loops in 2D at its normal pitch");
                    for(int paid=1;paid<=3;paid++)
                        for(int tier=1;tier<=4;tier++)
                        {
                            float pitch=DefenseAudio.PitchFor(paid,tier);
                            Check(pitch>0 && pitch<3 && (paid==1 || pitch>DefenseAudio.PitchFor(paid-1,tier)) && (tier==1 || pitch>DefenseAudio.PitchFor(paid,tier-1)),"Laser pitch rises with paid upgrades and merges: upgrade "+paid+", merge "+tier);
                        }
                    Check(Resources.Load<Mesh>("DefenseMeshes/ArrowHull")!=null,"Custom non-basic mesh assets are available");
                    Check(Vector3.Dot(game.Route.Tangent(2),game.Route.Tangent(9))<.5f,"Enemy route contains predetermined bends");
                    bool solved=InterceptSolver.TrySolve(new Vector3(0,3,0),new Vector3(10,.7f,4),new Vector3(0,0,3),17,Physics.gravity,out Vector3 launch,out float time);
                    Vector3 hit=new Vector3(0,3,0)+launch*time+.5f*Physics.gravity*time*time;
                    Check(solved && Vector3.Distance(hit,new Vector3(10,.7f,4)+new Vector3(0,0,3)*time)<.002f && Mathf.Abs(launch.magnitude-17)<.01f,"Predictive ballistic solution intersects a moving target at the launch speed");
                    Check(!InterceptSolver.TrySolve(Vector3.zero,new Vector3(100,0,0),new Vector3(30,0,0),5,Physics.gravity,out _,out _),"Unreachable targets return no launch solution");
                    game.HUD.transform.Find("Menu shade/Start or retry").GetComponent<Button>().onClick.Invoke();
                    Check(game.HUD.TutorialOpen && game.State==DefenseState.Title && game.HUD.TutorialPage==0,
                        "Start button opens the quick tutorial before any gameplay begins");
                    Transform guide=game.HUD.transform.Find("Quick tutorial");
                    Button next=guide.Find("Tutorial next").GetComponent<Button>();
                    Button back=guide.Find("Tutorial back").GetComponent<Button>();
                    Text guideText=guide.Find("Tutorial instructions").GetComponent<Text>();
                    game.HUD.MergeDemo.Seek(1);Check(game.HUD.MergeDemo.Page==0 && game.HUD.MergeDemo.ChosenKind==TowerKind.Pulse,"First slide animates the Pulse selection and firing example");
                    game.HUD.MergeDemo.Seek(5);Check(game.HUD.MergeDemo.ChosenKind==TowerKind.Siege,"First slide also animates the Siege selection and firing example");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("TUTORIAL_CHOOSE_PREVIEW.png");
                    next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==1 && guideText.text.Contains("LEFT-CLICK") && guideText.text.Contains("CYAN HEXAGON"),
                        "Tutorial explicitly explains selecting and left-clicking a cyan pad to place a tower");
                    game.HUD.MergeDemo.Seek(1);Check(!game.HUD.MergeDemo.ExamplePlaced && game.HUD.MergeDemo.ExampleCash==110,"Placement animation begins before purchasing a tower");
                    game.HUD.MergeDemo.Seek(4);Check(game.HUD.MergeDemo.ExamplePlaced && game.HUD.MergeDemo.ExampleCash==35,"Placement animation builds a tower and shows $110 minus $75");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("TUTORIAL_PLACEMENT_PREVIEW.png");
                    back.onClick.Invoke();Check(game.HUD.TutorialPage==0,"Tutorial Back returns to the previous explanation");
                    next.onClick.Invoke();next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==2 && guideText.text.Contains("$4") && guideText.text.Contains("MONEY"),
                        "Tutorial explains kill rewards and the money display");
                    int tutorialCash=game.Credits,tutorialKills=game.Kills;int enemyTypes=0;
                    foreach(EnemyKind kind in new[]{EnemyKind.Scout,EnemyKind.Runner,EnemyKind.Armored,EnemyKind.Motorcycle,EnemyKind.Truck,EnemyKind.Siege})
                    {
                        game.HUD.MergeDemo.SelectEnemy(enemyTypes++);game.HUD.MergeDemo.Seek(1);Vector3 before=game.HUD.MergeDemo.ExampleEnemyPosition;
                        game.HUD.MergeDemo.Seek(2);DefenseEnemy.EnemyStats enemyStats=DefenseEnemy.BaseStats(kind);
                        Check(game.HUD.MergeDemo.PreviewEnemyKind==kind && Vector3.Distance(before,game.HUD.MergeDemo.ExampleEnemyPosition)>.01f,"Enemy guide visibly moves the "+kind);
                        Check(game.HUD.MergeDemo.EnemyExplanation.Contains("+$"+enemyStats.Reward) && game.HUD.MergeDemo.EnemyExplanation.Contains("Base HP "+enemyStats.Health.ToString("0")),"Enemy guide shows actual HP and kill reward for "+kind);
                        game.HUD.MergeDemo.Seek(6);Check(game.HUD.MergeDemo.ExampleHealth==0 && game.HUD.MergeDemo.ExampleCash==35+enemyStats.Reward,"Animated "+kind+" defeat depletes HP and adds the correct example reward");
                    }
                    game.HUD.MergeDemo.SelectEnemy(0);guide.Find("Animated merge example/Next enemy").GetComponent<Button>().onClick.Invoke();
                    Check(game.HUD.MergeDemo.PreviewEnemyKind==EnemyKind.Runner,"Next enemy button browses the animated roster");
                    guide.Find("Animated merge example/Previous enemy").GetComponent<Button>().onClick.Invoke();
                    Check(game.HUD.MergeDemo.PreviewEnemyKind==EnemyKind.Scout,"Previous enemy button returns to the Scout");
                    game.HUD.MergeDemo.Seek(10);int replayEnemy=game.HUD.MergeDemo.EnemyIndex;
                    guide.Find("Animated merge example/Replay merge animation").GetComponent<Button>().onClick.Invoke();
                    Check(game.HUD.MergeDemo.EnemyIndex==replayEnemy && game.HUD.MergeDemo.AnimationTime==0,"Replay preserves the currently displayed enemy after automatic cycling");
                    game.HUD.MergeDemo.SelectEnemy(0);game.HUD.MergeDemo.Seek(2);Canvas.ForceUpdateCanvases();
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("TUTORIAL_ENEMIES_PREVIEW.png");
                    Check(game.Credits==tutorialCash && game.Kills==tutorialKills && game.Towers.Count==0 && game.Enemies.Count==0,"All tutorial examples leave real money, kills and actors untouched");
                    next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==3 && guideText.text.Contains("$100") && guideText.text.Contains("$200") && guideText.text.Contains("0.8"),
                        "Tutorial explains upgrade prices and stronger, taller towers");
                    game.HUD.MergeDemo.Seek(1);float initialDemoHeight=game.HUD.MergeDemo.DisplayedTowerHeight;
                    game.HUD.MergeDemo.Seek(4);Check(game.HUD.MergeDemo.ExampleUpgradeLevel==2 && game.HUD.MergeDemo.ExampleCash==250 && Mathf.Abs(game.HUD.MergeDemo.DisplayedTowerHeight-initialDemoHeight-.8f)<.001f,"Upgrade animation pays $100 and visibly raises the tower by 0.8");
                    game.HUD.MergeDemo.Seek(7);Check(game.HUD.MergeDemo.ExampleUpgradeLevel==3 && game.HUD.MergeDemo.ExampleCash==50 && Mathf.Abs(game.HUD.MergeDemo.DisplayedTowerHeight-initialDemoHeight-1.6f)<.001f,"Second upgrade animation pays $200 and raises the tower again");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("TUTORIAL_PREVIEW.png");
                    next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==4 && guideText.text.Contains("SAME TYPE") && guideText.text.Contains("DRAG"),
                        "Tutorial explains dragging and merging matching tower types");
                    Check(guideText.text.Contains("Buy AND merge") && guideText.text.Contains("RED Lv4"),
                        "Tutorial explains all three tiers and money-limited buying plus merging");
                    int demoCash=game.Credits;
                    game.HUD.MergeDemo.Seek(1f);
                    Check(!game.HUD.MergeDemo.ShowingMerged && game.HUD.MergeDemo.ExampleMergeLevel==1 && game.HUD.MergeDemo.ExampleCash==500,
                        "Animated example begins with base towers and example money");
                    game.HUD.MergeDemo.Seek(4.5f);
                    Check(game.HUD.MergeDemo.ShowingMerged && game.HUD.MergeDemo.ExampleMergeLevel==2 && game.HUD.MergeDemo.ExampleCash==450,
                        "Animated example visibly merges to green and spends only example cash");
                    game.HUD.MergeDemo.Seek(12.5f);Check(game.HUD.MergeDemo.ExampleMergeLevel==3 && game.HUD.MergeDemo.ExampleCash==400,"Animated second merge produces purple for $100");
                    game.HUD.MergeDemo.Seek(20.5f);Check(game.HUD.MergeDemo.ExampleMergeLevel==4 && game.HUD.MergeDemo.ExampleCash==300,"Animated final merge produces red for $200");
                    Check(game.Credits==demoCash && game.Towers.Count==0,
                        "Tutorial animation uses only visual models and never spends real game resources");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("ANIMATED_MERGE_TUTORIAL.png");
                    guide.Find("Animated merge example/Replay merge animation").GetComponent<Button>().onClick.Invoke();
                    Check(game.HUD.MergeDemo.AnimationTime==0 && !game.HUD.MergeDemo.ShowingMerged,
                        "Replay button restarts the merge demonstration");
                    next.onClick.Invoke();
                    Check(!game.HUD.TutorialOpen && game.State==DefenseState.Building && game.Credits==110,
                        "Completing the tutorial starts a clean game without spending cash");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("OPENING_GUIDE_PREVIEW.png");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-validateTutorialOnly")>=0)
                    { Debug.Log("TUTORIAL VALIDATION COMPLETE: "+checks+" checks passed");EditorApplication.ExitPlaymode();stage=7;return; }
                    Text instructions=game.HUD.transform.Find("Placement instructions").GetComponent<Text>();
                    Check(instructions.text.Contains("1 / 2 CHOOSE") && instructions.text.Contains("LEFT-CLICK A CYAN PAD"),
                        "Persistent gameplay overlay continues to explain tower selection and placement");
                    game.HUD.transform.Find("How to play").GetComponent<Button>().onClick.Invoke();
                    guide.Find("Tutorial skip").GetComponent<Button>().onClick.Invoke();
                    Check(!game.HUD.TutorialOpen && game.State==DefenseState.Building && !game.Paused,
                        "Help button reopens the guide and Close returns to the same game");
                    game.StartGame();
                    Check(game.Credits==110 && game.Towers.Count==0,"New game starts with $110 and no towers");
                    Check(!game.TryPlaceAt(new Vector3(-13,0,0)) && game.Credits==110,"Invalid placement spends no money");
                    game.Choose(TowerKind.Pulse);Check(game.TryPlaceAt(game.Pads[1]),"Player can buy a Pulse tower on a pad");
                    testTower=game.Towers[0];
                    Check(game.Credits==35,"Successful purchase spends the tower price");
                    Check(!game.TryPlaceAt(game.Pads[1]) && !game.TryPlaceAt(game.Pads[3]) && game.Credits==35,"Occupied pads and unaffordable purchases spend no money");
                    game.SelectTower(testTower);game.UpgradeSelected();Check(testTower.Level==1 && game.Credits==35,"Cannot upgrade without enough kill earnings");
                    game.LaunchWave();game.enabled=false;
                    Check(game.Credits==35,"Launching a wave does not grant free cash");
                    DefenseEnemy scout=game.SpawnEnemy(EnemyKind.Scout);scout.TakeDamage(1000);
                    Check(game.Credits==39 && game.KillEarnings==4 && game.Kills==1,"Scout kill pays exactly $4");
                    scout.TakeDamage(1000);Check(game.Credits==39 && game.Kills==1,"A defeated enemy cannot pay twice");
                    game.UpgradeSelected();Check(testTower.Level==1 && game.Credits==39,"One kill cannot fund an immediate upgrade");
                    for(int i=0;i<24;i++) game.SpawnEnemy(EnemyKind.Scout).TakeDamage(1000);
                    float oldHeight=testTower.Height,oldDamage=testTower.Damage,oldRate=testTower.FireRate,oldRange=testTower.Range;
                    game.SelectTower(testTower);game.UpgradeSelected();
                    Check(testTower.Level==2 && game.Credits==35,"Kill money purchases an upgrade for $100");
                    Check(Mathf.Abs(testTower.Height-oldHeight-.8f)<.001f && Mathf.Abs(testTower.Muzzle.parent.localPosition.y-testTower.Height)<.001f,"Upgrade visibly raises the turret and muzzle by 0.8 units");
                    Check(testTower.Damage>oldDamage && testTower.FireRate>oldRate && testTower.Range>oldRange,"Upgrade increases damage, fire rate and range");
                    DefenseEnemy runner=game.SpawnEnemy(EnemyKind.Runner),armored=game.SpawnEnemy(EnemyKind.Armored),boss=game.SpawnEnemy(EnemyKind.Siege);
                    Check(runner.Speed>scout.Speed && armored.Speed<scout.Speed && armored.MaxHealth>scout.MaxHealth,"Enemy types have different speed and HP");
                    runner.TakeDamage(1000);Check(game.Credits==40,"Runner kill pays $5");
                    armored.TakeDamage(1000);Check(game.Credits==48,"Armored kill pays $8");
                    boss.TakeDamage(1000);Check(game.Credits==78 && game.KillEarnings==143,"Siege kill pays $30 and total kill earnings are tracked");
                    game.Choose(TowerKind.Siege);Check(!game.TryPlaceAt(game.Pads[3]) && game.Credits==78,"Unaffordable towers are blocked during combat");
                    testTower.enabled=false;game.enabled=true;Time.timeScale=10;due=now+.05;stage++;break;
                case 1:
                    // Focused transition test only: clear scheduled enemies to exercise the wave boundary.
                    foreach(DefenseEnemy enemy in game.Enemies.ToArray()) enemy.TakeDamage(1000);
                    if(game.State!=DefenseState.Building) { due=now+.05;return; }
                    Time.timeScale=1;game.enabled=false;
                    Check(game.Wave==1 && game.PlacementWave==2,"Clearing a wave advances the build phase without an action allowance");
                    Check(game.Credits==118,"All ten training-wave kills pay correctly and there is no free wave-clear money");
                    game.Choose(TowerKind.Siege);Check(game.TryPlaceAt(game.Pads[3]),"Next wave permits buying one additional tower");
                    DefenseTower siege=game.SelectedTower;
                    Check(testTower.FireRate!=siege.FireRate && testTower.ProjectileSpeed!=siege.ProjectileSpeed && testTower.Height!=siege.Height,"Tower types still differ in fire rate, projectile speed and height");
                    game.SellSelected();
                    Check(game.Credits==85 && game.Towers.Count==1,"Selling refunds 70 percent");
                    game.Choose(TowerKind.Pulse);Check(game.TryPlaceAt(game.Pads[3]) && game.Credits==10,"Buying again in the same build phase is allowed when affordable");
                    game.SelectTower(testTower);game.LaunchWave();
                    while(game.Credits<testTower.UpgradeCost) game.SpawnEnemy(EnemyKind.Truck).TakeDamage(10000);
                    int upgradeBalance=game.Credits-200;
                    game.UpgradeSelected();float maxHeight=testTower.Height;
                    Check(testTower.Level==3 && game.Credits==upgradeBalance && maxHeight>2.9f,"A second upgrade spends $200 and makes the tower taller again");
                    game.UpgradeSelected();Check(testTower.Level==3 && game.Credits==upgradeBalance,"Maximum level cannot consume money or grow indefinitely");
                    game.StartGame();game.LaunchWave();
                    testEnemy=game.SpawnEnemy(EnemyKind.Scout);testEnemy.SetDistance(9);testEnemy.enabled=false;
                    Vector3 origin=testEnemy.Body.position+new Vector3(4,1.5f,0);
                    InterceptSolver.TrySolve(origin,testEnemy.Body.position,Vector3.zero,17,Physics.gravity,out Vector3 velocity,out _);
                    DefenseProjectile projectile=DefenseProjectile.Launch(game,origin,velocity,10,false);
                    Check(projectile.Body.useGravity && !projectile.Body.isKinematic,"Projectiles use dynamic Rigidbody gravity");
                    simulatedDue=Time.time+.8f;due=now+.1;stage++;break;
                case 2:
                    Check(game.Kills==1 && game.Credits==114,"An actual projectile collision kills an enemy and pays its money reward");
                    DefenseEnemy motorcycle=game.SpawnEnemy(EnemyKind.Motorcycle),truck=game.SpawnEnemy(EnemyKind.Truck);
                    Check(motorcycle.Speed>3.9f && motorcycle.MaxHealth==6 && truck.MaxHealth==160 && truck.Speed<1.25f,
                        "Motorcycles are fast and trucks have 160 base HP at a slower speed");
                    Check(truck.GetComponent<BoxCollider>().size.z>motorcycle.GetComponent<BoxCollider>().size.z &&
                        truck.transform.Find("Truck chassis")!=null && motorcycle.transform.Find("Motorcycle arrow fairing")!=null &&
                        Resources.Load<Mesh>("DefenseMeshes/Wheel")!=null,
                        "Vehicles have distinct models, fitted box hitboxes and authored wheel meshes");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureVehicles(motorcycle,truck);
                    motorcycle.TakeDamage(10000);Check(game.Credits==120,"Motorcycle kill pays $6");
                    truck.TakeDamage(10000);Check(game.Credits==160,"High-HP truck kill pays $40");
                    truck.TakeDamage(10000);Check(game.Credits==160,"Truck payout cannot be claimed twice");
                    int total=0,scouts=0;bool hasBike=false,hasTruck=false;
                    for(int wave=1;wave<=DefenseGame.TotalWaves;wave++)
                        foreach(EnemyKind kind in DefenseGame.WavePlan(wave))
                        { total++;if(kind==EnemyKind.Scout) scouts++;if(kind==EnemyKind.Motorcycle) hasBike=true;if(kind==EnemyKind.Truck) hasTruck=true; }
                    Check(DefenseGame.TotalWaves==10 && total==522 && scouts>240 && hasBike && hasTruck,
                        "Ten escalating waves contain 522 enemies with larger Scout groups and both new vehicle types");
                    DefenseEnemy lightScout=game.SpawnEnemy(EnemyKind.Scout);
                    Check(lightScout.transform.Find("Light scout chassis")!=null && lightScout.transform.Find("Enclosed glass cockpit")!=null && lightScout.transform.Find("Track pod")==null && lightScout.GetComponent<DefenseVehicleWheels>()!=null,
                        "Scout uses a futuristic light vehicle instead of bug-like track pods");
                    Check(lightScout.Speed==2 && lightScout.MaxHealth==3 && lightScout.Reward==4,"Scout redesign preserves training-wave gameplay stats");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureScout(lightScout);
                    ValidateMerging();
                    game.StartGame();testTower=TestTower(TowerKind.Pulse,1);
                    game.LaunchWave();testEnemy=game.SpawnEnemy(EnemyKind.Armored);testEnemy.SetDistance(7);
                    mergedFireTower=TestTower(TowerKind.Siege,7);DefenseTower feeder=TestTower(TowerKind.Siege,8);
                    game.TryMergeTowers(feeder,mergedFireTower);
                    mergedFireEnemy=game.SpawnEnemy(EnemyKind.Armored);mergedFireEnemy.SetDistance(game.Route.Length-14);
                    audioShotsAtStart=game.Audio.ShotsPlayed;shotsAtStart=testTower.ShotsFired;simulatedDue=Time.time+2.1f;due=now+.1;stage++;break;
                case 3:
                    Check(testEnemy!=null && Vector3.Distance(testEnemy.Body.position,game.Route.Position(testEnemy.DistanceTravelled)+Vector3.up*.7f)<.15f,"Enemy body stays synchronized with curved-route progress");
                    Check(testEnemy.CurrentVelocity.magnitude<=testEnemy.Speed*1.1f,"Reported target velocity matches physical speed");
                    int count=testTower.ShotsFired-shotsAtStart;
                    Check(game.GetComponents<DefenseAudio>().Length==1 && game.GetComponentsInChildren<AudioSource>().Length==33,"Audio system retains one music source and a bounded 32-voice shot pool");
                    Check(game.Audio.ShotsPlayed>audioShotsAtStart,"Real tower firing triggers the laser audio system");
                    Check(count>=4 && count<=6,"Tower continuously fires at its configured cadence");
                    Check(testEnemy.Health<testEnemy.MaxHealth,"Predictive tower shots hit a moving enemy");
                    Check(mergedFireTower.ShotsFired>0 && (mergedFireEnemy==null || mergedFireEnemy.Health<mergedFireEnemy.MaxHealth),
                        "Merged tower fires real ballistic projectiles that hit moving enemies");
                    testEnemy=game.SpawnEnemy(EnemyKind.Runner);testEnemy.SetDistance(16);game.HUD.OpenTutorial(false);pausedPosition=testEnemy.Body.position;
                    Check(Time.timeScale==0 && !game.IsCombat,"Opening the tutorial during combat pauses simulation");
                    for(int step=0;step<4;step++) game.HUD.NextTutorialPage();
                    demoTimeBeforePause=game.HUD.MergeDemo.AnimationTime;
                    due=now+.25;stage++;break;
                case 4:
                    Check(Vector3.Distance(testEnemy.Body.position,pausedPosition)<.001f,"Enemies remain still while paused");
                    Check(game.HUD.MergeDemo.AnimationTime>demoTimeBeforePause,
                        "Merge demonstration continues animating in unscaled time while the actual battle is paused");
                    int moneyBefore=game.Credits;int waveBefore=game.Wave;
                    game.HUD.CloseTutorial();
                    Check(!game.Paused && Time.timeScale==1 && game.Credits==moneyBefore && game.Wave==waveBefore,
                        "Closing combat help resumes without resetting money or the wave");
                    game.TogglePause();game.HUD.OpenTutorial(false);game.HUD.CloseTutorial();
                    Check(game.Paused && Time.timeScale==0,"Closing help preserves an already-paused game");
                    game.TogglePause();game.ToggleSpeed();Check(Time.timeScale==2,"Speed control still enables 2x");
                    game.HUD.OpenTutorial(false);game.HUD.CloseTutorial();
                    Check(!game.Paused && game.Fast && Time.timeScale==2,"Closing help restores the player's prior 2x speed");
                    game.ToggleSpeed();
                    testEnemy.SetDistance(game.Route.Length-.01f);simulatedDue=Time.time+.1f;due=now+.1;stage++;break;
                case 5:
                    Check(game.State==DefenseState.Lost,"Any enemy completing the path immediately ends the game");
                    AudioSource existingMusic=game.Audio.Music;
                    game.StartGame();Check(game.Audio.Music==existingMusic,"Restart preserves the background music source without restarting the song");Check(game.Credits==110 && game.Towers.Count==0 && game.Wave==0 && game.Kills==0 && game.KillEarnings==0,"Restart resets money, towers, rewards, waves and merge history");
                    game.enabled=true;BuildCampaignDefense();game.LaunchWave();Time.timeScale=10;
                    Debug.Log("DEFENSE CAMPAIGN: budget-limited purchases and merges, $110 start, kill money only");due=now+.05;stage++;break;
                case 6:
                    if(game.State==DefenseState.Lost) throw new Exception("Hard campaign lost in wave "+game.Wave+", money="+game.Credits+", kills="+game.Kills+", towers="+game.Towers.Count);
                    if(game.State==DefenseState.Won)
                    {
                        int expected=0;for(int wave=1;wave<=DefenseGame.TotalWaves;wave++) expected+=DefenseGame.WavePlan(wave).Count;
                        Check(game.Wave==10 && game.Kills==expected,"Expanded campaign is winnable: all ten waves and 522 enemies defeated");
                        Check(game.TotalTowersPurchased==8 && game.TotalMerges==2 && game.Towers.Count==6,
                            "Winning strategy pays for eight towers and two merges");
                        int units=0;foreach(DefenseTower tower in game.Towers) units+=tower.MergedUnits;
                        Check(units==8,"Merging conserves all purchased tower units");
                        int spent=0;foreach(DefenseTower tower in game.Towers) spent+=tower.Investment;
                        Check(game.Credits==DefenseGame.StartingCredits+game.KillEarnings-spent,"Campaign money exactly equals starting cash plus kill rewards minus purchases, merges and upgrades");
                        Check(game.Enemies.Count==0 && game.SpawnRemaining==0,"Victory waits for every active and scheduled enemy");
                        Debug.Log("DEFENSE VALIDATION COMPLETE: "+checks+" checks passed");
                        File.WriteAllText(Path.Combine(Application.dataPath,"../VALIDATION_RUN.txt"),checks+" checks passed; budget-limited campaign won; kills="+game.Kills+"; kill earnings="+game.KillEarnings);
                        EditorApplication.ExitPlaymode();stage++;return;
                    }
                    if(!midpointChecked && game.State==DefenseState.Building && game.Wave==5)
                    { Check(game.State==DefenseState.Building,"Wave five is now the midpoint and unlocks a sixth wave instead of victory");midpointChecked=true; }
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
    // Focused merge fixtures only. The later campaign starts fresh and buys every tower legally.
    private static DefenseTower TestTower(TowerKind kind,int pad)
    {
        GameObject root=new GameObject("Merge test "+kind);
        root.transform.position=game.Pads[pad]+Vector3.up*.2f;
        DefenseTower tower=root.AddComponent<DefenseTower>();tower.Initialize(game,kind,pad);game.Towers.Add(tower);
        return tower;
    }
    // Isolated mechanic tests earn fixture cash by killing fixture enemies; campaign resets everything.
    private static void FixtureFunds(int minimum)
    {
        int trucks=Mathf.Max(0,Mathf.CeilToInt((minimum-game.Credits)/40f));
        for(int i=0;i<trucks;i++) game.SpawnEnemy(EnemyKind.Truck).TakeDamage(10000);
    }
    private static DefenseTower FixtureTier(int level)
    {
        if(level==1)
        {
            for(int pad=0;pad<game.Pads.Length;pad++)
                if(!game.Towers.Exists(t=>t.PadIndex==pad)) return TestTower(TowerKind.Pulse,pad);
            throw new Exception("No fixture pad available");
        }
        DefenseTower first=FixtureTier(level-1),second=FixtureTier(level-1);
        Check(game.TryMergeTowers(first,second),"Matching fixture tier "+(level-1)+" merges successfully");
        return second;
    }
    private static void CheckTier(DefenseTower tower,int level,int units)
    {
        Color actual=tower.transform.Find("Fusion ring").GetComponent<MeshRenderer>().sharedMaterial.GetColor("_BaseColor");
        float expectedHeight=1.4f+.8f*(tower.Level-1);
        float baseRange=10.5f+.6f*(tower.Level-1);
        float baseRate=2.4f*(1f+.2f*(tower.Level-1));
        Check(Mathf.Abs(tower.FireRate-baseRate*Mathf.Pow(1.2f,level-1))<.001f,
            "Merge tier "+level+" compounds shot rate by 20 percent per merge");
        Check(Mathf.Abs(tower.Height-expectedHeight)<.001f && Mathf.Abs(tower.Muzzle.parent.localPosition.y-expectedHeight)<.001f && tower.transform.localScale.y==1,
            "Merge tier "+level+" preserves paid-upgrade height and muzzle elevation");
        Check(Mathf.Abs(tower.Range-baseRange*Mathf.Pow(1.05f,level-1))<.001f && tower.transform.localScale.x>1 && tower.transform.localScale.z>1,
            "Merge tier "+level+" widens the model and compounds range by 5 percent per merge");
        Check(tower.MergeLevel==level && tower.MergedUnits==units && Vector4.Distance(actual,DefenseTower.FusionColor(level))<.01f,
            "Merge level "+level+" has the correct color and "+units+" represented units");
    }
    private static void ValidateMerging()
    {
        game.StartGame();game.LaunchWave();
        DefenseTower source=TestTower(TowerKind.Pulse,3),target=TestTower(TowerKind.Pulse,1),different=TestTower(TowerKind.Siege,7);
        game.SelectTower(source);game.UpgradeSelected();
        int money=game.Credits,count=game.Towers.Count;
        Check(!game.TryMergeTowers(source,different) && game.Towers.Count==count && game.Credits==money,"Different tower types cannot merge or spend money");
        Check(!game.TryMergeTowers(source,source) && game.Credits==money,"Self merge spends nothing");
        Check(!game.TryMergeTowers(source,target) && game.Credits==money && target.MergeLevel==1,"Insufficient funds block merging without consuming towers");
        FixtureFunds(3000);money=game.Credits;
        Vector3 original=source.transform.position;
        Check(game.Merging.BeginDrag(source) && game.Merging.IsDraggingTower(source),"Drag creates a visual tower preview");
        game.Merging.Cancel();
        Check(source.transform.position==original && source.GetComponentInChildren<MeshRenderer>().enabled && game.Credits==money,"Cancelling restores tower visuals and spends nothing");
        game.Merging.BeginDrag(source);game.Merging.DropOn(null);
        Check(game.Towers.Count==count && game.Credits==money,"Empty drop spends nothing");
        game.Merging.BeginDrag(source);game.Merging.DropOn(different);
        Check(game.Towers.Count==count && game.Credits==money,"Wrong-type drop spends nothing");
        game.TogglePause();Check(!game.TryMergeTowers(source,target),"Merging is blocked while paused");game.TogglePause();
        game.HUD.OpenTutorial(false);Check(!game.TryMergeTowers(source,target),"Tutorial blocks actual merging");game.HUD.CloseTutorial();
        float height=source.Height,damage=source.Damage,rate=source.FireRate,range=source.Range;
        int investment=source.Investment+target.Investment+50;
        game.Merging.BeginDrag(source);Check(game.Merging.DropOn(target),"Matching drag/drop performs a paid merge");
        Check(game.Towers.Count==count-1 && !game.Towers.Contains(source) && target.PadIndex==1,"Merge consumes source and retains destination pad");
        Check(target.Level==2 && target.Investment==investment && game.Credits==money-50,"Green merge costs exactly $50 and retains paid upgrades and investment");
        CheckTier(target,2,2);
        Check(Mathf.Abs(target.Height-height)<.001f && target.Damage>damage && Mathf.Abs(target.FireRate-rate*1.2f)<.001f && Mathf.Abs(target.Range-range*1.05f)<.001f && target.transform.localScale.x>1 && target.transform.localScale.y==1,"Merge adds width and exactly 5 percent range without adding height");
        game.Choose(TowerKind.Pulse);Check(game.TryPlaceAt(game.Pads[3]),"Buying immediately after a merge in the same wave is allowed");
        DefenseTower third=game.SelectedTower;
        money=game.Credits;
        Check(!game.TryMergeTowers(third,target) && game.Credits==money && third.MergeLevel==1,"Unequal merge levels cannot combine or spend cash");
        DefenseTower fourth=TestTower(TowerKind.Pulse,5);
        Check(game.TryMergeTowers(fourth,third),"Merging immediately after buying in the same wave is allowed");
        money=game.Credits;Check(game.TryMergeTowers(third,target) && game.Credits==money-100,"Green plus green costs exactly $100");
        CheckTier(target,3,4);
        DefenseTower otherPurple=FixtureTier(3);
        money=game.Credits;Check(game.TryMergeTowers(otherPurple,target) && game.Credits==money-200,"Purple plus purple costs exactly $200");
        CheckTier(target,4,8);
        Check(!game.Merging.BeginDrag(target),"Red max towers cannot start another merge drag");
        DefenseTower otherRed=FixtureTier(4);
        money=game.Credits;count=game.Towers.Count;
        Check(!game.TryMergeTowers(otherRed,target) && game.Credits==money && game.Towers.Count==count,"Red plus red is rejected without spending or consuming towers");
        Check(target.UpgradeCost==1600,"Eight-unit merged tower scales the next paid upgrade cost");
        FixtureFunds(target.UpgradeCost);game.SelectTower(target);
        float beforeHeight=target.Height,beforeWidth=target.transform.localScale.x;
        game.UpgradeSelected();
        Check(Mathf.Abs(target.Height-beforeHeight-.8f)<.001f && target.transform.localScale.x==beforeWidth,"Paid upgrade adds height while preserving merged width");
        Check(target.Level==3 && target.MergeLevel==4,"Paid upgrades remain separate from capped merge levels");
        Ray ray=game.View.ScreenPointToRay(game.View.WorldToScreenPoint(target.transform.position+Vector3.up*target.Height));
        Check(game.Merging.PickTower(ray,target.transform.position)==target,"Tall red tower can be picked by its model");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("MERGE_PREVIEW.png");
        game.StartGame();game.LaunchWave();source=TestTower(TowerKind.Pulse,1);target=TestTower(TowerKind.Pulse,3);game.SelectTower(source);
        game.HUD.transform.Find("Merge").GetComponent<Button>().onClick.Invoke();
        Check(game.Merging.Active,"MERGE button arms the click alternative when the pair is affordable");
        Check(game.Merging.DropOn(target) && game.Credits==60 && game.TotalMerges==1,"Click-to-merge charges the same $50 as dragging");
        game.StartGame();game.LaunchWave();FixtureFunds(1000);game.Choose(TowerKind.Pulse);
        for(int pad=0;pad<game.Pads.Length;pad++) Check(game.TryPlaceAt(game.Pads[pad]),"Multiple affordable placements are allowed in a single wave");
        money=game.Credits;
        Check(game.Towers.Count==DefenseGame.TowerLimit && !game.TryPlaceAt(game.Pads[0]) && game.Credits==money,"Board capacity and occupied pads remain protected");
    }
    private static void BuildCampaignDefense()
    {
        int wave=game.PlacementWave;
        if(!campaignPlans.Contains(wave))
        {
            if(wave==4 || wave==8)
            {
                int sourcePad=wave==4 ? 5 : 6,targetPad=wave==4 ? 1 : 3;
                DefenseTower from=game.Towers.Find(t=>t.PadIndex==sourcePad),to=game.Towers.Find(t=>t.PadIndex==targetPad);
                if(from==null || to==null) throw new Exception("Campaign merge pair missing in wave "+wave);
                if(game.Credits<to.MergeCost) return;
                if(!game.TryMergeTowers(from,to)) throw new Exception("Campaign merge failed in wave "+wave);
            }
            else
            {
                int[] pads={1,3,5,-1,7,5,6,-1,2,9};
                TowerKind[] kinds={TowerKind.Pulse,TowerKind.Siege,TowerKind.Pulse,TowerKind.Pulse,TowerKind.Siege,
                    TowerKind.Pulse,TowerKind.Siege,TowerKind.Siege,TowerKind.Siege,TowerKind.Siege};
                int index=wave-1;
                if(game.Credits<DefenseTower.Cost(kinds[index])) return;
                game.Choose(kinds[index]);game.TryPlaceAt(game.Pads[pads[index]]);
            }
            campaignPlans.Add(wave);
        }
        foreach(int pad in new[]{3,7,1,5,6,2,9})
        {
            DefenseTower tower=game.Towers.Find(t=>t.PadIndex==pad);
            if(tower==null || tower.Level>=3) continue;
            if(game.Credits>=tower.UpgradeCost) { game.SelectTower(tower);game.UpgradeSelected(); }
            return;
        }
    }
    private static void CaptureScout(DefenseEnemy scout)
    {
        Camera view=game.View;Vector3 position=view.transform.position;Quaternion rotation=view.transform.rotation;float size=view.orthographicSize;
        Vector3 bodyPosition=scout.Body.position;
        try
        {
            scout.enabled=false;scout.Body.position=scout.transform.position=new Vector3(-13,.7f,-3);
            foreach(TrailRenderer trail in scout.GetComponentsInChildren<TrailRenderer>()) trail.Clear();
            view.transform.position=scout.transform.position+new Vector3(4,4.5f,6);view.transform.LookAt(scout.transform.position);view.orthographicSize=2.1f;
            game.HUD.gameObject.SetActive(false);scout.transform.Find("Health billboard").gameObject.SetActive(false);
            CaptureTo("SCOUT_PREVIEW.png");
        }
        finally
        {
            game.HUD.gameObject.SetActive(true);scout.transform.Find("Health billboard").gameObject.SetActive(true);
            scout.Body.position=scout.transform.position=bodyPosition;scout.enabled=true;
            view.transform.SetPositionAndRotation(position,rotation);view.orthographicSize=size;
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
    internal static void CaptureTo(string filename)
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
