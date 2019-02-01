using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;

public class WorldManager : MonoBehaviour {

    public static WorldManager Instance { get; private set; }
    public Transform objectTarget;

    Vector3Int gridVolume = new Vector3Int(3, 1, 3);

    Dictionary<Vector3, Chunk> chunks;

    TransformMemoryToken token;
    ChunkGrid grid;

    int globalDistanceScalar = 60;

    void Awake()
    {
        Instance = Instance ?? this;

        if (!objectTarget)
            throw new MissingReferenceException();

        chunks = new Dictionary<Vector3, Chunk>();
        grid = new ChunkGrid( gridVolume );
        token = new TransformMemoryToken( objectTarget, globalDistanceScalar /*SCALE WITH BASE RES*/);

        Initialize(grid.GetGrid(token.Latest()));
        Generate(
              grid.GetGrid(token.Latest())
          );
    }

    void FixedUpdate()
    {
        if (token.IsChanged())
        {
            Generate(
                grid.GetGrid( token.Latest() )
            );
        }

        token.Update();
    }

    void Initialize(Vector3[,,] grid)
    {
        for (int x = 0; x < grid.GetLength(0); x++){
            for (int y = 0; y < grid.GetLength(1); y++) {
                for (int z = 0; z < grid.GetLength(2); z++){
                    Chunk chunk = chunks[grid[x, y, z]] = new GameObject(grid[x, y, z].ToString()).AddComponent<Chunk>();
                    chunk.Render(
                        grid[x, y, z],
                        getTargetDistance(grid[x, y, z] * globalDistanceScalar)
                    );
                }
            }
        }
    }

    void Generate(Vector3[,,] grid)
    {
        Dictionary<Vector3, Chunk> updChunks = new Dictionary<Vector3, Chunk>();
        List<Vector3> newChunks = new List<Vector3>();

        List<Vector3> posses = grid.OfType<Vector3>().ToList();

        for (int x = 0; x < grid.GetLength(0); x++){
            for (int y = 0; y < grid.GetLength(1); y++){
                for (int z = 0; z < grid.GetLength(2); z++)
                {
                    if (!chunks.ContainsKey(grid[x, y, z]))
                        newChunks.Add( grid[x, y, z] );
                }
            }
        }

        foreach (KeyValuePair<Vector3, Chunk> entry in chunks) 
            if (!posses.Contains(entry.Key))
                updChunks.Add(entry.Key, entry.Value);

        int newChunkIndex = 0;

        foreach (KeyValuePair<Vector3, Chunk> entry in updChunks)
        {
            chunks.Remove(entry.Key);

            chunks[newChunks[newChunkIndex]] = entry.Value;
            chunks[newChunks[newChunkIndex]].Render(
                    newChunks[newChunkIndex],
                    getTargetDistance(newChunks[newChunkIndex] * globalDistanceScalar)
                );
            //StartCoroutine(delegateRender(newChunkIndex, chunks[newChunks[newChunkIndex]], newChunks[newChunkIndex]));
            newChunkIndex++;
        }
    }

    int getTargetDistance(Vector3 vector)
    {
        return 2;//Mathf.FloorToInt(Vector3.Distance(objectTarget.position, vector) / globalDistanceScalar);
    }

    IEnumerator delegateRender(float seconds, Chunk chunk, Vector3 to)
    {
        yield return new WaitForSeconds(seconds);
        //chunk.Render(to);

    }
}
