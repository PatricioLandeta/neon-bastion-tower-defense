using UnityEngine;
using UnityEngine.UI;

// Each tutorial page uses isolated visual models, with no combat actors or economy commands.
// This preview runs on unscaled time while the actual battle is paused.
public class DefenseMergeDemo : MonoBehaviour
{
    private const int PreviewLayer=30;
    private static readonly EnemyKind[] EnemyOrder={EnemyKind.Scout,EnemyKind.Runner,EnemyKind.Armored,EnemyKind.Motorcycle,EnemyKind.Truck,EnemyKind.Siege};
    private GameObject world;
    private Transform source,target,siege,sourceGimbal,targetGimbal,cursor,bolt,pad,padInset;
    private readonly Transform[] enemies=new Transform[6],healthRoots=new Transform[6],healthBars=new Transform[6];
    private readonly Transform[] sparks=new Transform[8];
    private Camera previewCamera;
    private RenderTexture image;
    private Text phaseLabel,actionLabel,upgradeButton;
    private LineRenderer rangeRing;
    private float elapsed;
    private int enemyOffset;
    public int Page { get; private set; }
    public bool ShowingMerged { get; private set; }
    public bool ExamplePlaced { get; private set; }
    public int ExampleMergeLevel { get; private set; }=1;
    public int ExampleUpgradeLevel { get; private set; }=1;
    public int ExampleCash { get; private set; }
    public float ExampleHealth { get; private set; }
    public float DisplayedTowerHeight => targetGimbal.localPosition.y;
    public Vector3 ExampleEnemyPosition => enemies[EnemyIndex].localPosition;
    public TowerKind ChosenKind { get; private set; }
    public int EnemyIndex => (enemyOffset+Mathf.FloorToInt(elapsed/9f))%EnemyOrder.Length;
    public EnemyKind PreviewEnemyKind => EnemyOrder[EnemyIndex];
    public float AnimationTime => elapsed;
    public string EnemyExplanation
    {
        get
        {
            DefenseEnemy.EnemyStats stats=DefenseEnemy.BaseStats(PreviewEnemyKind);
            return PreviewEnemyKind.ToString().ToUpper()+"  /  "+(EnemyIndex+1)+" OF 6\n"+
                "Base HP "+stats.Health.ToString("0")+"  ·  Speed "+stats.Speed.ToString("0.##")+"\n"+
                "KILL REWARD  +$"+stats.Reward+"\n\n"+
                "Base Pulse: "+Mathf.CeilToInt(stats.Health/1.15f)+" hits to defeat.\nBase Siege: "+Mathf.CeilToInt(stats.Health/12f)+" hits to defeat.\n"+
                "Shots cost no money. Kills ADD cash to MONEY.\n\n"+
                "Scout $4 / Runner $5 / Armored $8\nMotorcycle $6 / Truck $40 / Siege $30\n\n"+
                "Later waves add 12% base HP per wave.\nWave 1 Scouts: 3 HP, speed 2.\n"+
                "Use PREV / NEXT ENEMY or watch the cycle.\nThe fight preview is sped up; hits assume no misses.";
        }
    }

