using UnityEngine; 
using System.Collections;

public static class TerrainFeatures {

    public enum EFeatureType { //Initially used for marking quadrants
        None = 0, 
        Hill,
        Forest,
        Fields,
        Shore,

        Fort//maybe
    }

    public static Circle hill;
    public static Circle backhill;
    private const float HILL_MIN_RADIUS = TerrainGenerator.MAP_SIZE * 0.25f;
    private const float HILL_MAX_RADIUS = TerrainGenerator.MAP_SIZE * 0.325f;
    private static float hillHeight;
    private static Line pathWay;
    public static float pathExitRadians;

    public static Circle fieldCircle;
    public static Circle forestCircle;
    public static Shape[] shoreShapes = new Shape[5]; //Centre, VerCir, HorCir, VerBox, HorBox
    private static Selection shoreSelection;

    private static EFeatureType[] quadrants = new EFeatureType[4];

    private static int GetQuadIndex(EFeatureType ft) {
        for (int i = 0; i < 4; i++)
            if (quadrants[i] == ft)
                return i;
        return -1;
    }
    private static Vector3 GetQuadrandCentre(EFeatureType ft) {
        for (int i = 0; i < 4; i++)
            if (quadrants[i] == ft) 
                return GetQuadrandCentre(i); 
        return new Vector3();
    }
    private static Vector3 GetQuadrandCentre(int i) {
        const float SIZE = TerrainGenerator.MAP_SIZE;
        return new Vector3(-SIZE / 4 + (i % 2) * SIZE / 2,
                           0, 
                           -SIZE / 4 + (i / 2) * SIZE / 2);
    }

    public static float HillSizeCoefficient() { return hill.radius / HILL_MIN_RADIUS; }

    public static void AddFeatures() {
        GenHill();

        //Look at Quadrants
        int hillQuadNum = 0;
        if (hill.centre.z > 0)
            hillQuadNum += 2;
        if (hill.centre.x > 0)
            hillQuadNum++;
        quadrants[hillQuadNum] = EFeatureType.Hill;

        bool largeXShore = false, largeZShore = false;

        int numFeaturesToAdd = 4;
        while (numFeaturesToAdd > 1) {
            int i = Random.Range(0, 4);
            if (quadrants[i] == EFeatureType.None) {
                if (numFeaturesToAdd == 4) { //If hill
                    float absX = Mathf.Abs(hill.centre.x);
                    float absZ = Mathf.Abs(hill.centre.z);
                    float div = Mathf.Max(absX, absZ);
                    absX /= div; 
                    absZ /= div;
                    float delta = Mathf.Max(absX, absZ) - Mathf.Min(absX, absZ);
                    if (delta < 0.3f) {
                        int opp = (3 - hillQuadNum) % 4; //Make the hill opposite to the shore to allow enough space
                        quadrants[opp] = (EFeatureType)numFeaturesToAdd--;
                        largeXShore = largeZShore = true;
                    } else {
                        int xNeighbour, zNeighbour;
                        xNeighbour = hillQuadNum + (hillQuadNum % 2 == 0 ? 1 : -1);
                        zNeighbour = hillQuadNum + (hillQuadNum / 2 == 0 ? 2 : -2);
                        if (absX > absZ) {
                            quadrants[xNeighbour] = (EFeatureType)numFeaturesToAdd--;
                            largeZShore = true;
                        } else {
                            quadrants[zNeighbour] = (EFeatureType)numFeaturesToAdd--;
                            largeXShore = true;
                        }
                    }
                } else 
                    quadrants[i] = (EFeatureType)numFeaturesToAdd--;
            }
        }
        int fieldID = -1, forestID = -1;
        for (int i = 0; i < 4; i++) {
            if (quadrants[i] == EFeatureType.Fields)
                fieldID = i;
            if (quadrants[i] == EFeatureType.Forest)
                forestID = i;
        }
        CheckFieldAndForrestQuads(fieldID, forestID);

        fieldCircle.fadeRadius = fieldCircle.radius * 1.3f;
        Selection.FlattenTerrain(Selection.MakeSelection(fieldCircle), 0.6f);
        MarkShore(largeXShore, largeZShore);
        MarkPathway();
        //Shape.MarkCircle(ForestCircle, 30, 72, new Color(0, 1, 0));
        //Shape.MarkCircle(FieldCircle, 30, 72, new Color(1, 0.5f, 0));
    }

