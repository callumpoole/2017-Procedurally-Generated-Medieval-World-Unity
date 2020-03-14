//#define DEBUG_SHAPES

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct Coord {
    public Coord(int _x, int _z) { x = _x; z = _z; }
    public Coord(float _x, float _z) { x = Mathf.Max((int)_x, 0); z = Mathf.Max((int)_z, 0); }
    public int x, z;
    public bool Equals(Coord p) { 
        return (x == p.x) && (z == p.z);
    } 
    public override int GetHashCode() { return x ^ z; }
}
enum EShapeType : byte {
    none = 0,
    circle = 1,
    rect = 2
}
public abstract class Shape {
    public Vector3 centre;
    public float X { get { return centre.x; } set { centre.x = value; } }
    public float Y { get { return centre.y; } set { centre.y = value; } }
    public float Z { get { return centre.z; } set { centre.z = value; } }

    public static void MarkCircle(Circle circle, int bigCubeSize = 30, int iterations = 72, Color col = new Color(), Color fadeCol = new Color()) {
#if DEBUG_SHAPES
        circle.centre = GridUtils.ApplyHeightValue(circle.centre);
        Material m = CreateJunkMaterial();
        m.color = col;

        GameObject cirParent = new GameObject("CircleMarker");
        MakeMiniCube(circle.centre, m, bigCubeSize, cirParent.transform);

        for (int i = 0; i < iterations; i++)
        {
            MarkCircleInternal(circle.centre, circle.radius, i, iterations, m, bigCubeSize / 3, cirParent.transform);
        }
        if (circle.fadeRadius != -1)
        {
            Material m2 = CreateJunkMaterial();
            if (fadeCol == new Color())
                fadeCol = col * 0.6f;
            fadeCol.a = 1;
            m2.color = fadeCol;

            for (int i = 0; i < iterations; i++)
            {
                MarkCircleInternal(circle.centre, circle.fadeRadius, i, iterations, m2, bigCubeSize / 3, cirParent.transform);
            }
        }
#endif
    }
#if DEBUG_SHAPES
    private static void MarkCircleInternal(Vector3 pos, float radius, int i, int iterations, Material m, float size, Transform parent)
    {
        const float TAU = Mathf.PI * 2;
        Vector3 pp = new Vector3(pos.x + radius * Mathf.Cos((i / (float)iterations) * TAU), pos.y, pos.z + radius * Mathf.Sin((i / (float)iterations) * TAU));
        pp = GridUtils.ApplyHeightValue(pp);
        MakeMiniCube(pp, m, size, parent);
    }
#endif
    public static void MarkRect(Rectangle rect, Color col = new Color(), Color fadeCol = new Color()) {
#if DEBUG_SHAPES
        rect.centre = GridUtils.ApplyHeightValue(rect.centre);
        Material m = CreateJunkMaterial();
        m.color = col;

        GameObject rectParent = new GameObject("RectMarker");
        GameObject cube = MakeMiniCube(rect.centre, m, 30, rectParent.transform);

        MarkRectInternal(rect.centre, rect.size, m, rectParent.transform);
        
        if (rect.fadeSize != new Vector2(-1, -1))
        {
            Material m2 = CreateJunkMaterial();
            if (fadeCol == new Color())
                fadeCol = col * 0.6f;
            fadeCol.a = 1;
            m2.color = fadeCol;
            MarkRectInternal(rect.centre, rect.fadeSize, m2, rectParent.transform);
        }
        Transform[] transforms = rectParent.GetComponentsInChildren<Transform>();
        foreach (Transform t in transforms)
            t.parent = null;
        rectParent.transform.position = transforms[1].transform.position;
        foreach (Transform t in transforms)
            t.parent = rectParent.transform;
        rectParent.transform.eulerAngles = new Vector3(0, rect.rotation, 0);
        foreach (Transform t in transforms)
            t.position = GridUtils.ApplyHeightValue(t.position);
#endif
    }
#if DEBUG_SHAPES
    private static void MarkRectInternal(Vector3 pos, Vector2 size, Material m, Transform parent)
    {
        const float INTERACTION_SCALAR = 0.03f;
        int iterationsX = (int)(INTERACTION_SCALAR * size.x);
        int iterationsY = (int)(INTERACTION_SCALAR * size.y);
        for (int i = 0; i < iterationsY; i++)
        {
            float z = pos.z - size.y / 2 + (i / (float)iterationsY) * size.y * 2 / 2;
            Vector3 y1 = new Vector3(pos.x - size.x / 2, 0, z);
            Vector3 y2 = new Vector3(pos.x + size.x / 2, 0, z);

            MakeMiniCube(GridUtils.ApplyHeightValue(y1), m, 10, parent);
            MakeMiniCube(GridUtils.ApplyHeightValue(y2), m, 10, parent);
        }
        for (int i = 0; i < iterationsX; i++)
        {
            float x = pos.x - size.x / 2 + (i / (float)iterationsX) * size.x * 2 / 2;
            Vector3 x1 = new Vector3(x, 0, pos.z - size.y / 2);
            Vector3 x2 = new Vector3(x, 0, pos.z + size.y / 2);

            MakeMiniCube(GridUtils.ApplyHeightValue(x1), m, 10, parent);
            MakeMiniCube(GridUtils.ApplyHeightValue(x2), m, 10, parent);
        }
    }
#endif
    public static void MarkLine(Line line, Color col = new Color()) {
#if DEBUG_SHAPES
        Material m = CreateJunkMaterial();
        m.color = col;
        GameObject lineParent = new GameObject("LineMarker");
        for (int i = 0; i < line.points.Count; i++)
            MakeMiniCube(GridUtils.ApplyHeightValue(line.points[i]), m, 30, lineParent.transform);
#endif
    }
#if DEBUG_SHAPES
    private static GameObject MakeMiniCube(Vector3 pos, Material mat, float customSize = 10, Transform parent = null) {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = pos;
        cube.transform.localScale *= customSize;
        cube.GetComponent<MeshRenderer>().material = mat;
        if (parent != null)
            cube.transform.parent = parent;
        return cube;
    }
    static int matCounter = 0;
#endif
}
public class Circle : Shape {
    public Circle(Vector3 centre, float radius, float fadeRadius = -1) {
        base.centre = centre;
        this.radius = radius;
        this.fadeRadius = fadeRadius;
    }
    public float radius;
    public float fadeRadius = -1;
}
public class Rectangle : Shape {
    public Rectangle(Vector3 centre, Vector2 size, float rotation, Vector2 fadeSize = default(Vector2)) {
        base.centre = centre;
        this.size = size;
        this.rotation = rotation;
        if (fadeSize == default(Vector2))
            this.fadeSize = new Vector2(-1, -1);
        else
            this.fadeSize = fadeSize;
    }
    public Rectangle(Vector3 centre, float width, float height, float rotation, float fadeWidth = -1, float fadeHeight = -1) {
        base.centre = centre;
        size = new Vector2(width, height);
        fadeSize = new Vector2(fadeWidth, fadeHeight);
        this.rotation = rotation;
    }
    public Vector2 size;
    public float rotation;
    public Vector2 fadeSize = new Vector2(-1, -1);
    public float Width { get { return size.x; } set { size.x = value; } }
    public float Height { get { return size.y; } set { size.y = value; } }
    public float FadeWidth { get { return fadeSize.x; } set { fadeSize.x = value; } }
    public float FadeHeight { get { return fadeSize.y; } set { fadeSize.y = value; } }

