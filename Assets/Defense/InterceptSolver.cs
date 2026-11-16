using UnityEngine;

public static class InterceptSolver
{
    // Solve |r + targetVelocity*t - gravity*t²/2| = projectileSpeed*t.
    // Find the earliest positive root (low arc), then refine it with bisection.
    public static bool TrySolve(Vector3 origin, Vector3 target, Vector3 targetVelocity,
        float speed, Vector3 gravity, out Vector3 launchVelocity, out float flightTime)
    {
        launchVelocity=Vector3.zero; flightTime=0;
        if(speed<=0 || (target-origin).sqrMagnitude<.0001f) return false;
        Vector3 offset=target-origin;
        float previousTime=0;
        for(int sample=1;sample<=400;sample++)
        {
            float time=sample*.02f;
            if(Error(offset,targetVelocity,gravity,speed,time)<=0)
            {
                float low=previousTime, high=time;
                for(int iteration=0;iteration<24;iteration++)
                {
                    float mid=(low+high)*.5f;
                    if(Error(offset,targetVelocity,gravity,speed,mid)>0) low=mid; else high=mid;
                }
                flightTime=(low+high)*.5f;
                launchVelocity=(offset+targetVelocity*flightTime-.5f*gravity*flightTime*flightTime)/flightTime;
                return true;
            }
            previousTime=time;
        }
        return false;
    }
    private static float Error(Vector3 offset,Vector3 velocity,Vector3 gravity,float speed,float time)
    {
        Vector3 travel=offset+velocity*time-.5f*gravity*time*time;
        return travel.sqrMagnitude-speed*speed*time*time;
    }
}