    private static void CheckFieldAndForrestQuads(int fieldID, int forestID, bool goSmaller = false) {
        float circleRadius = TerrainGenerator.MAP_MIN_MAX / (goSmaller ? 3.1f : 2.8f);
        const int CHECKS_PER_QUAD = 5;
        float lowestVariance = float.MaxValue;
        int lowestQuadrantID = fieldID;
        Vector3 flattestPoint = new Vector3(); 
        for (int i = 0; i < CHECKS_PER_QUAD*2; i++) { 
            int quadrantID = i < CHECKS_PER_QUAD ? fieldID : forestID;
            Vector3 quadrantCentre = GetQuadrandCentre(quadrants[quadrantID]);
            Vector3 checkingLoc = quadrantCentre + GridUtils.GetPointInMapXZ() / 4;
            Circle circle = new Circle(checkingLoc, circleRadius);

            //Check it's not going to appear within the walls.
            if ((hill.centre - circle.centre).magnitude < circle.radius + hill.radius) 
                continue;

            Selection sel = Selection.MakeSelection(circle);
            int N = 0;
            float mean = 0;
            float variance = 0;
            float[] yVals = new float[sel.verts.Count];

            //Find Mean & Variance to find lowest Variance
            for (int j = 0; j < sel.verts.Count; j++, N++) {
                yVals[j] = GridUtils.VertexToPoint(sel.verts[j].x, sel.verts[j].z).y;
                mean += yVals[j];
            }
            mean /= N;
            for (int j = 0; j < sel.verts.Count; j++)
                variance += (yVals[j] - mean) * (yVals[j] - mean);
            variance /= N;
            if (variance < lowestVariance) {
                lowestVariance = variance;
                lowestQuadrantID = quadrantID;
                flattestPoint = checkingLoc;
            }
        }

        if (flattestPoint == Vector3.zero) { //If there was no possible room for the farmland to fit, try the algo again, but with a smaller circle
            CheckFieldAndForrestQuads(fieldID, forestID, true);
            return;
        }

        if (fieldID != lowestQuadrantID) {
            //Swap the preassigned quadrants
            quadrants[lowestQuadrantID] = EFeatureType.Fields;
            quadrants[fieldID] = EFeatureType.Forest; 
        }
        fieldCircle = new Circle(flattestPoint, circleRadius); 
        float forestRadius = (TerrainGenerator.MAP_MIN_MAX / 3) * Random.Range(0.65f, 0.88f);
        float forestFadeRadius = forestRadius * Random.Range(1.2f, 1.35f);
        forestCircle = new Circle(GetQuadrandCentre(EFeatureType.Forest), forestRadius, forestFadeRadius);
        while ((hill.centre - forestCircle.centre).magnitude < forestFadeRadius + hill.radius) {
            forestFadeRadius = forestRadius * Random.Range(1.1f, 1.35f);
            forestCircle = new Circle(GetQuadrandCentre(EFeatureType.Forest) + GridUtils.GetPointInMapXZ()/4, forestRadius, forestFadeRadius);
        }
    }

    private static void MarkPathway() {
        pathWay = new Line();
        pathExitRadians = Mathf.Atan2(hill.centre.normalized.z, hill.centre.normalized.x) + Mathf.PI;

        Vector3 vectorFromAngle = new Vector3(Mathf.Cos(pathExitRadians), 0, Mathf.Sin(pathExitRadians));
        pathWay.points.Add(hill.centre + vectorFromAngle * hill.radius);
        pathWay.points.Add(hill.centre + vectorFromAngle * hill.fadeRadius * 1.1f);

        Circle raisePt2 = new Circle(pathWay.points[1], TerrainGenerator.MAP_MIN_MAX / 7, TerrainGenerator.MAP_MIN_MAX / 2);
        Selection.RaiseTerrain(raisePt2, 50);

        //_Rect[] paths = _Rect.MakeRectPath(pathWay, 80, 200, 200);
        Rectangle[] paths = Rectangle.MakeRectPath(pathWay, 130, 0, 0);

        Selection path0Sel = Selection.MakeSelection(paths[0]);
        Selection.BlurTerrain(path0Sel, 3, 1);

        //Shape.MarkLine(pathWay, new Color(1, 0.5f, 0));
        //Shape.MarkRect(paths[0]);
    }

