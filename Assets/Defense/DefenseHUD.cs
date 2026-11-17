using UnityEngine;
using UnityEngine.UI;

public class DefenseHUD : MonoBehaviour
{
    private DefenseGame game;
    private Font font;
    private Text stats,tip,details,title,subtitle,description,pulseText,siegeText,launchText,pauseText,speedText;
    private Button launch,upgrade,sell,merge;
    private GameObject menu;
    private Image pulseImage,siegeImage;
    private Button pulseButton,siegeButton;
    private GameObject tutorial;
    private Text tutorialProgress,tutorialHeading,tutorialBody,tutorialNextLabel,tutorialSkipLabel;
    private Button tutorialBack;
    private GameObject mergeDemoPanel;
    public DefenseMergeDemo MergeDemo { get; private set; }
    private int tutorialPage;
    private bool tutorialStartsGame,tutorialPausedByUs;
    public bool TutorialOpen => tutorial!=null && tutorial.activeSelf;
    public int TutorialPage => tutorialPage;
    private static readonly string[] TutorialTitles={"CHOOSE YOUR TOWER","PLACE ON A CYAN PAD","KILLS EARN MONEY","UPGRADE YOUR DEFENSE","THREE MERGE TIERS"};
    private static readonly string[] TutorialBodies={
        "You start with $110.\n\nPULSE — $75: rapid fire, useful against Scouts and motorcycles.\nSIEGE — $110: 12-damage shells, one shot every 1.67 seconds.\n\nClick a tower card at the bottom, or press 1 for Pulse / 2 for Siege.\nFor your first tower, try a Pulse on the gold START HERE pad.\nWave 1 is a slower training wave; upgrades are optional.",
        "Move the pointer over a glowing CYAN HEXAGON on the board.\nLEFT-CLICK the pad to place your selected tower.\n\nBuild and merge as often as you can afford. There is no per-wave action limit.\nRoads, unmarked ground and occupied pads cannot take a new tower.\n\nPress SPACE or LAUNCH WAVE when your defense is ready.\nKeep enough cash for both coverage and upgrades.",
        "Every defeated enemy immediately adds cash to MONEY at the top.\nWatch for the gold +$ reward above the enemy.\n\nScout $4  /  Runner $5  /  Motorcycle $6\nArmored $8  /  Siege $30  /  Truck $40\n\nSave cash to buy your next wave's tower or improve an existing one.\nUnspent money carries over. There is no free wave-clear bonus.",
        "LEFT-CLICK a placed tower to select it and see its range and stats.\nClick UPGRADE or press U when you can afford it.\n\nLevel 2 costs $100. Level 3 costs $200.\nMerged towers multiply these costs by their combined tower count.\nEach upgrade adds damage, firing speed, range and 0.8 units of height.\n\nA $75 Pulse leaves $35. Wave 1 pays $40 —\nsave for another tower before spending on upgrades!",
        "HOLD left mouse on a tower.\nDRAG onto SAME TYPE + MERGE LEVEL\nand RELEASE. M / MERGE also works.\n\nLv1 + Lv1 → GREEN Lv2 ($50)\nLv2 + Lv2 → PURPLE Lv3 ($100)\nLv3 + Lv3 → RED Lv4 ($200)\nRED is the final merge tier.\n\nBuy AND merge in the same wave.\nYour MONEY is the limit. Bad drops\ncost nothing. Merges widen + add 5% range.\nOnly paid upgrades add height."

    };
    private readonly Color accent=new Color(.35f,.9f,1f);
    private readonly Color surface=new Color(.025f,.045f,.09f,.98f);

