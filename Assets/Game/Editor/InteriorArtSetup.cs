using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class InteriorArtSetup
{
    const string Art="Assets/Game/Art/Interior";
    static Material wall,floor,wood,stone,metal,ivory,brass,paper,glow,leaf,soil;
    [MenuItem("Nemzeti Dohánybolt/Apply Interior Art")]
    public static void UpgradeCurrentScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        CreateMaterials();var scene=SceneManager.GetActiveScene();
        var room=Find("Compact Layout");var counter=Find("Counter Assembly");var top=Find("Counter top");var body=Find("Service counter");
        if(room==null||counter==null||top==null||body==null)throw new InvalidOperationException("Open ShopPrototype first.");
        Vector3 savedCounter=counter.position;Quaternion savedRotation=counter.rotation;
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        string[] walls={"Left wall","Back wall","Front wall left","Front wall right","Staff side wall","Expansion partition","Shop side wall front","Shop side wall rear","Extension outer wall","Extension back wall","Extension front wall"};
        foreach(var item in all.Where(t=>walls.Contains(t.name)).ToArray())
        {
            Surface(item,wall,2);WallTrim(item);
        }
        foreach(string name in new[]{"Floor","Extension floor"}){var t=all.FirstOrDefault(p=>p!=null&&p.name==name);if(t!=null)Surface(t,floor,1.2f);}
        foreach(string name in new[]{"Ceiling","Extension ceiling"}){var t=all.FirstOrDefault(p=>p!=null&&p.name==name);if(t!=null)Surface(t,ivory,1);}
        Surface(body,wood,1.4f);top.GetComponent<MeshFilter>().sharedMesh=SaveMesh("CountertopBevel",BeveledTop());Surface(top,stone,1);
        var detail=Owned(counter,"Interior Counter Detail",body.position,body.rotation);
        Vector3 b=body.lossyScale;float front=-b.z*.5f-.008f;
        Box(detail,"Brushed kick plate",new Vector3(0,-b.y*.5f+.075f,front),new Vector3(b.x-.04f,.12f,.018f),metal);
        Box(detail,"Counter front upper edge",new Vector3(0,b.y*.5f-.08f,front),new Vector3(b.x,.012f,.016f),brass);
        for(int i=1;i<4;i++)Box(detail,"Panel joint "+i,new Vector3(-b.x*.5f+i*b.x/4,0,front+.004f),new Vector3(.008f,b.y-.25f,.01f),metal);
        for(int side=-1;side<=1;side+=2)Box(detail,"Walnut end stile",new Vector3(side*(b.x*.5f-.035f),0,front),new Vector3(.065f,b.y-.16f,.02f),wood);
        var topDetail=Owned(counter,"Interior Countertop Props",top.position+top.up*top.lossyScale.y*.5f,top.rotation);
        float edge=top.lossyScale.x*.5f;
        Mug(topDetail,new Vector3(edge-.15f,0,-.22f));Plant(topDetail,new Vector3(-edge+.15f,0,-.22f));
        Box(detail,"Credit notice card",new Vector3(b.x*.28f,.14f,front-.014f),new Vector3(.35f,.20f,.006f),paper);
        Text(detail,"Credit notice","HITEL NINCS.\nTegnap sem volt.",new Vector3(b.x*.28f,.14f,front-.019f),.032f,new Color(.15f,.13f,.10f));
        var badge=Cylinder(detail,"18 plus badge",new Vector3(-b.x*.29f,.18f,front-.013f),new Vector3(.13f,.003f,.13f),brass);badge.localRotation=Quaternion.Euler(90,0,0);
        Text(detail,"18 plus","18+",new Vector3(-b.x*.29f,.18f,front-.019f),.032f,Color.black);
        var decor=Owned(room,"Interior Art",room.position,room.rotation);
        Lighting(decor);RefreshRackArt();
        Box(decor,"Opening hours card",new Vector3(1.25f,1.62f,-4.30f),new Vector3(.42f,.50f,.012f),paper);
        var hours=Text(decor,"Opening hours","NYITVATARTÁS\nH–P: 06–20\nSZO: 07–14\nVAS: ZÁRVA",new Vector3(1.25f,1.62f,-4.289f),.039f,new Color(.13f,.15f,.14f));hours.transform.localRotation=Quaternion.Euler(0,180,0);
        if(counter.position!=savedCounter||counter.rotation!=savedRotation)throw new InvalidOperationException("Counter placement changed.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("INTERIOR_ART_READY: textured surfaces, compact cigarette display, counter details and shop lighting.");
    }
    static void CreateMaterials()
    {
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        foreach(string file in new[]{"FloorTiles","Walnut","WallPaint"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"/"+file+".png");
            if(importer==null)throw new InvalidOperationException("Missing interior texture: "+file);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
        floor=Mat("CeramicFloor",Color.white,.18f,0,"FloorTiles");wood=Mat("WalnutLaminate",Color.white,.22f,0,"Walnut");wall=Mat("WarmPaint",Color.white,.05f,0,"WallPaint");
        stone=Mat("CountertopCharcoal",new Color(.16f,.18f,.18f),.30f);metal=Mat("PowderCoatTeal",new Color(.09f,.145f,.15f),.22f,.25f);
        ivory=Mat("WarmIvory",new Color(.73f,.71f,.65f),.12f);brass=Mat("AgedBrass",new Color(.52f,.43f,.26f),.3f,.5f);
        paper=Mat("NoticePaper",new Color(.82f,.78f,.62f),.05f);leaf=Mat("PlantLeaves",new Color(.12f,.24f,.12f),.18f);soil=Mat("CoffeeAndSoil",new Color(.045f,.027f,.017f),.08f);
        glow=Mat("FluorescentDiffuser",new Color(.94f,.91f,.78f),.20f);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.94f,.91f,.78f)*1.15f);EditorUtility.SetDirty(glow);
        AssetDatabase.SaveAssets();
    }
    static Material Mat(string name,Color color,float smoothness,float metallic=0,string texture=null)
    {
        string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",metallic);
        if(texture!=null)m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/"+texture+".png");EditorUtility.SetDirty(m);return m;
    }
    static void Surface(Transform target,Material material,float units)
    {
        var filter=target.GetComponent<MeshFilter>();var renderer=target.GetComponent<MeshRenderer>();if(filter==null||renderer==null)return;
        renderer.sharedMaterial=material;
        var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];Vector3 size=target.lossyScale;
        for(int i=0;i<vertices.Length;i++)
        {
            var v=Vector3.Scale(vertices[i],size);var n=normals[i];
            uv[i]=Mathf.Abs(n.y)>Mathf.Abs(n.x)&&Mathf.Abs(n.y)>Mathf.Abs(n.z)?new Vector2(v.x,v.z)/units:Mathf.Abs(n.z)>Mathf.Abs(n.x)?new Vector2(v.x,v.y)/units:new Vector2(v.z,v.y)/units;
        }
        mesh.uv=uv;filter.sharedMesh=SaveMesh("Surface_"+target.name.Replace(' ','_'),mesh);
    }
    static void WallTrim(Transform wallObject)
    {
        var trim=Owned(wallObject,"Interior Wall Trim",wallObject.position,wallObject.rotation);var size=wallObject.lossyScale;
        bool side=size.x<size.z;float sign=side?(wallObject.position.x<0?1:-1):(wallObject.position.z<0?1:-1);
        float y=.44f-wallObject.position.y;
        Vector3 position=side?new Vector3(sign*(size.x*.5f+.008f),y,0):new Vector3(0,y,sign*(size.z*.5f+.008f));
        Vector3 dims=side?new Vector3(.012f,.88f,size.z):new Vector3(size.x,.88f,.012f);
        Box(trim,"Painted lower panel",position,dims,metal);
        var rail=position;rail.y=.90f-wallObject.position.y;var railSize=dims;railSize.y=.025f;
        Box(trim,"Chair rail",rail,railSize,ivory);rail.y=.055f-wallObject.position.y;railSize.y=.11f;Box(trim,"Skirting board",rail,railSize,stone);
    }
    public static void RefreshRackArt()
    {
        if(metal==null){metal=AssetDatabase.LoadAssetAtPath<Material>(Art+"/PowderCoatTeal.mat");ivory=AssetDatabase.LoadAssetAtPath<Material>(Art+"/WarmIvory.mat");wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"/WalnutLaminate.mat");brass=AssetDatabase.LoadAssetAtPath<Material>(Art+"/AgedBrass.mat");paper=AssetDatabase.LoadAssetAtPath<Material>(Art+"/NoticePaper.mat");glow=AssetDatabase.LoadAssetAtPath<Material>(Art+"/FluorescentDiffuser.mat");}
        if(metal==null)return;var rack=Find("Cigarette rack");if(rack==null)return;
        var root=Owned(rack,"Interior Rack Detail",rack.position,rack.rotation);
        var back=rack.Find("Rack back");back.localPosition=new Vector3(0,1.13f,.16f);back.localScale=new Vector3(2.20f,2.08f,.08f);back.GetComponent<MeshRenderer>().sharedMaterial=metal;
        for(int row=0;row<4;row++)
        {
            float y=.23f+row*.42f;var shelf=rack.Find("Shelf "+row);shelf.localPosition=new Vector3(0,y,0);shelf.localScale=new Vector3(2.16f,.045f,.30f);shelf.GetComponent<MeshRenderer>().sharedMaterial=metal;
            Box(root,"Shelf front ticket rail "+row,new Vector3(0,y+.001f,-.158f),new Vector3(2.16f,.044f,.014f),ivory);
            for(int col=0;col<8;col++)
            {
                var cell=rack.Find("Cigarette slot "+row+"-"+col);if(cell==null)continue;
                cell.localPosition=new Vector3((col-3.5f)*.25f,y+.0225f,0);cell.GetComponent<ProductSlot>().Configure(new Vector3(.23f,.37f,.26f));
            }
            for(int brand=0;brand<4;brand++)
            {
                float x=(brand-1.5f)*.50f;Text(root,"Shelf price "+row+"-"+brand,ShopCatalog.Money(ShopCatalog.Prices[brand]),new Vector3(x,y-.001f,-.167f),.024f,new Color(.09f,.10f,.10f));
                if(brand<3)Box(root,"Lane divider",new Vector3(x+.25f,y+.075f,.025f),new Vector3(.007f,.09f,.24f),ivory);
            }
        }
        foreach(int side in new[]{-1,1})Box(root,"Cabinet walnut side",new Vector3(side*1.12f,1.13f,.015f),new Vector3(.06f,2.10f,.36f),wood);
        Box(root,"Brand header",new Vector3(0,2.07f,.03f),new Vector3(2.20f,.26f,.20f),metal);
        Box(root,"Header edge",new Vector3(0,2.205f,.025f),new Vector3(2.24f,.012f,.23f),brass);
        Box(root,"Shelf light diffuser",new Vector3(0,1.93f,-.035f),new Vector3(2.08f,.012f,.035f),glow);
        for(int brand=0;brand<4;brand++)
        {
            var old=rack.Find("Brand price "+brand);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            float x=(brand-1.5f)*.50f;
            Text(root,"Brand name "+brand,ShopCatalog.Names[brand],new Vector3(x,2.09f,-.077f),.037f,new Color(.91f,.88f,.78f));
            var color=Mat("BrandAccent"+brand,brand==3?new Color(.70f,.55f,.27f):ProductVisuals.Colors[brand],.18f);
            Box(root,"Brand accent "+brand,new Vector3(x,1.995f,-.079f),new Vector3(.40f,.008f,.006f),color);
        }
        Text(root,"Rack health notice","18+  •  A dohányzás káros az egészségre",new Vector3(0,.115f,-.10f),.027f,new Color(.88f,.86f,.80f));
        var oldSign=Find("Tobacco sign");if(oldSign!=null)oldSign.gameObject.SetActive(false);
        Box(root,"Shop sign frame",new Vector3(0,2.46f,.045f),new Vector3(2.44f,.37f,.065f),brass);
        Box(root,"Shop sign face",new Vector3(0,2.46f,.007f),new Vector3(2.39f,.32f,.016f),metal);
        Text(root,"Shop title","NEMZETI DOHÁNYBOLT",new Vector3(0,2.495f,-.004f),.095f,new Color(.94f,.91f,.80f));
        Text(root,"Shop subtitle","KIS BOLT. NAGY TÖRTÉNETEK.",new Vector3(0,2.385f,-.004f),.037f,new Color(.78f,.73f,.57f));
        Box(root,"Staff notice paper",new Vector3(-1.68f,1.43f,.26f),new Vector3(.56f,.62f,.012f),paper);
        Text(root,"Staff notice","SZEMÉLYIT\nKÉRÜNK.\n\nA bajusz nem okmány.",new Vector3(-1.68f,1.43f,.25f),.043f,new Color(.14f,.16f,.15f));
    }
    static void Lighting(Transform root)
    {
        var sun=Find("Shop Light").GetComponent<Light>();sun.intensity=.85f;sun.color=new Color(.88f,.94f,1f);sun.shadows=LightShadows.None;sun.shadowStrength=.65f;sun.shadowBias=.01f;sun.shadowNormalBias=.03f;
        var old=Find("Interior light").GetComponent<Light>();old.transform.position=new Vector3(.5f,2.72f,.25f);old.type=LightType.Spot;old.transform.rotation=Quaternion.Euler(90,0,0);old.spotAngle=115;old.innerSpotAngle=80;old.intensity=3;old.range=5.5f;old.color=new Color(1,.94f,.82f);old.shadows=LightShadows.None;
        Lamp(root,new Vector3(.5f,2.94f,.25f));Lamp(root,new Vector3(.5f,2.94f,-2.7f));
        var customer=Point(root,"Customer area fill",new Vector3(.5f,2.64f,-2.7f),3f,5.5f,new Color(.96f,.97f,1));customer.type=LightType.Spot;customer.transform.rotation=Quaternion.Euler(90,0,0);customer.spotAngle=115;customer.innerSpotAngle=80;
        var rack=Find("Cigarette rack");Point(root,"Cigarette display fill",rack.TransformPoint(new Vector3(0,1.9f,-.35f)),.65f,2.1f,new Color(1,.94f,.82f));
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.44f,.47f,.50f);RenderSettings.ambientEquatorColor=new Color(.40f,.39f,.35f);RenderSettings.ambientGroundColor=new Color(.23f,.21f,.18f);RenderSettings.reflectionIntensity=.35f;
    }
    static Light Point(Transform root,string name,Vector3 world,float intensity,float range,Color color)
    {var go=new GameObject(name);go.transform.SetParent(root,true);go.transform.position=world;var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=range;light.intensity=intensity;light.color=color;light.shadows=LightShadows.None;return light;}
    static void Lamp(Transform root,Vector3 position)
    {
        var lamp=new GameObject("Fluorescent fixture").transform;lamp.SetParent(root,false);lamp.position=position;
        Box(lamp,"Fixture housing",Vector3.zero,new Vector3(1.22f,.045f,.20f),metal);
        for(int i=-1;i<=1;i+=2){var tube=Cylinder(lamp,"Warm white tube",new Vector3(0,-.032f,i*.047f),new Vector3(.026f,.53f,.026f),glow);tube.localRotation=Quaternion.Euler(0,0,90);}
    }
    static void Mug(Transform root,Vector3 p)
    {
        var mug=new GameObject("Cashier coffee mug").transform;mug.SetParent(root,false);mug.localPosition=p;
        Cylinder(mug,"Ceramic mug",new Vector3(0,.05f,0),new Vector3(.09f,.05f,.09f),ivory);
        Cylinder(mug,"Coffee surface",new Vector3(0,.101f,0),new Vector3(.073f,.001f,.073f),soil);
        var handle=new GameObject("Mug handle");handle.transform.SetParent(mug,false);handle.AddComponent<MeshFilter>().sharedMesh=SaveMesh("MugHandle",HandleMesh());handle.AddComponent<MeshRenderer>().sharedMaterial=ivory;
    }
    static void Plant(Transform root,Vector3 p)
    {
        var plant=new GameObject("Survivor plant").transform;plant.SetParent(root,false);plant.localPosition=p;
        var terracotta=Mat("Terracotta",new Color(.36f,.19f,.11f),.15f);
        Cylinder(plant,"Plant pot",new Vector3(0,.045f,0),new Vector3(.105f,.045f,.105f),terracotta);
        Cylinder(plant,"Pot soil",new Vector3(0,.091f,0),new Vector3(.093f,.001f,.093f),soil);
        var mesh=SaveMesh("PlantLeaf",LeafMesh());
        for(int i=0;i<7;i++)
        {
            var go=new GameObject("Leaf "+i);go.transform.SetParent(plant,false);go.transform.localPosition=new Vector3(0,.09f,0);go.transform.localRotation=Quaternion.Euler(i%2==0?20:42,i*137.5f,0);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=leaf;
        }
    }
    static Transform Owned(Transform parent,string name,Vector3 world,Quaternion rotation)
    {
        var existing=parent.Find(name);if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var root=new GameObject(name).transform;root.SetPositionAndRotation(world,rotation);root.SetParent(parent,true);return root;
    }
    static Transform Find(string name)=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==name);
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material m)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=m;return go;
    }
    static Transform Cylinder(Transform parent,string name,Vector3 p,Vector3 scale,Material m)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=m;return go.transform;
    }
    static TextMesh Text(Transform parent,string name,string value,Vector3 p,float size,Color color)=>ProductVisuals.Text(parent,name,value,p,Quaternion.identity,size,color);
    static Mesh SaveMesh(string name,Mesh mesh)
    {
        mesh.name=name;string path=Art+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
    }
    static Mesh BeveledTop()
    {
        var mesh=new Mesh();var v=new List<Vector3>();var triangles=new List<int>();float[] ys={-.5f,-.36f,.36f,.5f};float[] inset={.008f,0,0,.008f};
        for(int ring=0;ring<3;ring++)for(int side=0;side<8;side++)
        {
            int first=v.Count;v.AddRange(new[]{Corner(side,ys[ring],inset[ring]),Corner(side,ys[ring+1],inset[ring+1]),Corner(side+1,ys[ring+1],inset[ring+1]),Corner(side+1,ys[ring],inset[ring])});triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
        }
        for(int cap=0;cap<2;cap++)for(int side=0;side<8;side++)
        {int first=v.Count;float y=cap==0?-.5f:.5f;v.Add(new Vector3(0,y,0));v.Add(Corner(cap==0?side:side+1,y,.008f));v.Add(Corner(cap==0?side+1:side,y,.008f));triangles.AddRange(new[]{first,first+1,first+2});}
        mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Vector3 Corner(int i,float y,float inset)
    {
        float x=.5f-inset,z=.5f-inset;const float bx=.006f,bz=.022f;
        Vector2[] p={new Vector2(-x+bx,-z),new Vector2(x-bx,-z),new Vector2(x,-z+bz),new Vector2(x,z-bz),new Vector2(x-bx,z),new Vector2(-x+bx,z),new Vector2(-x,z-bz),new Vector2(-x,-z+bz)};return new Vector3(p[i%8].x,y,p[i%8].y);
    }
    static Mesh HandleMesh()
    {
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<=20;i++)for(int j=0;j<=8;j++)
        {float a=i/20f*Mathf.PI*2,b=j/8f*Mathf.PI*2;var center=new Vector3(.048f+.026f*Mathf.Cos(a),.05f+.034f*Mathf.Sin(a),0);var n=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b));vertices.Add(center+n*.006f);normals.Add(n);if(i<20&&j<8){int k=i*9+j;triangles.AddRange(new[]{k,k+9,k+10,k,k+10,k+1});}}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
    }
    static Mesh LeafMesh()
    {
        var mesh=new Mesh();mesh.vertices=new[]{Vector3.zero,new Vector3(-.018f,.065f,.018f),new Vector3(0,.15f,.05f),new Vector3(.018f,.065f,.018f),new Vector3(0,.07f,.028f)};
        mesh.triangles=new[]{0,1,4,1,2,4,2,3,4,3,0,4,4,1,0,4,2,1,4,3,2,4,0,3};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