    public void Initialize(DefenseGame game,RawImage screen,Text phase,Text action,RectTransform pointer,Text upgradeControl)
    {
        phaseLabel=phase;actionLabel=action;cursor=pointer;upgradeButton=upgradeControl;
        world=new GameObject("Tutorial examples — isolated visuals");world.transform.position=new Vector3(0,-1000,0);
        source=Model("Example source Pulse");target=Model("Example destination Pulse");siege=Model("Example Siege");
        DefenseArt.Tower(source,TowerKind.Pulse,1.4f,out sourceGimbal,out _);
        DefenseArt.Tower(target,TowerKind.Pulse,1.4f,out targetGimbal,out _);
        DefenseArt.Tower(siege,TowerKind.Siege,2.8f,out _,out _);
        DefenseArt.MarkFusion(source,DefenseArt.Cyan);DefenseArt.MarkFusion(target,DefenseArt.Cyan);
        rangeRing=DefenseArt.Ring(world.transform,"Example range",new Color(.1f,1f,1.3f));
        pad=PreviewShape("Example cyan hex pad",new Color(.08f,.75f,.9f),new Vector3(1.2f,.25f,1.2f),"HexPlatform");
        padInset=PreviewShape("Example pad inset",new Color(.065f,.1f,.17f),new Vector3(1.05f,.25f,1.05f),"HexPlatform");padInset.localPosition=Vector3.up*.08f;
        bolt=PreviewShape("Illustrative laser",new Color(.2f,2f,2f),new Vector3(.12f,.12f,.4f));
        for(int i=0;i<enemies.Length;i++)
        {
            enemies[i]=Model("Example "+EnemyOrder[i]);
            DefenseArt.Enemy(enemies[i],EnemyOrder[i],out healthRoots[i],out healthBars[i]);
            foreach(DefenseVehicleWheels wheels in enemies[i].GetComponentsInChildren<DefenseVehicleWheels>()) wheels.enabled=false;
            foreach(TrailRenderer trail in enemies[i].GetComponentsInChildren<TrailRenderer>()) { trail.emitting=false;trail.Clear(); }
        }
        for(int i=0;i<sparks.Length;i++) sparks[i]=PreviewShape("Reward spark",new Color(2f,1.3f,.1f),Vector3.one*.12f);
        GameObject cameraObject=new GameObject("Tutorial preview camera",typeof(Camera));cameraObject.transform.SetParent(world.transform,false);
        previewCamera=cameraObject.GetComponent<Camera>();previewCamera.enabled=false;
        previewCamera.transform.localPosition=new Vector3(6,6,-9);previewCamera.transform.LookAt(world.transform.position+Vector3.up*.8f);
        previewCamera.orthographic=true;previewCamera.orthographicSize=3.6f;
        previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.02f,.035f,.07f);
        previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=40;previewCamera.cullingMask=1<<PreviewLayer;previewCamera.allowMSAA=true;
        image=new RenderTexture(1280,576,24,RenderTextureFormat.ARGB32){antiAliasing=4,name="Live tutorial examples"};
        previewCamera.targetTexture=image;screen.texture=image;game.View.cullingMask&=~(1<<PreviewLayer);
        foreach(Transform part in world.GetComponentsInChildren<Transform>(true)) part.gameObject.layer=PreviewLayer;
        SetPage(0);
    }
    private Transform Model(string modelName)
    {
        Transform root=new GameObject(modelName).transform;root.SetParent(world.transform,false);return root;
    }
    private Transform PreviewShape(string modelName,Color color,Vector3 scale,string mesh="Spire")
    {
        Transform root=Model(modelName);root.localScale=scale;
        root.gameObject.AddComponent<MeshFilter>().sharedMesh=DefenseArt.GetMesh(mesh);
        root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=DefenseArt.ColorMaterial(modelName,color);return root;
    }
    public void SetPage(int page) { Page=page;enemyOffset=0;Seek(0); }
    public void CycleEnemy(int delta) { SelectEnemy((EnemyIndex+delta+EnemyOrder.Length)%EnemyOrder.Length); }
    public void SelectEnemy(int index) { enemyOffset=Mathf.Clamp(index,0,EnemyOrder.Length-1);Seek(0); }
    public void Replay() { if(Page==2) enemyOffset=EnemyIndex;Seek(0); }
    public void Seek(float seconds)
    {
        elapsed=Mathf.Max(0,seconds);ApplyAnimation();
        if(previewCamera!=null && world.activeInHierarchy && SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) previewCamera.Render();
    }
    private void ResetModels()
    {
        source.gameObject.SetActive(false);target.gameObject.SetActive(false);siege.gameObject.SetActive(false);bolt.gameObject.SetActive(false);
        foreach(Transform enemy in enemies) enemy.gameObject.SetActive(false);
        foreach(Transform spark in sparks) spark.gameObject.SetActive(false);
        pad.gameObject.SetActive(false);padInset.gameObject.SetActive(false);upgradeButton.transform.parent.gameObject.SetActive(false);
        rangeRing.enabled=false;ShowingMerged=false;ExamplePlaced=false;ExampleMergeLevel=1;ExampleUpgradeLevel=1;
        source.localPosition=new Vector3(-2.25f,0,0);target.localPosition=new Vector3(2.25f,0,0);siege.localPosition=new Vector3(2.25f,0,0);
        source.localScale=target.localScale=Vector3.one;
        DefenseArt.RaiseTower(source,sourceGimbal,TowerKind.Pulse,1.4f,1);DefenseArt.RaiseTower(target,targetGimbal,TowerKind.Pulse,1.4f,1);
        foreach(Transform part in target) if(part.name.StartsWith("Upgrade collar")) part.gameObject.SetActive(false);
        SetFusion(source,1);SetFusion(target,1);
        cursor.gameObject.SetActive(true);actionLabel.color=new Color(.3f,.9f,1f);
    }
    private static void SetFusion(Transform root,int level)
    {
        DefenseArt.MarkFusion(root,DefenseTower.FusionColor(level));
        root.Find("Fusion ring").gameObject.SetActive(level>1);root.Find("Fusion beacon").gameObject.SetActive(level>1);
    }
    private void PointAt(Vector3 point)
    {
        Vector3 viewport=previewCamera.WorldToViewportPoint(point);
        RectTransform pointer=(RectTransform)cursor;pointer.anchorMin=pointer.anchorMax=new Vector2(viewport.x,viewport.y);pointer.anchoredPosition=Vector2.zero;
    }
    private void Range(Transform tower,float radius)
    { rangeRing.enabled=true;DefenseArt.SetRing(rangeRing,tower.position,radius); }
    private void AnimateBolt(Vector3 start,Vector3 end,float cycle)
    {
        bolt.gameObject.SetActive(cycle<.65f);bolt.position=Vector3.Lerp(start,end,Mathf.Clamp01(cycle/.65f));
        if((end-start).sqrMagnitude>.001f) bolt.rotation=Quaternion.LookRotation(end-start);
    }
    private void ApplyAnimation()
    {
        if(world==null) return;
        ResetModels();
        previewCamera.orthographicSize=Page==4 ? 2.5f : Page==2 ? 2.6f : 3.2f;
        switch(Page)
        {
            case 0: ChooseExample();break;
            case 1: PlacementExample();break;
            case 2: EnemyExample();break;
            case 3: UpgradeExample();break;
            default: MergeExample();break;
        }
    }
    private void ChooseExample()
    {
        float phase=elapsed%8f;ChosenKind=phase<4 ? TowerKind.Pulse : TowerKind.Siege;ExampleCash=110;
        source.gameObject.SetActive(true);siege.gameObject.SetActive(true);
        Transform selected=ChosenKind==TowerKind.Pulse ? source : siege;
        selected.localScale=Vector3.one*(1+.035f*Mathf.Sin(elapsed*5));Range(selected,1.35f);
        PointAt(selected.position+Vector3.up*(ChosenKind==TowerKind.Pulse ? 2.2f : 3.5f));
        float fireRate=ChosenKind==TowerKind.Pulse ? 2.4f : .6f;
        AnimateBolt(selected.position+Vector3.up*(ChosenKind==TowerKind.Pulse ? 1.4f : 2.8f),selected.position+new Vector3(0,1,3),elapsed*fireRate%1);
        phaseLabel.text="CHOOSE "+ChosenKind.ToString().ToUpper()+"  ·  PRESS "+(ChosenKind==TowerKind.Pulse ? "1" : "2");
        actionLabel.text=ChosenKind==TowerKind.Pulse ? "$75  ·  1.15 DAMAGE  ·  2.4 SHOTS/S" : "$110  ·  12 DAMAGE  ·  0.6 SHOTS/S";
    }
    private void PlacementExample()
    {
        float phase=elapsed%7f;ExamplePlaced=phase>=3;ExampleCash=ExamplePlaced ? 35 : 110;
        pad.gameObject.SetActive(true);padInset.gameObject.SetActive(true);
        target.localPosition=Vector3.up*.2f;Range(target,1.25f);
        if(!ExamplePlaced)
        {
            source.gameObject.SetActive(true);source.localPosition=Vector3.Lerp(new Vector3(-2.5f,.5f,0),Vector3.up*.2f,Mathf.SmoothStep(0,1,phase/3));
            PointAt(source.position+Vector3.up*2);
        }
        else { target.gameObject.SetActive(true);PointAt(target.position+Vector3.up*2); }
        phaseLabel.text=phase<1.2f ? "1  SELECT PULSE / PRESS 1" : phase<3 ? "2  MOVE TO A FREE CYAN PAD" : "3  LEFT-CLICK → TOWER BUILT";
        actionLabel.text=ExamplePlaced ? "EXAMPLE MONEY  $110 − $75 = $35" : "EXAMPLE MONEY $110  ·  PULSE COST $75";
    }
    private void EnemyExample()
    {
        float phase=elapsed%9f;int index=EnemyIndex;EnemyKind kind=EnemyOrder[index];DefenseEnemy.EnemyStats stats=DefenseEnemy.BaseStats(kind);
        Transform enemy=enemies[index];bool heavy=kind==EnemyKind.Truck || kind==EnemyKind.Siege || kind==EnemyKind.Armored;
        float damage=heavy ? 12 : 1.15f;int hits=Mathf.CeilToInt(stats.Health/damage);
        int landed=Mathf.FloorToInt(Mathf.Clamp01((phase-3.4f)/2.2f)*hits);
        ExampleHealth=Mathf.Max(0,stats.Health-landed*damage);bool defeated=landed>=hits;
        enemy.gameObject.SetActive(!defeated);enemy.localPosition=new Vector3(Mathf.Min(2.5f,-3+stats.Speed*.3f*phase),.25f,0);enemy.localRotation=Quaternion.LookRotation(Vector3.right);
        healthBars[index].localScale=new Vector3(ExampleHealth/stats.Health,1,1);healthRoots[index].rotation=previewCamera.transform.rotation;
        foreach(Transform wheel in enemy.GetComponentsInChildren<Transform>(true))
            if(wheel.name=="Rolling wheel") wheel.localRotation=Quaternion.Euler(-elapsed*stats.Speed*90,0,0);
        ExampleCash=35+(defeated ? stats.Reward : 0);cursor.gameObject.SetActive(false);
        if(phase>=3.4f && !defeated) AnimateBolt(world.transform.position+new Vector3(0,2.4f,-2),enemy.position+Vector3.up*.3f,(phase-3.4f)*hits/2.2f%1);
        if(defeated)
            for(int i=0;i<sparks.Length;i++)
            {
                float angle=i*Mathf.PI/4,spread=Mathf.Min(1,phase-5.6f);sparks[i].gameObject.SetActive(phase<6.8f);
                sparks[i].position=enemy.position+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle)+.5f,0)*spread;
            }
        phaseLabel.text=kind.ToString().ToUpper()+"  ·  "+(phase<3.4f ? "WATCH ITS SPEED" : defeated ? "DEFEATED → +$"+stats.Reward : "FAST-FORWARD: "+landed+" / "+hits+" HITS");
        actionLabel.text="HP "+ExampleHealth.ToString("0.#")+" / "+stats.Health+"  ·  EXAMPLE MONEY $"+ExampleCash;
        if(defeated) actionLabel.color=new Color(1f,.8f,.3f);
    }
    private void UpgradeExample()
    {
        float phase=elapsed%10f;ExampleUpgradeLevel=phase<3 ? 1 : phase<6 ? 2 : 3;ExampleCash=ExampleUpgradeLevel==1 ? 350 : ExampleUpgradeLevel==2 ? 250 : 50;
        target.gameObject.SetActive(true);target.localPosition=Vector3.zero;
        float height=1.4f+.8f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,3.7f,phase))+.8f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,6.7f,phase));
        DefenseArt.RaiseTower(target,targetGimbal,TowerKind.Pulse,height,ExampleUpgradeLevel);
        foreach(Transform part in target) if(part.name.StartsWith("Upgrade collar"))
        { part.gameObject.layer=PreviewLayer;part.gameObject.SetActive(part.name=="Upgrade collar 2" && ExampleUpgradeLevel>=2 || part.name=="Upgrade collar 3" && ExampleUpgradeLevel>=3); }
        upgradeButton.transform.parent.gameObject.SetActive(phase>=1.5f);
        upgradeButton.text=phase<3 ? "U  UPGRADE\n$100" : phase<6 ? "U  UPGRADE\n$200" : "LEVEL 3\nMAX";
        upgradeButton.color=phase>=6 ? new Color(.4f,1f,.7f) : Color.white;
        if(phase<1.5f) PointAt(target.position+Vector3.up*(height+.7f));
        else { RectTransform pointer=(RectTransform)cursor;pointer.anchorMin=pointer.anchorMax=new Vector2(.81f,.21f);pointer.anchoredPosition=Vector2.zero; }
        Range(target,(10.5f+.6f*(ExampleUpgradeLevel-1))*.2f);
        AnimateBolt(targetGimbal.position,targetGimbal.position+Vector3.forward*3,elapsed*(2.4f*(1+.2f*(ExampleUpgradeLevel-1)))%1);
        phaseLabel.text=phase<1.5f ? "1  CLICK YOUR PLACED TOWER" : phase<3 ? "2  CLICK UPGRADE / PRESS U" : phase<6 ? "PAID $100 → UPGRADE 2" : "PAID $200 → UPGRADE 3 / MAX";
        actionLabel.text="EXAMPLE MONEY $"+ExampleCash+"  ·  HEIGHT "+height.ToString("0.0")+"  ·  DAMAGE "+(1.15f*(1+.4f*(ExampleUpgradeLevel-1))).ToString("0.00");
    }
    private void MergeExample()
    {
        int step=Mathf.FloorToInt(elapsed/8f)%3;float phase=elapsed%8f;int inputLevel=step+1,fee=50*(1<<step);
        float drag=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.4f,3.4f,phase));float grow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.4f,3.9f,phase));
        ShowingMerged=phase>=3.4f;ExampleMergeLevel=inputLevel+(ShowingMerged ? 1 : 0);ExampleCash=500-(ShowingMerged ? fee : 0);
        source.gameObject.SetActive(!ShowingMerged);target.gameObject.SetActive(true);
        source.localPosition=new Vector3(Mathf.Lerp(-2.25f,2.25f,drag),Mathf.Sin(drag*Mathf.PI)*.4f,0);
        source.localScale=new Vector3(1+.18f*step,1,1+.18f*step);target.localScale=new Vector3(1+.18f*(step+grow),1,1+.18f*(step+grow));
        SetFusion(source,inputLevel);SetFusion(target,ExampleMergeLevel);
        phaseLabel.text=phase<1.4f ? "1  HOLD LEFT MOUSE" : phase<3.4f ? "2  DRAG ONTO MATCHING LEVEL" : "3  RELEASE → "+(ExampleMergeLevel==2 ? "GREEN" : ExampleMergeLevel==3 ? "PURPLE" : "RED MAX");
        actionLabel.text="Lv"+inputLevel+" + Lv"+inputLevel+" · $"+fee+" · EXAMPLE CASH $"+ExampleCash;
        PointAt((ShowingMerged ? target.position : source.position)+Vector3.up*2);
    }
    void OnEnable() { if(world!=null) { world.SetActive(true);Replay(); } }
    void OnDisable() { if(world!=null) world.SetActive(false); }
    void LateUpdate()
    {
        if(world==null) return;elapsed+=Time.unscaledDeltaTime;ApplyAnimation();
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) previewCamera.Render();
    }
    void OnDestroy()
    {
        if(world!=null) Destroy(world);
        if(image!=null) { image.Release();Destroy(image); }
    }
}