    private static void MarkShore(bool isLargeX = false, bool isLargeZ = false) {
        Vector3 centre = GetQuadrandCentre(EFeatureType.Shore);
        const float LARGE_SCALAR = 1.6f;
        float xScalar = (isLargeX ? LARGE_SCALAR : 1);
        float zScalar = (isLargeZ ? LARGE_SCALAR : 1);
        //Debug.Log("Shore:" + GetQuadIndex(featureType.Shore));

        int hillQuadrant = GetQuadIndex(EFeatureType.Hill);
        const float MIN_MAX = TerrainGenerator.MAP_MIN_MAX;
        int xMax = (int)(centre.x * 2);
        int zMax = (int)(centre.z * 2);
        int x1 = (int)(xMax / MIN_MAX);
        int z1 = (int)(zMax / MIN_MAX);

        int xWall = hillQuadrant % 2 == 0 ? 1 : -1;
        int zWall = hillQuadrant > 1 ? -1 : 1;
        float h = Random.Range(MIN_MAX/3, MIN_MAX/1.5f);
        float w = Random.Range(MIN_MAX/3, MIN_MAX/1.5f);
        float distance = Random.Range(MIN_MAX / 2, MIN_MAX / 4);

        Circle shore = new Circle(new Vector3 (xMax, 0, zMax), TerrainGenerator.MAP_MIN_MAX / Random.Range(2f, 2.8f));
        Rectangle vertical = new Rectangle(new Vector3  (xMax, 0, z1 * (MIN_MAX - h * zScalar / 2.5f)), new Vector2(distance, h * zScalar), 0);
        Rectangle horizontal = new Rectangle(new Vector3(x1 * (MIN_MAX - w * xScalar / 2.5f), 0, zMax), new Vector2(w * xScalar, distance), 0);
        Circle vCurve = new Circle(new Vector3(xMax, 0, z1 * (MIN_MAX - h * zScalar)), Random.Range(0.8f, 1.2f) * distance / 2);
        Circle hCurve = new Circle(new Vector3(x1 * (MIN_MAX - w * xScalar), 0, zMax), Random.Range(0.8f, 1.2f) * distance / 2);

        shore.fadeRadius = shore.radius * 1.3f;
        vertical.fadeSize = vertical.size * 1.3f;
        horizontal.fadeSize = horizontal.size * 1.3f;
        vCurve.fadeRadius = vCurve.radius * 1.3f;
        hCurve.fadeRadius = hCurve.radius * 1.3f;

        shoreShapes[0] = shore;
        shoreShapes[1] = vCurve;
        shoreShapes[2] = hCurve;
        shoreShapes[3] = vertical;
        shoreShapes[4] = horizontal;

        shoreSelection = Selection.MakeSelection(shore) + Selection.MakeSelection(vertical) + Selection.MakeSelection(horizontal) + 
                                                          Selection.MakeSelection(vCurve)   + Selection.MakeSelection(hCurve);

        Selection.RaiseTerrain(shoreSelection, -Random.Range(125, 160));

        //Shape.MarkRect(vertical, new Color(0, 0, 0.2f));
        //Shape.MarkRect(horizontal, new Color(0, 0, 0.4f));
        //Shape.MarkCircle(Shore, 30, 72, new Color(0f, 0f, 0.3f));
        //Shape.MarkCircle(VCurve, 30, 72, new Color(0f, 0f, 0.3f));
        //Shape.MarkCircle(HCurve, 30, 72, new Color(0f, 0f, 0.3f));
    }

