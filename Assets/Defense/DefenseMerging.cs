using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Mouse selection, drag preview and fusion share the same checked merge operation.
public class DefenseMerging : MonoBehaviour
{
    private DefenseGame game;
    private Transform battlefield,ghost;
    private DefenseTower pressedTower,source,hover;
    private Vector2 pressPosition;
    private LineRenderer link,targetRing;
    public bool Active => source!=null;
    public bool IsDraggingTower(DefenseTower tower) => ghost!=null && source==tower;

    public void Initialize(DefenseGame owner,Transform field)
    {
        game=owner;battlefield=field;
        link=DefenseArt.Ring(field,"Merge link",new Color(.8f,.4f,1f));link.positionCount=2;link.enabled=false;
        targetRing=DefenseArt.Ring(field,"Merge target",DefenseArt.Cyan);targetRing.enabled=false;
    }
    public bool CanMerge(DefenseTower from,DefenseTower to)
    {
        return game.WaveActionsRemaining>0 && game.CanBuild && !game.Paused && !game.HUD.TutorialOpen && from!=null && to!=null && from!=to
            && from.Kind==to.Kind && game.Towers.Contains(from) && game.Towers.Contains(to);
    }
    public bool TryMerge(DefenseTower from,DefenseTower to)
    {
        Cancel();
        if(game.WaveActionsRemaining==0)
        { game.HUD.Message("Wave action already used. Choose PLACE or MERGE in the next wave; you cannot do both.");return false; }
        if(!CanMerge(from,to))
        {
            game.HUD.Message("Merge requires two different placed towers of the SAME TYPE: Pulse + Pulse or Siege + Siege.");
            return false;
        }
        if(!game.TryConsumeWaveAction(WaveAction.Merge)) return false;
        to.Absorb(from);
        game.Towers.Remove(from);from.gameObject.SetActive(false);Destroy(from.gameObject);
        game.SelectTower(to);
        DefenseArt.Burst(to.transform.position+Vector3.up*to.Height,new Color(1.5f,.4f,2f),.8f);
        game.HUD.Message("MERGED! "+to.Kind+" x"+to.MergedUnits+": bigger, taller and stronger. MERGE used this wave's action. Placing and another merge are locked until next wave.");
        game.HUD.Refresh();return true;
    }
    public void BeginSelectedMerge()
    {
        DefenseTower selected=game.SelectedTower;Cancel();
        if(game.WaveActionsRemaining==0) { game.HUD.Message("No wave action left. Placing and merging share ONE action; try next wave.");return; }
        if(!game.CanBuild || game.Paused || game.HUD.TutorialOpen || selected==null) return;
        if(!game.Towers.Exists(t=>t!=selected && t.Kind==selected.Kind))
        { game.HUD.Message("Buy another "+selected.Kind+" in a later wave, then merge the matching pair.");return; }
        source=selected;link.enabled=true;
        game.HUD.Message("Click another "+source.Kind+" tower to merge into it. ESC cancels. Different levels are allowed.");
    }
    public bool BeginDrag(DefenseTower tower)
    {
        Cancel();
        if(game.WaveActionsRemaining==0) { game.HUD.Message("No action left: you may PLACE or MERGE once per wave, not both.");return false; }
        if(!game.CanBuild || game.Paused || game.HUD.TutorialOpen || tower==null || !game.Towers.Contains(tower)) return false;
        source=tower;game.SelectTower(tower);
        ghost=DefenseArt.MergeGhost(tower,battlefield);tower.SetVisualsVisible(false);link.enabled=true;
        game.HUD.Message("Drag onto a matching "+tower.Kind+" tower and release. Empty ground or a different type cancels the merge.");
        return true;
    }
    public bool DropOn(DefenseTower target)
    {
        DefenseTower from=source;Cancel();
        if(target==null) { game.HUD.Message("Merge cancelled. Both towers stay on their original pads.");return false; }
        return TryMerge(from,target);
    }
    public void Cancel()
    {
        if(source!=null && ghost!=null) source.SetVisualsVisible(true);
        if(ghost!=null) { ghost.gameObject.SetActive(false);Destroy(ghost.gameObject); }
        ghost=null;source=null;hover=null;pressedTower=null;
        if(link!=null) link.enabled=false;
        if(targetRing!=null) targetRing.enabled=false;
    }
    public DefenseTower PickTower(Ray ray,Vector3 ground)
    {
        DefenseTower best=null;float nearest=float.PositiveInfinity;
        foreach(DefenseTower tower in game.Towers)
        {
            if(IsDraggingTower(tower)) continue;
            if(tower.SelectionBounds.IntersectRay(ray,out float distance) && distance<nearest)
            { best=tower;nearest=distance; }
        }
        if(best!=null) return best;
        int pad=game.NearestPad(ground);
        return pad<0 ? null : game.Towers.Find(t=>t.PadIndex==pad && !IsDraggingTower(t));
    }
    private void Preview(Vector3 ground,DefenseTower target)
    {
        if(source==null) return;
        if(ghost!=null) ghost.position=ground+Vector3.up*.2f;
        link.SetPosition(0,source.transform.position+Vector3.up*source.Height);
        link.SetPosition(1,target!=null ? target.transform.position+Vector3.up*target.Height : ground+Vector3.up*source.Height);
        targetRing.enabled=target!=null;
        if(target!=null)
        {
            DefenseArt.SetRing(targetRing,target.transform.position,1.3f*target.transform.localScale.x);
            targetRing.sharedMaterial.SetColor("_BaseColor",CanMerge(source,target) ? new Color(.2f,2f,.6f) : DefenseArt.Pink);
        }
        if(target!=hover)
        {
            hover=target;
            if(target!=null) game.HUD.Message(CanMerge(source,target) ? "MATCH! Release or click to combine the two "+source.Kind+" towers." : "These towers do not match. Drop cancels; no towers or cash are lost.");
        }
    }
    public void HandlePointer()
    {
        if(!game.CanBuild || game.Paused || game.HUD.TutorialOpen || Mouse.current==null) { Cancel();return; }
        Mouse mouse=Mouse.current;
        if(EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject())
        {
            if(ghost!=null && !mouse.leftButton.isPressed) Cancel();
            if(mouse.leftButton.wasReleasedThisFrame) pressedTower=null;
            return;
        }
        Ray ray=game.View.ScreenPointToRay(mouse.position.ReadValue());
        Plane plane=new Plane(Vector3.up,Vector3.zero);
        if(!plane.Raycast(ray,out float distance)) { if(!mouse.leftButton.isPressed) Cancel();return; }
        Vector3 ground=ray.GetPoint(distance);
        DefenseTower target=PickTower(ray,ground);
        if(source!=null && ghost==null)
        {
            Preview(ground,target);
            if(mouse.leftButton.wasPressedThisFrame) DropOn(target);
            return;
        }
        if(mouse.leftButton.wasPressedThisFrame)
        {
            if(target!=null) { game.SelectTower(target);pressedTower=target;pressPosition=mouse.position.ReadValue(); }
            else game.TryPlaceAt(ground);
        }
        if(pressedTower!=null && mouse.leftButton.isPressed && Vector2.Distance(pressPosition,mouse.position.ReadValue())>6f && ghost==null)
        { DefenseTower tower=pressedTower;BeginDrag(tower); }
        if(ghost!=null)
        {
            // Re-pick after hiding the dragged source so the destination wins the hit test.
            target=PickTower(ray,ground);Preview(ground,target);
            if(!mouse.leftButton.isPressed) DropOn(target);
        }
        if(source==null)
        {
            int pad=game.NearestPad(ground);targetRing.enabled=pad>=0;
            if(pad>=0)
            {
                targetRing.sharedMaterial.SetColor("_BaseColor",DefenseArt.Cyan);
                DefenseArt.SetRing(targetRing,game.Pads[pad],1.3f);
            }
        }
        if(mouse.leftButton.wasReleasedThisFrame) pressedTower=null;
    }
}
