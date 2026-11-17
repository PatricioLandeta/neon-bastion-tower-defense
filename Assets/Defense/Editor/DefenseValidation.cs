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
                    game.HUD.transform.Find("Menu shade/Start or retry").GetComponent<Button>().onClick.Invoke();
                    Check(game.HUD.TutorialOpen && game.State==DefenseState.Title && game.HUD.TutorialPage==0,
                        "Start button opens the quick tutorial before any gameplay begins");
                    Transform guide=game.HUD.transform.Find("Quick tutorial");
                    Button next=guide.Find("Tutorial next").GetComponent<Button>();
                    Button back=guide.Find("Tutorial back").GetComponent<Button>();
                    Text guideText=guide.Find("Tutorial instructions").GetComponent<Text>();
                    next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==1 && guideText.text.Contains("LEFT-CLICK") && guideText.text.Contains("CYAN HEXAGON"),
                        "Tutorial explicitly explains selecting and left-clicking a cyan pad to place a tower");
                    back.onClick.Invoke();Check(game.HUD.TutorialPage==0,"Tutorial Back returns to the previous explanation");
                    next.onClick.Invoke();next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==2 && guideText.text.Contains("$10") && guideText.text.Contains("MONEY"),
                        "Tutorial explains kill rewards and the money display");
                    next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==3 && guideText.text.Contains("$45") && guideText.text.Contains("$90") && guideText.text.Contains("0.8"),
                        "Tutorial explains upgrade prices and stronger, taller towers");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("TUTORIAL_PREVIEW.png");
                    next.onClick.Invoke();
                    Check(game.HUD.TutorialPage==4 && guideText.text.Contains("SAME TYPE") && guideText.text.Contains("DRAG"),
                        "Tutorial explains dragging and merging matching tower types");
                    Check(guideText.text.Contains("CANNOT do both") && guideText.text.Contains("ONE ACTION"),
                        "Tutorial explains the exclusive PLACE-or-MERGE wave action");
                    int demoCash=game.Credits,demoActions=game.WaveActionsRemaining;
                    game.HUD.MergeDemo.Seek(1f);
                    Check(!game.HUD.MergeDemo.ShowingMerged && game.HUD.MergeDemo.ExampleActionsRemaining==1,
                        "Animated example begins with two towers and one available action");
                    game.HUD.MergeDemo.Seek(4.5f);
                    Check(game.HUD.MergeDemo.ShowingMerged && game.HUD.MergeDemo.ExampleActionsRemaining==0,
                        "Animated example visibly merges and locks placement by spending its example action");
                    Check(game.Credits==demoCash && game.WaveActionsRemaining==demoActions && game.Towers.Count==0,
                        "Tutorial animation uses only visual models and never spends real game resources");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("ANIMATED_MERGE_TUTORIAL.png");
                    guide.Find("Animated merge example/Replay merge animation").GetComponent<Button>().onClick.Invoke();
                    Check(game.HUD.MergeDemo.AnimationTime==0 && !game.HUD.MergeDemo.ShowingMerged,
                        "Replay button restarts the merge demonstration");
                    next.onClick.Invoke();
                    Check(!game.HUD.TutorialOpen && game.State==DefenseState.Building && game.Credits==110 && game.PlacementsRemaining==1,
                        "Completing the tutorial starts a clean game without spending cash or the build allowance");
                    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("OPENING_GUIDE_PREVIEW.png");
                    Text instructions=game.HUD.transform.Find("Placement instructions").GetComponent<Text>();
                    Check(instructions.text.Contains("1 / 2 CHOOSE") && instructions.text.Contains("LEFT-CLICK A CYAN PAD"),
                        "Persistent gameplay overlay continues to explain tower selection and placement");
                    game.HUD.transform.Find("How to play").GetComponent<Button>().onClick.Invoke();
                    guide.Find("Tutorial skip").GetComponent<Button>().onClick.Invoke();
                    Check(!game.HUD.TutorialOpen && game.State==DefenseState.Building && !game.Paused,
                        "Help button reopens the guide and Close returns to the same game");
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
                    Check(game.Credits==240,"All ten training-wave kills pay correctly and there is no free wave-clear money");
                    game.Choose(TowerKind.Siege);Check(game.TryPlaceAt(game.Pads[3]),"Next wave permits buying one additional tower");
                    DefenseTower siege=game.SelectedTower;
                    Check(testTower.FireRate!=siege.FireRate && testTower.ProjectileSpeed!=siege.ProjectileSpeed && testTower.Height!=siege.Height,"Tower types still differ in fire rate, projectile speed and height");
                    game.SellSelected();
                    Check(game.Credits==207 && game.Towers.Count==1 && game.PlacementsRemaining==0,"Selling refunds 70 percent but does not restore the wave allowance");
                    Check(!game.TryPlaceAt(game.Pads[3]) && game.Credits==207,"Sell-and-rebuild cannot bypass the one-per-wave rule");
                    game.SelectTower(testTower);game.SellSelected();Check(game.Towers.Count==1,"Cannot strand the player by selling their only tower after using the allowance");
                    game.UpgradeSelected();float maxHeight=testTower.Height;
                    Check(testTower.Level==3 && game.Credits==117 && maxHeight>2.9f,"A second upgrade spends $90 and makes the tower taller again");
                    game.UpgradeSelected();Check(testTower.Level==3 && game.Credits==117,"Maximum level cannot consume money or grow indefinitely");
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
                    Check(DefenseGame.TotalWaves==10 && total==522 && scouts>240 && hasBike && hasTruck,
                        "Ten escalating waves contain 522 enemies with larger Scout groups and both new vehicle types");
                    ValidateMerging();
                    game.StartGame();testTower=TestTower(TowerKind.Pulse,1);
                    game.LaunchWave();testEnemy=game.SpawnEnemy(EnemyKind.Armored);testEnemy.SetDistance(7);
                    mergedFireTower=TestTower(TowerKind.Siege,7);DefenseTower feeder=TestTower(TowerKind.Siege,8);
                    game.TryMergeTowers(feeder,mergedFireTower);
                    mergedFireEnemy=game.SpawnEnemy(EnemyKind.Armored);mergedFireEnemy.SetDistance(game.Route.Length-14);
                    shotsAtStart=testTower.ShotsFired;simulatedDue=Time.time+2.1f;due=now+.1;stage++;break;
                case 3:
                    Check(testEnemy!=null && Vector3.Distance(testEnemy.Body.position,game.Route.Position(testEnemy.DistanceTravelled)+Vector3.up*.7f)<.15f,"Enemy body stays synchronized with curved-route progress");
                    Check(testEnemy.CurrentVelocity.magnitude<=testEnemy.Speed*1.1f,"Reported target velocity matches physical speed");
                    int count=testTower.ShotsFired-shotsAtStart;
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
                    game.StartGame();Check(game.Credits==110 && game.Towers.Count==0 && game.Wave==0 && game.Kills==0 && game.KillEarnings==0 && game.PlacementsRemaining==1,"Restart resets money, towers, rewards, waves and build allowance");
                    game.enabled=true;BuildCampaignDefense();game.LaunchWave();Time.timeScale=10;
                    Debug.Log("DEFENSE CAMPAIGN: one placement per wave, $110 start, kill money only");due=now+.05;stage++;break;
                case 6:
                    if(game.State==DefenseState.Lost) throw new Exception("Hard campaign lost in wave "+game.Wave+", money="+game.Credits+", kills="+game.Kills+", towers="+game.Towers.Count);
                    if(game.State==DefenseState.Won)
                    {
                        int expected=0;for(int wave=1;wave<=DefenseGame.TotalWaves;wave++) expected+=DefenseGame.WavePlan(wave).Count;
                        Check(game.Wave==10 && game.Kills==expected,"Expanded campaign is winnable: all ten waves and 522 enemies defeated");
                        Check(game.TotalTowersPurchased==8 && game.TotalMerges==2 && game.Towers.Count==6,
                            "Winning strategy uses eight PLACE actions and two MERGE actions, never both in one wave");
                        int units=0;foreach(DefenseTower tower in game.Towers) units+=tower.MergedUnits;
                        Check(units==8,"Merging conserves all purchased tower units");
                        int spent=0;foreach(DefenseTower tower in game.Towers) spent+=tower.Investment;
                        Check(game.Credits==DefenseGame.StartingCredits+game.KillEarnings-spent,"Campaign money exactly equals starting cash plus kill rewards minus purchases and upgrades");
                        Check(game.Enemies.Count==0 && game.SpawnRemaining==0,"Victory waits for every active and scheduled enemy");
                        Debug.Log("DEFENSE VALIDATION COMPLETE: "+checks+" checks passed");
                        File.WriteAllText(Path.Combine(Application.dataPath,"../VALIDATION_RUN.txt"),checks+" checks passed; one-tower-per-wave campaign won; kills="+game.Kills+"; kill earnings="+game.KillEarnings);
                        EditorApplication.ExitPlaymode();stage++;return;
                    }
                    if(game.TowersPlacedThisWave>1 || game.WaveActionsUsed>1) throw new Exception("Campaign exceeded the one-tower allowance");
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
    // Focused merge fixtures only. The later campaign starts fresh and buys every tower legally.
    private static DefenseTower TestTower(TowerKind kind,int pad)
    {
        GameObject root=new GameObject("Merge test "+kind);
        root.transform.position=game.Pads[pad]+Vector3.up*.2f;
        DefenseTower tower=root.AddComponent<DefenseTower>();tower.Initialize(game,kind,pad);game.Towers.Add(tower);
        return tower;
    }
    private static void ValidateMerging()
    {
        game.StartGame();game.LaunchWave();
        DefenseTower source=TestTower(TowerKind.Pulse,3),target=TestTower(TowerKind.Pulse,1),different=TestTower(TowerKind.Siege,7);
        game.SelectTower(source);game.UpgradeSelected();
        int money=game.Credits,count=game.Towers.Count;
        Check(!game.TryMergeTowers(source,different) && game.Towers.Count==count && game.Credits==money && game.WaveActionsRemaining==1,
            "Mismatched towers are rejected without spending cash or the shared action");
        Check(!game.TryMergeTowers(source,source) && game.WaveActionsRemaining==1,"Self merge does not spend an action");
        Vector3 original=source.transform.position;
        Check(game.Merging.BeginDrag(source) && game.Merging.IsDraggingTower(source),"Drag creates a visual tower preview");
        game.Merging.Cancel();
        Check(source.transform.position==original && source.GetComponentInChildren<MeshRenderer>().enabled && game.WaveActionsRemaining==1,
            "Cancelling restores the tower and leaves the shared action available");
        game.Merging.BeginDrag(source);game.Merging.DropOn(null);
        Check(game.Towers.Count==count && game.WaveActionsRemaining==1,"Empty drop uses no tower or action");
        game.Merging.BeginDrag(source);game.Merging.DropOn(different);
        Check(game.Towers.Count==count && game.WaveActionsRemaining==1,"Wrong-type drop uses no tower or action");
        game.TogglePause();Check(!game.TryMergeTowers(source,target),"Merging is blocked while paused");game.TogglePause();
        game.HUD.OpenTutorial(false);Check(!game.TryMergeTowers(source,target),"Tutorial blocks actual merging");game.HUD.CloseTutorial();
        float height=source.Height,damage=source.Damage,rate=source.FireRate,range=source.Range;
        int investment=source.Investment+target.Investment;
        game.Merging.BeginDrag(source);Check(game.Merging.DropOn(target),"Matching drag/drop performs a merge");
        Check(game.Towers.Count==count-1 && !game.Towers.Contains(source) && target.PadIndex==1,
            "Merge consumes exactly the source and retains the destination pad");
        Check(target.MergedUnits==2 && target.Level==2 && target.Investment==investment,
            "Merge retains the higher paid level and complete investment");
        Check(target.Height>height && target.Damage>damage && target.FireRate>rate && target.Range>range && target.transform.localScale.x>1,
            "Merge grows real geometry and raises combat stats");
        Check(game.Credits==money && game.WaveActionsRemaining==0 && game.LastWaveAction==WaveAction.Merge,
            "Successful merge spends the wave action and no extra cash");
        game.SpawnEnemy(EnemyKind.Truck).TakeDamage(10000);money=game.Credits;
        game.Choose(TowerKind.Pulse);
        Check(money>=75 && !game.TryPlaceAt(game.Pads[3]) && game.Credits==money,
            "After MERGE, placing a tower is blocked even when money and a pad are available");
        DefenseTower third=TestTower(TowerKind.Pulse,5);
        Check(!game.TryMergeTowers(target,third) && third.MergedUnits==1 && game.WaveActionsRemaining==0,
            "Second merge in the same wave is blocked");
        game.SelectTower(target);game.UpgradeSelected();
        Check(target.Level==3 && game.WaveActionsRemaining==0,"Paid cash upgrades remain available after the wave action is used");
        Ray ray=game.View.ScreenPointToRay(game.View.WorldToScreenPoint(target.transform.position+Vector3.up*target.Height));
        Check(game.Merging.PickTower(ray,target.transform.position)==target,"Tall fused tower can be picked by its model");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-captureDefense")>=0) CaptureTo("MERGE_PREVIEW.png");
        game.StartGame();game.Choose(TowerKind.Pulse);game.TryPlaceAt(game.Pads[1]);
        source=game.Towers[0];target=TestTower(TowerKind.Pulse,3);
        Check(!game.TryMergeTowers(source,target) && game.Towers.Count==2 && game.LastWaveAction==WaveAction.Place,
            "After PLACE, merging is blocked even with a matching partner");
        game.StartGame();source=TestTower(TowerKind.Pulse,1);target=TestTower(TowerKind.Pulse,3);game.SelectTower(source);
        game.HUD.transform.Find("Merge").GetComponent<Button>().onClick.Invoke();
        Check(game.Merging.Active,"MERGE button arms the click alternative while an action is available");
        Check(game.Merging.DropOn(target) && game.WaveActionsRemaining==0 && game.TotalMerges==1,
            "Click-to-merge also consumes the same shared action");
    }
    private static void BuildCampaignDefense()
    {
        if(game.WaveActionsRemaining>0)
        {
            int wave=game.PlacementWave;
            if(wave==4 || wave==8)
            {
                int sourcePad=wave==4 ? 5 : 6,targetPad=wave==4 ? 1 : 3;
                DefenseTower from=game.Towers.Find(t=>t.PadIndex==sourcePad),to=game.Towers.Find(t=>t.PadIndex==targetPad);
                if(from==null || to==null || !game.TryMergeTowers(from,to)) throw new Exception("Campaign merge failed in wave "+wave);
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
        }
        foreach(int pad in new[]{3,7,1,5,6,2,9})
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
