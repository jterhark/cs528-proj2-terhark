using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using CsvHelper;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;

public class StarLoader : MonoBehaviour
{
    
    //Public Props
    public ParticleSystem system;
    public Material lineMaterial;
    public GameObject constellationMenu;
    public GameObject togglePrefab;
    public GameObject menu;
    public GameObject starPrefab;
    
    //queues used for looper
    private Queue<Tuple<int, Action<int>>> queue = new Queue<Tuple<int, Action<int>>>();
    private Queue<Tuple<int, Action<int>>> actions = new Queue<Tuple<int, Action<int>>>();

    //map from hid to gameobject representing star
    private readonly Dictionary<int, GameObject> _stars = new Dictionary<int, GameObject>();
    
    //stars without an hid
    private readonly List<GameObject> _orphans = new List<GameObject>();

    //map from csv id to star data
    private readonly Dictionary<int, dynamic> allStars = new Dictionary<int, dynamic>();

    //map culture name to gameobject containing all constellations for that object
    private readonly Dictionary<string, GameObject> _cultures = new Dictionary<string, GameObject>();
    
    // GameObject where all stars will be placed as children
    private GameObject _starsRoot;
    
    //frequency of processor;
    private int _frequency; 
    
    //data location
    private string _streamingAssets; 
    

    
    void Start()
    {
        //setup
        _streamingAssets = Application.streamingAssetsPath;
        _frequency = SystemInfo.processorFrequency;
        _starsRoot = new GameObject();
        _starsRoot.SetActive(false);
        _starsRoot.transform.position = new Vector3(0, 0, 0);
        _starsRoot.name = "stars";
        
        //read file and process it on another thread
        //starts loading constellations when done.
        new Thread(LoadStars).Start(null);
    }



