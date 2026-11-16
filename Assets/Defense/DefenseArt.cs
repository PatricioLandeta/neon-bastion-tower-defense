using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class DefenseArt
{
    public static readonly Color Cyan=new Color(.08f,1.7f,2.2f);
    public static readonly Color Pink=new Color(2f,.15f,.65f);
    private static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    private static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    public static readonly string[] MeshNames={"OctagonalHull","ArrowHull","HeavyHull","Spire","HexPlatform","Road","RoadEdge"};

    public static Material ColorMaterial(string name,Color color)
    {
        if(materials.TryGetValue(name,out Material cached) && cached!=null) return cached;
        Material material=new Material(Resources.Load<Shader>("NeonGlow"));
        material.name=name;material.SetColor("_BaseColor",color);materials[name]=material;
        return material;
    }
    public static Mesh GetMesh(string name)
    {
        if(meshes.TryGetValue(name,out Mesh cached) && cached!=null) return cached;
        Mesh mesh=Resources.Load<Mesh>("DefenseMeshes/"+name);
        if(mesh==null) mesh=GenerateMesh(name);
        meshes[name]=mesh;return mesh;
    }
    public static Mesh GenerateMesh(string name)
    {
        if(name=="Road" || name=="RoadEdge") return RoadMesh(new DefenseRoute(),name=="Road" ? 1.7f : 2.15f);
        if(name=="ArrowHull") return Extrude(name,new Vector2[]{new Vector2(0,1),new Vector2(.65f,-.65f),new Vector2(0,-.3f),new Vector2(-.65f,-.65f)},.55f);
        if(name=="Spire")
        {
            List<Vector3> vertices=new List<Vector3>();List<int> triangles=new List<int>();
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
                Vector3 p=new Vector3(Mathf.Cos(a)*.6f,0,Mathf.Sin(a)*.6f), q=new Vector3(Mathf.Cos(b)*.6f,0,Mathf.Sin(b)*.6f);
                Triangle(vertices,triangles,p,new Vector3(0,1.8f,0),q);
                Triangle(vertices,triangles,q,new Vector3(0,-.7f,0),p);
            }
            return Finish(name,vertices,triangles);
        }
        int sides=name=="HexPlatform" ? 6 : 8;
        Vector2[] outline=new Vector2[sides];
        for(int i=0;i<sides;i++)
        {
            float angle=(i+.5f)*Mathf.PI*2/sides;
            outline[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
        }
        return Extrude(name,outline,name=="HeavyHull" ? .8f : .5f);
    }
    private static Mesh Extrude(string name,Vector2[] outline,float height)
    {
        List<Vector3> vertices=new List<Vector3>();List<int> triangles=new List<int>();
        for(int i=0;i<outline.Length;i++)
        {
            Vector2 a=outline[i],b=outline[(i+1)%outline.Length];
            Vector3 p=new Vector3(a.x,height/2,a.y),q=new Vector3(b.x,height/2,b.y);
            Vector3 bottomP=p-Vector3.up*height,bottomQ=q-Vector3.up*height;
            Triangle(vertices,triangles,new Vector3(0,height/2,0),q,p);
            Triangle(vertices,triangles,new Vector3(0,-height/2,0),bottomP,bottomQ);
            Triangle(vertices,triangles,p,q,bottomQ);Triangle(vertices,triangles,p,bottomQ,bottomP);
        }
        return Finish(name,vertices,triangles);
    }
    private static Mesh RoadMesh(DefenseRoute route,float width)
    {
        List<Vector3> vertices=new List<Vector3>();List<int> triangles=new List<int>();
        for(int i=0;i<route.Points.Count;i++)
        {
            Vector3 direction=(route.Points[Mathf.Min(i+1,route.Points.Count-1)]-route.Points[Mathf.Max(0,i-1)]).normalized;
            Vector3 side=Vector3.Cross(Vector3.up,direction)*width/2;
            vertices.Add(route.Points[i]-side);vertices.Add(route.Points[i]+side);
            if(i==0) continue;
            int previous=(i-1)*2,current=i*2;
            triangles.Add(previous);triangles.Add(current+1);triangles.Add(previous+1);
            triangles.Add(previous);triangles.Add(current);triangles.Add(current+1);
        }
        return Finish("Winding route",vertices,triangles);
    }
    private static void Triangle(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c)
    { int index=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(index);t.Add(index+1);t.Add(index+2); }
    private static Mesh Finish(string name,List<Vector3> v,List<int> t)
    { Mesh mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh; }

    private static GameObject Shape(Transform parent,string name,string mesh,Vector3 position,Vector3 scale,Material material)
    {
        GameObject item=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));item.transform.SetParent(parent,false);
        item.transform.localPosition=position;item.transform.localScale=scale;
        item.GetComponent<MeshFilter>().sharedMesh=GetMesh(mesh);item.GetComponent<MeshRenderer>().sharedMaterial=material;return item;
    }
    private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        GameObject item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name=name;item.transform.SetParent(parent,false);
        item.transform.localPosition=position;item.transform.localScale=scale;
        Collider hitbox=item.GetComponent<Collider>();hitbox.enabled=false;Object.Destroy(hitbox);
        item.GetComponent<Renderer>().sharedMaterial=material;return item;
    }

    public static void Tower(Transform root,TowerKind kind,float height,out Transform turret,out Transform muzzle)
    {
        Material metal=ColorMaterial("Blue titanium",new Color(.16f,.28f,.48f));
        Material glow=ColorMaterial(kind==TowerKind.Pulse ? "Ion" : "Amber",kind==TowerKind.Pulse ? Cyan : new Color(2f,1f,.15f));
        Shape(root,"Hexagonal foundation","HexPlatform",Vector3.up*.25f,new Vector3(.95f,1f,.95f),metal);
        Shape(root,"Power ring","HexPlatform",Vector3.up*.52f,new Vector3(.76f,.2f,.76f),glow);
        Shape(root,"Raised column","OctagonalHull",Vector3.up*(height/2),new Vector3(.46f,height*2-.5f,.46f),metal);
        GameObject pivot=new GameObject("Predictive gimbal");pivot.transform.SetParent(root,false);pivot.transform.localPosition=Vector3.up*height;turret=pivot.transform;
        Shape(turret,"Faceted turret","HeavyHull",Vector3.zero,new Vector3(.62f,.85f,.7f),metal);
        Box(turret,"Capacitor",new Vector3(0,.22f,-.1f),new Vector3(.4f,.12f,.6f),glow);
        if(kind==TowerKind.Pulse)
        {
            Box(turret,"Twin barrel port",new Vector3(-.25f,0,.7f),new Vector3(.16f,.2f,1.2f),metal);
            Box(turret,"Twin barrel starboard",new Vector3(.25f,0,.7f),new Vector3(.16f,.2f,1.2f),metal);
        }
        else Box(turret,"Siege accelerator",new Vector3(0,0,.75f),new Vector3(.42f,.4f,1.5f),metal);
        Box(turret,"Muzzle light",new Vector3(0,0,1.32f),new Vector3(.3f,.16f,.09f),glow);
        muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(turret,false);muzzle.localPosition=new Vector3(0,0,1.5f);
    }

    public static void Enemy(Transform root,EnemyKind kind,out Transform barRoot,out Transform bar)
    {
        bool heavy=kind==EnemyKind.Armored || kind==EnemyKind.Siege;
        float size=kind==EnemyKind.Siege ? 1.4f : 1f;
        Color paint=kind==EnemyKind.Runner ? new Color(1.4f,.3f,.45f) : heavy ? new Color(.7f,.25f,1.3f) : new Color(.22f,.65f,.75f);
        Material armor=ColorMaterial("Armor "+kind,paint),rim=ColorMaterial("Enemy core",Pink);
        Shape(root,"Custom drone hull",kind==EnemyKind.Runner ? "ArrowHull" : "HeavyHull",Vector3.zero,new Vector3(.65f*size,.65f*size,.85f*size),armor);
        Box(root,"Visor",new Vector3(0,.12f,.5f*size),new Vector3(.6f*size,.16f,.12f),rim);
        for(int side=-1;side<=1;side+=2)
        {
            Shape(root,"Track pod","OctagonalHull",new Vector3(side*.56f*size,-.12f,0),new Vector3(.19f*size,.5f,.8f*size),ColorMaterial("Track",new Color(.035f,.05f,.1f)));
            Box(root,"Engine",new Vector3(side*.4f*size,0,-.7f*size),new Vector3(.15f,.15f,.12f),rim);
        }
        if(heavy) Shape(root,"Armor crown","Spire",new Vector3(0,.15f,0),new Vector3(.35f,.45f,.35f),rim);
        barRoot=new GameObject("Health billboard").transform;barRoot.SetParent(root,false);barRoot.localPosition=Vector3.up*(size+.4f);
        Box(barRoot,"HP background",Vector3.zero,new Vector3(1.1f,.1f,.05f),ColorMaterial("Health background",new Color(.035f,.05f,.1f)));
        Transform anchor=new GameObject("HP left anchor").transform;anchor.SetParent(barRoot,false);anchor.localPosition=new Vector3(-.52f,0,-.04f);
        bar=new GameObject("HP fill scale").transform;bar.SetParent(anchor,false);
        Box(bar,"HP",new Vector3(.52f,0,0),new Vector3(1.04f,.065f,.04f),ColorMaterial("Health",Cyan));
    }

    public static void Bolt(Transform root,bool heavy)
    {
        Material glow=ColorMaterial(heavy ? "Shell amber" : "Bullet cyan",heavy ? new Color(2f,1f,.2f) : Cyan);
        Shape(root,"Projectile core","Spire",Vector3.zero,Vector3.one*(heavy ? .22f : .15f),glow);
        TrailRenderer trail=root.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=glow;
        trail.time=.16f;trail.startWidth=heavy ? .23f : .12f;trail.endWidth=0;trail.minVertexDistance=.06f;
    }

    public static void World(DefenseGame game,Transform root)
    {
        Material ground=ColorMaterial("Obsidian terrain",new Color(.065f,.10f,.17f));
        GameObject slab=Box(root,"Floating defense sector",new Vector3(0,-.7f,0),new Vector3(43,1.3f,26),ground);
        BoxCollider floor=slab.AddComponent<BoxCollider>();floor.center=Vector3.zero;floor.size=Vector3.one;
        Shape(root,"Road luminous edge","RoadEdge",new Vector3(0,.015f,0),Vector3.one,ColorMaterial("Route edge",new Color(.08f,.45f,.55f)));
        Shape(root,"Curved asphalt","Road",new Vector3(0,.035f,0),Vector3.one,ColorMaterial("Asphalt",new Color(.11f,.18f,.25f)));
        for(float distance=1;distance<game.Route.Length;distance+=2.1f)
        {
            Vector3 point=game.Route.Position(distance)+Vector3.up*.065f;
            GameObject dash=Box(root,"Route dash",point,new Vector3(.11f,.02f,.65f),ColorMaterial("Route markers",new Color(.35f,.65f,.65f)));
            dash.transform.rotation=Quaternion.LookRotation(game.Route.Tangent(distance));
        }
        for(int i=0;i<game.Pads.Length;i++)
        {
            Vector3 position=game.Pads[i];
            Shape(root,"Build pad "+(i+1),"HexPlatform",position,new Vector3(1.2f,.25f,1.2f),ColorMaterial("Pad glow",new Color(.08f,.75f,.9f)));
            Shape(root,"Pad inset","HexPlatform",position+Vector3.up*.08f,new Vector3(1.05f,.25f,1.05f),ground);
            Box(root,"Pad plus",position+Vector3.up*.16f,new Vector3(.6f,.02f,.11f),ColorMaterial("Pad symbols",Cyan));
            Box(root,"Pad plus",position+Vector3.up*.17f,new Vector3(.11f,.02f,.6f),ColorMaterial("Pad symbols",Cyan));
        }
        Vector3 end=game.Route.Position(game.Route.Length), start=game.Route.Position(0);
        Shape(root,"Relay platform","HexPlatform",end+Vector3.up*.1f,new Vector3(1.8f,.8f,1.8f),ColorMaterial("Relay metal",new Color(.14f,.25f,.38f)));
        Shape(root,"Relay crystal","Spire",end+Vector3.up*1.4f,new Vector3(1f,1.4f,1f),ColorMaterial("Relay light",Cyan));
        Shape(root,"Spawn portal","HexPlatform",start,new Vector3(1.4f,.5f,1.4f),ColorMaterial("Spawn light",Pink));
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI/4;
            Shape(root,"Portal shard","Spire",start+new Vector3(Mathf.Cos(angle),.5f,Mathf.Sin(angle)),new Vector3(.2f,.5f,.2f),ColorMaterial("Spawn light",Pink));
        }
        System.Random random=new System.Random(111);
        for(int i=0;i<32;i++)
        {
            float x=(float)random.NextDouble()*42-21;
            float z=(i%2==0 ? 1 : -1)*(10.8f+(float)random.NextDouble());
            Shape(root,"Border crystal","Spire",new Vector3(x,0,z),new Vector3(.3f,.5f+(float)random.NextDouble(),.3f),ColorMaterial("Scenery crystal",new Color(.16f,.28f,.5f)));
        }
        for(int x=-20;x<=20;x+=4) Box(root,"Terrain circuit",new Vector3(x,-.035f,0),new Vector3(.045f,.02f,25),ColorMaterial("Grid",new Color(.085f,.14f,.22f)));
        GameObject volumeObject=new GameObject("Neon glow volume");volumeObject.transform.SetParent(root,false);
        Volume volume=volumeObject.AddComponent<Volume>();volume.isGlobal=true;
        volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
        Bloom bloom=volume.profile.Add<Bloom>(true);bloom.intensity.Override(.5f);bloom.threshold.Override(1.1f);
    }

    public static LineRenderer Ring(Transform parent,string name,Color color)
    {
        GameObject root=new GameObject(name,typeof(LineRenderer));root.transform.SetParent(parent,false);
        LineRenderer line=root.GetComponent<LineRenderer>();line.useWorldSpace=true;line.sharedMaterial=ColorMaterial(name,color);
        line.widthMultiplier=.11f;line.positionCount=97;
        return line;
    }
    public static void SetRing(LineRenderer ring,Vector3 center,float radius)
    {
        for(int i=0;i<97;i++){float a=i*Mathf.PI*2/96;ring.SetPosition(i,center+new Vector3(Mathf.Cos(a)*radius,.2f,Mathf.Sin(a)*radius));}
    }
    public static void Burst(Vector3 position,Color color,float duration=.5f)
    {
        GameObject root=new GameObject("Impact sparks");root.transform.position=position;
        ParticleSystem particles=root.AddComponent<ParticleSystem>();
        particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=particles.main;main.duration=duration;main.loop=false;main.startLifetime=duration;main.startSpeed=3.5f;main.startSize=.13f;main.gravityModifier=.4f;main.maxParticles=15;
        var emission=particles.emission;emission.enabled=false;
        var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.12f;
        particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=ColorMaterial(color==Cyan ? "Cyan sparks" : "Pink sparks",color);
        particles.Play();particles.Emit(12);Object.Destroy(root,duration+1f);
    }
}
