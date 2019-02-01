using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {

    public void Render(Vector4 dimensions, float distance)
    {
        MCubes.instance.Generate(dimensions, distance, this.gameObject);
    }

    public void Dispose()
    {
        Destroy(this.gameObject);
    }
}
