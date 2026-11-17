using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

public enum DefenseState { Title, Building, Combat, Won, Lost }

public class DefenseGame : MonoBehaviour
{
    public const int TowerLimit=10;
    public const int TotalWaves=10;
    public const int StartingCredits=110;
    public int TowersPlacedThisWave { get; private set; }
    public int TotalTowersPurchased { get; private set; }
    public int TotalMerges { get; private set; }
    public int PlacementWave => State==DefenseState.Building ? Mathf.Min(TotalWaves,Wave+1) : Mathf.Max(1,Wave);
    public int KillEarnings { get; private set; }
    public DefenseState State { get; private set; }=DefenseState.Title;
    public bool IsCombat => State==DefenseState.Combat && !Paused;
    public bool CanBuild => State==DefenseState.Building || State==DefenseState.Combat;
    public bool Paused { get; private set; }
    public bool Fast { get; private set; }
    public int Credits { get; private set; }
    public int Kills { get; private set; }
    public int Wave { get; private set; }
    public int SpawnRemaining => wavePlan.Count-spawned;
    public DefenseRoute Route { get; private set; }
    public Camera View { get; private set; }
    public DefenseAudio Audio { get; private set; }
    public DefenseHUD HUD { get; private set; }
    public DefenseMerging Merging { get; private set; }
    public TowerKind SelectedKind { get; private set; }
    public DefenseTower SelectedTower { get; private set; }
    public readonly List<DefenseEnemy> Enemies=new List<DefenseEnemy>();
    public readonly List<DefenseTower> Towers=new List<DefenseTower>();
    public readonly Vector3[] Pads={new Vector3(-16,0,-1),new Vector3(-9,0,-1),new Vector3(-9,0,8.5f),
        new Vector3(-.5f,0,0),new Vector3(0,0,-8.5f),new Vector3(8,0,0),new Vector3(8,0,9.2f),
        new Vector3(16,0,0),new Vector3(16,0,-8.5f),new Vector3(-16,0,8.5f)};
    private List<EnemyKind> wavePlan=new List<EnemyKind>();
    private int spawned;
    private float nextSpawn;
    private LineRenderer rangeRing;
    private Transform battlefield;

    void Start()
    {
        Time.timeScale=1;Physics.gravity=new Vector3(0,-9.81f,0);
        Physics.IgnoreLayerCollision(8,8,true);Physics.IgnoreLayerCollision(8,9,false);Physics.IgnoreLayerCollision(8,0,false);
        Route=new DefenseRoute();
        GameObject cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";
        View=cameraObject.GetComponent<Camera>();View.transform.position=new Vector3(27,38,-35);
        View.transform.LookAt(new Vector3(0,0,-.5f));View.orthographic=true;View.nearClipPlane=.1f;View.farClipPlane=180;
        View.clearFlags=CameraClearFlags.SolidColor;View.backgroundColor=new Color(.012f,.018f,.045f);View.allowHDR=true;
        View.allowMSAA=true;View.allowDynamicResolution=false;
        UniversalAdditionalCameraData cameraData=View.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing=true;
        cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality=AntialiasingQuality.High;
        Audio=gameObject.AddComponent<DefenseAudio>();Audio.Initialize(this);
        FitCamera();
        battlefield=new GameObject("Battlefield").transform;
        DefenseArt.World(this,battlefield);
        rangeRing=DefenseArt.Ring(battlefield,"Selected range",new Color(.05f,.55f,.65f));rangeRing.enabled=false;
        if(EventSystem.current==null) new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        HUD=new GameObject("Defense interface").AddComponent<DefenseHUD>();HUD.Initialize(this);
        Merging=gameObject.AddComponent<DefenseMerging>();Merging.Initialize(this,battlefield);
        HUD.ShowMenu("NEON BASTION","THE RELAY MUST HOLD", "Build your defense. Stop every drone before it reaches the cyan relay.\nOne escaped enemy ends the run. Survive all ten waves.\n\nBuild and merge whenever you can afford it. No per-wave action limit.\nEvery kill earns cash. Upgrade towers to make them stronger and taller.","ENTER SECTOR");
    }
    private void FitCamera() { View.orthographicSize=Mathf.Max(21.5f,33f/Mathf.Max(.5f,View.aspect)); }

