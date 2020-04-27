using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using CsvHelper;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UI.Toggle;

public class StarLoader : MonoBehaviour
{
//    public TextAsset file;
    public Queue<Tuple<int, Action<int>>> queue = new Queue<Tuple<int, Action<int>>>();

    public Queue<Tuple<int, Action<int>>> actions = new Queue<Tuple<int, Action<int>>>();

//    public Queue<Action> queue = new Queue<Action>();
//    public Queue<Action> actions = new Queue<Action>();
    private Dictionary<int, GameObject> _stars = new Dictionary<int, GameObject>();

    private Dictionary<string, List<List<Tuple<GameObject, GameObject>>>> constellations =
        new Dictionary<string, List<List<Tuple<GameObject, GameObject>>>>();

    private Dictionary<int, dynamic> allStars = new Dictionary<int, dynamic>();

    private Dictionary<string, GameObject> test = new Dictionary<string, GameObject>();
    private List<GameObject> _orphans = new List<GameObject>();
    public Uri path;
    public DirectoryInfo info;
    private GameObject starsRoot;
    public GameObject constellationMenu;
    public GameObject togglePrefab;
    public GameObject menu;
    private int frequency;
    public GameObject starPrefab;

    private string streamingAssets;
    public ParticleSystem system;
    public Material lineMaterial;


    // Start is called before the first frame update
    void Start()
    {
        streamingAssets = Application.streamingAssetsPath;
        frequency = SystemInfo.processorFrequency;
        Debug.Log("Main");
        Debug.Log(Thread.CurrentThread.ManagedThreadId);
        starsRoot = new GameObject();
        
        starsRoot.SetActive(false);
        
        starsRoot.transform.position = new Vector3(0, 0, 0);
        starsRoot.name = "stars";
//        var item = Instantiate(togglePrefab, constellationMenu.transform);
//        var x = item.GetComponent<Toggle>();
//        x.group = constellationMenu.GetComponent<ToggleGroup>();
//        x.onValueChanged.AddListener((value) =>
//        {
//            Debug.Log("Hello from toggle: " + value);
//        });
//        var m = menu.GetComponent<OMenu>();
//        item.GetComponent<RectTransform>().localPosition += new Vector3(0.0f,-0.0804f*3.0f, 0.0f);
//        
//        
//        m.menuItems[4] = item.GetComponent<Selectable>();

//        for (var i = 0; i < 5; i++)
//        {
//            createMenuItem($"test{i}", i);
//        }

        new Thread(LoadStars).Start(null);
//        new Thread(LoadConstellations).Start(Application.dataPath);
    }

    private GameObject createMenuItem(string culture, int index)
    {
        var item = Instantiate(togglePrefab, constellationMenu.transform);
        var toggle = item.GetComponent<Toggle>();
        toggle.group = constellationMenu.GetComponent<ToggleGroup>();
        toggle.onValueChanged.AddListener((value) => { if(value) DrawConstellations(culture); });
        menu.GetComponent<OMenu>().menuItems[index] = item.GetComponent<Selectable>();
        item.GetComponent<RectTransform>().localPosition += new Vector3(0.7f * (index/15), (-0.0804f * (index % 15)), 0.0f);
//        item.transform.GetChild(0).transform.GetComponent<Label>().text = title;
        item.GetComponentInChildren<Text>().text = culture;
        
        return item;
    }