    public static Rectangle[] MakeRectPath(Line line, float width, float fadeWidthExtra = 0, float fadeAlongExtra = 0) {
        Rectangle[] path = new Rectangle[line.points.Count - 1];
        for (int i = 0; i < line.points.Count - 1; i++) {
            Vector3 midpoint = (line.points[i] + line.points[i + 1])/2;
            Vector3 AtoB = line.points[i + 1] - line.points[i];
            float angle = 90 - Mathf.Rad2Deg * Mathf.Atan2(AtoB.z, AtoB.x);
            path[i] = new Rectangle(midpoint, new Vector2(width, AtoB.magnitude), angle, 
                    new Vector2(width + fadeWidthExtra, AtoB.magnitude + fadeAlongExtra));
        }
        return path;
    }
}
public class Line {
    public List<Vector3> points = new List<Vector3>();
    public void AllignPointsToSurface() {
        for(int i = 0; i < points.Count; i++)
            points[i] = GridUtils.ApplyHeightValue(points[i]); 
    }
    public Vector3 GetInVec(int index) {
        int leftInd  = ((index - 1)+(points.Count))%points.Count;
        int rightInd = ((index + 1)+(points.Count))%points.Count;
        Vector3 left = points[leftInd] - points[index];
        Vector3 right = points[rightInd] - points[index];
        Vector3 ans = left + right;
        ans.y = 0;
        return ans.normalized;
    }
    public Vector3 GetOutVec(int index) {
        return -GetInVec(index);
    }
    public float GetInAngle(int index) {
        Vector3 v = GetInVec(index);
        return Mathf.Atan2(v.z, v.x);
    }
    public float GetOutAngle(int index) {
        Vector3 v = GetOutVec(index);
        return Mathf.Atan2(v.z, v.x);
    }
}
public class Selection {
    public List<Shape> shapes;
    List<EShapeType> types;
    public List<Coord> verts;
    public List<float> intensities;
    public Selection() {
        shapes = new List<Shape>();
        types = new List<EShapeType>();
        verts = new List<Coord>();
        intensities = new List<float>();
    }