    private static void GenHill() {

        hill = new Circle(new Vector3(), Random.Range(HILL_MIN_RADIUS, HILL_MAX_RADIUS));
        hill.fadeRadius = hill.radius * Random.Range(1.6f, 1.8f);
        hillHeight = Random.Range(90, 130) * 1.9f * ((hill.radius / TerrainGenerator.MAP_SIZE) / 0.23f); //Scale height with size
        hill.centre = SelectHillLocation();
        Selection hillSelection = Selection.MakeSelection(hill);

        const int ITERATIONS = 15;
        const float TAU = 2 * Mathf.PI;
        Circle[] subHills = new Circle[ITERATIONS];
        float[] subHillHeights = new float[ITERATIONS];
        for (int i = 0; i < ITERATIONS; i++) {
            float radians = Random.Range(-0.2f, 0.2f) * TAU * i / (float)ITERATIONS; 
            float distance = Random.Range(0.7f, 1.4f);
            Vector3 hillLocation = new Vector3(hill.X + hill.radius * Mathf.Cos(radians)*distance, 
                                               hill.Y, 
                                               hill.Z + hill.radius * Mathf.Sin(radians)*distance);
            hillLocation = GridUtils.ApplyHeightValue(hillLocation);
            float radius = (hill.fadeRadius - hill.radius) * Random.Range(0.4f, 0.6f) * distance;
            float fadeScalar = Random.Range(1.5f, 3f);
            subHills[i] = new Circle(hillLocation, radius, radius * fadeScalar);
            
            hillSelection += Selection.MakeSelection(subHills[i]) * Random.Range(-0.30f, 0.40f) * distance/1.8f * fadeScalar/2;
            //_Selection.RaiseTerrain(subHills[i], hillHeight * 5000 * Random.Range(0.7f, 0.9f)); 
        }
        Selection.RaiseTerrain(hillSelection, hillHeight);
        
        Vector3 backHillDir = hill.centre + hill.centre.normalized * hill.radius * (3 / 4.0f);
        backhill = new Circle(backHillDir, hill.radius/2f, hill.fadeRadius/1.75f);
        Selection backHillSelection = Selection.MakeSelection(backhill);
        Selection.RaiseTerrain(Selection.MakeSelection(backhill), hillHeight / 4f);
        Selection.FlattenTerrain(Selection.MakeSelection(backhill), 0.3f);
        
        Circle hillFaded = new Circle(hill.centre, hill.radius*0.7f, hill.radius * 1.3f);
        Selection hillFadedSelection = Selection.MakeSelection(hillFaded) - backHillSelection/2;
        Selection.FlattenTerrain(hillFadedSelection, 0.40f);

        //Shape.MarkCircle(hill, 30, 36*2, new Color(0, 0, 1));
        //for (int i = 0; i < iterations; i++)
        //    Shape.MarkCircle(subHills[i], 20, 36 / 3, new Color(0, 0.3f, 1));

    }
    private static Vector3 SelectHillLocation() {
        float avoidCenterRadius = TerrainGenerator.MAP_SIZE / 4.5f;
        //Shape.MarkCircle(new _Circle(new Vector3(), avoidCenterRadius), 30, 36, new Color(0, 0, 0));
        while (true) {
            Vector3 p = GridUtils.GetPointInMapXZ();

            bool shouldContinue =
                //Check if too close to the centre
                p.magnitude < avoidCenterRadius ||
                //Check if too close to the edge
                p.x + hill.radius > TerrainGenerator.MAP_MIN_MAX ||
                p.x - hill.radius < -TerrainGenerator.MAP_MIN_MAX ||
                p.z + hill.radius > TerrainGenerator.MAP_MIN_MAX ||
                p.z - hill.radius < -TerrainGenerator.MAP_MIN_MAX;

            if (shouldContinue)
                continue;

            return GridUtils.ApplyHeightValue(p);
        } 
    }
    public static Vector3 GetPointNotOnHill(float paddingRadius = 0, float edgePadding = 0) {
        Vector3 pos = new Vector3();
        while (true) {
            pos = GridUtils.GetPointInMapXZ();
            Vector3 hillToV = pos - hill.centre;
            if (hillToV.magnitude > hill.fadeRadius + paddingRadius)
            {
                //Cheaper than the commented version below (De Morgan's Law)
                if (!(pos.x <= -TerrainGenerator.MAP_MIN_MAX + edgePadding || pos.x >= TerrainGenerator.MAP_MIN_MAX - edgePadding ||
                      pos.z <= -TerrainGenerator.MAP_MIN_MAX + edgePadding || pos.z >= TerrainGenerator.MAP_MIN_MAX - edgePadding))
                //if (v.x > -TerrainGenerator.MAP_MIN_MAX + edgePadding && v.x < TerrainGenerator.MAP_MIN_MAX - edgePadding &&
                //    v.z > -TerrainGenerator.MAP_MIN_MAX + edgePadding && v.z < TerrainGenerator.MAP_MIN_MAX - edgePadding)
                {
                    return GridUtils.ApplyHeightValue(pos);
                }
            }
        } 
    }
}
