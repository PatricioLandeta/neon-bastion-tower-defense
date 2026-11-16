using System.Collections.Generic;
using UnityEngine;

public class DefenseVehicleWheels : MonoBehaviour
{
    private DefenseEnemy enemy;
    private List<Transform> wheels;
    private float radius;
    public void Initialize(DefenseEnemy actor,List<Transform> parts,float wheelRadius)
    { enemy=actor;wheels=parts;radius=wheelRadius; }
    void Update()
    {
        if(enemy==null || !enemy.IsAlive || !enemy.Game.IsCombat) return;
        float degrees=enemy.Speed/radius*Mathf.Rad2Deg*Time.deltaTime;
        foreach(Transform wheel in wheels) wheel.Rotate(Vector3.right,-degrees,Space.Self);
    }
}
