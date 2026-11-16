using UnityEngine;

public class DefenseProjectile : MonoBehaviour
{
    public Rigidbody Body { get; private set; }
    private DefenseGame game;
    private float damage;
    private bool consumed;

    public static DefenseProjectile Launch(DefenseGame game,Vector3 origin,Vector3 velocity,float damage,bool heavy)
    {
        GameObject root=new GameObject(heavy ? "Siege shell" : "Pulse round");
        root.transform.position=origin;root.layer=8;
        DefenseProjectile shot=root.AddComponent<DefenseProjectile>();
        shot.game=game;shot.damage=damage;
        DefenseArt.Bolt(root.transform,heavy);
        SphereCollider hitbox=root.AddComponent<SphereCollider>();hitbox.radius=heavy ? .25f : .18f;hitbox.center=Vector3.zero;
        shot.Body=root.AddComponent<Rigidbody>();
        shot.Body.useGravity=true;shot.Body.mass=.15f;
        shot.Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        shot.Body.linearVelocity=velocity;
        Destroy(root,8f);
        return shot;
    }

    void OnCollisionEnter(Collision collision)
    {
        if(consumed) return;
        consumed=true;
        if(game.IsCombat)
        {
            DefenseEnemy enemy=collision.collider.GetComponentInParent<DefenseEnemy>();
            if(enemy!=null) enemy.TakeDamage(damage);
            DefenseArt.Burst(transform.position,DefenseArt.Cyan,.35f);
        }
        gameObject.SetActive(false);Destroy(gameObject);
    }
}
