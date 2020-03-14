using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class PrefabPlacer {
    private static int numWallSegs = 0;
    private static Vector3 entranceLoc;
    private static Selection entranceSlopeSelection;
    private static Vector3 castleLoc;
    private static List<Line> pathway;
    private static Selection pathwaySel;
    private static bool isChurchOnTheLeft = false;
    private static bool areHousesOnTheLeft = false;
    private static List<GameObject> houses;
    private static List<GameObject> stalls;
    private static List<GameObject> forestFlora;
    private static List<GameObject> guardTowers;
    private static Selection forestSel;
    private static List<Rectangle> farmlands;
    private static float waterHeight;

    public static void PlaceObjects() {
        waterHeight = GameObject.Find("MyLargeWater").transform.position.y;

        PlaceCityWall();
        PlaceCastle();
        CreateInnerPathway();
        AssignWhereThingsAppear();
        PlaceHousesAndStalls();
        PlaceChurchAndBlacksmith();
        PlaceGuardTowers();

        PlaceForest();
        PlaceFloraEverywhere();
        PlaceFlowersAlongPath();

        CreateFarmland();

        PlaceBoat();
    }

    private static void PlaceCityWall() {
        Circle hill = TerrainFeatures.hill;
        float radians = TerrainFeatures.pathExitRadians;
        Line wallLine = new Line();
        numWallSegs = Random.Range(4, 9); //4 to 8 (Square, Pentagon, Hexagon, Septagon or Octogon)
        float deltaRadians = 2*Mathf.PI / numWallSegs; 
        radians += 0.5f * deltaRadians;

        for (int i = 0; i < numWallSegs; i++, radians += deltaRadians) { //Create the points, raise terrain and blur points are that point
            wallLine.points.Add(hill.centre + 1.1f * hill.radius * new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians)));
        }

        { //This is to level the land between the points
            Vector3 AToB = wallLine.points[0] - wallLine.points[numWallSegs - 1];
            float length = AToB.magnitude;
            Vector3 centre = (wallLine.points[0] + wallLine.points[numWallSegs - 1]) / 2;
            float angle = -Mathf.Rad2Deg * Mathf.Atan2(AToB.z, AToB.x);
            const float WIDTH = 100;
            const float FADE = 100;
            Rectangle r = new Rectangle(centre, new Vector2(length, WIDTH), angle, new Vector2(length + FADE, WIDTH + FADE));

            Selection.BlurTerrain(Selection.MakeSelection(r), 3, 3);
            //Shape.MarkRect(r);
        }

        wallLine.AllignPointsToSurface();
        for (int i = 0; i < wallLine.points.Count; i++) {
            Vector3 wallPoint = wallLine.points[i];
            float height = wallPoint.y;
            if (!GridUtils.IsPointInMapXZ(wallLine.points[i])) { //This ensures that towers that appear out of the map, are at the correct height.
                Vector3 vv = wallPoint;
                vv.x = Mathf.Clamp(wallPoint.x, -TerrainGenerator.MAP_MIN_MAX+50, TerrainGenerator.MAP_MIN_MAX-50);
                vv.z = Mathf.Clamp(wallPoint.z, -TerrainGenerator.MAP_MIN_MAX+50, TerrainGenerator.MAP_MIN_MAX-50);
                vv = GridUtils.ApplyHeightValue(vv);
                wallPoint.y = vv.y;
            }
            wallPoint.y += 10;
            wallLine.points[i] = wallPoint;
        }

        //Shape.MarkLine(wallLine, new Color(128, 0, 200));

        GameObject CityWalls = new GameObject("CityWalls");
        GameObject CornerWalls = new GameObject("CornerWalls");
        GameObject WallSegments = new GameObject("WallSegments");
        CornerWalls.transform.parent = WallSegments.transform.parent = CityWalls.transform;

        GameObject wallCornerPrefab = Resources.Load<GameObject>("Prefabs/WallCornerTextured");
        GameObject wallCornerBasicPrefab = Resources.Load<GameObject>("Prefabs/WallCornerBasic");
        for (int i = 0; i < numWallSegs; i++) {
            GameObject corner = GameObject.Instantiate(wallCornerPrefab, wallLine.points[i], Quaternion.identity);
            corner.transform.eulerAngles = new Vector3(0, 270 - Mathf.Rad2Deg * wallLine.GetOutAngle(i), 0);
            corner.transform.parent = CornerWalls.transform;

            //Make terrain right height:
            Circle c = new Circle(wallLine.points[i], 55, 200);
            Selection.MakeHeight(Selection.MakeSelection(c, true, 2f), corner.transform.position.y, 0.1f);
            //Shape.MarkCircle(c);

            GameObject lowerCorner = GameObject.Instantiate(wallCornerBasicPrefab, wallLine.points[i] - new Vector3(0, 150, 0), corner.transform.rotation);
            lowerCorner.name = "LowerCorner";
            lowerCorner.transform.parent = corner.transform;
        }

        GameObject wallPrefab = Resources.Load<GameObject>("Prefabs/WallLowPoly");
        GameObject wallBasicPrefab = Resources.Load<GameObject>("Prefabs/WallBasic");
        float entranceDegrees = 0;
        const float SPACING = 127; 

        List<GameObject> walls = PlacePrefabsAlongLine(wallLine, wallPrefab, SPACING, ref entranceDegrees, 108);
        for (int i = 0; i < walls.Count; i++) {
            if (walls[i].name != "Enterance") {
                GameObject lowerWall = GameObject.Instantiate(wallBasicPrefab, walls[i].transform.position - new Vector3(0,60,0), walls[i].transform.rotation);
                lowerWall.name = "LowerWall";
                lowerWall.transform.parent = walls[i].transform;
                walls[i].transform.parent = WallSegments.transform;
            } else { //If Enterance
                walls[i].transform.position = (walls[i - 1].transform.position + walls[i + 1].transform.position) / 2;

                //Set height at Enterance with two rects to control fade more 
                Vector3 p = walls[i].transform.position;
                const float fade = 300;
                Rectangle rectInner = new Rectangle(p, new Vector2(1.5f * SPACING, 50), entranceDegrees, new Vector2(fade + 1.5f * SPACING, fade + 100));
                Selection.MakeHeight(Selection.MakeSelection(rectInner), p.y);

                Rectangle rectOuter = new Rectangle(p, new Vector2(1.5f * SPACING, 2.5f * SPACING), entranceDegrees, new Vector2(250 + 1.5f * SPACING, 1400));
                entranceSlopeSelection = Selection.MakeSelection(rectOuter);
                Selection.MakeHeight(Selection.MakeSelection(rectOuter, 3), p.y); 

                //Shape.MarkRect(r, new Color(0.2f, 0.8f, 0.9f));
                //Shape.MarkRect(r2, new Color(0.8f, 0.8f, 0.9f)); 

                entranceLoc = GridUtils.ApplyHeightValue(walls[i].transform.position);
            }
        }


    }
    private static List<GameObject> PlacePrefabsAlongLine(Line l, GameObject prefab, float spacing, ref float entranceDegrees, float startSpacingAtEach = 0)
    {
        List<GameObject> instances = new List<GameObject>();
         
        //Loop through the wall segments (e.g. each side of pentagon if count == 5)
        for (int i = 0; i < numWallSegs; i++) {
            Vector3 current = l.points[i];
            Vector3 next = l.points[(i + 1) % numWallSegs];

            //Set height as average of both
            current.y = next.y = (current.y + next.y) / 2; 
            Vector3 AToB = next - current;
            Vector3 AToBNorm = AToB.normalized;
            float degrees = -Mathf.Rad2Deg * Mathf.Atan2(AToBNorm.z, AToBNorm.x);
            int maxJ = (int)Mathf.Floor(AToB.magnitude / spacing) -1;
            int jvalueEntance = -1;

            //Check if the side for the entrace, and calculate where the midpoint 
            //for the entrace will be in terms of wall mesh pieces
            if (i == numWallSegs - 1) 
                jvalueEntance = (maxJ + (maxJ % 2)) / 2 - 1;  

            //Spawn many wall meshes along one side of the pentagon for example
            for (int j = 0; j < maxJ+1; j++) {
                if (jvalueEntance == -1 || (j != jvalueEntance && j != jvalueEntance + 1)) {
                    GameObject inst = GameObject.Instantiate(prefab, current + AToBNorm * startSpacingAtEach + AToBNorm * j * spacing, Quaternion.identity);
                    inst.transform.eulerAngles = new Vector3(0, degrees, 0);
                    instances.Add(inst);

                    //Last wall, offset by half if possible (As each mesh has two sections, can hide half in another to take up less space)
                    if (j == maxJ) {
                        if (j * spacing + spacing * 1.5f < AToB.magnitude) {
                            GameObject inst2 = GameObject.Instantiate(prefab, current + AToBNorm * startSpacingAtEach + AToBNorm * (j * spacing + spacing / 2), Quaternion.identity);
                            inst2.transform.eulerAngles = new Vector3(0, degrees, 0);
                            inst2.name = "ExtraWallToConnectIt";
                            instances.Add(inst2); 
                        }
                    }
                //If half way along the side with the entrace, spawn the entrance instead of wall
                } else if (j == jvalueEntance) {
                    GameObject entrancePrefab = Resources.Load<GameObject>("Prefabs/EntranceCloser");
                    GameObject inst = GameObject.Instantiate(entrancePrefab, current + AToBNorm * startSpacingAtEach + AToBNorm * j * spacing * 1.5f, Quaternion.identity);
                    inst.transform.eulerAngles = new Vector3(0, degrees, 0);
                    inst.name = "Enterance";
                    entranceDegrees = degrees;
                    instances.Add(inst);
                }  
            }  //END FOR J LOOP
        } //END FOR I LOOP
        return instances;
    }

    private static void PlaceCastle() {
        Vector3 point = GridUtils.ApplyHeightValue(TerrainFeatures.backhill.centre);
        Vector3 castleToEntr = entranceLoc - point;
        point = GridUtils.ApplyHeightValue(point + castleToEntr / (numWallSegs + 2 * (numWallSegs%2==1?1:2)));

        //Place the Castle
        float angle = -Mathf.Rad2Deg * Mathf.Atan2(castleToEntr.z, castleToEntr.x) + 90;
        GameObject castle = (GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Castle"), point - new Vector3(0,5,0), Quaternion.identity));
        castle.transform.eulerAngles = new Vector3(0, angle, 0);
        castleLoc = castle.transform.position;

        //Place the Castle Tower
        Vector3 castToEntNoYNorm = castleToEntr;
        castToEntNoYNorm.y = 0;
        castToEntNoYNorm.Normalize();
        int leftTowerID = Random.Range(1, 4); 
        int rightTowerID = Random.Range(1, 4);
        Vector3 leftDir = Vector3.Cross(Vector3.up, castToEntNoYNorm);
        const float DISTANCE_TO_SIDE = 170;
        Vector3 leftPos = point + leftDir * DISTANCE_TO_SIDE + new Vector3(0,Random.Range(0, 150),0);
        Vector3 rightPos = point - leftDir * DISTANCE_TO_SIDE + new Vector3(0,Random.Range(0, 150),0);
        GameObject leftTower = (GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/CastleT" + leftTowerID), leftPos, Quaternion.identity));
        GameObject rightTower = (GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/CastleT" + rightTowerID), rightPos, Quaternion.identity));
        leftTower.transform.eulerAngles =  new Vector3(0, angle + Random.Range(0,45), 0);
        rightTower.transform.eulerAngles = new Vector3(0, angle - Random.Range(0,45), 0);

        //Place the Flag (if it can)
        GameObject flagPre = Resources.Load<GameObject>("Prefabs/Flag");
        const float DISTANCE_FLAG_ABOVE_TOWER = 290;
        Vector3 flagPos = Vector3.zero;
        if (leftTowerID < 3) 
            flagPos = leftPos + new Vector3(0, DISTANCE_FLAG_ABOVE_TOWER, 0);
        else if (rightTowerID < 3) 
            flagPos = rightPos + new Vector3(0, DISTANCE_FLAG_ABOVE_TOWER, 0); 
        if (leftTowerID < 3 || rightTowerID < 3) {
            GameObject flag = GameObject.Instantiate(flagPre, flagPos, Quaternion.identity);
            flag.transform.localScale *= 0.7f;
        }

        //Spawn the pools of water
        Vector3 leftWCpos = point  + castToEntNoYNorm * 105 + leftDir * 87 + new Vector3(0,10,0);
        Vector3 rightWCpos = point + castToEntNoYNorm * 105 - leftDir * 87 + new Vector3(0,10,0);
        GameObject waterCirclePrefab = Resources.Load<GameObject>("Prefabs/WaterCircle");
        GameObject leftWaterCircle = (GameObject.Instantiate(waterCirclePrefab, leftWCpos, Quaternion.identity));
        GameObject rightWaterCircle = (GameObject.Instantiate(waterCirclePrefab, rightWCpos, Quaternion.identity));

        //Spawn the fountains that pour into the pools
        Vector3 fontOffset = new Vector3(0, 100, 0) - castToEntNoYNorm*10;
        GameObject castleFountainPrefab = Resources.Load<GameObject>("Prefabs/FontCastle");
        GameObject leftWaterFount = (GameObject.Instantiate(castleFountainPrefab, leftWCpos + fontOffset, Quaternion.identity));
        GameObject rightWaterFount = (GameObject.Instantiate(castleFountainPrefab, rightWCpos + fontOffset, Quaternion.identity));
        leftWaterFount.transform.eulerAngles = new Vector3(0,  angle - 90, 300);
        rightWaterFount.transform.eulerAngles = new Vector3(0, angle - 90, 300);

        //Set Parenting
        leftTower.transform.parent = castle.transform;
        rightTower.transform.parent = castle.transform;
        leftWaterCircle.transform.parent = castle.transform;
        rightWaterCircle.transform.parent = castle.transform;
        leftWaterFount.transform.parent = castle.transform;
        rightWaterFount.transform.parent = castle.transform;
    }

    private static void CreateInnerPathway() {
        //Bottom, Middle, Top
        pathway = new List<Line>();
        Line p1 = new Line(); 
        Vector3 entToCas = castleLoc - entranceLoc;
        p1.points.Add(entranceLoc);
        p1.points.Add(entranceLoc + entToCas / 2);
        p1.points.Add(castleLoc);
        pathway.Add(p1);

        //Left, Middle, Right
        Line p2 = new Line(); 
        Vector3 leftDir = -Vector3.Cross(Vector3.up, entToCas.normalized);
        Vector3 rightDir = -leftDir;
        float leftAng = Mathf.Deg2Rad * Random.Range(-30.0f, 30.0f);
        float rightAng = Mathf.Deg2Rad * Random.Range(-30.0f, 30.0f);
        Vector3 newLeftDir = GridUtils.RotateVectorByAngleXZ(leftDir, leftAng);
        Vector3 newRightDir = GridUtils.RotateVectorByAngleXZ(rightDir, rightAng);

        float tweakAdjustments = 1.35f - 1 / 3f + (numWallSegs == 4 ? -.2f : 0); //Hacky fine-tuning/magic numbers :P
        p2.points.Add(p1.points[1] + newLeftDir  * Random.Range(325, 375) * (Mathf.Abs(leftAng)/30  + tweakAdjustments) * TerrainFeatures.HillSizeCoefficient());
        p2.points.Add(p1.points[1]);
        p2.points.Add(p1.points[1] + newRightDir * Random.Range(325, 375) * (Mathf.Abs(rightAng)/30 + tweakAdjustments) * TerrainFeatures.HillSizeCoefficient());
        pathway.Add(p2);

        //LeftMinor, Middle, RightMinor
        Line p3 = new Line(); 
        float leftMinorAngle  = Mathf.Deg2Rad * (leftAng  < 0 ? 1 : -1) * Random.Range(45.0f, 57.5f);
        float rightMinorAngle = Mathf.Deg2Rad * (rightAng < 0 ? 1 : -1) * Random.Range(45.0f, 57.5f);
        p3.points.Add(p1.points[1] + GridUtils.RotateVectorByAngleXZ(leftDir, leftMinorAngle) * Random.Range(300, 400) * TerrainFeatures.HillSizeCoefficient());
        p3.points.Add(p1.points[1]);
        p3.points.Add(p1.points[1] + GridUtils.RotateVectorByAngleXZ(rightDir, rightMinorAngle) * Random.Range(300, 400) * TerrainFeatures.HillSizeCoefficient());
        pathway.Add(p3);
        
        //Left(0) and Right(1) Networks
        for (int i = 0; i < 2; i++) {
            Line pNetwork = new Line(); 
            float sumDegrees = i==0?0:360;
            Vector3 newDir = -(i==0?newLeftDir:newRightDir);
            const float MIN_DEGREES = 55;
            const float MAX_DEGREES = 100;
            while ((i == 0 && sumDegrees < 270 - (numWallSegs==4?30:0)) || (i==1 && sumDegrees > 90 + (numWallSegs==4?30:0))) {
                float degrees = (i==0?1:-1) * Random.Range(MIN_DEGREES, MAX_DEGREES);
                sumDegrees += degrees;
                newDir = GridUtils.RotateVectorByAngleXZ(newDir, Mathf.Deg2Rad * degrees);
                float distFrom180 = Mathf.Abs(sumDegrees - 180);
                pNetwork.points.Add(p2.points[i*2] + newDir * Random.Range(270, 360) * (distFrom180/(180*1.40f) + (1.0f-1/1.40f))); //To the end
                pNetwork.points.Add(p2.points[i*2]); //And back
            }
            pathway.Add(pNetwork);
        }

        pathwaySel = new Selection();
        int pathCounter = 0;
        foreach (Line l in pathway) {
            float thickness = 0;
            switch (pathCounter++) {
                case 0:         thickness = 60; break;
                case 1:         thickness = 40; break;
                case 2:         thickness = 30; break;
                case 3: case 4: thickness = 20; break;
            }
            Rectangle[] rects = Rectangle.MakeRectPath(l, thickness, 2, 2);
            for(int i = 0; i < rects.Length; i++)
                pathwaySel += Selection.MakeSelection(rects[i], 1);
        }
        Selection.RaiseTerrain(pathwaySel, 12);

        //foreach (_Line l in pathway)
        //    Shape.MarkLine(l, new Color(100, 50, 0));
    }
    private static void AssignWhereThingsAppear() {
        areHousesOnTheLeft = Random.Range(0, 2) == 0; //Either left or right branch cluster
        isChurchOnTheLeft  = Random.Range(0, 2) == 0; //Either left or right lone branch

        float leftFurthestSubBrach = 0;
        float rightFurthestSubBrach = 0;
        Vector3 leftCentre = pathway[1].points[0];
        Vector3 rightCentre = pathway[1].points[2];

        for (int i = 0; i < pathway[3].points.Count; i += 2) //Every odd one is only the center, so increment by 2 
            leftFurthestSubBrach = Mathf.Max((pathway[3].points[i] - leftCentre).magnitude, leftFurthestSubBrach);

        for (int i = 0; i < pathway[4].points.Count; i += 2) //Every odd one is only the center, so increment by 2 
            rightFurthestSubBrach = Mathf.Max((pathway[4].points[i] - rightCentre).magnitude, rightFurthestSubBrach);
    }


    private static void PlaceHousesAndStalls() {
        GameObject[] housePrefabs = new GameObject[4];
        for (int i = 0; i < 4; i++)
            housePrefabs[i] = Resources.Load<GameObject>("Prefabs/House" + (i + 1));
        GameObject[] stallPrefabs = new GameObject[3];
        for (int i = 0; i < 3; i++)
            stallPrefabs[i] = Resources.Load<GameObject>("Prefabs/MarketStall" + (i + 1));

        houses = new List<GameObject>();
        stalls = new List<GameObject>();
        PlaceStrucutreAroundNetwork(true, housePrefabs, houses, 0.45f, 0, 100, 70);
        PlaceStrucutreAroundNetwork(false, stallPrefabs, stalls, 0.25f, 90, 50, 40);

        GameObject houseContainer = new GameObject("All Houses");
        GameObject stallsContainer = new GameObject("All Stalls");
        foreach (GameObject house in houses)
            house.transform.parent = houseContainer.transform;
        foreach (GameObject stall in stalls)
            stall.transform.parent = stallsContainer.transform;
    }
    private static void PlaceStrucutreAroundNetwork(bool isHouses, GameObject[] structures, List<GameObject> struList, float scale, float rotOffset, float radiusGap, float custRectPathWidth = 50) { 
        Line network = pathway[isHouses == areHousesOnTheLeft ? 3 : 4];
        Rectangle[] pathRects = Rectangle.MakeRectPath(network, custRectPathWidth);
        Rectangle[] placementRects = Rectangle.MakeRectPath(network, 120);
        Rectangle[] parentPathway = Rectangle.MakeRectPath(pathway[1], 50);
        Rectangle[] masterPathway = Rectangle.MakeRectPath(pathway[0], 50);
        const float RADIUS_GAP_FROM_CENTRE = 120;

        for (int i = 0; i < network.points.Count; i += 2) {
            Selection placementSel = Selection.MakeSelection(placementRects[i]); //Expanded selection around path marked rect
            for (int j = 0; j < pathRects.Length; j += 2)
                placementSel = placementSel - Selection.MakeSelection(pathRects[j]); //Except for the pathline
            placementSel -= Selection.MakeSelection(parentPathway[areHousesOnTheLeft ? 0 : 1]); //Also Except for the larger pathway
            //Also Except for the main pathway
            placementSel -= Selection.MakeSelection(masterPathway[0]);
            placementSel -= Selection.MakeSelection(masterPathway[1]); 

            for (int j = 0; j < placementSel.verts.Count; j++) { //Loops through all vertices along the path boundry
                if (placementSel.intensities[j] == 1) {
                    bool canPlace = true;
                    Vector3 vertPos = GridUtils.VertexToPoint(placementSel.verts[j].x, placementSel.verts[j].z);
                    if ((network.points[1] - vertPos).magnitude < RADIUS_GAP_FROM_CENTRE) {
                        canPlace = false;
                    } else {
                        for (int k = 0; k < struList.Count && canPlace; k++) //Loop through all current houses/stalls to check there's space nearby
                            if ((struList[k].transform.position - vertPos).magnitude < radiusGap)
                                canPlace = false;
                    }
                    if (canPlace) {
                        Vector3 pos = GridUtils.ApplyHeightValue(vertPos);
                        Circle flatten = new Circle(pos, 30, 50);
                        Selection.MakeHeight(Selection.MakeSelection(flatten), pos.y, 0.15f);

                        GameObject structure = GameObject.Instantiate(structures[Random.Range(0, structures.Length)], pos, Quaternion.identity);
                        structure.transform.localScale *= scale;
                        //Rotate:
                        Vector3 perpendicular = Vector3.Cross(Vector3.up, network.points[1] - network.points[i]).normalized;
                        if ((network.points[i] - vertPos).magnitude > (network.points[i] + perpendicular - vertPos).magnitude)
                            structure.transform.eulerAngles = new Vector3(0, -Mathf.Rad2Deg * Mathf.Atan2(perpendicular.z, perpendicular.x) + rotOffset + 180, 0);
                        else
                            structure.transform.eulerAngles = new Vector3(0, -Mathf.Rad2Deg * Mathf.Atan2(perpendicular.z, perpendicular.x) + rotOffset, 0);

                        struList.Add(structure);
                    }
                }
            }
        }
    }

    private static void PlaceChurchAndBlacksmith() {
        GameObject[] prefabs = new GameObject[2] {
            Resources.Load<GameObject>("Prefabs/Church"),
            Resources.Load<GameObject>("Prefabs/Blacksmith")
        };

        //i == 0 : Church
        //i == 1 : Blacksmith
        for (int i = 0; i < 2; i++) {
            Vector3 end = pathway[2].points[(isChurchOnTheLeft == (i == 0)) ? 0 : 2];
            Vector3 midToEnd = end - pathway[2].points[1];

            GameObject building = GameObject.Instantiate(prefabs[i], GridUtils.ApplyHeightValue(end + midToEnd.normalized * 100), Quaternion.identity);
            building.transform.localScale *= 0.7f;
            float angle = -Mathf.Rad2Deg * Mathf.Atan2(midToEnd.z, midToEnd.x) - 90;
            building.transform.eulerAngles = new Vector3(0, angle, 0);
        }
    }
    private static void PlaceGuardTowers() {
        guardTowers = new List<GameObject>();
        GameObject guardTowerPrefab = Resources.Load<GameObject>("Prefabs/GuardTower");
        Vector3 end = pathway[0].points[0];
        Vector3 midToEnd = end - pathway[0].points[1];
        Vector3 perpendicular = Vector3.Cross(Vector3.up, midToEnd.normalized);
        float degrees = -Mathf.Rad2Deg * Mathf.Atan2(midToEnd.z, midToEnd.x);

        //Spawn Guard Towers (2 rows, left and right of each)
        for (int i = 0; i < 4; i++) {
            Vector3 alongPath = end + midToEnd.normalized * (i<2 ? Random.Range(80, 200) : Random.Range(250, 450));
            Vector3 sideFromPath = perpendicular * Random.Range(130, 250) * (i%2==0 ? 1 : -1);
            Vector3 pos = GridUtils.ApplyHeightValue(alongPath + sideFromPath);
            GameObject guardTower = GameObject.Instantiate(guardTowerPrefab, pos, Quaternion.identity);
            guardTower.transform.localScale *= 0.4f;
            guardTower.transform.eulerAngles = new Vector3(0, degrees + (i % 2 == 0 ? 0 : 180), 0);
            guardTowers.Add(guardTower);
        }
    }

    private static void PlaceForest() {
        forestFlora = new List<GameObject>();
        Circle forest = TerrainFeatures.forestCircle;
        const int NUM_FLORA_TYPES = 7;
        Selection selection = Selection.MakeSelection(forest, true, 2f); //The circle forest selection 

        Rectangle extForest;
        {
            Vector3 forestToHill = TerrainFeatures.hill.centre - forest.centre;
            float rectAngle = -Mathf.Rad2Deg * Mathf.Atan2(forestToHill.z, forestToHill.x); 
            extForest = new Rectangle(forest.centre, new Vector2(700, 850), rectAngle, new Vector2(Random.Range(850, 1100), Random.Range(1400, 1800)));
            selection += Selection.MakeSelection(extForest, 4f);
            //Shape.MarkRect(extFor);
        }
        forestSel = selection;

        GameObject allOfForest = new GameObject("All Of Forest");
        GameObject[] thingsToSpawn = new GameObject[NUM_FLORA_TYPES] { 
            Resources.Load<GameObject>("Flora/Tree0"),
            Resources.Load<GameObject>("Flora/Tree1"),
            Resources.Load<GameObject>("Flora/Tree2"),
            Resources.Load<GameObject>("Flora/Tree3"),
            Resources.Load<GameObject>("Flora/Tree4"),
            Resources.Load<GameObject>("Flora/Fern"),
            Resources.Load<GameObject>("Flora/GrassLess")
        };
        const float TREE_GAP = 90;
        float[] distGapFromItem = new float[NUM_FLORA_TYPES] { TREE_GAP, TREE_GAP, TREE_GAP, TREE_GAP, TREE_GAP, 80, 70 };
        for (int i = 0; i < selection.verts.Count; i++) { 
            if (selection.intensities[i] < 1 && Random.Range(0.0f, 1.0f) > selection.intensities[i]) //Probability of spawning within the fade boundary
                continue; //If unlucky, no spawn, next!
            Vector3 p = GridUtils.VertexToPoint(selection.verts[i].x, selection.verts[i].z);
            if (p.y < waterHeight) //Stop it from spawning in the water.
                continue;
            if ((TerrainFeatures.hill.centre - p).magnitude < TerrainFeatures.hill.radius * 1.15f) //If on the hill (in the city)
                continue;
            if ((TerrainFeatures.fieldCircle.centre - p).magnitude < TerrainFeatures.fieldCircle.radius * 1.05f) //If within the farmland
                continue;

            bool shouldSpawn = true;
            for (int j = 0; j < forestFlora.Count && shouldSpawn; j++) { //Check it's not too close to the neighbouring flora
                int otherId = int.Parse(forestFlora[j].name.Substring(3));
                if ((forestFlora[j].transform.position - p).magnitude < distGapFromItem[otherId])
                    shouldSpawn = false;
            }

            if (shouldSpawn) {
                Vector3 displace = new Vector3(Random.Range(-10, 10), -5, Random.Range(-10, 10));
                int id = Random.Range(0, NUM_FLORA_TYPES);
                GameObject flora = GameObject.Instantiate(thingsToSpawn[id], p + displace, Quaternion.identity);
                flora.transform.eulerAngles = new Vector3(0, Random.Range(0.0f, 360.0f), 0);
                if (id < 5) //Trees
                    flora.transform.localScale *= Random.Range(0.4f, 0.9f);
                else if (id == 5) //Fern
                    flora.transform.localScale *= Random.Range(0.4f, 0.8f);
                else //Grass
                    flora.transform.localScale *= Random.Range(1.3f, 1.7f);

                flora.name = "fID" + id;
                flora.transform.parent = allOfForest.transform;
                forestFlora.Add(flora);
            }
        }

    }
    private static void PlaceFloraEverywhere() {
        Rectangle all = new Rectangle(Vector3.zero, new Vector2(TerrainGenerator.MAP_SIZE, TerrainGenerator.MAP_SIZE), 0);
        Selection allSelection = Selection.MakeSelection(all);
        Circle hillExclusionArea = new Circle(TerrainFeatures.hill.centre, TerrainFeatures.hill.radius * 1.15f);
        Circle fieldExclusionArea = new Circle(TerrainFeatures.fieldCircle.centre, TerrainFeatures.fieldCircle.radius * 1.10f);

        //No Go Areas
        allSelection -= Selection.MakeSelection(fieldExclusionArea); //Not the farmland
        allSelection -= forestSel;                         //Not the forest
        allSelection -= Selection.MakeSelection(hillExclusionArea);  //Not the hill
        for (int i = 0; i < TerrainFeatures.shoreShapes.Length; i++) { //Not the Shore
            const int SPACING = 160;
            if (i < 3) { //Circle
                Circle c = (Circle)TerrainFeatures.shoreShapes[i];
                c.radius += SPACING;
                allSelection -= Selection.MakeSelection(c);
            } else { //Rectangle
                Rectangle r = (Rectangle)TerrainFeatures.shoreShapes[i];
                r.size += new Vector2(SPACING, SPACING);
                allSelection -= Selection.MakeSelection(r);
            }
        }

        //Remove Entrance slope and guard towers from selection
        for (int i = 0; i < entranceSlopeSelection.intensities.Count; i++) //Not on the entrance slope
            entranceSlopeSelection.intensities[i] = 1;
        allSelection -= entranceSlopeSelection;
        for (int i = 0; i < guardTowers.Count; i++) //Not on the guard towers
            allSelection -= Selection.MakeSelection(new Circle(guardTowers[i].transform.position, 45));


        GameObject allGrassAndTreesInField = new GameObject("All Misc grass and trees");
        GameObject shortGrassPrefabs = Resources.Load<GameObject>("Flora/GrassLess");
        GameObject[] treePrefabs = new GameObject[] {
            Resources.Load<GameObject>("Flora/Tree0"),
            Resources.Load<GameObject>("Flora/Tree1"),
            Resources.Load<GameObject>("Flora/Tree2"),
            Resources.Load<GameObject>("Flora/Tree3"),
            Resources.Load<GameObject>("Flora/Tree4"),
        };
        for (int i = 0; i < allSelection.verts.Count; i++)
        {
            int rand = Random.Range(0, 2500);
            //Spawn grass or tree (Probability: 22/2500 = 0.88%)
            if (rand <= 22)
            {
                Vector3 pos = GridUtils.VertexToPoint(allSelection.verts[i].x, allSelection.verts[i].z) - new Vector3(0, 8, 0);
                GameObject g;
                if (rand <= 19)
                { //Spawn grass (Probability: 19/2500 = 0.76%)
                    g = GameObject.Instantiate(shortGrassPrefabs, pos, Quaternion.identity);
                    g.transform.localScale *= Random.Range(1.4f, 1.9f);
                }
                else
                { //Spawn tree (Probability: 3/2500 = 0.12%)
                    g = GameObject.Instantiate(treePrefabs[Random.Range(0, 5)], pos, Quaternion.identity);
                    g.transform.localScale *= Random.Range(0.4f, 0.9f);
                }
                g.transform.parent = allGrassAndTreesInField.transform;
            }
        }
    }
    private static void PlaceFlowersAlongPath() {
        GameObject flowerPrefab = Resources.Load<GameObject>("Flora/RedFlower");
        const float INNER_WIDTH = 60;
        const float OUTER_WIDTH = 100;
        Rectangle[] masterPathwayInner = Rectangle.MakeRectPath(pathway[0], INNER_WIDTH);
        Rectangle[] masterPathwayOuter = Rectangle.MakeRectPath(pathway[0], OUTER_WIDTH);
        Selection inner = Selection.MakeSelection(masterPathwayInner[0]) + Selection.MakeSelection(masterPathwayInner[1]);
        Selection outer = Selection.MakeSelection(masterPathwayOuter[0]) + Selection.MakeSelection(masterPathwayOuter[1]);
        outer -= inner;
        GameObject allFlowersContainer = new GameObject("All City Path Flowers");

        for (int i = 0; i < outer.verts.Count; i++) {
            //1 in 9 change to spawn a flower given that it is far enough away from the castle and entrance
            if (Random.Range(0, 9) == 0) { 
                Vector3 pos = GridUtils.VertexToPoint(outer.verts[i].x, outer.verts[i].z);
                if ((castleLoc - pos).magnitude > 100 && (entranceLoc - pos).magnitude > 100) {
                    GameObject flowerInstance = GameObject.Instantiate(flowerPrefab, pos, Quaternion.identity);
                    flowerInstance.transform.localScale *= 0.2f;
                    flowerInstance.transform.parent = allFlowersContainer.transform;
                } 
            }
        }
    }

    private static void CreateFarmland() {
        farmlands = new List<Rectangle>();
        Circle farmCircle = new Circle(TerrainFeatures.fieldCircle.centre, TerrainFeatures.fieldCircle.radius * 0.80f); //A copy without the fade, and slightly smaller
        Selection selection = Selection.MakeSelection(farmCircle);
        int numVert = selection.verts.Count;

        GameObject soilPrefab = Resources.Load<GameObject>("FarmStuff/TilledSoil");
        GameObject fencePostPrefab = Resources.Load<GameObject>("FarmStuff/FencePostP");
        GameObject fenceBarPrefab = Resources.Load<GameObject>("FarmStuff/FenceBarsP");

        const int MAX_ATTEMPTS = 20;
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
            int verts = Random.Range(0, numVert);
            Vector3 pos = GridUtils.VertexToPoint(selection.verts[verts].x, selection.verts[verts].z);
            float angle = Random.Range(0.0f, 360.0f);
            bool okToPlace = true;
            for (int i = 0; i < farmlands.Count && okToPlace; i++) {
                float maxRadius = Mathf.Sqrt((farmlands[i].size.x * farmlands[i].size.x) + 
                                             (farmlands[i].size.y * farmlands[i].size.y));
                if ((farmlands[i].centre - pos).magnitude < maxRadius)
                    okToPlace = false;
            }
            for (int i = 1; i < 3 && okToPlace; i++) { //Check if near to shore
                Circle c = (Circle)TerrainFeatures.shoreShapes[i];
                if ((c.centre - pos).magnitude < c.radius * 1.3f)
                    okToPlace = false;
            }

            if (okToPlace) {
                Rectangle rect = new Rectangle(pos, new Vector2(Random.Range(120, 280), Random.Range(120, 280)), angle);
                rect.fadeSize = rect.size * 1.2f;
                //Shape.MarkRect(r);
                Selection.FlattenTerrain(Selection.MakeSelection(rect), 0f);
                pos = GridUtils.ApplyHeightValue(pos);

                Vector3 POST_HEIGHT_OFFSET = new Vector3(0, 7.5f, 0);
                Vector3 BAR_HEIGHT_OFFSET = new Vector3(0,-2.5f,0);
                Vector3[] midPointOffsets = new Vector3[] {
                    new Vector3(rect.size.x, 0, 0) / 2,
                    new Vector3(-rect.size.x, 0, 0) / 2,
                    new Vector3(0, 0, rect.size.y) / 2,
                    new Vector3(0, 0, -rect.size.y) / 2,
                };
                Vector3[] cornerOffsets = new Vector3[] {
                    new Vector3(rect.size.x, 0, rect.size.y)   / 2,
                    new Vector3(-rect.size.x, 0, -rect.size.y) / 2,
                    new Vector3(-rect.size.x, 0, rect.size.y)  / 2,
                    new Vector3(rect.size.x, 0, -rect.size.y)  / 2,
                };

                GameObject soilInstance = GameObject.Instantiate(soilPrefab, pos - POST_HEIGHT_OFFSET, Quaternion.identity);
                soilInstance.transform.eulerAngles = new Vector3(0, angle, 0);
                soilInstance.transform.localScale = new Vector3(rect.size.x, 20, rect.size.y) * 0.9f;

                float negativeAngleRads = Mathf.Deg2Rad * -angle;

                //Spawn Fence Posts
                for (int i = 0; i < 4; i++) {
                    GameObject.Instantiate(fencePostPrefab, 
                        pos + GridUtils.RotateVectorByAngleXZ(cornerOffsets[i] + POST_HEIGHT_OFFSET, negativeAngleRads), 
                        soilInstance.transform.localRotation);
                }
          
                //Spawn Fence Bars
                for (int i = 0; i < 4; i++) {
                    //Spawn horizontal bars
                    GameObject bar = GameObject.Instantiate(fenceBarPrefab, 
                        pos + GridUtils.RotateVectorByAngleXZ(midPointOffsets[i] + BAR_HEIGHT_OFFSET, negativeAngleRads), 
                        soilInstance.transform.localRotation);

                    //Rotate 2 by 90deg
                    if (i < 2)
                        bar.transform.eulerAngles = new Vector3(0, angle + 90, 0);

                    //Scale the bar according to the farm size
                    Vector3 barScale = bar.transform.localScale;
                    barScale.x *= (i < 2 ? rect.size.y : rect.size.x) / 50;
                    bar.transform.localScale = barScale;
                }

                farmlands.Add(rect);
            } 
        }
    }

    private static void PlaceBoat() {
        GameObject boatPrefab = Resources.Load<GameObject>("Prefabs/Boat");
        int shoreShapeID = Random.Range(0, 5); //Find a piece(shape) of water to spawn on
        Vector3 p = TerrainFeatures.shoreShapes[shoreShapeID].centre;
        p *= 0.90f; //Bring it a bit closer to the map centre
        p.y = waterHeight; //On the water

        GameObject boatInstance = GameObject.Instantiate(boatPrefab, p, Quaternion.identity);
        boatInstance.transform.eulerAngles = new Vector3(0, Random.Range(0, 360), 0);
        boatInstance.transform.localScale *= 0.5f;
        boatInstance.AddComponent<BoatBob>();
    }
}