    public static Selection operator+(Selection s1, Selection s2) {
        return AddOrSubtract(s1, s2, true);
    }
    public static Selection operator-(Selection s1, Selection s2) {
        return AddOrSubtract(s1, s2, false);
    }
    private static Selection AddOrSubtract(Selection s1, Selection s2, bool add) {
        //New the grid
        float[][] gridVals;
        gridVals = new float[TerrainGenerator.SUB_DIVS][];
        for (int i = 0; i < TerrainGenerator.SUB_DIVS; i++)
            gridVals[i] = new float[TerrainGenerator.SUB_DIVS];

        //Cache and add the intensities
        for (int i = 0; i < s1.verts.Count; i++)
        {
            Coord c = s1.verts[i];
            gridVals[c.x][c.z] = s1.intensities[i];
        }
        for (int i = 0; i < s2.verts.Count; i++)
        {
            Coord c = s2.verts[i];
            gridVals[c.x][c.z] = Mathf.Min(1, gridVals[c.x][c.z] + s2.intensities[i] * (add ? 1 : -1));
        }

        //Record intensities back into s1 if > 0
        s1.verts = new List<Coord>();
        s1.intensities = new List<float>();
        for (int x = 0; x < TerrainGenerator.SUB_DIVS; x++)
        {
            for (int z = 0; z < TerrainGenerator.SUB_DIVS; z++)
            {
                if (gridVals[x][z] > 0)
                {
                    s1.verts.Add(new Coord(x, z));
                    s1.intensities.Add(gridVals[x][z]);
                }
            }
        }
        return s1;
    }
    public static Selection operator *(Selection selection, float n) {
        for (int i = 0; i < selection.verts.Count; i++)
            selection.intensities[i] *= n;
        return selection;
    }
    public static Selection operator /(Selection selection, float n) {
        for (int i = 0; i < selection.verts.Count; i++)
            selection.intensities[i] /= n;
        return selection;
    }

