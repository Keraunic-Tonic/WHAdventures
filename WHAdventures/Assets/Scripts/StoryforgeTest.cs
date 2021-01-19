using System;
using System.Collections;
using System.Collections.Generic;
using SFAPI;
using UnityEngine;

public class StoryforgeTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Storyforge sf = Storyforge.Get();
        Script scriptById = sf.GetSceneItem<Script>("Fcn-e3x-KVE2");
        print(scriptById);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
