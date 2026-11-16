using System.Collections.Generic;
using UnityEngine;

public sealed class DefenseRoute
{
    public readonly List<Vector3> Points = new List<Vector3>();
    private readonly List<float> distances = new List<float>();
    public float Length => distances[distances.Count - 1];

    public DefenseRoute()
    {
        Vector3[] corners = { new Vector3(-18,0,-6), new Vector3(-13,0,-6),
            new Vector3(-13,0,5), new Vector3(-5,0,5), new Vector3(-5,0,-5),
            new Vector3(4,0,-5), new Vector3(4,0,6), new Vector3(12,0,6),
            new Vector3(12,0,-5), new Vector3(18,0,-5) };
        Add(corners[0]);
        for (int i=1;i<corners.Length-1;i++)
        {
            Vector3 incoming=corners[i]-(corners[i]-corners[i-1]).normalized*1.5f;
            Vector3 outgoing=corners[i]+(corners[i+1]-corners[i]).normalized*1.5f;
            Segment(incoming);
            for(int step=1;step<=12;step++)
            {
                float t=step/12f;
                Add((1-t)*(1-t)*incoming+2*(1-t)*t*corners[i]+t*t*outgoing);
            }
        }
        Segment(corners[corners.Length-1]);
    }

    private void Segment(Vector3 end)
    {
        Vector3 start=Points[Points.Count-1];
        int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(start,end)/.3f));
        for(int i=1;i<=count;i++) Add(Vector3.Lerp(start,end,i/(float)count));
    }
    private void Add(Vector3 point)
    {
        float distance=Points.Count==0 ? 0 : distances[distances.Count-1]+Vector3.Distance(point,Points[Points.Count-1]);
        Points.Add(point); distances.Add(distance);
    }
    public Vector3 Position(float distance)
    {
        distance=Mathf.Clamp(distance,0,Length);
        int low=0, high=distances.Count-1;
        while(high-low>1) { int mid=(low+high)/2; if(distances[mid]>distance) high=mid; else low=mid; }
        float span=distances[high]-distances[low];
        return Vector3.Lerp(Points[low],Points[high],span>0 ? (distance-distances[low])/span : 0);
    }
    public Vector3 Tangent(float distance) => (Position(distance+.1f)-Position(distance-.1f)).normalized;
}
