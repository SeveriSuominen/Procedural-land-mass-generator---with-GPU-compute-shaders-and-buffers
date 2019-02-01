using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


static class D3Math
{
    public static int Floor(this int value, int interval)
    {
        var remainder = value % interval;
        return value - remainder;
    }

    public static float Floor(this float value, float interval)
    {
        var remainder = value % interval;
        return value - remainder;
    }

    public static int ScalarDistanceValidate(this float distance, int max)
    {
        return (int) (distance > max ? max : distance);
    }

    public static int ScalarResolution(this int distance)
    {
        int N = new int[] { 64, 32, 16, 8 }[
          (int) distance -1
        ];
        return N;
    }

    public static int ScalarResolution(this float distance, int limit)
    {
        distance = distance.ScalarDistanceValidate(/* here validate limit/max */ limit);

        int N = new int[] { 64, 32, 16, 8 }[
          (int)distance - 1
        ];
        return N;
    }
}


public class MCubes : MonoBehaviour {

    public static MCubes instance { get; private set; }
    public ComputeShader noise, mcubes, normals, clear;
    ComputeBuffer meshBuffer, noiseBuffer,  cubeEdgeFlags, triangleConnectionTable;
    RenderTexture normalsBuffer;

    public Material hdrp;
    public float scale = 1, resScale = 5f;
    NoiseGpu gpuN;
    
    private void Awake()
    {
        instance = instance ?? this;
    }

