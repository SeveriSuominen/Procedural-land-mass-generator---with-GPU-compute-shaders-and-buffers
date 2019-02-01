using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testme : MonoBehaviour {

    public MeshFilter filter;
    float locTime;
    // Use this for initialization
    void Start () {
        filter = filter ?? GetComponent<MeshFilter>();
        //MCubes.instance.Generate(Time.time);
    }
}
