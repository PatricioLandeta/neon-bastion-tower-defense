using UnityEngine;

// A visual suggestion only: players may still choose any valid pad and either tower.
public class DefenseStarterHint : MonoBehaviour
{
    private DefenseGame game;
    private LineRenderer ring;
    private Transform caption;
    public void Initialize(DefenseGame owner)
    {
        game=owner;
        ring=DefenseArt.Ring(transform,"Starter pad gold",new Color(1.5f,1.05f,.2f));
        DefenseArt.SetRing(ring,game.Pads[1],1.5f);
        GameObject label=new GameObject("Start here label",typeof(TextMesh));label.transform.SetParent(transform,false);
        label.transform.position=game.Pads[1]+new Vector3(0,3.1f,0);caption=label.transform;
        TextMesh text=label.GetComponent<TextMesh>();text.text="START HERE\nPulse recommended";
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=48;text.characterSize=.18f;
        text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(1f,.85f,.35f);
        label.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;caption.rotation=game.View.transform.rotation;
    }
    void LateUpdate()
    {
        if(game==null) return;
        bool show=(game.State==DefenseState.Title || game.State==DefenseState.Building) && game.Wave==0 && game.Towers.Count==0;
        ring.enabled=show;caption.gameObject.SetActive(show);
        if(show) caption.rotation=game.View.transform.rotation;
    }
}