    public static Selection MakeSelection(Circle circle, bool fade = true, float exponential = 1) {
        Selection s = new Selection();
        s.shapes.Add(circle);
        s.types.Add(EShapeType.circle);

        int subDiv = TerrainGenerator.SUB_DIVS;
        for (int z = 0; z < subDiv; z++) {
            for (int x = 0; x < subDiv; x++) {
                Vector3 point = GridUtils.VertexToPoint(x, z);
                Vector3 circleToPoint = point - circle.centre;
                if (circleToPoint.magnitude < circle.radius) {
                    s.verts.Add(new Coord(x, z));
                    s.intensities.Add(1);
                } else if (circleToPoint.magnitude < circle.fadeRadius) {
                    s.verts.Add(new Coord(x, z));
                    if (fade)
                        s.intensities.Add(Mathf.Pow(1-(float)PerlinNoise.fade((circleToPoint.magnitude - circle.radius) / (double)(circle.fadeRadius - circle.radius)), exponential));
                    else
                        s.intensities.Add(1-(circleToPoint.magnitude - circle.radius) / (circle.fadeRadius - circle.radius));
                }
            }
        }
        return s;
    }
    public static Selection MakeSelection(Rectangle rect, float exponential = 1) {
        Selection s = new Selection();
        s.shapes.Add(rect);
        s.types.Add(EShapeType.rect); 

        Vector2 fadeSize = new Vector2((rect.FadeWidth - rect.Width) / 2, (rect.FadeHeight - rect.Height) / 2);
        int subDiv = TerrainGenerator.SUB_DIVS;
        for (int z = 0; z < subDiv; z++) {
            for (int x = 0; x < subDiv; x++) { 
                Vector3 p = GridUtils.VertexToPoint(x, z);
                if (rect.rotation != 0) {
                    p = p - rect.centre;
                    float angle = Mathf.Atan2(p.z, p.x);
                    angle += Mathf.Deg2Rad * rect.rotation;
                    p = p.magnitude * new Vector3(Mathf.Cos(angle), p.y, Mathf.Sin(angle));
                    p = -p + rect.centre;
                }
                //In in inner box
                if (p.x > rect.X - rect.Width / 2 && p.x < rect.X + rect.Width / 2 && p.z < rect.Z + rect.Height / 2 && p.z > rect.Z - rect.Height / 2) { 
                    s.verts.Add(new Coord(x, z));
                    s.intensities.Add(1);
                }
                //Else if outer box
                else if (p.x > rect.X - rect.FadeWidth / 2 && p.x < rect.X + rect.FadeWidth / 2 && p.z < rect.Z + rect.FadeHeight / 2 && p.z > rect.Z - rect.FadeHeight / 2) { 
                    s.verts.Add(new Coord(x, z));
                    float iValueX = 0;
                    float iValueZ = 0;
                    
                    if (p.x < rect.X - rect.Width / 2)
                        iValueX = (p.x - (rect.centre.x - rect.FadeWidth / 2)) / fadeSize.x;

                    else if (p.x > rect.X + rect.Width / 2)
                        iValueX = 1-(p.x - (rect.centre.x + rect.Width / 2)) / fadeSize.x;

                    if (p.z > rect.Z + rect.Height / 2)
                        iValueZ = 1-(p.z - (rect.centre.z + rect.Height / 2)) / fadeSize.y;

                    else if (p.z < rect.Z - rect.Height / 2)
                        iValueZ = (p.z - (rect.centre.z - rect.FadeHeight / 2)) / fadeSize.y;

                    if (iValueX != 0 && iValueZ != 0) {
                        float div = 1;
                        if (iValueX >= iValueZ) 
                            div += iValueZ / iValueX;
                        else
                            div += iValueX / iValueZ; 
                        s.intensities.Add(Mathf.Pow(Mathf.Max(1 - Mathf.Sqrt((1 - iValueX) * (1 - iValueX) + (1 - iValueZ) * (1 - iValueZ)) / Mathf.Sqrt(div), 0), exponential)); 
                    }
                    else 
                        s.intensities.Add(Mathf.Pow(Mathf.Sqrt(iValueX * iValueX + iValueZ * iValueZ), exponential));
                }
            }
        }
        return s;
    }

    public static void RaiseTerrain(Selection s, float height) {
        for (int i = 0; i < s.verts.Count; i++)
            TerrainGenerator.heightMap[s.verts[i].z][s.verts[i].x] += height * s.intensities[i];
    }
    public static void RaiseTerrain(Circle c, float height) { 
        RaiseTerrain(MakeSelection(c), height);
    }
    public static void RaiseTerrain(Rectangle r, float height) {
        RaiseTerrain(MakeSelection(r), height);
    }

    public static void MakeHeight(Selection s, float height, float tollerance = 0) {
        float oppositeOfTollerance = 1 - tollerance;
        for (int i = 0; i < s.verts.Count; i++) {
            float deltaHeight = height - TerrainGenerator.heightMap[s.verts[i].z][s.verts[i].x];
            TerrainGenerator.heightMap[s.verts[i].z][s.verts[i].x] += deltaHeight * s.intensities[i] * oppositeOfTollerance;
        }
    }
    public static void FlattenTerrain(Selection s, float tollerance = 0.5f) {
        float sumHeight = 0;
        float contriubtors = 0;
        for (int i = 0; i < s.verts.Count; i++) {
            sumHeight += TerrainGenerator.heightMap[s.verts[i].z][s.verts[i].x] * s.intensities[i];
            contriubtors += s.intensities[i];
        }
        float targetHeight = sumHeight / contriubtors;
        MakeHeight(s, targetHeight, tollerance);
    }

