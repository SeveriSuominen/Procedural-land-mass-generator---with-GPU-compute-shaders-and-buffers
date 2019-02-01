using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkGrid {

    public Vector3Int volume { get; private set; }
 
    public ChunkGrid(Vector3Int volume)
    {
        this.volume = volume;
    }

    public Vector3[,,] GetGrid(Vector3 position)
    {
        position.y = 0;

        int volx = volume.x + volume.x + 1;
        int voly = volume.y + volume.y + 1;
        int volz = volume.z + volume.z + 1;

        /* with distance precalculated for mesh lodding options */
        Vector3[,,] grid = new Vector3[volx, voly, volz]; 

        for (int xi = 0, x = volume.x * -1 ; x <= volume.x; x++, xi++) {
            for (int yi = 0, y = volume.y * -1; y <= volume.y; y++, yi++) {
                for (int zi = 0, z = volume.z * -1; z <= volume.z; z++, zi++) {
                    //float distance = Vector3.Distance(new Vector3(x, y, z), Vector3.zero);
                    grid[xi, yi, zi] = new Vector3(x, y, z) + position;
                }
            }
        }
        return grid;
    }
}
