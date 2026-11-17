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
    private Button previousEnemy,nextEnemy;
    public DefenseMergeDemo MergeDemo { get; private set; }
    private int tutorialPage;
    private bool tutorialStartsGame,tutorialPausedByUs;
    public bool TutorialOpen => tutorial!=null && tutorial.activeSelf;
    public int TutorialPage => tutorialPage;
    private static readonly string[] TutorialTitles={"CHOOSE YOUR TOWER","PLACE ON A CYAN PAD","KILLS EARN MONEY","UPGRADE YOUR DEFENSE","THREE MERGE TIERS"};
    private static readonly string[] TutorialBodies={
        "Protect the CYAN RELAY. One escaped enemy ends the run.\n\nStart with $110. Select a tower card or press 1 / 2.\n\nPULSE — $75\n1.15 damage · 2.4 shots/s · range 10.5\nFast fire suits Scouts and motorcycles.\n\nSIEGE — $110\n12 damage · 0.6 shots/s · range 14\nHeavy shells suit armored enemies and trucks.\n\nThe animated ring shows which tower is selected.\nBegin with Pulse on the gold START HERE pad.",
        "1  Choose PULSE / SIEGE using 1 / 2 or its card.\n2  Move onto a free glowing CYAN HEXAGON.\n3  LEFT-CLICK the pad to buy and place the tower.\n\nExample: $110 − $75 Pulse = $35 left.\nRoads, empty terrain and occupied pads reject builds.\nIf you cannot afford it, no money is spent.\n\nPress SPACE / LAUNCH WAVE when ready.\nTowers aim and shoot automatically.\nBuy, merge and upgrade during setup OR combat.\nThere is no per-wave action limit; cash is the limit.\n\nUse several bends for better road coverage.",
        "",
        "1  LEFT-CLICK a placed tower to select it.\n2  Check the upgrade price and your MONEY.\n3  Click UPGRADE or press U.\n\nPaid level 1 → 2 costs $100.\nPaid level 2 → 3 costs $200. Level 3 is the cap.\n\nEach upgrade: +0.8 height, more damage,\nfaster shots, more range and sharper laser sound.\nThe example shows $350 → $250 → $50.\n\nMerged towers multiply prices by their tower units.\nA green x2 tower costs $200, then $400.\n\nUpgrades add HEIGHT. Merges add WIDTH.\nWave 1 pays $40: save for coverage first.",
        "HOLD left mouse on a placed tower.\nDRAG onto SAME TYPE + MERGE LEVEL.\nRELEASE to combine. M / MERGE also works.\n\nLv1 + Lv1 → GREEN Lv2 ($50)\nLv2 + Lv2 → PURPLE Lv3 ($100)\nLv3 + Lv3 → RED Lv4 ($200 / MAX)\n\nTwo towers become one; the other pad is freed.\nKeep the higher paid upgrade level.\nMerge: wider, +5% range, +20% shot rate.\nEach tier makes the laser sound sharper.\nMerging adds no height. Paid upgrades do.\n\nBuy AND merge in the same wave if you can pay.\nWrong type / level or too little cash spends nothing."
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
        Panel("Tutorial card",tutorial.transform,.08f,.12f,.92f,.91f,surface);
        Panel("Tutorial accent",tutorial.transform,.08f,.904f,.92f,.91f,accent);
        tutorialProgress=Label("Tutorial progress",tutorial.transform, .12f,.85f,.88f,.89f,"",17);tutorialProgress.color=accent;
        tutorialHeading=Label("Tutorial heading",tutorial.transform, .12f,.75f,.88f,.85f,"",43);tutorialHeading.fontStyle=FontStyle.Bold;
        tutorialBody=Label("Tutorial instructions",tutorial.transform, .57f,.32f,.88f,.74f,"",22,TextAnchor.MiddleLeft);
        Label("Tutorial shortcuts",tutorial.transform,.2f,.252f,.8f,.274f,"ENTER / SPACE: NEXT     ESC: SKIP OR CLOSE     H: REOPEN GUIDE",13).color=accent;
        tutorialBack=MakeButton("Tutorial back",tutorial.transform,.20f,.17f,.34f,.25f,"BACK",PreviousTutorialPage);
        tutorialSkipLabel=MakeButton("Tutorial skip",tutorial.transform,.36f,.17f,.58f,.25f,"SKIP & PLAY",CloseTutorial).GetComponentInChildren<Text>();
        tutorialNextLabel=MakeButton("Tutorial next",tutorial.transform,.60f,.17f,.80f,.25f,"NEXT",NextTutorialPage).GetComponentInChildren<Text>();
        mergeDemoPanel=Panel("Animated merge example",tutorial.transform, .12f,.35f,.53f,.74f,new Color(.02f,.035f,.07f,1)).gameObject;
        GameObject imageObject=new GameObject("Live 3D preview",typeof(RectTransform),typeof(RawImage));
        imageObject.transform.SetParent(mergeDemoPanel.transform,false);Rect(imageObject,0,.14f,1,.91f);
        RawImage raw=imageObject.GetComponent<RawImage>();raw.raycastTarget=false;
        Text phase=Label("Demo phase",mergeDemoPanel.transform,.01f,.88f,.99f,1,"",12);
        Text action=Label("Demo action counter",mergeDemoPanel.transform,.01f,.02f,.99f,.16f,"",12);
        Text cursor=Label("Demo mouse cursor",imageObject.transform,.2f,.5f,.2f,.5f,"▼",27);cursor.color=Color.white;
        cursor.rectTransform.sizeDelta=new Vector2(40,40);
        Image upgradeExample=Panel("Example upgrade button",imageObject.transform,.65f,.03f,.98f,.18f,new Color(.04f,.2f,.26f,1));upgradeExample.raycastTarget=false;
        Text upgradeCaption=Label("Upgrade caption",upgradeExample.transform,0,0,1,1,"",14);
        MergeDemo=mergeDemoPanel.AddComponent<DefenseMergeDemo>();MergeDemo.Initialize(game,raw,phase,action,cursor.rectTransform,upgradeCaption);
        MakeButton("Replay merge animation",mergeDemoPanel.transform,.15f,-.19f,.85f,-.105f,"REPLAY EXAMPLE",()=>MergeDemo.Replay());
        previousEnemy=MakeButton("Previous enemy",mergeDemoPanel.transform,0,-.085f,.48f,0,"< PREV ENEMY",()=>MergeDemo.CycleEnemy(-1));
        nextEnemy=MakeButton("Next enemy",mergeDemoPanel.transform,.52f,-.085f,1,0,"NEXT ENEMY >",()=>MergeDemo.CycleEnemy(1));
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
        mergeDemoPanel.SetActive(true);MergeDemo.SetPage(tutorialPage);
        previousEnemy.gameObject.SetActive(tutorialPage==2);nextEnemy.gameObject.SetActive(tutorialPage==2);
        Rect(tutorialBody.gameObject,.57f,.32f,.88f,.74f);
        tutorialBody.fontSize=18;tutorialBody.resizeTextMaxSize=18;
        tutorialProgress.text="FIELD GUIDE  /  STEP "+(tutorialPage+1)+" OF "+TutorialTitles.Length;
        tutorialHeading.text=TutorialTitles[tutorialPage];tutorialBody.text=tutorialPage==2 ? MergeDemo.EnemyExplanation : TutorialBodies[tutorialPage];
        tutorialBack.interactable=tutorialPage>0;
        tutorialSkipLabel.text=tutorialStartsGame ? "SKIP & PLAY" : "CLOSE GUIDE";
        tutorialNextLabel.text=tutorialPage==TutorialTitles.Length-1 ? (tutorialStartsGame ? "START DEFENDING" : "BACK TO GAME") : "NEXT  >";
    }
    void LateUpdate()
    {
        if(TutorialOpen && tutorialPage==2) tutorialBody.text=MergeDemo.EnemyExplanation;
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
