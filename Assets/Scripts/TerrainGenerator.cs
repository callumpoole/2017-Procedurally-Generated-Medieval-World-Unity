using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    public const int SUB_DIVS = 255;
    public const float MAP_SIZE = 2500;
    public const float MAP_MIN_MAX = MAP_SIZE / 2;
    public const float STRIDE = MAP_SIZE / SUB_DIVS;
    public const float MIN_HEIGHT = 25f;
    public const float MAX_HEIGHT = 75f;

    public static float[][] heightMap;
    
    Mesh MeshGenTerrain() {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        Vector3[] verts = new Vector3[SUB_DIVS * SUB_DIVS];
        int[] indicies = new int[(SUB_DIVS-1) * (SUB_DIVS-1) * 6];
        Vector2[] uvs = new Vector2[SUB_DIVS * SUB_DIVS];
        const int TILES_PER_TEXTURE = 4; //In a given direction (along one axis), measurement of length not area!

        for (int z = 0; z < SUB_DIVS; z++) {
            for (int x = 0; x < SUB_DIVS; x++) {
                int vertexOffset = z * SUB_DIVS + x;
                verts[vertexOffset] = new Vector3(-MAP_SIZE / 2 + x * STRIDE, heightMap[z][x], -MAP_SIZE / 2 + z * STRIDE);
                uvs[vertexOffset]   = new Vector2(x/(float)(TILES_PER_TEXTURE),
                                                  z/(float)(TILES_PER_TEXTURE));

                if (x != SUB_DIVS - 1 && z != SUB_DIVS - 1) {
                    int indexOffset = z * (SUB_DIVS - 1) * 6 + x * 6;
                    indicies[indexOffset + 0] = vertexOffset + 0;              //x;
                    indicies[indexOffset + 1] = vertexOffset + SUB_DIVS + 1;   //z*subDiv+x+1;
                    indicies[indexOffset + 2] = vertexOffset + 1;              //x+1;
                    indicies[indexOffset + 3] = vertexOffset + 0;              //x;
                    indicies[indexOffset + 4] = vertexOffset + SUB_DIVS;       //z*subDiv+x;
                    indicies[indexOffset + 5] = vertexOffset + SUB_DIVS + 1;   //z*subDiv+x+1;
                }
            }
        }
        
        m.vertices = verts;
        m.triangles = indicies;
        m.uv = uvs;
        m.RecalculateNormals();

        //Only needed for multiple textures, if implemented
        m.SetTriangles(indicies, 0);

        return m;
    }

    void Awake() {
        //Random.seed = 9;

        heightMap = new float[SUB_DIVS][];
        for (int i = 0; i < SUB_DIVS; i++)
            heightMap[i] = new float[SUB_DIVS];

        PerlinNoise.GenerateNoise(100); 
        GeneratePerlinHeightValues(120, 0.1f, 10);  //was 100, 0.06, 15

        TerrainFeatures.AddFeatures();
        PrefabPlacer.PlaceObjects();
        ApplySeasonalTheme();
        UpdateFlagColour();
         
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = MeshGenTerrain();
        GetComponent<MeshCollider>().sharedMesh = mf.mesh;
        mf.mesh.subMeshCount = 2;
        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.materials[0].mainTexture = Resources.Load<Texture>("Textures/ShortGrass 2048 Seamless");
        mr.materials[1].mainTexture = Resources.Load<Texture>("Textures/151"); //Unused
        
        CreateTerrainEdges(300);
    }

    void CreateTerrainEdges(float depthDown) {

        for (int i = 0; i < 4; i++) {
            GameObject Wall = new GameObject("Wall" + (i + 1));
            Wall.AddComponent<MeshFilter>();
            Wall.AddComponent<MeshRenderer>().material = (Material)Resources.Load("Materials/Mat_Terrain_Sides", typeof(Material));

            //An unexplainable random number that just fixes the overhang
            const float OVERHANG = 5.9f;

            Mesh m = new Mesh();
            m.name = "WallMesh1";
            Vector3[] verts = new Vector3[2 * SUB_DIVS + 2];
            Vector2[] uvs = new Vector2[2 * SUB_DIVS + 2];
            int[] inds = new int[6 * (SUB_DIVS + 1)];

            float heighestHeight = 0;
            for (int j = 0; j < SUB_DIVS - 1; j++) {
                for (int k = 0; k < SUB_DIVS - 1; k++) {
                    if (heightMap[j][k] > heighestHeight)
                        heighestHeight = heightMap[j][k];
                }
            }
            float UVDeltaHeight = heighestHeight + depthDown;

            for (int j = 0; j <= SUB_DIVS; j++) {
                //4 edges of the square
                switch (i) {
                    case 0:
                        verts[(j * 2) + 0] = new Vector3(-MAP_SIZE / 2 + j * STRIDE,                            -depthDown, MAP_SIZE / 2 - OVERHANG);
                        verts[(j * 2) + 1] = new Vector3(-MAP_SIZE / 2 + j * STRIDE, heightMap[SUB_DIVS - 1][j % SUB_DIVS], MAP_SIZE / 2 - OVERHANG);
                        break;
                    case 1:
                        verts[(j * 2) + 0] = new Vector3(-MAP_SIZE / 2 + j * STRIDE,                 -depthDown, -MAP_SIZE / 2);
                        verts[(j * 2) + 1] = new Vector3(-MAP_SIZE / 2 + j * STRIDE, heightMap[0][j % SUB_DIVS], -MAP_SIZE / 2);
                        break;
                    case 2:
                        verts[(j * 2) + 0] = new Vector3(-MAP_SIZE / 2,                 -depthDown, -MAP_SIZE / 2 + j * STRIDE);
                        verts[(j * 2) + 1] = new Vector3(-MAP_SIZE / 2, heightMap[j % SUB_DIVS][0], -MAP_SIZE / 2 + j * STRIDE);
                        break;
                    case 3:
                        verts[(j * 2) + 0] = new Vector3(MAP_SIZE / 2 - OVERHANG,                            -depthDown, -MAP_SIZE / 2 + j * STRIDE);
                        verts[(j * 2) + 1] = new Vector3(MAP_SIZE / 2 - OVERHANG, heightMap[j % SUB_DIVS][SUB_DIVS - 1], -MAP_SIZE / 2 + j * STRIDE);
                        break;
                } 
                uvs[(j * 2) + 0] = new Vector2(0 + j / ((float)SUB_DIVS), 0);
                uvs[(j * 2) + 1] = new Vector2(0 + j / ((float)SUB_DIVS), 1);
            }
            for (int j = 0; j < verts.Length/2-2; j++) {
                int indexOffset = j * 6;
                inds[indexOffset + 0] = j * 2 + 0;
                inds[indexOffset + 1] = j * 2 + ((i % 2 == 0) ? 3 : 1);
                inds[indexOffset + 2] = j * 2 + ((i % 2 == 0) ? 1 : 3);
                inds[indexOffset + 3] = j * 2 + 0;
                inds[indexOffset + 4] = j * 2 + ((i % 2 == 0) ? 2 : 3);
                inds[indexOffset + 5] = j * 2 + ((i % 2 == 0) ? 3 : 2);
            } 
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = inds;
            m.RecalculateNormals(); 
            Wall.GetComponent<MeshFilter>().mesh = m;  
        }
    }
    

    void GenerateRandomHeightValues() {
        for (int z = 0; z < SUB_DIVS; z++) {
            for (int x = 0; x < SUB_DIVS; x++) {
                heightMap[z][x] = Random.Range(MIN_HEIGHT, MAX_HEIGHT);
            }
        }
    }

    void GeneratePerlinHeightValues(float amplitude, float frequency, int iterations, float freqScaler = 1) {
        float amp, freq;

        for (int z = 0; z < SUB_DIVS; z++) {
            for (int x = 0; x < SUB_DIVS; x++) {
                amp = amplitude;
                freq = frequency;
                for (int i = 0; i < iterations; i++) {
                    heightMap[z][x] += amp * PerlinNoise.Perlin(freq * x / SUB_DIVS, freq * z / SUB_DIVS);
                    amp /= 2;
                    freq *= 2 * freqScaler;
                } 
            }
        }
    }

    void ApplySeasonalTheme() {
        float hue = Random.Range(0.085f, 0.33f);
        float sat = Random.Range(0.33f, 0.66f);
        float val = Random.Range(0.70f, 0.9f);
        Color c = Color.HSVToRGB(hue, sat, val);

        Material terrMat = Resources.Load<Material>("TerrainMat");
        terrMat.color = c;

        Material fernMat = Resources.Load<Material>("SeasonalMaterials/01 - Default");
        fernMat.color = Color.HSVToRGB(hue, Random.Range(0.88f, 1f), val); ;

        Material leaves1 = Resources.Load<Material>("SeasonalMaterials/7276");
        Material leaves2 = Resources.Load<Material>("SeasonalMaterials/texture_leaves_by_kuschelirmel_stock");
        float leaSat = Random.Range(0.5f, 0.9f);
        leaves1.color = Color.HSVToRGB(hue, leaSat, hue*1.5f + 0.5f);
        leaves2.color = Color.HSVToRGB(hue, leaSat, hue*1.5f + 0.5f);
    }

    void UpdateFlagColour() {
        Material flagMat = Resources.Load<Material>("FlagColour");
        flagMat.color = Color.HSVToRGB(Random.Range(0.0f, 1.0f), 
                                       Random.Range(0.7f, 1f), 
                                       1 - (Random.Range(0.0f, 1.0f) * Random.Range(0.0f, 1.0f)));
    }

}
