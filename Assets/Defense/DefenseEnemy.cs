using UnityEngine;

public enum EnemyKind { Scout, Runner, Armored, Siege }

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
    public int Reward => Kind==EnemyKind.Siege ? 80 : Kind==EnemyKind.Armored ? 20 : 12;
    private bool finished;
    private Transform bar;
    private Transform barRoot;

    public void Initialize(DefenseGame game,EnemyKind kind,int wave)
    {
        Game=game;Kind=kind;
        Speed=kind==EnemyKind.Runner ? 3.9f : kind==EnemyKind.Armored ? 1.65f : kind==EnemyKind.Siege ? 1.25f : 2.6f;
        float hp=kind==EnemyKind.Runner ? 3f : kind==EnemyKind.Armored ? 12f : kind==EnemyKind.Siege ? 48f : 4f;
        MaxHealth=hp*(1f+.12f*(wave-1)); Health=MaxHealth;
        Body=gameObject.AddComponent<Rigidbody>(); Body.isKinematic=true; Body.useGravity=false;
        SphereCollider collider=gameObject.AddComponent<SphereCollider>(); collider.radius=kind==EnemyKind.Siege ? .85f : .62f;collider.center=Vector3.zero;
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
