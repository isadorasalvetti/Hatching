using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using UnityEngine;
using Color = UnityEngine.Color;
using Image = SixLabors.ImageSharp.Image;

public class ProcessHatching
{
    private Texture2D[] _textures;
    private float _dSeparation;
    private float _level;
    private float _dTest;
    private int _gridSize = 50;

    private List<List<Vector2>> Lines = new List<List<Vector2>>(); //Stores line points, in order from start to end.
    private List<Tuple<List<Vector2>, List<Vector2>>> NextLineCandidates = new List<Tuple<List<Vector2>, List<Vector2>>>(); //Candidates to seed next line.
    private List<Vector2>[,] PointGrid; //Stores points in a grid. Facilitate distance calculations
    
    private List<Vector2> DebugPoints = new List<Vector2>();
    private List<Vector2> DebugPointsLine = new List<Vector2>();
    
    public ProcessHatching(Texture2D[] texture, float dSeparation = 0.01f, float dTest = 0.8f,
        int gridSize = 0, float level = 1.0f){
        _level = level; // Used to signal which area should be hatched.
        _textures = texture;
        _dSeparation = (int)(dSeparation * _textures[0].width);
        _dSeparation = Mathf.Max(5, _dSeparation);
        _dTest = (int)(dTest * _dSeparation);
        _dTest = Mathf.Max(3, _dTest);
        if (gridSize > 0) _gridSize = gridSize;

        PointGrid = new List<Vector2>[(int)(_textures[0].width/(_dSeparation))+1, (int)(_textures[0].height/(_dSeparation))+1];
        int testX, testY; getGridCoords(_textures[0].width, _textures[0].height, out testX, out testY);
        
    }
    
    bool isInvalidColor(Vector2 newPoint, int lookingIndex){
        Texture2D texture = _textures[lookingIndex];
        Color col = readTexturePixel(texture, (int)newPoint.x, (int)newPoint.y);
        return col.a > _level || col.b > 0.9f;
        return Mathf.Abs(col.r) < Single.Epsilon && Mathf.Abs(col.g) < Single.Epsilon;
    }

    Color readTexturePixel(Texture2D texture, int u, int v)
    {
        int x = u;
        int y = texture.height - v;
        return texture.GetPixel(x, y);
    }

    bool isPositionOutTexture(Vector2 newPoint) {
        return newPoint.x < 0 || newPoint.y < 0 || newPoint.x > _textures[0].width || newPoint.y > _textures[0].height;
    }
    
    void addPointToGrid(Vector2 point){
        int gridX, gridY; getGridCoords(point, out gridX, out gridY);
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector2>();
        PointGrid[gridX, gridY].Add(point);
    }

    void getGridCoords(float x, float y, out int gridX, out int gridY){
        gridX = (int) (x / (_dSeparation));
        gridY = (int) (y / (_dSeparation));
        //Debug.Log(x.ToString() + ", " + y.ToString() + ", " + gridX.ToString() + ", " + gridY.ToString());
    }
    
    void getGridCoords(Vector2 point, out int gridX, out int gridY){
        gridX = (int) (point.x / (_dSeparation));
        gridY = (int) (point.y / (_dSeparation));
        //Debug.Log(point.x.ToString() + ", " + point.y.ToString() + ", " + gridX.ToString() + ", " + gridY.ToString());
    }

