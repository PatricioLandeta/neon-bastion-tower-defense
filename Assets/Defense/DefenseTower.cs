using UnityEngine;

public enum TowerKind { Pulse, Siege }

public class DefenseTower : MonoBehaviour
{
    public DefenseGame Game { get; private set; }
    public TowerKind Kind { get; private set; }
    public int Level { get; private set; }=1;
    public int PadIndex { get; private set; }
    public int Investment { get; private set; }
    public float FireRate => (Kind==TowerKind.Pulse ? 2.4f : .75f)*(1f+.2f*(Level-1));
    public float ProjectileSpeed => Kind==TowerKind.Pulse ? 17f : 23f;
    public float Height => Kind==TowerKind.Pulse ? 1.4f : 2.8f;
    public float Range => (Kind==TowerKind.Pulse ? 10.5f : 14f)+.6f*(Level-1);
    public float Damage => (Kind==TowerKind.Pulse ? 1.15f : 4.5f)*(1f+.4f*(Level-1));
    public int UpgradeCost => 45*Level;
    public int ShotsFired { get; private set; }
    public Transform Muzzle { get; private set; }
    private Transform turret;
    private float nextShot;

    public void Initialize(DefenseGame game,TowerKind kind,int pad)
    {
        Game=game;Kind=kind;PadIndex=pad;Investment=Cost(kind);
        DefenseArt.Tower(transform,kind,Height,out turret,out Transform muzzle);
        Muzzle=muzzle;
    }
    public static int Cost(TowerKind kind) => kind==TowerKind.Pulse ? 75 : 110;
    public bool Upgrade()
    {
        if(Level>=3 || !Game.Spend(UpgradeCost)) return false;
        Investment+=UpgradeCost;Level++;
        DefenseArt.Burst(transform.position+Vector3.up*Height,DefenseArt.Cyan,.65f);
        return true;
    }

    void FixedUpdate()
    {
        if(!Game.IsCombat) return;
        DefenseEnemy target=null;
        float progress=-1;
        foreach(DefenseEnemy enemy in Game.Enemies)
        {
            Vector3 offset=enemy.transform.position-transform.position;offset.y=0;
            if(enemy.IsAlive && offset.sqrMagnitude<=Range*Range && enemy.DistanceTravelled>progress)
            { target=enemy;progress=enemy.DistanceTravelled; }
        }
        if(target==null) return;
        if(!InterceptSolver.TrySolve(Muzzle.position,target.Body.position,target.CurrentVelocity,
            ProjectileSpeed,Physics.gravity,out Vector3 launch,out float flight)) return;
        turret.rotation=Quaternion.LookRotation(launch);
        if(Time.time<nextShot) return;
        // Turning the barrel changes the muzzle position; refine from the actual launch point.
        if(!InterceptSolver.TrySolve(Muzzle.position,target.Body.position,target.CurrentVelocity,
            ProjectileSpeed,Physics.gravity,out launch,out flight)) return;
        turret.rotation=Quaternion.LookRotation(launch);
        DefenseProjectile.Launch(Game,Muzzle.position,launch,Damage,Kind==TowerKind.Siege);
        ShotsFired++;
        nextShot=Time.time+1f/FireRate;
    }
}