    // Use this for initialization
    public void Generate(Vector4 dimensions, float distance, GameObject gobj) {

        var distanceIndexer = (int)(distance > 4 ? 4 : distance);

        distance = distance.ScalarDistanceValidate(1);
        distance = (distance).Floor(1) + 1;

        int N = distanceIndexer.ScalarResolution();

       // distance = distance == 0 ? 1 : distance;

        //distance = distance.Floor(2);

        int SIZE = N * N * N * 3 * 5;

        gpuN = new NoiseGpu(7);
        gpuN.LoadResourcesFor4DNoise();

        noiseBuffer = new ComputeBuffer(N * N * N, sizeof(float));

        meshBuffer = new ComputeBuffer(SIZE, sizeof(float) * 7);
        float[] val = new float[SIZE * 7];
        for (int i = 0; i < SIZE * 7; i++)
            val[i] = -1.0f;

        meshBuffer.SetData(val); 

        normalsBuffer = new RenderTexture(N, N, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

        normalsBuffer.dimension = TextureDimension.Tex3D;
        normalsBuffer.enableRandomWrite = true;
        normalsBuffer.useMipMap = false;
        normalsBuffer.volumeDepth = N;
        normalsBuffer.Create();

        noise.SetInt("_Width", N);
        noise.SetInt("_Height", N);

        noise.SetInt("_Resolution", N);

        var nScale = N - 2;

        noise.SetVector("_Pos", (new Vector3(dimensions.x * (N - 3), dimensions.y * (N - 3), dimensions.z * (N - 3))));
        noise.SetFloat("_Scale", distance);
        noise.SetFloat("_ResScale", resScale);
        
        noise.SetFloat("_Frequency", 0.02f);
        noise.SetFloat("_Lacunarity", 2.0f);
        noise.SetFloat("_Gain", 0.5f);
        noise.SetFloat("_Time", dimensions.w);

        noise.SetTexture(0, "_PermTable1D", gpuN.PermutationTable1D);
        noise.SetTexture(0, "_PermTable2D", gpuN.PermutationTable2D);
        noise.SetTexture(0, "_Gradient4D", gpuN.Gradient4D);

        noise.SetBuffer(0, "_Result", noiseBuffer);

        noise.Dispatch(0, N / 8, N / 8, N / 8);

        normals.SetInt("_Width", N);
        normals.SetInt("_Height", N);
        normals.SetBuffer(0, "_Noise", noiseBuffer);
        normals.SetTexture(0, "_Result", normalsBuffer);

        normals.Dispatch(0, N / 8, N / 8, N / 8);

        cubeEdgeFlags = new ComputeBuffer(256, sizeof(int));
        cubeEdgeFlags.SetData(MarchingCubesTables.CubeEdgeFlags);

        triangleConnectionTable = new ComputeBuffer(256 * 16, sizeof(int));
        triangleConnectionTable.SetData(MarchingCubesTables.TriangleConnectionTable);

        mcubes.SetInt("_Width", N);
        mcubes.SetInt("_Height", N);
        mcubes.SetInt("_Depth", N);
        mcubes.SetInt("_Border", 1);
        mcubes.SetFloat("_Target", 0.0f);
        mcubes.SetBuffer(0, "_Voxels", noiseBuffer);
        mcubes.SetTexture(0, "_Normals", normalsBuffer);
        mcubes.SetBuffer(0, "_Buffer", meshBuffer);
        mcubes.SetBuffer(0, "_CubeEdgeFlags", cubeEdgeFlags);
        mcubes.SetBuffer(0, "_TriangleConnectionTable", triangleConnectionTable);

        mcubes.Dispatch(0, N / 8, N / 8, N / 8);

        /*noise.Dispatch(0, N / 8, N / 8, N / 8);

        var data = new float[SIZE];
        noiseBuffer.GetData(data);

        var vertices = new Vector3[SIZE];

        for (int i = 0;  i < SIZE; i++)
            vertices[i] = new Vector3(
                data[i], data[i], data[i]
            ) * 15f;

        return vertices;*/

        ReadBackMesh(gobj, meshBuffer, dimensions, SIZE, N, distance);
    }

    struct Vert
    {
        public Vector4 position;
        public Vector3 normal;
    };

    List<GameObject> ReadBackMesh(GameObject gobj, ComputeBuffer meshBuffer, Vector4 dimensions, int SIZE, int n, float dis)
    {
        //Get the data out of the buffer.
        Vert[] verts = new Vert[SIZE];
        meshBuffer.GetData(verts);

        //Extract the positions, normals and indexes.
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> index = new List<int>();

        List<GameObject> objects = new List<GameObject>();

        int idx = 0;
        for (int i = 0; i < SIZE; i++)
        {
            //If the marching cubes generated a vert for this index
            //then the position w value will be 1, not -1.
            if (verts[i].position.w != -1)
            {
                positions.Add(verts[i].position);
                normals.Add(verts[i].normal);
                index.Add(idx++);
            }
            /*
                        int maxTriangles = 32000;

                        if (idx >= maxTriangles)
                        {
                            objects.Add(MakeGameObject(positions, normals, index,dimensions));
                            idx = 0;
                            positions.Clear();
                            normals.Clear();
                            index.Clear();
                        }*/
        }

        objects.Add(MakeGameObject(gobj, positions, normals, index, dimensions, n, dis));

        //positions.Clear();
        //normals.Clear();
        //index.Clear();

        noiseBuffer.Release();
        meshBuffer.Release();
        cubeEdgeFlags.Release();
        triangleConnectionTable.Release();
        normalsBuffer.Release();

        return objects;
    }

    GameObject MakeGameObject(GameObject gobj,  List<Vector3> positions, List<Vector3> normals, List<int> index, Vector4 dimensions, int N, float dis)
    {
        dimensions = dimensions * dis;

        Mesh mesh = new Mesh();
        mesh.vertices = positions.ToArray();
        mesh.normals = normals.ToArray();
        mesh.bounds = new Bounds(new Vector3(0, N / 2, 0), new Vector3(N, N, N));
        mesh.SetTriangles(index.ToArray(), 0);

        GameObject go = gobj;
        if (!go.GetComponent<MeshFilter>())
            go.AddComponent<MeshFilter>();

        if (!go.GetComponent<MeshRenderer>())
            go.AddComponent<MeshRenderer>();

        //go.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
        go.GetComponent<MeshFilter>().mesh = mesh;
        go.isStatic = true;

        MeshCollider collider = go.GetComponent<MeshCollider>();

        if (!collider)
           collider = go.AddComponent<MeshCollider>();

        collider.sharedMesh = mesh;

        go.transform.parent = transform;

        var powerScaler = new int[] { 1, 2, 4, 8, 16 }[(int)dis];

        //Draw mesh next too the one draw procedurally.
        var nScale = N - 2;
        go.transform.localPosition = new Vector3(dimensions.x * (N - 3), dimensions.y * (N - 3), dimensions.z * (N - 3)) *  (powerScaler * 0.5f);
        go.transform.localScale = new Vector3(powerScaler, powerScaler, powerScaler);
        go.GetComponent<MeshRenderer>().material = hdrp;

        return go;
    }

    void OnDestroy()
    {
        //MUST release buffers.
        noiseBuffer.Release();
        meshBuffer.Release();
        cubeEdgeFlags.Release();
        triangleConnectionTable.Release();
        normalsBuffer.Release();
    }
}