    // Update is called once per frame
    void Update()
    {
        //check if there are any actions in the queue to execute
        lock (queue)
        {
            if (queue.Count > 0)
            {
                actions = queue;
                queue = new Queue<Tuple<int, Action<int>>>();
            }
        }

        if (actions.Count <= 0) return; //nothing to do
        
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

    private void LoadStars(object o)
    {
        var bytes = File.ReadAllBytes(_streamingAssets + "/HYG-Database/hygdata_v3.csv");

        //get csv file ready
        using (var mem = new MemoryStream(bytes))
        using (var stream = new StreamReader(mem))
        using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();
            while (csv.Read())
            {
                if (queue.Count > _frequency)
                {
                    Thread.Sleep(100);
                }

                //extract properties from CSV
                var id = csv.GetField<int?>("hip");
                var dbId = csv.GetField<int>("id");
                dynamic x = new ExpandoObject();
                x.Name = csv.GetField<string>("proper");
                x.Id = id;
                x.X = csv.GetField<float>("x") * 5.0f;
                x.Y = csv.GetField<float>("y") * 5.0f;
                x.Z = csv.GetField<float>("z") * 5.0f;

                allStars.Add(dbId, x);

                //create star on main thread
                lock (queue)
                {
                    queue.Enqueue(new Tuple<int, Action<int>>(dbId, (p) =>
                    {
                        var star = allStars[p];
                        if (star.Id != null && _stars.ContainsKey(star.Id)) return; //some stars have duplicate hids in db
                        var y = Instantiate(starPrefab, _starsRoot.transform, true);
                        y.gameObject.transform.position = new Vector3(star.X, star.Y, star.Z);

                        if (star.Name == "Sol")
                        {
                            y.tag = "sol";
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

            Debug.Log("Done Loading Stars!");
            
            //create star particles
            lock (queue)
            {
                queue.Enqueue(new Tuple<int, Action<int>>(-1, _ =>
                {
                    var emitParams = new ParticleSystem.EmitParams();
                    foreach (var star in allStars)
                    {
                        emitParams.position = new Vector3(star.Value.X, star.Value.Y, star.Value.Z);
                        emitParams.startColor = star.Value.Name == "Sol" ? Color.red : Color.white;
                        system.Emit(emitParams, 1);
                    }

                    Debug.Log("Particles Done");
                }));
            }

            //now that stars are loaded, start loading constellations
            lock (queue)
            {
                queue.Enqueue(new Tuple<int, Action<int>>(-1,
                    (_) => { new Thread(LoadConstellations).Start(null); }));
            }
        }

        Debug.Log($"Stars: {_stars.Count}");
        Debug.Log($"Orphans: {_orphans.Count}");
    }

    private void LoadConstellations(object _)
    {
    //map from culture name to list of constellations (which are lists of tuples)
        var constellations = new Dictionary<string, List<List<Tuple<GameObject, GameObject>>>>();
    
        var subs = new DirectoryInfo(Path.Combine(_streamingAssets, "skycultures")).EnumerateDirectories();
        foreach (var dir in subs)
        {
            var infoPath = Path.Combine(dir.FullName, "info.ini");
            var constPath = Path.Combine(dir.FullName, "constellationship.fab");
            if (!File.Exists(infoPath) || !File.Exists(constPath))
            {
                Debug.Log("Cannot load (missing file): " + dir.FullName);
                continue;
            }

            var about = File.ReadAllLines(infoPath);
            var con = File.ReadAllLines(constPath)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains('#'));
            var cultureName = about
                .Select(x => x.Split('='))
                .First(x => x[0] == "name ")[1].Trim();

            var hips = con
                .Select(                 //foreach culture
                    x => x.Split(new[] {'\t', ' '})     //split on tab or space
                        .Where(l => !string.IsNullOrEmpty(l))    //get rid of empty lines
                        .Skip(2)                                 //skip first two values
                        .Select(int.Parse)                       //convert to int
                        .ToList())                               //list of stars (constellation)
                .ToList();               //list of constellations (culture)

            constellations.Add(cultureName, new List<List<Tuple<GameObject, GameObject>>>());
            
            //convert to pairs of lines to draw
            foreach (var hip in hips)
            {
                var tups = new List<Tuple<int, int>>();
                for (var i = 0; i < hip.Count - 1; i += 2)
                {
                    tups.Add(new Tuple<int, int>(hip[i], hip[i + 1]));
                }

                var all = tups.Where(x => x.Item1 != x.Item2)
                    .Where(x => _stars.ContainsKey(x.Item1) && _stars.ContainsKey(x.Item2)) //make sure hid exists
                    .Select(x => new Tuple<GameObject, GameObject>(_stars[x.Item1], _stars[x.Item2]));

                constellations[cultureName].Add(all.ToList());
            }


            lock (queue)
            {
                //create gameobjects with line renderers
                queue.Enqueue(new Tuple<int, Action<int>>(-1, (ign) =>
                {
                    var root = new GameObject {name = cultureName};
                    root.SetActive(false);
                    root.transform.position = new Vector3(0, 0, 0);
                    CreateMenuItem("Off", 0);

                    foreach (var c in constellations[cultureName])
                    {
                        //draw lines
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

                    _cultures.Add(cultureName, root);
                    CreateMenuItem(cultureName, _cultures.Count);
                }));
            }
        }

        //turn on Western Constellations
        lock (queue)
        {
            queue.Enqueue(new Tuple<int, Action<int>>(-1, (ign) => { DrawConstellations("Western"); }));
        }
    }

    public void DrawConstellations(string culture)
    {
        foreach (var x in _cultures)
        {
            x.Value.SetActive(x.Key == culture);
        }
    }
    
    private GameObject CreateMenuItem(string culture, int index)
    {
        var item = Instantiate(togglePrefab, constellationMenu.transform);
        var toggle = item.GetComponent<Toggle>();
        toggle.group = constellationMenu.GetComponent<ToggleGroup>();
        toggle.onValueChanged.AddListener((value) =>
        {
            if (value) DrawConstellations(culture);
        });
        menu.GetComponent<OMenu>().menuItems[index] = item.GetComponent<Selectable>();
        item.GetComponent<RectTransform>().localPosition +=
            new Vector3(0.7f * (index / 15), (-0.0804f * (index % 15)), 0.0f);
        item.GetComponentInChildren<Text>().text = culture;

        return item;
    }
}