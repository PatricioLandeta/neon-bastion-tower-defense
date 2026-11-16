using UnityEngine;

public enum EnemyKind { Scout, Runner, Armored, Siege, Motorcycle, Truck }

public class DefenseEnemy : MonoBehaviour
{
    public DefenseGame Game { get; private set; }
    public EnemyKind Kind { get; private set; }
    public float Speed { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public float DistanceTravelled { get; private set; }
    public Vector3 CurrentVelocity { get; private set; }
    public Rigidbody Body { get; private set; }
    public bool IsAlive => !finished && Health>0;
    public int Reward { get; private set; }
    private bool finished;
    private Transform bar;
    private Transform barRoot;

    public void Initialize(DefenseGame game,EnemyKind kind,int wave)
    {
        Game=game;Kind=kind;
        float hp;
        switch(kind)
        {
            case EnemyKind.Runner: Speed=3.9f;hp=3;Reward=15;break;
            case EnemyKind.Armored: Speed=1.65f;hp=12;Reward=25;break;
            case EnemyKind.Siege: Speed=1.25f;hp=48;Reward=100;break;
            case EnemyKind.Motorcycle: Speed=5.2f;hp=6;Reward=20;break;
            case EnemyKind.Truck: Speed=1.05f;hp=160;Reward=120;break;
            default: Speed=2.6f;hp=4;Reward=10;break;
        }
        MaxHealth=hp*(1f+.12f*(wave-1)); Health=MaxHealth;
        Body=gameObject.AddComponent<Rigidbody>();Body.isKinematic=true;Body.useGravity=false;
        if(kind==EnemyKind.Truck || kind==EnemyKind.Motorcycle)
        {
            BoxCollider hitbox=gameObject.AddComponent<BoxCollider>();
            hitbox.center=kind==EnemyKind.Truck ? new Vector3(0,.28f,0) : new Vector3(0,.4f,0);
            hitbox.size=kind==EnemyKind.Truck ? new Vector3(1.9f,1.65f,3.9f) : new Vector3(.75f,1.65f,1.95f);
        }
        else
        {
            SphereCollider hitbox=gameObject.AddComponent<SphereCollider>();
            hitbox.radius=kind==EnemyKind.Siege ? .85f : .62f;hitbox.center=Vector3.zero;
        }
        gameObject.layer=9;
        DefenseArt.Enemy(transform,kind,out barRoot,out bar);
        SetDistance(0);
    }

    public void SetDistance(float distance)
    {
        DistanceTravelled=Mathf.Clamp(distance,0,Game.Route.Length);
        Vector3 position=Game.Route.Position(DistanceTravelled)+Vector3.up*.7f;
        Body.position=position;transform.position=position;
        CurrentVelocity=Game.Route.Tangent(DistanceTravelled)*Speed;
    }

    void FixedUpdate()
    {
        if(!IsAlive || !Game.IsCombat) { CurrentVelocity=Vector3.zero; return; }
        float next=DistanceTravelled+Speed*Time.fixedDeltaTime;
        if(next>=Game.Route.Length)
        {
            finished=true;CurrentVelocity=Vector3.zero;
            Game.EnemyEscaped(this);
            return;
        }
        Vector3 position=Game.Route.Position(next)+Vector3.up*.7f;
        CurrentVelocity=(position-Body.position)/Time.fixedDeltaTime;
        DistanceTravelled=next;
        Body.MovePosition(position);
        if(CurrentVelocity.sqrMagnitude>.001f) Body.MoveRotation(Quaternion.LookRotation(CurrentVelocity));
    }

    void LateUpdate()
    {
        if(barRoot!=null && Game.View!=null) barRoot.rotation=Game.View.transform.rotation;
    }

    public void TakeDamage(float damage)
    {
        if(!IsAlive || damage<=0 || !Game.IsCombat) return;
        Health=Mathf.Max(0,Health-damage);
        bar.localScale=new Vector3(Health/MaxHealth,1,1);
        if(Health>0) return;
        finished=true;
        DefenseArt.Burst(transform.position,DefenseArt.Pink);
        gameObject.SetActive(false);
        Game.EnemyDestroyed(this);
        Destroy(gameObject);
    }
}