    public static void BlurTerrain(Selection s, int kernalSize = 1, int iterations = 1) {
        for (int it = 0; it < iterations; it++) {
            float[] outputHeights = new float[s.verts.Count];

            //Calculate the blur values
            for (int i = 0; i < s.verts.Count; i++) {
                float sumNeigh = 0;
                int div = 0;
                Coord c = s.verts[i];
                for (int z = -kernalSize; z <= kernalSize; z++) {
                    for (int x = -kernalSize; x <= kernalSize; x++) {
                        if (c.x + x >= 0 && c.x + x < TerrainGenerator.SUB_DIVS && c.z + z >= 0 && c.z + z < TerrainGenerator.SUB_DIVS) {
                            sumNeigh += TerrainGenerator.heightMap[c.z + z][c.x + x];
                            div++;
                        }
                    }
                }
                outputHeights[i] = sumNeigh / div;
            }

            //Set the blur values
            for (int i = 0; i < s.verts.Count; i++) {
                Coord c = s.verts[i];
                TerrainGenerator.heightMap[c.z][c.x] = outputHeights[i] * s.intensities[i] + 
                                                       TerrainGenerator.heightMap[c.z][c.x] * (1 - s.intensities[i]);
            }
        }
    }
}


public static class GridUtils {
    public static Vector3 VertexToPoint(int vx, int vz) {
        return new Vector3(-TerrainGenerator.MAP_MIN_MAX + vx * TerrainGenerator.STRIDE, 
                           GetHeightAt(vx, vz), 
                           -TerrainGenerator.MAP_MIN_MAX + vz * TerrainGenerator.STRIDE);
    }
    public static Coord PointToNearestVertex(Vector3 v) {
        v += new Vector3(TerrainGenerator.MAP_MIN_MAX, 0, TerrainGenerator.MAP_MIN_MAX);
        float vx = v.x / TerrainGenerator.STRIDE;
        float vz = v.z / TerrainGenerator.STRIDE;
        return new Coord(Mathf.Round(vx), Mathf.Round(vz));
    }
    public static float GetHeightAt(Vector3 point) {
        Coord coord = PointToNearestVertex(point);
        return GetHeightAt(coord.x, coord.z);
    }
    public static float GetHeightAt(int vx, int vz) {
        if (vx > 0 && vx < TerrainGenerator.SUB_DIVS && vz > 0 && vz < TerrainGenerator.SUB_DIVS)
            return TerrainGenerator.heightMap[vz][vx];
        return 0;
    }
    public static Vector3 ApplyHeightValue(Vector3 point) {
        point.y = GetHeightAt(point);
        return point;
    }
    public static bool IsPointInMapXZ(Vector3 point) {
        //Faster than Negating and using && (De Morgan's Law)
        return !(point.x > TerrainGenerator.MAP_MIN_MAX || 
                 point.x < -TerrainGenerator.MAP_MIN_MAX || 
                 point.z > TerrainGenerator.MAP_MIN_MAX || 
                 point.z < -TerrainGenerator.MAP_MIN_MAX);
    }
    public static Vector3 GetPointInMapXZ()
    {
        return new Vector3(Random.Range(-TerrainGenerator.MAP_MIN_MAX, TerrainGenerator.MAP_MIN_MAX),
                           0,
                           Random.Range(-TerrainGenerator.MAP_MIN_MAX, TerrainGenerator.MAP_MIN_MAX));
    }
    public static Vector3 RotateVectorByAngleXZ(Vector3 v, float radians) {
        return new Vector3(v.x * Mathf.Cos(radians) - v.z * Mathf.Sin(radians), 
                           v.y, 
                           v.x * Mathf.Sin(radians) + v.z * Mathf.Cos(radians));
    }
}
