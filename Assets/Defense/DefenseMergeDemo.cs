using UnityEngine;
using UnityEngine.UI;

// Visual-only tutorial scene: no DefenseTower components, colliders or economy commands.
// Unscaled time keeps the demonstration moving while Help pauses the actual battle.
public class DefenseMergeDemo : MonoBehaviour
{
    private const int PreviewLayer=30;
    private GameObject world;
    private Transform source,target,sourceGimbal,targetGimbal,cursor;
    private Camera previewCamera;
    private RenderTexture image;
    private Text phaseLabel,actionLabel;
    private RawImage display;
    private float elapsed;
    public bool ShowingMerged { get; private set; }
    public int ExampleMergeLevel { get; private set; }=1;
    public int ExampleCash { get; private set; }
    public float AnimationTime => elapsed;

    public void Initialize(DefenseGame game,RawImage screen,Text phase,Text action,RectTransform pointer)
    {
        display=screen;phaseLabel=phase;actionLabel=action;cursor=pointer;
        world=new GameObject("Tutorial merge demonstration — isolated visuals");
        world.transform.position=new Vector3(0,-1000,0);
        source=new GameObject("Example source Pulse").transform;source.SetParent(world.transform,false);
        target=new GameObject("Example destination Pulse").transform;target.SetParent(world.transform,false);
        DefenseArt.Tower(source,TowerKind.Pulse,1.4f,out sourceGimbal,out _);
        DefenseArt.Tower(target,TowerKind.Pulse,1.4f,out targetGimbal,out _);
        DefenseArt.MarkFusion(target,DefenseTower.FusionColor(2));target.Find("Fusion ring").gameObject.SetActive(false);
        GameObject cameraObject=new GameObject("Tutorial preview camera",typeof(Camera));
        cameraObject.transform.SetParent(world.transform,false);
        previewCamera=cameraObject.GetComponent<Camera>();previewCamera.enabled=false;
        previewCamera.transform.localPosition=new Vector3(6,6,-9);
        previewCamera.transform.LookAt(world.transform.position+Vector3.up*.7f);
        previewCamera.orthographic=true;previewCamera.orthographicSize=3.2f;
        previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.02f,.035f,.07f);
        previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=40;
        previewCamera.cullingMask=1<<PreviewLayer;previewCamera.allowMSAA=true;
        image=new RenderTexture(1024,640,24,RenderTextureFormat.ARGB32){antiAliasing=4,name="Tutorial merge preview"};
        previewCamera.targetTexture=image;display.texture=image;
        game.View.cullingMask&=~(1<<PreviewLayer);
        foreach(Transform part in world.GetComponentsInChildren<Transform>(true)) part.gameObject.layer=PreviewLayer;
        Replay();
    }
    public void Replay() { Seek(0); }
    public void Seek(float seconds)
    {
        elapsed=seconds;
        ApplyAnimation();
        if(previewCamera!=null && world.activeInHierarchy && SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) previewCamera.Render();
    }
    private void ApplyAnimation()
    {
        if(world==null) return;
        int step=Mathf.FloorToInt(elapsed/8f)%3;
        float phase=elapsed%8f;
        int inputLevel=step+1;
        int fee=50*(1<<step);
        float drag=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.4f,3.4f,phase));
        float grow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.4f,3.9f,phase));
        ShowingMerged=phase>=3.4f;
        ExampleMergeLevel=inputLevel+(ShowingMerged ? 1 : 0);
        ExampleCash=500-(ShowingMerged ? fee : 0);
        source.gameObject.SetActive(!ShowingMerged);
        source.localPosition=new Vector3(Mathf.Lerp(-2.25f,2.25f,drag),Mathf.Sin(drag*Mathf.PI)*.4f,0);
        target.localPosition=new Vector3(2.25f,0,0);
        target.localScale=new Vector3(1+.18f*(step+grow),1,1+.18f*(step+grow));
        source.localScale=new Vector3(1+.18f*step,1,1+.18f*step);
        DefenseArt.RaiseTower(source,sourceGimbal,TowerKind.Pulse,1.4f+.65f*step,1);
        DefenseArt.MarkFusion(source,DefenseTower.FusionColor(inputLevel));
        source.Find("Fusion ring").gameObject.SetActive(inputLevel>1);
        source.Find("Fusion beacon").gameObject.SetActive(inputLevel>1);
        DefenseArt.MarkFusion(target,DefenseTower.FusionColor(ExampleMergeLevel));
        DefenseArt.RaiseTower(target,targetGimbal,TowerKind.Pulse,1.4f+.65f*(step+grow),1);
        target.Find("Fusion ring").gameObject.SetActive(ExampleMergeLevel>1);
        target.Find("Fusion beacon").gameObject.SetActive(ExampleMergeLevel>1);
        phaseLabel.text=phase<1.4f ? "AUTO DEMO 1: HOLD LEFT MOUSE" : phase<3.4f ? "AUTO DEMO 2: DRAG ONTO MATCHING PULSE" : "RELEASE → "+(ExampleMergeLevel==2 ? "GREEN" : ExampleMergeLevel==3 ? "PURPLE" : "RED MAX");
        actionLabel.text="Lv"+inputLevel+" + Lv"+inputLevel+" · $"+fee+" · EXAMPLE CASH $"+ExampleCash;
        actionLabel.color=ShowingMerged ? new Color(1f,.7f,.4f) : new Color(.3f,.9f,1f);
        Vector3 viewport=previewCamera.WorldToViewportPoint((ShowingMerged ? target.position : source.position)+Vector3.up*2f);
        RectTransform pointer=(RectTransform)cursor;
        pointer.anchorMin=pointer.anchorMax=new Vector2(viewport.x,viewport.y);pointer.anchoredPosition=Vector2.zero;
    }
    void OnEnable() { if(world!=null) { world.SetActive(true);Replay(); } }
    void OnDisable() { if(world!=null) world.SetActive(false); }
    void LateUpdate()
    {
        if(world==null) return;
        elapsed+=Time.unscaledDeltaTime;ApplyAnimation();
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) previewCamera.Render();
    }
    void OnDestroy()
    {
        if(world!=null) Destroy(world);
        if(image!=null) { image.Release();Destroy(image); }
    }
}