    List<Vector2> getSurroundingPoints(int gridX, int gridY)
    {
        List<Vector2> combinedList = new List<Vector2>();
        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++)
        {
            int dimX = PointGrid.GetLength(0); int dimY = PointGrid.GetLength(1);
            int Gx = gridX + i; int Gy = gridY + j;
            if (Gx < 0 || Gy < 0 || Gx >= dimX || Gy >= dimY) continue;
            if (PointGrid[Gx, Gy] != null) combinedList.AddRange(PointGrid[Gx, Gy]);
        }
        return combinedList;
    }
    
    public void StartRandomSeed() {
        // Looks for a pixel with valid curvature in image.

        Debug.Log("Looking for seeds");
        for (int u = 0; u < _textures[0].width; u += _gridSize)
        for (int v = 0; v < _textures[0].height; v += _gridSize)
        {
            Vector2 testPoint = new Vector2(u, v);
            if (!isInvalidColor(new Vector2(u, v), 0))
            {
                int gridX, gridY; getGridCoords(u, v, out gridX, out gridY);
                
                if (CheckSurroundingPoints(testPoint)) {
                    Vector2 seedPoint = new Vector2(u, v);
                    AddLine(seedPoint, SamplePixelColorFromTexture(_textures[0], seedPoint));
                    GetNextSeed();
                    return;
                }
                
            }
        }
    }

    void GetNextSeed()
    {
        if (Lines.Count > 1500) throw new Exception("Max number of lines reached");

        while (NextLineCandidates.Count > 0)
        {
            List<Vector2> line = NextLineCandidates[0].Item1;
            List<Vector2> directions = NextLineCandidates[0].Item2;
            int[] mults = {1, -1};
            foreach (int mult in mults)
            {
                for(int i = 0; i < line.Count; i++) {
                    Vector2 point = line[i];
                    Vector2 previousDirection = directions[i];
                    Vector2 testPoint = point + mult * _dSeparation * Math2.rotateVec2(previousDirection, 90).normalized;
                    if (testPoint.x < 0 || testPoint.y < 0 || testPoint.x > _textures[0].width ||
                        testPoint.y > _textures[0].height) continue;
                    
                    if (CheckSurroundingPoints(testPoint))
                        if (!isInvalidColor(testPoint, 0))
                        {
                            if (!(previousDirection == Vector2.zero)) {
                                Vector2 initialDirection = FindBestDirection(previousDirection, testPoint, 0.9f, 1.1f);
                                if (!(initialDirection == Vector2.zero)){
                                    AddLine(testPoint, initialDirection);
                                }
                            }
                        }
                }
            }
            NextLineCandidates.RemoveAt(0);
        }
        
    }

    void AddLine(Vector2 seed, Vector2 initialDirection)
    {
        //Creates new line starting at seed.
        List<Vector2> line = new List<Vector2>();
        List<Vector2> lineDirections = new List<Vector2>();

        line.Add(seed);
        lineDirections.Add(initialDirection);
        foreach (int mult in new int[2]{1, -1}) {
            Vector2 direction;
            Vector2 oldLineDirection;
            if (mult == -1) direction = FindBestDirection(initialDirection, seed, -1.1f, -0.9f);
            else {
                direction = initialDirection;
            }
            oldLineDirection = direction;
            Vector2 newPoint = seed;
            
            for (int i = 0; i < 1000; i++)
            {
                newPoint = GetNextPoint(newPoint, ref direction, oldLineDirection);
                if (newPoint == Vector2.zero || direction == Vector2.zero) break;
                if (mult > 0) {
                    line.Add(newPoint);
                    lineDirections.Add(direction);
                }
                else {
                    line.Insert(0, newPoint);
                    lineDirections.Insert(0, direction);
                }
                
                oldLineDirection = direction;
                if (line.Count > 100) break;
            }
        }

        if (line.Count > 5) {
            foreach (var point in line) {
                addPointToGrid(point);
            }
            Lines.Add(line);
            NextLineCandidates.Add(new Tuple<List<Vector2>, List<Vector2>>(line, lineDirections));
        }

    }

    Vector2 SamplePixelColorFromTexture(Texture2D texture, Vector2 point){
        Color pixelColor =  readTexturePixel(texture, (int)point.x, (int)point.y);
        Vector2 new_direction = rg(pixelColor);
        new_direction = (new_direction * 2 - Vector2.one).normalized;
        return new_direction;
    }

    Vector2 GetNextPoint(Vector2 previousPoint, ref Vector2 direction, Vector2 previousDirection, bool repeat=true)
    {
        //Gets next valid point for a line
        Vector2 newPoint = previousPoint + _dSeparation * direction;

        //Reject points outside of image and finds adequate point in between.
        if (isPositionOutTexture(newPoint)) newPoint = GetIntermediaryPoint(previousPoint, newPoint);
        if (isInvalidColor(newPoint, 0)) newPoint = GetIntermediaryPoint(previousPoint, newPoint);
        if (!CheckSurroundingPoints(newPoint)) {
            return Vector2.zero;
        }
        Vector2 new_direction = FindBestDirection(direction, newPoint, 0.95f, 1.1f);
        
        if(newPoint == Vector2.zero || new_direction == Vector2.zero) {return Vector2.zero;}

        direction = new_direction;
        return newPoint;
    }

    bool CheckSurroundingPoints(Vector2 newPoint, bool debug=false){
        int gridX, gridY; getGridCoords(newPoint, out gridX, out gridY);
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector2>();
        foreach(Vector2 comparePoint in getSurroundingPoints(gridX, gridY)) {
            Vector2 compareVector = newPoint - comparePoint;
            if (compareVector.magnitude < _dTest) {
                if (debug) DebugPoints.Add(newPoint);
                return false;
            }
        }
        return true;
    }

    Vector2 FindBestDirection(Vector2 direction, Vector2 samplePoint, float cutOff){
        for (int i = 0; i < 4; i++) {
            Vector2 newDirection = SamplePixelColorFromTexture(_textures[i], samplePoint);
            float cosDiff = Vector2.Dot(direction, newDirection);
            if (cosDiff > cutOff) {
                return newDirection;
            }
        }
        return Vector2.zero;
    }
    
    Vector2 FindOppositeDirection(Vector2 direction, Vector2 samplePoint, float cutOff){
        for (int i = 0; i < 4; i++) {
            Vector2 newDirection = SamplePixelColorFromTexture(_textures[i], samplePoint);
            float cosDiff = Vector2.Dot(direction, newDirection);
            if (cosDiff < cutOff) {
                return newDirection;
            }
        }
        return Vector2.zero;
    }
    
    Vector2 FindBestDirection(Vector2 direction, Vector2 samplePoint, float rangeLow, float rangeHigh){
        for (int i = 0; i < 4; i++) {
            Vector2 newDirection = SamplePixelColorFromTexture(_textures[i], samplePoint);
            float cosDiff = Vector2.Dot(direction, newDirection);
            if (cosDiff < rangeHigh && cosDiff > rangeLow) {
                return newDirection;
            }
        }
        return Vector2.zero;
    }

    Vector2 GetIntermediaryPoint(Vector2 first, Vector2 second){
        //gets last valid point between fist and second points.
        Vector2 direction = (second - first)/10;
        if (direction.magnitude < 0.00001f) return Vector2.zero;
        Vector2 pointToCheck = first + direction;
        Vector2 lastPointFound = Vector2.zero;
        for (int i = 0; i < 10 ; i++){
            if(isPositionOutTexture(pointToCheck) || isInvalidColor(pointToCheck, 0)){
                return lastPointFound;
            }
            pointToCheck = pointToCheck + direction/10;
            lastPointFound = pointToCheck;
        }
        return lastPointFound;
    }
    
    Rgba32[] colors = {Rgba32.Blue, Rgba32.Black, Rgba32.Purple, Rgba32.Yellow, Rgba32.Green};
    public void DrawHatchings(Image bitmap)
    {
        Debug.Log("Drawing Lines");
        Debug.Log("Lines: " + Lines.Count.ToString());
        int k = 0;
        foreach (List<Vector2> line in Lines)
        {
            if (line.Count > 2)
            {
                PointF[] pointFline = new PointF[line.Count];
                for (int v = 0; v < line.Count; v++) pointFline[v] = new PointF(line[v].x, line[v].y);
                //Debug.Log("Line: " + string.Join(", ",
                //              new List<PointF>(pointFline).ConvertAll(j => j.ToString()).ToArray()));
                bitmap.Mutate(x => x.DrawLines(colors[k], 1, pointFline));
            }
            k=(k+1)%5;
        }

        DrawDebug(bitmap, DebugPoints, Rgba32.Red);
        DrawDebug(bitmap, DebugPointsLine, Rgba32.DarkRed);
        //DrawDebug(bitmap, DebugPointsGrid, Rgba32.Coral);
    }

    private void DrawDebug(Image bitmap, List<Vector2> list, Rgba32 color){
        if (list.Count > 2) {
            for (int v = 0; v < list.Count; v++) {
                PointF point = new PointF(list[v].x, list[v].y);
                bitmap.Mutate(x => x.Draw(color, 1, new RectangleF(point, new SizeF(1, 1))));
            }
        }
    }

    Vector2 rg(Color color)
    {
        float r = color.r;
        float g = color.g;
        return new Vector2(r, g);
    }

}