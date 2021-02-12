using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SFAPI;

public class StoryforgeTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Storyforge sf = Storyforge.Get();
        // Character character = GetCharacterByGameID("VV");
        // Debug.Log("The character is called: " + character.SubtitleName);
        Debug.Log(sf);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