    public void StartGame()
    {
        if(Merging!=null) Merging.Cancel();
        foreach(DefenseEnemy enemy in Enemies.ToArray()) if(enemy!=null) { enemy.gameObject.SetActive(false);Destroy(enemy.gameObject); }
        foreach(DefenseTower tower in Towers.ToArray()) if(tower!=null) { tower.gameObject.SetActive(false);Destroy(tower.gameObject); }
        Enemies.Clear();Towers.Clear();ClearShots();
        Credits=StartingCredits;Kills=0;KillEarnings=0;TowersPlacedThisWave=0;TotalTowersPurchased=0;TotalMerges=0;Wave=0;spawned=0;wavePlan.Clear();SelectedTower=null;SelectedKind=TowerKind.Pulse;
        Paused=false;Fast=false;Time.timeScale=1;State=DefenseState.Building;rangeRing.enabled=false;
        HUD.HideMenu();HUD.Message("Wave 1: try a Pulse on the gold START HERE pad. No upgrades needed for this training wave. Save kill money for towers, merges and upgrades.");HUD.Refresh();
    }
    public void Choose(TowerKind kind)
    {
        if(Merging!=null) Merging.Cancel();
        SelectedKind=kind;SelectedTower=null;rangeRing.enabled=false;
        HUD.Message(kind==TowerKind.Pulse ? "PULSE: rapid fire · $75. Build whenever you can afford it." : "SIEGE: heavy ballistic shells · $110. Build whenever you can afford it.");HUD.Refresh();
    }
    public bool TryPlaceAt(Vector3 point)
    {
        if(!CanBuild || Paused || HUD.TutorialOpen) return false;
        int pad=NearestPad(point);
        if(pad<0) { HUD.Message("Build only on marked cyan hex pads.");return false; }
        DefenseTower existing=Towers.Find(t=>t.PadIndex==pad);
        if(existing!=null) { SelectTower(existing);return false; }
        if(Towers.Count>=TowerLimit) { HUD.Message("Tower limit reached (10). Upgrade or sell an existing tower.");return false; }
        int cost=DefenseTower.Cost(SelectedKind);
        if(Credits<cost) { HUD.Message("Not enough money. Kill enemies to save for this tower.");return false; }
        Spend(cost);TowersPlacedThisWave++;TotalTowersPurchased++;
        GameObject root=new GameObject(SelectedKind+" tower");root.transform.position=Pads[pad]+Vector3.up*.2f;
        DefenseTower tower=root.AddComponent<DefenseTower>();tower.Initialize(this,SelectedKind,pad);Towers.Add(tower);
        SelectTower(tower);DefenseArt.Burst(root.transform.position+Vector3.up,DefenseArt.Cyan,.45f);
        HUD.Message("Tower built. Keep buying, merging or upgrading as your budget allows.");HUD.Refresh();return true;
    }
    public int NearestPad(Vector3 point)
    {
        point.y=0;int best=-1;float distance=1.35f*1.35f;
        for(int i=0;i<Pads.Length;i++){float d=(point-Pads[i]).sqrMagnitude;if(d<distance){distance=d;best=i;}}
        return best;
    }
    public void SelectTower(DefenseTower tower)
    {
        SelectedTower=tower;rangeRing.enabled=true;DefenseArt.SetRing(rangeRing,tower.transform.position,tower.Range);HUD.Refresh();
    }
    public void RecordMerge() { TotalMerges++; }
    public bool Spend(int amount)
    {
        if(Credits<amount) { HUD.Message("Not enough money. Each kill pays cash for towers and upgrades.");return false; }
        Credits-=amount;return true;
    }
    public void UpgradeSelected()
    {
        if(Merging!=null) Merging.Cancel();
        if(!CanBuild || Paused || SelectedTower==null) return;
        if(SelectedTower.Level>=3) HUD.Message("This tower is at maximum level.");
        else if(SelectedTower.Upgrade()) { SelectTower(SelectedTower);HUD.Message("Upgrade installed: taller tower, more damage, faster fire and longer range."); }
        HUD.Refresh();
    }
    public void SellSelected()
    {
        if(Merging!=null) Merging.Cancel();
        if(!CanBuild || Paused || SelectedTower==null) return;
        if(Towers.Count==1 && Credits+Mathf.FloorToInt(SelectedTower.Investment*.7f)<DefenseTower.Cost(TowerKind.Pulse)) { HUD.Message("Keep your only tower until its refund and your cash can buy a replacement.");return; }
        int refund=Mathf.FloorToInt(SelectedTower.Investment*.7f);Credits+=refund;
        Towers.Remove(SelectedTower);SelectedTower.gameObject.SetActive(false);Destroy(SelectedTower.gameObject);SelectedTower=null;rangeRing.enabled=false;
        HUD.Message("Tower sold for $"+refund+". Spend the refund on your next defense.");HUD.Refresh();
    }