    // Update is called once per frame
    void Update()
    {
        //Thread.Sleep(5000);
        lock (queue)
        {
            if (queue.Count > 0)
            {
                actions = queue;
                queue = new Queue<Tuple<int, Action<int>>>();
            }
        }

        if (actions.Count > 0)
        {
            foreach (var action in actions)
            {
                try
                {
                    action.Item2(action.Item1);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
            }

            actions.Clear();
        }
    }

    private void LoadStars(object o)
    {
//        var bytes = o as byte[];

        var bytes = File.ReadAllBytes(streamingAssets + "/HYG-Database/hygdata_v3.csv");
        
        Debug.Log("Not Main");
        Debug.Log(Thread.CurrentThread.ManagedThreadId);

        lock (queue)
        {
            queue.Enqueue(new Tuple<int, Action<int>>(-1, (_) =>
            {
                Debug.Log("In Queue");
                Debug.Log(Thread.CurrentThread.ManagedThreadId);
            }));
        }

        Debug.Log($"Number of bytes: {bytes.Count()}");

        using (var mem = new MemoryStream(bytes))
        using (var stream = new StreamReader(mem))
        using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();
            int i = 0;
            while (csv.Read())
            {
                if (queue.Count > frequency)
                {
                    Thread.Sleep(100);
                }

                i++;
                var id = csv.GetField<int?>("hip");
                var dbId = csv.GetField<int>("id");
                dynamic x = new ExpandoObject();
                x.Name = csv.GetField<string>("proper");
                x.Id = id;
                x.X = csv.GetField<float>("x") * 10.0f;
                x.Y = csv.GetField<float>("y") * 10.0f;
                x.Z = csv.GetField<float>("z") * 10.0f;
                
                allStars.Add(dbId, x);


                lock (queue)
                {
                    var who = x;
                    var u = id ?? -1;
                    queue.Enqueue(new Tuple<int, Action<int>>(dbId, (p) =>
                    {
                        var star = allStars[p];
                        if (star.Id==null) return;
                        if (_stars.ContainsKey(star.Id)) return;
//                        var y = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        var y = Instantiate(starPrefab, starsRoot.transform, true);
                        y.tag = "sphere";
                        y.gameObject.transform.position = new Vector3(star.X, star.Y, star.Z);

                        if (star.Name == "Sol")
                        {
                            y.GetComponent<Renderer>().material.color = new Color(1f, 0.15f, 0f);
                        }

                        if (star.Id != null) 
                        {
                            _stars.Add(star.Id, y);
                        }
                        else
                        {
                            _orphans.Add(y);
                        }
                    }));
                }
            }

            Debug.Log("iter:" + i);
            Debug.Log("Done Loading Stars!");
            lock (queue)
            {
                queue.Enqueue(new Tuple<int, Action<int>>(-1, _ =>
                {
                    var emitParams = new ParticleSystem.EmitParams();
                    foreach (var star in _stars)
                    {
                        emitParams.position = star.Value.transform.position;
//                        emitParams.startColor = Color.blue;
//                        break;
                        system.Emit(emitParams, 1);
                    }
                    Debug.Log("Particles Done");

//                    var arr = _stars.Select(x => new ParticleSystem.Particle()
//                    {
////                        position = x.Value.transform.position
//position = new Vector3(0,0,0)
//                    }).ToArray();
//                    system.SetParticles(arr);
                    
                }));
            }

            lock (queue)
            {
                queue.Enqueue(new Tuple<int, Action<int>>(-1, (_) => { new Thread(LoadConstellations).Start(Application.dataPath); }));
            }
        }


//        lock (queue)
//        {
//            queue.Enqueue(() => { Debug.Log("objs: " + GameObject.FindGameObjectsWithTag("sphere").Length); });
//        }

        Debug.Log($"Stars: {_stars.Count}");
        Debug.Log($"Orphans: {_orphans.Count}");
    }

    private void LoadConstellations(object o)
    {
        var dataPath = o as string;
        var z = Path.Combine(streamingAssets, "skycultures");
        var infoFile = new DirectoryInfo(z);
        var subs = infoFile.EnumerateDirectories();
        foreach (var dir in subs)
        {
            var infoPath = Path.Combine(dir.FullName, "info.ini");
            var constPath = Path.Combine(dir.FullName, "constellationship.fab");
            if (!File.Exists(infoPath) || !File.Exists(constPath))
            {
                Debug.Log("Cannot load (missing file): " + dir.FullName);
                continue;
            }

            var info = File.ReadAllLines(infoPath);
            var con = File.ReadAllLines(constPath)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains('#'));
            var name = info
                .Select(x => x.Split('='))
                .First(x => x[0] == "name ")[1].Trim();

            var hips = con
                .Select(
                    x => x.Split(new[] {'\t', ' '})
                        .Where(l => !string.IsNullOrEmpty(l))
                        .Skip(2)
                        .Select(int.Parse)
                        .ToList())
                .ToList();

            constellations.Add(name, new List<List<Tuple<GameObject, GameObject>>>());
            foreach (var hip in hips)
            {
                var tups = new List<Tuple<int, int>>();
                for (var i = 0; i < hip.Count - 1; ++i)
                {
                    tups.Add(new Tuple<int, int>(hip[i], hip[i + 1]));
                }

                var all = tups.Where(x => x.Item1 != x.Item2)
                    .Where(x => _stars.ContainsKey(x.Item1) && _stars.ContainsKey(x.Item2))
                    .Select(x => new Tuple<GameObject, GameObject>(_stars[x.Item1], _stars[x.Item2]));

                constellations[name].Add(all.ToList());
            }


            lock (queue)
            {
                queue.Enqueue(new Tuple<int, Action<int>>(-1, (_) =>
                {
                    var root = new GameObject();
                    root.name = name;
                    root.SetActive(false);
                    root.transform.position = new Vector3(0, 0, 0);
                    createMenuItem("Off", 0);

                    foreach (var c in constellations[name])
                    {
                        foreach (var (item1, item2) in c)
                        {
                            var x = new GameObject();
                            x.transform.position = new Vector3(0, 0, 0);
                            x.transform.parent = root.transform;
                            var rend = x.AddComponent<LineRenderer>();
                            rend.widthMultiplier = 0.3f;
                            var lines = new Vector3[2];
                            lines[0] = item1.transform.position;
                            lines[1] = item2.transform.position;
                            rend.SetPositions(lines);
                            rend.shadowCastingMode = ShadowCastingMode.Off;
                            rend.receiveShadows = false;
                            rend.material = lineMaterial;
                        }
                    }

                    test.Add(name, root);
                    createMenuItem(name, test.Count);
                }));
            }

            Debug.Log(name);
        }

        lock (queue)
        {
            queue.Enqueue(new Tuple<int, Action<int>>(-1, (_) => { DrawConstellations("Western"); }));
        }
    }

    public void DrawConstellations(string culture)
    {
        foreach (var x in test)
        {
            x.Value.SetActive(x.Key == culture);
        }
    }
}