    public void Initialize(DefenseGame owner)
    {
        game=owner;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.pixelPerfect=true;
        CanvasScaler scale=gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1440,900);scale.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        Panel("Top rail",transform,0,.90f,1,1,surface);
        Text brand=Label("Brand",transform,.025f,.935f,.36f,.985f,"NEON BASTION",27,TextAnchor.MiddleLeft);brand.fontStyle=FontStyle.Bold;
        Label("Tagline",transform,.025f,.905f,.4f,.94f,"T A C T I C A L   R E L A Y   D E F E N S E",11,TextAnchor.MiddleLeft).color=accent;
        MakeButton("How to play",transform,.26f,.937f,.345f,.983f,"? HELP",()=>OpenTutorial(false));
        stats=Label("Mission stats",transform,.36f,.915f,.78f,.99f,"",21);
        pauseText=MakeButton("Pause",transform,.81f,.923f,.89f,.98f,"P  PAUSE",()=>game.TogglePause()).GetComponentInChildren<Text>();
        speedText=MakeButton("Speed",transform,.90f,.923f,.98f,.98f,"1× SPEED",()=>game.ToggleSpeed()).GetComponentInChildren<Text>();
        Panel("Top accent",transform,0,.898f,1,.902f,new Color(.08f,.6f,.7f));
        Panel("Command deck",transform,0,0,1,.205f,surface);
        tip=Label("Placement instructions",transform,.025f,.157f,.975f,.202f,"",17,TextAnchor.MiddleLeft);tip.color=accent;
        Button pulse=MakeButton("Pulse tower",transform,.025f,.027f,.20f,.145f,"",()=>game.Choose(TowerKind.Pulse));pulseButton=pulse;pulseImage=pulse.GetComponent<Image>();pulseText=pulse.GetComponentInChildren<Text>();
        Button siege=MakeButton("Siege tower",transform,.215f,.027f,.39f,.145f,"",()=>game.Choose(TowerKind.Siege));siegeButton=siege;siegeImage=siege.GetComponent<Image>();siegeText=siege.GetComponentInChildren<Text>();
        details=Label("Selection details",transform,.41f,.047f,.69f,.14f,"Select a tower to inspect it.",15,TextAnchor.MiddleLeft);
        upgrade=MakeButton("Upgrade",transform,.70f,.105f,.82f,.145f,"U  UPGRADE",()=>game.UpgradeSelected());
        merge=MakeButton("Merge",transform,.70f,.063f,.82f,.103f,"M  MERGE",()=>game.Merging.BeginSelectedMerge());
        sell=MakeButton("Sell",transform,.70f,.021f,.82f,.061f,"X  SELL",()=>game.SellSelected());
        launch=MakeButton("Launch wave",transform,.835f,.027f,.975f,.145f,"LAUNCH WAVE\n[ SPACE ]",()=>game.LaunchWave());launchText=launch.GetComponentInChildren<Text>();
        launch.GetComponent<Image>().color=new Color(.06f,.34f,.37f);
        menu=Panel("Menu shade",transform,0,0,1,1,new Color(.005f,.012f,.035f,.88f)).gameObject;
        Panel("Mission card",menu.transform,.18f,.16f,.82f,.85f,surface);
        Panel("Mission card accent",menu.transform,.18f,.844f,.82f,.85f,accent);
        Label("Eyebrow",menu.transform,.2f,.75f,.8f,.81f,"S E C T O R   0 7   /   D E F E N S E   C O M M A N D",15).color=accent;
        title=Label("Title",menu.transform,.19f,.61f,.81f,.75f,"",57);title.fontStyle=FontStyle.Bold;
        subtitle=Label("Subtitle",menu.transform,.2f,.55f,.8f,.61f,"",20);subtitle.color=accent;
        description=Label("Instructions",menu.transform,.22f,.30f,.78f,.54f,"",20);
        MakeButton("Start or retry",menu.transform,.34f,.205f,.66f,.285f,"ENTER SECTOR",()=>OpenTutorial(true));
        BuildTutorial();
        Refresh();
    }
    public void Message(string value) { tip.text="1 / 2 CHOOSE TOWER  ·  LEFT-CLICK A CYAN PAD TO BUILD  ·  BUILD + MERGE WITH CASH  ·  H HELP\n"+value; }
    private void BuildTutorial()
    {
        tutorial=Panel("Quick tutorial",transform,0,0,1,1,new Color(.005f,.012f,.035f,.94f)).gameObject;
        Panel("Tutorial card",tutorial.transform,.16f,.12f,.84f,.89f,surface);
        Panel("Tutorial accent",tutorial.transform,.16f,.884f,.84f,.89f,accent);
        tutorialProgress=Label("Tutorial progress",tutorial.transform,.20f,.79f,.80f,.85f,"",17);tutorialProgress.color=accent;
        tutorialHeading=Label("Tutorial heading",tutorial.transform,.18f,.66f,.82f,.79f,"",43);tutorialHeading.fontStyle=FontStyle.Bold;
        tutorialBody=Label("Tutorial instructions",tutorial.transform,.22f,.32f,.78f,.66f,"",22,TextAnchor.MiddleLeft);
        Label("Tutorial shortcuts",tutorial.transform,.2f,.27f,.8f,.31f,"ENTER / SPACE: NEXT     ESC: SKIP OR CLOSE     H: REOPEN GUIDE",13).color=accent;
        tutorialBack=MakeButton("Tutorial back",tutorial.transform,.20f,.17f,.34f,.25f,"BACK",PreviousTutorialPage);
        tutorialSkipLabel=MakeButton("Tutorial skip",tutorial.transform,.36f,.17f,.58f,.25f,"SKIP & PLAY",CloseTutorial).GetComponentInChildren<Text>();
        tutorialNextLabel=MakeButton("Tutorial next",tutorial.transform,.60f,.17f,.80f,.25f,"NEXT",NextTutorialPage).GetComponentInChildren<Text>();
        mergeDemoPanel=Panel("Animated merge example",tutorial.transform,.20f,.35f,.51f,.68f,new Color(.02f,.035f,.07f,1)).gameObject;
        GameObject imageObject=new GameObject("Live 3D preview",typeof(RectTransform),typeof(RawImage));
        imageObject.transform.SetParent(mergeDemoPanel.transform,false);Rect(imageObject,0,.14f,1,.91f);
        RawImage raw=imageObject.GetComponent<RawImage>();raw.raycastTarget=false;
        Text phase=Label("Demo phase",mergeDemoPanel.transform,.01f,.88f,.99f,1,"",12);
        Text action=Label("Demo action counter",mergeDemoPanel.transform,.01f,.02f,.99f,.16f,"",12);
        Text cursor=Label("Demo mouse cursor",imageObject.transform,.2f,.5f,.2f,.5f,"▼",27);cursor.color=Color.white;
        cursor.rectTransform.sizeDelta=new Vector2(40,40);
        MergeDemo=mergeDemoPanel.AddComponent<DefenseMergeDemo>();MergeDemo.Initialize(game,raw,phase,action,cursor.rectTransform);
        MakeButton("Replay merge animation",mergeDemoPanel.transform,.15f,-.09f,.85f,0f,"REPLAY EXAMPLE",()=>MergeDemo.Replay());
        mergeDemoPanel.SetActive(false);
        tutorial.SetActive(false);
    }
    public void OpenTutorial(bool startNewGame)
    {
        if(TutorialOpen) return;
        tutorialStartsGame=startNewGame;tutorialPage=0;
        tutorialPausedByUs=game.CanBuild && !game.Paused;
        if(tutorialPausedByUs) game.TogglePause();
        tutorial.transform.SetAsLastSibling();tutorial.SetActive(true);RefreshTutorial();
    }
    public void NextTutorialPage()
    {
        if(!TutorialOpen) return;
        if(tutorialPage==TutorialTitles.Length-1) { CloseTutorial();return; }
        tutorialPage++;RefreshTutorial();
    }
    public void PreviousTutorialPage()
    {
        if(!TutorialOpen) return;
        tutorialPage=Mathf.Max(0,tutorialPage-1);RefreshTutorial();
    }
    private void RefreshTutorial()
    {
        bool mergingPage=tutorialPage==4;
        mergeDemoPanel.SetActive(mergingPage);
        Rect(tutorialBody.gameObject,mergingPage ? .54f : .22f,.32f,.80f,.66f);
        tutorialBody.fontSize=mergingPage ? 18 : 22;tutorialBody.resizeTextMaxSize=mergingPage ? 18 : 22;
        tutorialProgress.text="QUICK START  /  STEP "+(tutorialPage+1)+" OF "+TutorialTitles.Length;
        tutorialHeading.text=TutorialTitles[tutorialPage];tutorialBody.text=TutorialBodies[tutorialPage];
        tutorialBack.interactable=tutorialPage>0;
        tutorialSkipLabel.text=tutorialStartsGame ? "SKIP & PLAY" : "CLOSE GUIDE";
        tutorialNextLabel.text=tutorialPage==TutorialTitles.Length-1 ? (tutorialStartsGame ? "START DEFENDING" : "BACK TO GAME") : "NEXT  >";
    }
    public void CloseTutorial()
    {
        if(!TutorialOpen) return;
        bool start=tutorialStartsGame;
        tutorial.SetActive(false);
        if(tutorialPausedByUs && game.Paused) game.TogglePause();
        tutorialPausedByUs=false;
        if(start) game.StartGame();
        else if(game.CanBuild) Message(game.Paused ? "Still paused. Press P to resume when ready." : "Choose 1 / 2, click a cyan pad, then press SPACE. Click a tower and U to upgrade.");
    }

    public void ShowReward(int amount,Vector3 worldPosition)
    {
        Vector3 screen=game.View.WorldToScreenPoint(worldPosition+Vector3.up*1.4f);
        if(screen.z<=0) return;
        Text label=Label("Kill reward",transform,.5f,.5f,.5f,.5f,"+$"+amount,24);
        label.fontStyle=FontStyle.Bold;label.color=new Color(1f,.85f,.3f);
        RectTransform rect=label.rectTransform;rect.sizeDelta=new Vector2(140,44);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screen,null,out Vector2 local);
        rect.anchoredPosition=local;
        Shadow shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);
        label.gameObject.AddComponent<DefenseCashPopup>().Initialize(label);
    }

    public void Refresh()
    {
        stats.text="MONEY  $"+game.Credits+"     TOWERS  "+game.Towers.Count+" / "+DefenseGame.TowerLimit+"\nWAVE  "+game.PlacementWave+" / "+DefenseGame.TotalWaves+"     HOSTILES  "+(game.Enemies.Count+game.SpawnRemaining)+"\nMERGE TIERS  GREEN → PURPLE → RED";
        pulseText.text="[ 1 ]   PULSE   /   $75\nRapid · speed 17 · height 1.4";
        siegeText.text="[ 2 ]   SIEGE   /   $110\n12 damage · 0.6 shots/s\nSpeed 23 · height 2.8";
        pulseImage.color=game.SelectedTower==null && game.SelectedKind==TowerKind.Pulse ? new Color(.04f,.25f,.34f) : new Color(.055f,.08f,.14f);
        siegeImage.color=game.SelectedTower==null && game.SelectedKind==TowerKind.Siege ? new Color(.28f,.18f,.055f) : new Color(.055f,.08f,.14f);
        pulseButton.interactable=game.CanBuild && !game.Paused && game.Credits>=DefenseTower.Cost(TowerKind.Pulse) && game.Towers.Count<DefenseGame.TowerLimit;
        siegeButton.interactable=game.CanBuild && !game.Paused && game.Credits>=DefenseTower.Cost(TowerKind.Siege) && game.Towers.Count<DefenseGame.TowerLimit;
        DefenseTower tower=game.SelectedTower;
        details.text=tower==null ? "CHOOSE 1 / 2 → CLICK A CYAN PAD\nBuy + merge as your money allows" :
            tower.Kind.ToString().ToUpper()+"  /  UPGRADE "+tower.Level+"  /  "+tower.MergeName+" Lv"+tower.MergeLevel+"\nDamage "+tower.Damage.ToString("0.0")+" · "+tower.FireRate.ToString("0.0")+" shots/s\nRange "+tower.Range.ToString("0.0")+" · height "+tower.Height.ToString("0.0");
        upgrade.interactable=game.CanBuild && !game.Paused && tower!=null && tower.Level<3 && game.Credits>=tower.UpgradeCost;
        merge.interactable=tower!=null && game.Towers.Exists(t=>game.Merging.CanMerge(tower,t));
        merge.GetComponentInChildren<Text>().text=tower==null ? "M  MERGE" : tower.MergeLevel>=DefenseTower.MaxMergeLevel ? "RED MAX" : "M  MERGE / $"+tower.MergeCost;
        sell.interactable=game.CanBuild && !game.Paused && tower!=null && (game.Towers.Count>1 || game.Credits+Mathf.FloorToInt(tower.Investment*.7f)>=DefenseTower.Cost(TowerKind.Pulse));
        upgrade.GetComponentInChildren<Text>().text=tower!=null && tower.Level<3 ? "U  UPGRADE / $"+tower.UpgradeCost : "U  UPGRADE";
        sell.GetComponentInChildren<Text>().text=tower!=null ? "X  SELL / +$"+Mathf.FloorToInt(tower.Investment*.7f) : "X  SELL";
        launch.interactable=game.State==DefenseState.Building && !game.Paused;
        launchText.text=game.State==DefenseState.Combat ? "WAVE ACTIVE\nAUTO FIRE" : "LAUNCH WAVE "+(game.Wave+1)+"\n[ SPACE ]";
        pauseText.text=game.Paused ? "P  RESUME" : "P  PAUSE";speedText.text=game.Fast ? "2× SPEED" : "1× SPEED";
    }
    public void ShowMenu(string heading,string subheading,string body,string action)
    {
        title.text=heading;subtitle.text=subheading;description.text=body;
        menu.transform.Find("Start or retry").GetComponentInChildren<Text>().text=action;
        menu.SetActive(true);
    }
    public void HideMenu() { menu.SetActive(false); }
    private Image Panel(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
    {
        GameObject item=new GameObject(name,typeof(RectTransform),typeof(Image));item.transform.SetParent(parent,false);Rect(item,x0,y0,x1,y1);
        Image image=item.GetComponent<Image>();image.color=color;return image;
    }
    private Text Label(string name,Transform parent,float x0,float y0,float x1,float y1,string value,int size,TextAnchor alignment=TextAnchor.MiddleCenter)
    {
        GameObject item=new GameObject(name,typeof(RectTransform),typeof(Text));item.transform.SetParent(parent,false);Rect(item,x0,y0,x1,y1);
        Text text=item.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.color=new Color(.86f,.94f,1f);text.alignment=alignment;
        text.raycastTarget=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;return text;
    }
    private Button MakeButton(string name,Transform parent,float x0,float y0,float x1,float y1,string caption,UnityEngine.Events.UnityAction action)
    {
        Image image=Panel(name,parent,x0,y0,x1,y1,new Color(.055f,.13f,.21f));
        Button button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        ColorBlock colors=button.colors;colors.highlightedColor=new Color(.65f,1f,1f);colors.disabledColor=new Color(.33f,.4f,.5f);button.colors=colors;
        Label("Label",image.transform,.04f,.04f,.96f,.96f,caption,17);return button;
    }
    private void Rect(GameObject item,float x0,float y0,float x1,float y1)
    {
        RectTransform rect=item.GetComponent<RectTransform>();rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
    }
}