    public static List<EnemyKind> WavePlan(int wave)
    {
        int[] sizes={10,24,32,40,48,56,64,72,80,96};
        int count=sizes[Mathf.Clamp(wave-1,0,sizes.Length-1)];
        List<EnemyKind> plan=new List<EnemyKind>(count);
        for(int i=0;i<count;i++)
        {
            EnemyKind kind=EnemyKind.Scout;
            if(wave>=2 && i%5==1) kind=EnemyKind.Runner;
            if(wave>=3 && i%6==3) kind=EnemyKind.Armored;
            if(wave>=4 && i%7==5) kind=EnemyKind.Motorcycle;
            if(wave>=5 && i==count/2) kind=EnemyKind.Truck;
            if(wave==5 && i==count-1) kind=EnemyKind.Siege;
            if(wave>=6 && i==count-1) kind=EnemyKind.Truck;
            if(wave>=7 && i%17==10) kind=EnemyKind.Truck;
            if(wave==10 && i==count-5) kind=EnemyKind.Truck;
            plan.Add(kind);
        }
        return plan;
    }
    public static string WaveName(int wave)
    {
        string[] names={"Scout patrol","Reinforcements","Armored column","Motorcycle rush","Heavy convoy",
            "Open highway","Iron road","Overdrive","Last perimeter","Final convoy"};
        return names[Mathf.Clamp(wave-1,0,names.Length-1)];
    }
    public void LaunchWave()
    {
        if(Merging!=null) Merging.Cancel();
        if(State!=DefenseState.Building || Paused) return;
        Wave++;State=DefenseState.Combat;wavePlan=WavePlan(Wave);spawned=0;nextSpawn=Time.time+.8f;
        HUD.Message("WAVE "+Wave+" / "+WaveName(Wave)+". Build, merge and upgrade using kill money.");HUD.Refresh();
    }
    public DefenseEnemy SpawnEnemy(EnemyKind kind)
    {
        GameObject root=new GameObject(kind+(kind==EnemyKind.Truck || kind==EnemyKind.Motorcycle ? " vehicle" : " drone"));DefenseEnemy enemy=root.AddComponent<DefenseEnemy>();enemy.Initialize(this,kind,Mathf.Max(1,Wave));Enemies.Add(enemy);return enemy;
    }
    public void EnemyDestroyed(DefenseEnemy enemy)
    {
        if(!Enemies.Remove(enemy)) return;
        int reward=enemy.Reward;
        Credits+=reward;KillEarnings+=reward;Kills++;
        HUD.ShowReward(reward,enemy.transform.position);HUD.Refresh();CheckWaveEnd();
    }
    public void EnemyEscaped(DefenseEnemy enemy)
    {
        if(State!=DefenseState.Combat) return;
        State=DefenseState.Lost;Finish("RELAY BREACHED","A "+enemy.Kind.ToString().ToUpper()+" completed the route.","One breach is enough. Try moving towers to cover several bends,\nand combine rapid Pulse fire with heavy Siege shells.");
    }
    private void CheckWaveEnd()
    {
        if(State!=DefenseState.Combat || SpawnRemaining>0 || Enemies.Count>0) return;
        ClearShots();
        if(Wave>=TotalWaves) { State=DefenseState.Won;Finish("SECTOR SECURED","ALL "+TotalWaves+" WAVES DEFEATED","The relay is safe.\n"+Kills+" enemies destroyed · "+Towers.Count+" towers deployed."); }
        else { State=DefenseState.Building;TowersPlacedThisWave=0;HUD.Message("Wave cleared! Wave "+(Wave+1)+": spend wisely on new towers, matching merges and upgrades.");HUD.Refresh(); }
    }
    private void Finish(string title,string subtitle,string description)
    {
        if(Merging!=null) Merging.Cancel();
        Paused=false;Fast=false;Time.timeScale=1;ClearShots();rangeRing.enabled=false;
        HUD.ShowMenu(title,subtitle,description,"PLAY AGAIN");HUD.Refresh();
    }
    private void ClearShots()
    {
        foreach(DefenseProjectile shot in FindObjectsByType<DefenseProjectile>()) { shot.gameObject.SetActive(false);Destroy(shot.gameObject); }
    }
    public void TogglePause()
    {
        if(Merging!=null) Merging.Cancel();
        if(!CanBuild) return;
        Paused=!Paused;ApplyTime();HUD.Message(Paused ? "PAUSED · Press P or the pause button to resume." : "Resumed. Protect the cyan relay.");HUD.Refresh();
    }
    public void ToggleSpeed() { if(!CanBuild) return;Fast=!Fast;ApplyTime();HUD.Refresh(); }
    private void ApplyTime() { Time.timeScale=Paused ? 0 : Fast ? 2 : 1; }
    void Update()
    {
        if(View==null) return;
        FitCamera();Keyboard keys=Keyboard.current;
        if(HUD.TutorialOpen)
        {
            if(keys!=null)
            {
                if(keys.escapeKey.wasPressedThisFrame) HUD.CloseTutorial();
                else if(keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) HUD.NextTutorialPage();
                else if(keys.leftArrowKey.wasPressedThisFrame) HUD.PreviousTutorialPage();
            }
            return;
        }
        if(keys!=null && keys.hKey.wasPressedThisFrame) { HUD.OpenTutorial(false);return; }
        if(keys!=null)
        {
            if((State==DefenseState.Title || State==DefenseState.Lost || State==DefenseState.Won) && keys.enterKey.wasPressedThisFrame) HUD.OpenTutorial(true);
            else if(CanBuild)
            {
                if(keys.pKey.wasPressedThisFrame) TogglePause();
                if(!Paused)
                {
                    if(keys.digit1Key.wasPressedThisFrame) Choose(TowerKind.Pulse);
                    if(keys.digit2Key.wasPressedThisFrame) Choose(TowerKind.Siege);
                    if(keys.spaceKey.wasPressedThisFrame) LaunchWave();
                    if(keys.uKey.wasPressedThisFrame) UpgradeSelected();
                    if(keys.xKey.wasPressedThisFrame) SellSelected();
                    if(keys.mKey.wasPressedThisFrame) Merging.BeginSelectedMerge();
                    if(keys.escapeKey.wasPressedThisFrame) Merging.Cancel();
                }
            }
        }
        if(IsCombat && spawned<wavePlan.Count && Time.time>=nextSpawn)
        { SpawnEnemy(wavePlan[spawned++]);nextSpawn=Time.time+(Wave==1 ? 1.4f : Mathf.Max(.4f,.9f-Wave*.05f));HUD.Refresh(); }
        Merging.HandlePointer();
    }
    public bool TryMergeTowers(DefenseTower source,DefenseTower target) => Merging.TryMerge(source,target);
    void OnDestroy() { Time.timeScale=1; }
}
