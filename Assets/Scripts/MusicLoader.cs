using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MusicLoader : MonoBehaviour
{

    public List<AudioClip> sources;
    public GameObject togglePrefab;
    public GameObject musicMenu;
    public OMenu menu;
    public AudioSource player;

    // Start is called before the first frame update
    void Start()
    {
        if (sources.Any())
        {
            player.Stop();
            player.clip = sources[0];
            player.Play();
        }

        CreateMenuItem("Off", 0);
        var i = 0;
        foreach (var clip in sources)
        {
            CreateMenuItem(clip.name, ++i);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    private GameObject CreateMenuItem(string culture, int index)
    {
        var item = Instantiate(togglePrefab, musicMenu.transform);
        var toggle = item.GetComponent<Toggle>();
        toggle.group = musicMenu.GetComponent<ToggleGroup>();
        toggle.onValueChanged.AddListener((value) => { if(value) ToggleAudio(culture); });
        menu.GetComponent<OMenu>().menuItems[index] = item.GetComponent<Selectable>();
        item.GetComponent<RectTransform>().localPosition += new Vector3(0.7f * (index/15), (-0.0804f * (index % 15)), 0.0f);
        item.GetComponentInChildren<Text>().text = culture;
        
        return item;
    }

    private void ToggleAudio(string name)
    {
        if (name == "Off")
        {
            player.Stop();
        }
        else
        {
            player.Stop();
            player.clip = sources.First(x => x.name == name);
            player.Play();
        }
    }

}
