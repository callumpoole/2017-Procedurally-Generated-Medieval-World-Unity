using UnityEngine;
using System.Collections;

public static class PerlinNoise {
    static Vector2[][] gradientPoints;
    public static int gridSize;

    public static void GenerateNoise(int gridSize) {
        PerlinNoise.gridSize = gridSize;
        gradientPoints = new Vector2[PerlinNoise.gridSize+1][];
        for (int i = 0; i < PerlinNoise.gridSize+1; i++)
            gradientPoints[i] = new Vector2[PerlinNoise.gridSize+1];

        for(int z = 0; z < PerlinNoise.gridSize+1; z++)
            for(int x = 0; x < PerlinNoise.gridSize+1; x++) {
                float angle = Random.Range(0.0f, 360.0f);
                float mag = Random.Range(0.0f, 1.0f);
                gradientPoints[z][x].x = Mathf.Cos(angle) * mag;
                gradientPoints[z][x].y = Mathf.Sin(angle) * mag;
            }
    }

    /// <param name="x">Between 0 and 1 </param>
    /// <param name="z">Between 0 and 1 </param>
    public static float Perlin(float x, float z) {
        x *= gridSize;
        z *= gridSize;

        //Making sure it doesn't go over to prevent an out of bounds exception
        if (x >= gridSize) x = gridSize - 0.1f; 
        if (z >= gridSize) z = gridSize - 0.1f;

        int xc = (int)Mathf.Floor(x);   //The X Coord of the top left bounding box
        int zc = (int)Mathf.Floor(z);   //The Z Coord of the top left bounding box
        Vector2 gtl = gradientPoints[zc][xc];       //The gradient for the top left     bounding box corner
        Vector2 gtr = gradientPoints[zc][xc+1];     //The gradient for the top right    bounding box corner
        Vector2 gbl = gradientPoints[zc+1][xc];     //The gradient for the bottom left  bounding box corner
        Vector2 gbr = gradientPoints[zc+1][xc+1];   //The gradient for the bottom right bounding box corner

        Vector2 ptl = new Vector2(x, z) - new Vector2(xc, zc);      //Displacement for the top left     to the point
        Vector2 ptr = new Vector2(x, z) - new Vector2(xc+1, zc);    //Displacement for the top right    to the point
        Vector2 pbl = new Vector2(x, z) - new Vector2(xc, zc+1);    //Displacement for the bottom left  to the point
        Vector2 pbr = new Vector2(x, z) - new Vector2(xc+1, zc+1);  //Displacement for the bottom right to the point

        float dtl = Vector2.Dot(gtl, ptl); 
        float dtr = Vector2.Dot(gtr, ptr); 
        float dbl = Vector2.Dot(gbl, pbl); 
        float dbr = Vector2.Dot(gbr, pbr);

        ptl.x = (float)fade(ptl.x);
        ptl.y = (float)fade(ptl.y);

        float at = Mathf.Lerp(dtl, dtr, ptl.x);
        float ab = Mathf.Lerp(dbl, dbr, ptl.x);

        return Mathf.Lerp(at, ab, ptl.y);
    }


    //Ref: Function from: http://flafla2.github.io/2014/08/09/perlinnoise.html
    public static double fade(double t) {
        // Fade function as defined by Ken Perlin.  This eases coordinate values
        // so that they will ease towards integral values.  This ends up smoothing
        // the final output.
        return t * t * t * (t * (t * 6 - 15) + 10);         // 6t^5 - 15t^4 + 10t^3
    }
}
