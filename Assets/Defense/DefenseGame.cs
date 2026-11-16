using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

public enum DefenseState { Title, Building, Combat, Won, Lost }

public class DefenseGame : MonoBehaviour
{
    public const int TowerLimit=6;
    public const int TotalWaves=5;
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
    public DefenseHUD HUD { get; private set; }
    public TowerKind SelectedKind { get; private set; }
    public DefenseTower SelectedTower { get; private set; }
    public readonly List<DefenseEnemy> Enemies=new List<DefenseEnemy>();
    public readonly List<DefenseTower> Towers=new List<DefenseTower>();
    public readonly Vector3[] Pads={new Vector3(-16,0,-1),new Vector3(-9,0,-1),new Vector3(-9,0,8.5f),
        new Vector3(-.5f,0,0),new Vector3(0,0,-8.5f),new Vector3(8,0,0),new Vector3(8,0,9.2f),
        new Vector3(16,0,0),new Vector3(16,0,-8.5f)};
    private List<EnemyKind> wavePlan=new List<EnemyKind>();
    private int spawned;
    private float nextSpawn;
    private LineRenderer rangeRing;
    private LineRenderer padRing;
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
        View.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        FitCamera();
        battlefield=new GameObject("Battlefield").transform;
        DefenseArt.World(this,battlefield);
        rangeRing=DefenseArt.Ring(battlefield,"Selected range",new Color(.05f,.55f,.65f));rangeRing.enabled=false;
        padRing=DefenseArt.Ring(battlefield,"Build cursor",DefenseArt.Cyan);padRing.enabled=false;
        if(EventSystem.current==null) new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        HUD=new GameObject("Defense interface").AddComponent<DefenseHUD>();HUD.Initialize(this);
        HUD.ShowMenu("NEON BASTION","THE RELAY MUST HOLD", "Build your defense. Stop every drone before it reaches the cyan relay.\nOne escaped enemy ends the run. Survive all five waves.\n\nChoose a tower, then click a glowing hex pad.\nTowers track, predict and fire automatically.","ENTER SECTOR");
    }
    private void FitCamera() { View.orthographicSize=Mathf.Max(21.5f,33f/Mathf.Max(.5f,View.aspect)); }

    public void StartGame()
    {
        foreach(DefenseEnemy enemy in Enemies.ToArray()) if(enemy!=null) { enemy.gameObject.SetActive(false);Destroy(enemy.gameObject); }
        foreach(DefenseTower tower in Towers.ToArray()) if(tower!=null) { tower.gameObject.SetActive(false);Destroy(tower.gameObject); }
        Enemies.Clear();Towers.Clear();ClearShots();
        Credits=320;Kills=0;Wave=0;spawned=0;wavePlan.Clear();SelectedTower=null;SelectedKind=TowerKind.Pulse;
        Paused=false;Fast=false;Time.timeScale=1;State=DefenseState.Building;rangeRing.enabled=false;
        HUD.HideMenu();HUD.Message("Choose a tower, then click a cyan hex pad. Press SPACE when ready.");HUD.Refresh();
    }
    public void Choose(TowerKind kind)
    {
        SelectedKind=kind;SelectedTower=null;rangeRing.enabled=false;
        HUD.Message(kind==TowerKind.Pulse ? "PULSE: rapid fire · 75 credits. Click a free cyan hex pad." : "SIEGE: heavy ballistic shells · 110 credits. Click a free cyan hex pad.");HUD.Refresh();
    }
    public bool TryPlaceAt(Vector3 point)
    {
        if(!CanBuild || Paused) return false;
        int pad=NearestPad(point);
        if(pad<0) { HUD.Message("Build only on marked cyan hex pads.");return false; }
        DefenseTower existing=Towers.Find(t=>t.PadIndex==pad);
        if(existing!=null) { SelectTower(existing);return false; }
        if(Towers.Count>=TowerLimit) { HUD.Message("Tower limit reached (6). Upgrade or sell an existing tower.");return false; }
        int cost=DefenseTower.Cost(SelectedKind);
        if(!Spend(cost)) return false;
        GameObject root=new GameObject(SelectedKind+" tower");root.transform.position=Pads[pad]+Vector3.up*.2f;
        DefenseTower tower=root.AddComponent<DefenseTower>();tower.Initialize(this,SelectedKind,pad);Towers.Add(tower);
        SelectTower(tower);DefenseArt.Burst(root.transform.position+Vector3.up,DefenseArt.Cyan,.45f);
        HUD.Message("Tower online. Select a tower to inspect, upgrade or sell it.");HUD.Refresh();return true;
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
    public bool Spend(int amount)
    {
        if(Credits<amount) { HUD.Message("Not enough credits. Destroy enemies to earn more.");return false; }
        Credits-=amount;return true;
    }
    public void UpgradeSelected()
    {
        if(!CanBuild || Paused || SelectedTower==null) return;
        if(SelectedTower.Level>=3) HUD.Message("This tower is at maximum level.");
        else if(SelectedTower.Upgrade()) { SelectTower(SelectedTower);HUD.Message("Upgrade installed: more damage, faster fire, longer range."); }
        HUD.Refresh();
    }
    public void SellSelected()
    {
        if(!CanBuild || Paused || SelectedTower==null) return;
        int refund=Mathf.FloorToInt(SelectedTower.Investment*.7f);Credits+=refund;
        Towers.Remove(SelectedTower);SelectedTower.gameObject.SetActive(false);Destroy(SelectedTower.gameObject);SelectedTower=null;rangeRing.enabled=false;
        HUD.Message("Tower sold for "+refund+" credits. Its pad and tower slot are available again.");HUD.Refresh();
    }

    public static List<EnemyKind> WavePlan(int wave)
    {
        List<EnemyKind> plan=new List<EnemyKind>();int count=wave==1 ? 8 : wave==2 ? 11 : wave==3 ? 13 : wave==4 ? 16 : 20;
        for(int i=0;i<count;i++)
            plan.Add(wave==5 && i==count-1 ? EnemyKind.Siege : wave>=3 && i%4==2 ? EnemyKind.Armored : wave>=2 && i%3==1 ? EnemyKind.Runner : EnemyKind.Scout);
        return plan;
    }
    public void LaunchWave()
    {
        if(State!=DefenseState.Building || Paused) return;
        Wave++;State=DefenseState.Combat;wavePlan=WavePlan(Wave);spawned=0;nextSpawn=Time.time+.8f;
        HUD.Message("WAVE "+Wave+" incoming. Towers fire automatically. You can still build and upgrade.");HUD.Refresh();
    }
    public DefenseEnemy SpawnEnemy(EnemyKind kind)
    {
        GameObject root=new GameObject(kind+" drone");DefenseEnemy enemy=root.AddComponent<DefenseEnemy>();enemy.Initialize(this,kind,Mathf.Max(1,Wave));Enemies.Add(enemy);return enemy;
    }
    public void EnemyDestroyed(DefenseEnemy enemy)
    {
        if(!Enemies.Remove(enemy)) return;
        Credits+=enemy.Reward;Kills++;HUD.Refresh();CheckWaveEnd();
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
        if(Wave>=TotalWaves) { State=DefenseState.Won;Finish("SECTOR SECURED","ALL FIVE WAVES DEFEATED","The relay is safe.\n"+Kills+" enemies destroyed · "+Towers.Count+" towers deployed."); }
        else { State=DefenseState.Building;Credits+=45;HUD.Message("Wave cleared! +45 credits. Reinforce, upgrade, then press SPACE for wave "+(Wave+1)+".");HUD.Refresh(); }
    }
    private void Finish(string title,string subtitle,string description)
    {
        Paused=false;Fast=false;Time.timeScale=1;ClearShots();rangeRing.enabled=false;padRing.enabled=false;
        HUD.ShowMenu(title,subtitle,description,"PLAY AGAIN");HUD.Refresh();
    }
    private void ClearShots()
    {
        foreach(DefenseProjectile shot in FindObjectsByType<DefenseProjectile>()) { shot.gameObject.SetActive(false);Destroy(shot.gameObject); }
    }
    public void TogglePause()
    {
        if(!CanBuild) return;
        Paused=!Paused;ApplyTime();HUD.Message(Paused ? "PAUSED · Press P or the pause button to resume." : "Resumed. Protect the cyan relay.");HUD.Refresh();
    }
    public void ToggleSpeed() { if(!CanBuild) return;Fast=!Fast;ApplyTime();HUD.Refresh(); }
    private void ApplyTime() { Time.timeScale=Paused ? 0 : Fast ? 2 : 1; }
    void Update()
    {
        if(View==null) return;
        FitCamera();Keyboard keys=Keyboard.current;
        if(keys!=null)
        {
            if((State==DefenseState.Title || State==DefenseState.Lost || State==DefenseState.Won) && keys.enterKey.wasPressedThisFrame) StartGame();
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
                }
            }
        }
        if(IsCombat && spawned<wavePlan.Count && Time.time>=nextSpawn)
        { SpawnEnemy(wavePlan[spawned++]);nextSpawn=Time.time+Mathf.Max(.55f,1f-Wave*.07f);HUD.Refresh(); }
        HandlePointer();
    }
    private void HandlePointer()
    {
        padRing.enabled=false;
        if(!CanBuild || Paused || Mouse.current==null || (EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject())) return;
        Ray ray=View.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane=new Plane(Vector3.up,Vector3.zero);
        if(!plane.Raycast(ray,out float distance)) return;
        Vector3 point=ray.GetPoint(distance);int pad=NearestPad(point);
        if(pad>=0) { padRing.enabled=true;DefenseArt.SetRing(padRing,Pads[pad],1.3f); }
        if(Mouse.current.leftButton.wasPressedThisFrame) TryPlaceAt(point);
    }
    void OnDestroy() { Time.timeScale=1; }
}
