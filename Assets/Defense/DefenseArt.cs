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
    public static readonly string[] MeshNames={"OctagonalHull","ArrowHull","HeavyHull","Spire","HexPlatform","Road","RoadEdge","Wheel"};

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
        if(name=="Wheel")
        {
            List<Vector3> v=new List<Vector3>();List<int> t=new List<int>();
            for(int i=0;i<20;i++)
            {
                float a=i*Mathf.PI*2/20,b=(i+1)*Mathf.PI*2/20;
                Vector3 p=new Vector3(-.5f,Mathf.Cos(a),Mathf.Sin(a)),q=new Vector3(-.5f,Mathf.Cos(b),Mathf.Sin(b));
                Vector3 r=q+Vector3.right,u=p+Vector3.right;
                Triangle(v,t,new Vector3(-.5f,0,0),p,q);Triangle(v,t,new Vector3(.5f,0,0),r,u);
                Triangle(v,t,p,u,r);Triangle(v,t,p,r,q);
            }
            return Finish(name,v,t);
        }
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

    public static void RaiseTower(Transform root,Transform turret,TowerKind kind,float height,int level)
    {
        Transform column=root.Find("Raised column");
        column.localPosition=Vector3.up*(height/2);
        column.localScale=new Vector3(.46f,height*2-.5f,.46f);
        turret.localPosition=Vector3.up*height;
        Material glow=ColorMaterial(kind==TowerKind.Pulse ? "Ion" : "Amber",kind==TowerKind.Pulse ? Cyan : new Color(2f,1f,.15f));
        if(level>1)
        {
            Transform collar=root.Find("Upgrade collar "+level);
            if(collar==null) collar=Shape(root,"Upgrade collar "+level,"HexPlatform",Vector3.zero,new Vector3(.56f,.15f,.56f),glow).transform;
            collar.localPosition=Vector3.up*(height-.65f);
        }
    }

    public static void MarkFusion(Transform root,Color color)
    {
        Material material=ColorMaterial("Fusion energy "+color,color);
        foreach(MeshRenderer part in root.GetComponentsInChildren<MeshRenderer>(true))
            if(part.name=="Power ring" || part.name=="Capacitor" || part.name=="Muzzle light" || part.name.StartsWith("Upgrade collar")) part.sharedMaterial=material;
        Transform ring=root.Find("Fusion ring");
        if(ring==null) ring=Shape(root,"Fusion ring","HexPlatform",Vector3.up*.64f,new Vector3(1.03f,.16f,1.03f),material).transform;
        ring.GetComponent<MeshRenderer>().sharedMaterial=material;
        Transform beacon=root.Find("Fusion beacon");
        if(beacon==null) beacon=Shape(root,"Fusion beacon","HexPlatform",Vector3.zero,new Vector3(.7f,.16f,.7f),material).transform;
        beacon.localPosition=Vector3.up*(root.Find("Predictive gimbal")!=null ? root.Find("Predictive gimbal").localPosition.y-.4f : 1.6f);
        beacon.GetComponent<MeshRenderer>().sharedMaterial=material;
    }

    public static Transform MergeGhost(DefenseTower source,Transform parent)
    {
        Transform ghost=new GameObject("Dragged tower preview").transform;
        ghost.SetParent(parent,false);
        Material material=ColorMaterial("Merge ghost",new Color(.15f,.7f,1f));
        foreach(MeshFilter original in source.GetComponentsInChildren<MeshFilter>())
        {
            GameObject part=new GameObject("Preview part",typeof(MeshFilter),typeof(MeshRenderer));
            part.transform.SetParent(ghost,false);
            part.transform.localPosition=original.transform.position-source.transform.position;
            part.transform.rotation=original.transform.rotation;
            part.transform.localScale=original.transform.lossyScale;
            part.GetComponent<MeshFilter>().sharedMesh=original.sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        ghost.position=source.transform.position;
        return ghost;
    }

    public static void Enemy(Transform root,EnemyKind kind,out Transform barRoot,out Transform bar)
    {
        if(kind==EnemyKind.Scout)
        {
            LightScout(root);HealthBar(root,1.2f,out barRoot,out bar);return;
        }
        if(kind==EnemyKind.Truck || kind==EnemyKind.Motorcycle)
        {
            Vehicle(root,kind);
            HealthBar(root,kind==EnemyKind.Truck ? 2.3f : 1.65f,out barRoot,out bar);
            return;
        }
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
        HealthBar(root,size+.4f,out barRoot,out bar);
    }

    private static void HealthBar(Transform root,float height,out Transform barRoot,out Transform bar)
    {
        barRoot=new GameObject("Health billboard").transform;barRoot.SetParent(root,false);barRoot.localPosition=Vector3.up*height;
        Box(barRoot,"HP background",Vector3.zero,new Vector3(1.1f,.1f,.05f),ColorMaterial("Health background",new Color(.035f,.05f,.1f)));
        Transform anchor=new GameObject("HP left anchor").transform;anchor.SetParent(barRoot,false);anchor.localPosition=new Vector3(-.52f,0,-.04f);
        bar=new GameObject("HP fill scale").transform;bar.SetParent(anchor,false);
        Box(bar,"HP",new Vector3(.52f,0,0),new Vector3(1.04f,.065f,.04f),ColorMaterial("Health",Cyan));
    }

    private static void LightScout(Transform root)
    {
        Material shell=ColorMaterial("Scout obsidian body",new Color(.06f,.11f,.2f));
        Material glass=ColorMaterial("Scout dark glass",new Color(.09f,.22f,.33f));
        Material light=ColorMaterial("Scout cyan circuitry",new Color(.05f,1.6f,2f));
        Material rubber=ColorMaterial("Scout tire",new Color(.025f,.035f,.06f));
        Box(root,"Light scout chassis",new Vector3(0,-.16f,0),new Vector3(.94f,.22f,1.65f),shell);
        Shape(root,"Light scout nose","ArrowHull",new Vector3(0,-.02f,.32f),new Vector3(.55f,.36f,.8f),shell);
        Shape(root,"Enclosed glass cockpit","HeavyHull",new Vector3(0,.15f,-.1f),new Vector3(.35f,.45f,.43f),glass);
        Box(root,"Cyan windshield",new Vector3(0,.23f,.27f),new Vector3(.5f,.12f,.06f),light);
        Box(root,"Front light blade",new Vector3(0,-.08f,.85f),new Vector3(.72f,.055f,.06f),light);
        Box(root,"Rear energy strip",new Vector3(0,-.09f,-.86f),new Vector3(.7f,.055f,.05f),light);
        List<Transform> wheels=new List<Transform>();
        for(int side=-1;side<=1;side+=2)
        {
            Box(root,"Light scout circuit rail",new Vector3(side*.47f,-.02f,0),new Vector3(.05f,.07f,1.35f),light);
            foreach(float z in new[]{-.52f,.53f})
            {
                Transform wheel=VehicleWheel(root,new Vector3(side*.52f,-.3f,z),.28f,.16f,rubber,light);
                Shape(wheel,"Luminous wheel disc","Wheel",Vector3.zero,new Vector3(1.04f,.78f,.78f),light);
                wheels.Add(wheel);
            }
        }
        root.gameObject.AddComponent<DefenseVehicleWheels>().Initialize(root.GetComponent<DefenseEnemy>(),wheels,.28f);
        Transform exhaust=new GameObject("Scout light wake").transform;exhaust.SetParent(root,false);exhaust.localPosition=new Vector3(0,-.2f,-.9f);
        TrailRenderer wake=exhaust.gameObject.AddComponent<TrailRenderer>();wake.sharedMaterial=light;
        wake.time=.25f;wake.startWidth=.18f;wake.endWidth=0;wake.minVertexDistance=.08f;
        wake.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;wake.receiveShadows=false;
    }

    private static void Vehicle(Transform root,EnemyKind kind)
    {
        Material rubber=ColorMaterial("Rubber",new Color(.018f,.025f,.045f));
        Material steel=ColorMaterial("Vehicle frame",new Color(.12f,.2f,.3f));
        List<Transform> wheels=new List<Transform>();
        if(kind==EnemyKind.Truck)
        {
            Material orange=ColorMaterial("Truck armor",new Color(.8f,.3f,.08f));
            Material amber=ColorMaterial("Truck running lights",new Color(2f,.8f,.12f));
            Box(root,"Truck chassis",new Vector3(0,-.12f,0),new Vector3(1.5f,.25f,3.7f),steel);
            Shape(root,"Armored truck cab","HeavyHull",new Vector3(0,.28f,1.05f),new Vector3(.8f,1.3f,.8f),orange);
            Shape(root,"Heavy cargo body","HeavyHull",new Vector3(0,.32f,-.65f),new Vector3(.8f,1.45f,1.25f),orange);
            Box(root,"Truck windshield",new Vector3(0,.52f,1.57f),new Vector3(1.1f,.34f,.09f),ColorMaterial("Vehicle glass",Cyan));
            Box(root,"Truck grille",new Vector3(0,.05f,1.82f),new Vector3(1.15f,.24f,.08f),rubber);
            Box(root,"Truck ram bumper",new Vector3(0,-.16f,1.9f),new Vector3(1.6f,.2f,.17f),steel);
            for(int side=-1;side<=1;side+=2)
            {
                Box(root,"Truck headlight",new Vector3(side*.55f,.13f,1.83f),new Vector3(.22f,.15f,.09f),amber);
                Box(root,"Cargo luminous rail",new Vector3(side*.71f,.7f,-.65f),new Vector3(.06f,.08f,1.8f),amber);
                foreach(float z in new[]{1.18f,-.65f,-1.35f})
                    wheels.Add(VehicleWheel(root,new Vector3(side*.88f,-.36f,z),.34f,.22f,rubber,amber));
            }
            for(int i=0;i<3;i++) Box(root,"Cargo reinforcement",new Vector3(0,.91f,-1.35f+i*.6f),new Vector3(1.3f,.07f,.12f),steel);
        }
        else
        {
            Material red=ColorMaterial("Motorcycle fairing",new Color(1.3f,.12f,.27f));
            Material glow=ColorMaterial("Motorcycle lights",Cyan);
            Shape(root,"Motorcycle arrow fairing","ArrowHull",new Vector3(0,.05f,.08f),new Vector3(.33f,.6f,.85f),red);
            Box(root,"Bike seat",new Vector3(0,.3f,-.23f),new Vector3(.36f,.13f,.45f),rubber);
            Box(root,"Handlebars",new Vector3(0,.37f,.42f),new Vector3(.75f,.07f,.1f),steel);
            Shape(root,"Rider torso","Spire",new Vector3(0,.42f,-.08f),new Vector3(.3f,.35f,.23f),steel);
            Shape(root,"Rider helmet","OctagonalHull",new Vector3(0,1.03f,.01f),new Vector3(.23f,.65f,.25f),red);
            Box(root,"Rider visor",new Vector3(0,1.05f,.22f),new Vector3(.32f,.1f,.05f),glow);
            Box(root,"Motorcycle headlight",new Vector3(0,.18f,.72f),new Vector3(.19f,.12f,.08f),glow);
            wheels.Add(VehicleWheel(root,new Vector3(0,-.32f,.65f),.38f,.24f,rubber,glow));
            wheels.Add(VehicleWheel(root,new Vector3(0,-.32f,-.65f),.38f,.24f,rubber,glow));
        }
        root.gameObject.AddComponent<DefenseVehicleWheels>().Initialize(root.GetComponent<DefenseEnemy>(),wheels,kind==EnemyKind.Truck ? .34f : .38f);
    }

    private static Transform VehicleWheel(Transform root,Vector3 position,float radius,float width,Material rubber,Material glow)
    {
        Transform wheel=Shape(root,"Rolling wheel","Wheel",position,new Vector3(width,radius,radius),rubber).transform;
        Box(wheel,"Glowing wheel spoke",Vector3.zero,new Vector3(1.03f,.18f,1.45f),glow);
        return wheel;
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
        GameObject starter=new GameObject("Recommended opening marker");starter.transform.SetParent(root,false);
        starter.AddComponent<DefenseStarterHint>().Initialize(game);
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
