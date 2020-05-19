﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using UnityEngine;
using Color = UnityEngine.Color;
using Image = SixLabors.ImageSharp.Image;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class ProcessHatching
{
    private Texture2D[] _textures;
    private Texture2D _outline;
    private float _dSeparation;
    private float _level;
    private float _dTest;
    private int _gridSize = 50;

    private List<List<Vector2>> Lines = new List<List<Vector2>>(); //Stores line points, in order from start to end.
    private List<Tuple<List<Vector2>, List<Vector2>>> NextLineCandidates = new List<Tuple<List<Vector2>, List<Vector2>>>(); //Candidates to seed next line.
    
    private List<Vector2> NextSeedCandidates = new List<Vector2>();
    private List<Vector2> NextSeedCandidatesDirections = new List<Vector2>();
    
    private List<Vector2>[,] PointGrid; //Stores points in a grid. Facilitate distance calculations
    private List<Vector2>[,] DirectionGrid; //Stores direction points in a grid.

    private List<Vector2>[,] PointGridToCompare;
    private List<Vector2>[,] DirectionGridToCompare;

    private List<Vector2> DebugPoints = new List<Vector2>();
    private List<Vector2> DebugPointsLine = new List<Vector2>();

    private int stoppedByInvalidColor = 0;
    private int stoppedByGridConflict = 0;
    private int stoppedByLackOfDirecton = 0;

    public ProcessHatching(Texture2D[] texture, Texture2D outline, float dSeparation = 0.01f, float dTest = 0.8f,
        int gridSize = 0, float level = 1.0f) {
        _level = level; // Used to signal which area should be hatched.
        _textures = texture;
        _outline = outline;
        _dSeparation = (int)(dSeparation * _textures[0].width);
        _dSeparation = Mathf.Max(5, _dSeparation);
        _dTest = (int)(dTest * _dSeparation);
        _dTest = Mathf.Max(3, _dTest);
        if (gridSize > 0) _gridSize = gridSize;

        PointGrid = new List<Vector2>[(int)(_textures[0].width/(_dSeparation))+1, (int)(_textures[0].height/(_dSeparation))+1];
        DirectionGrid = new List<Vector2>[(int)(_textures[0].width/(_dSeparation))+1, (int)(_textures[0].height/(_dSeparation))+1];
        int testX, testY; GetGridCoords(_textures[0].width, _textures[0].height, out testX, out testY);
        
    }

    public void SetCompareGrids(List<Vector2>[,] Points, List<Vector2>[,] Directions) {
        PointGridToCompare = Points;
        DirectionGridToCompare = Directions;
    }

    public void GetCompareGrids(out List<Vector2>[,] Points, out List<Vector2>[,] Directions) {
        Points = PointGrid;
        Directions = DirectionGrid;
    }

    bool IsInvalidColor(Vector2 newPoint, int lookingIndex){
        Texture2D texture = _textures[lookingIndex];
        Color col = readTexturePixel(texture, (int)newPoint.x, (int)newPoint.y);
        return col.a > _level || col.b > 0.9f;
    }

    Color readTexturePixel(Texture2D texture, int u, int v) {
        int x = u;
        int y = texture.height - v;
        return texture.GetPixel(x, y);
    }

    bool checkSurroundingPixelColors(Texture2D texture, Vector2 centerPoint, int radius, Vector3 target) {
        float tolerance = 0.05f;
        int uStart = (int)centerPoint.x - radius/2;
        int vStart = (int)centerPoint.y - radius/2;
        for (int i = 0; i < radius; i++) {
            for (int j = 0; j < radius; j++) {
                int uSample = uStart + i;
                int vSample = vStart + j;
                Color sampleColor = readTexturePixel(texture, uSample, vSample);
                float diffMin = (new Vector3(sampleColor.r, sampleColor.g, sampleColor.b) - target).magnitude;
                float diffPlus = (new Vector3(sampleColor.r, sampleColor.g, sampleColor.b) + target).magnitude;
                if (diffMin < tolerance || diffPlus < tolerance) return true;
            }    
        }
        return false;
    }

    bool IsPositionOutTexture(Vector2 newPoint) {
        return newPoint.x < 0 || newPoint.y < 0 || newPoint.x > _textures[0].width || newPoint.y > _textures[0].height;
    }
    
    void AddPointToGrid(Vector2 point, Vector2 direction){
        int gridX, gridY; GetGridCoords(point, out gridX, out gridY);
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector2>();
        if (DirectionGrid[gridX, gridY] == null) DirectionGrid[gridX, gridY] = new List<Vector2>();
        PointGrid[gridX, gridY].Add(point);
        DirectionGrid[gridX, gridY].Add(direction);
    }

    void GetGridCoords(float x, float y, out int gridX, out int gridY) {
        gridX = (int) (x / (_dSeparation));
        gridY = (int) (y / (_dSeparation));
        //Debug.Log(x.ToString() + ", " + y.ToString() + ", " + gridX.ToString() + ", " + gridY.ToString());
    }
    
    void GetGridCoords(Vector2 point, out int gridX, out int gridY) {
        gridX = (int) (point.x / (_dSeparation));
        gridY = (int) (point.y / (_dSeparation));
        //Debug.Log(point.x.ToString() + ", " + point.y.ToString() + ", " + gridX.ToString() + ", " + gridY.ToString());
    }

    List<Vector2> GetSurroundingPoints(int gridX, int gridY, int gridSpan=1)
    {
        List<Vector2> combinedList = new List<Vector2>();
        for (int i = -1*gridSpan; i <= 1*gridSpan; i++)
            for (int j = -1*gridSpan; j <= 1*gridSpan; j++) {
                int dimX = PointGrid.GetLength(0); int dimY = PointGrid.GetLength(1);
                int Gx = gridX + i; int Gy = gridY + j;
                if (Gx < 0 || Gy < 0 || Gx >= dimX || Gy >= dimY) continue;
                if (PointGrid[Gx, Gy] != null) combinedList.AddRange(PointGrid[Gx, Gy]);
            }
        return combinedList;
    }
    
    
    Vector2 GetClosestDirection(Vector2 point, float maxDistance) {
        Vector2 maxDirection = Vector2.zero;
        int gridX, gridY;
        GetGridCoords(point.x, point.y, out gridX, out gridY);
        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++) {
                int dimX = PointGridToCompare.GetLength(0); int dimY = PointGridToCompare.GetLength(1);
                int Gx = gridX + i; int Gy = gridY + j;
                if (Gx < 0 || Gy < 0 || Gx >= dimX || Gy >= dimY) continue;
                if (PointGridToCompare[Gx, Gy] != null)
                    foreach (var gridEntry in PointGridToCompare[Gx, Gy]){
                        Vector3 compareVector = gridEntry - point;
                        float distance = compareVector.magnitude;
                        if (compareVector.magnitude < maxDistance) {
                            maxDistance = distance;
                            maxDirection = gridEntry;
                        }
                    }
            }
        return maxDirection.normalized;
    }

    public void StartRandomSeed() {
        // Looks for a pixel with valid curvature in image.
        for (int u = 0; u < _textures[0].width; u += 2)
            for (int v = 0; v < _textures[0].height; v += 2) {
                Vector2 seedPoint = new Vector2(u, v);
                    if (!IsInvalidColor(seedPoint, 0)) {
                        Vector2 seedDirection;
                        if (DirectionGridToCompare != null) { // Get best direction compared to last computed lines.
                            Vector2 comparingDirection = GetClosestDirection(seedPoint, _dSeparation*2);
                            if (comparingDirection == Vector2.zero) continue;
                            seedDirection = FindBestDirection(comparingDirection, seedPoint, -0.7f, 0.7f);
                        } else //This is the first pass, there is nothing to compare to. 
                            seedDirection = SamplePixelColorFromTexture(_textures[0], seedPoint);
                        if (AddLine(seedPoint, seedDirection)){
                            Debug.Log(String.Format("Found random seed at {0}", seedPoint));
                            GetNextSeed();
                            return;
                        }
                    }
            }
    }

    public void SoftenComputedLines() {
        List<int> linesToRemove = new List<int>();
        List<List<Vector2>> newSoftLines = new List<List<Vector2>>();
        List<List<Vector2>> softeningResult; 
        
        for (int i = 0; i < Lines.Count; i++) {
            softeningResult = SoftenLine(Lines[i]);
            if (softeningResult != null) {
                newSoftLines.AddRange(softeningResult);
                linesToRemove.Add(i);
            }
        }
        for (int index = linesToRemove.Count-1; index >= 0; index--) {
            Lines.RemoveAt(index);
        }
        Debug.Log(String.Format("Softened {0}", linesToRemove.Count));
        Lines.AddRange(newSoftLines);

    }
    

    void GetNextSeed() {
        if (Lines.Count > 1500) throw new Exception("Max number of lines reached");
        while (NextLineCandidates.Count > 0) {
            List<Vector2> line = NextLineCandidates[0].Item1;
            List<Vector2> directions = NextLineCandidates[0].Item2;
            
            int[] mults = {1, -1};
            foreach (int mult in mults) {
                for (int i = 0; i < line.Count; i++) {
                    Vector2 point = line[i];
                    Vector2 previousDirection = directions[i];
                    Vector2 perpendicularDirection = new Vector2(previousDirection.y, previousDirection.x);

                    if (mult == -1) perpendicularDirection *= -1;

                    Vector2 testPoint = point + _dSeparation * perpendicularDirection;
                    if (testPoint.x < 0 || testPoint.y < 0 || testPoint.x > _textures[0].width ||
                        testPoint.y > _textures[0].height) continue;

                    if (CheckSurroundingPoints(testPoint)) {
                        bool isOutline = checkSurroundingPixelColors(_outline, testPoint, 3, Vector3.zero);
                        if (!IsInvalidColor(testPoint, 0) || !isOutline) {
                            Vector2 initialDirection = FindBestDirection(previousDirection, testPoint, 0.9f, 1.1f);
                            if (!(initialDirection == Vector2.zero)) {
                                if (DirectionGridToCompare != null) {
                                    Vector2 comparingDirection = GetClosestDirection(testPoint, _dSeparation*2);
                                    if (comparingDirection == Vector2.zero) continue;
                                    if (Mathf.Abs(Vector2.Dot(comparingDirection, initialDirection)) > 0.4f) continue;
                                }
                                AddLine(testPoint, initialDirection);
                            }
                        }
                    }
                }
            }
            NextLineCandidates.RemoveAt(0);
        }
    }

    void Alt_GetNextSeed() {
        if (Lines.Count > 1500) throw new Exception("Max number of lines reached");
        while (NextSeedCandidates.Count > 0) {
            Vector2 newSeedCandidateDirection = NextSeedCandidatesDirections[0];
            for (int s = 1; s < 3; s++)
                for (int i = 0; i < 360; i++) {
                    Vector2 newCandidate = NextSeedCandidates[0] + 
                                           Math2.rotateVec2(_dSeparation*s*Vector2.right, i);
                    if (SamplePixelColorFromTexture(_outline, newCandidate, false).x < 0.9 &&
                            !IsInvalidColor(newCandidate, 0) &&
                            CheckSurroundingPoints(newCandidate)) {
                        DebugPointsLine.Add(newCandidate);
                        Vector2 newInitialDirection = FindBestDirection(newSeedCandidateDirection, newCandidate, 0.9f, 1.1f);
                        AddLine(newCandidate, newInitialDirection);
                        break;
                    }
                }
            NextSeedCandidates.RemoveAt(0);
            NextSeedCandidatesDirections.RemoveAt(0);
        }
    }

    bool AddLine(Vector2 seed, Vector2 initialDirection) {
        DebugPointsLine.Add(seed);
        
        //Creates new line starting at seed.
        List<Vector2> line = new List<Vector2>();
        List<Vector2> lineDirections = new List<Vector2>();

        Vector2 direction = initialDirection;
        
        line.Add(seed);
        lineDirections.Add(initialDirection);
        foreach (int mult in new int[2] {1, -1}) {
            if (mult == -1) {
                direction = FindBestDirection(initialDirection, seed, -1.1f, -0.97f);
            }

            Vector2 newPoint = seed;

            for (int i = 0; i < 1000; i++) {
                newPoint = GetNextPoint(newPoint, ref direction);
                if (newPoint == Vector2.zero || direction == Vector2.zero) break;
                if (mult > 0) {
                    line.Add(newPoint);
                    lineDirections.Add(direction);
                }
                else {
                    line.Insert(0, newPoint);
                    lineDirections.Insert(0, direction);
                }
                
                if (line.Count > 600) break;
                if (direction == Vector2.zero){
                    stoppedByLackOfDirecton += 1;
                    break;
                }
            }
        }

        if (line.Count > 2) {
            foreach (var point in line) {
                AddPointToGrid(point, direction);
            }
            Lines.Add(line);
            NextLineCandidates.Add(new Tuple<List<Vector2>, List<Vector2>>(line, lineDirections));
            return true;
        }
        return false;
    }

    Vector2 SamplePixelColorFromTexture(Texture2D texture, Vector2 point, bool convert=true) {
        Color pixelColor =  readTexturePixel(texture, (int)point.x, (int)point.y);
        Vector2 newDirection = rg(pixelColor);
        if (convert) newDirection = (newDirection * 2 - Vector2.one).normalized;
        return newDirection;
    }

    Vector2 GetNextPoint(Vector2 previousPoint, ref Vector2 direction) {
        //Gets next valid point for a line
        Vector2 k1 = previousPoint + 2.5f * direction;
        Vector2 k1Direction = FindBestDirection(direction, k1, 0.9f, 1.1f);
        Vector2 k2 = k1 + 2.5f * k1Direction;
        Vector2 k2Direction = FindBestDirection(direction, k2, 0.9f, 1.1f);
        
        if(k1Direction == Vector2.zero) {
            if (k2Direction == Vector2.zero) {
                stoppedByLackOfDirecton += 1;
                return Vector2.zero;
            }
            direction = k1;
        }
        else direction = (k1Direction + k2Direction) / 2;
        
        Vector2 newPoint = previousPoint + 5.0f * direction;

        //Reject points outside of image and finds adequate point in between.
        if (IsPositionOutTexture(newPoint)) return Vector2.zero;
        if (IsInvalidColor(newPoint, 0) || 
                checkSurroundingPixelColors(_outline, newPoint, 3, Vector3.zero)) {
            stoppedByInvalidColor += 1;
            return Vector2.zero; 
        }

        if (!CheckSurroundingPoints(newPoint)) {
            stoppedByGridConflict += 1;
            return Vector2.zero;
        }
        
        //Returns:
        direction = FindBestDirection(direction, newPoint, 0.975f, 1.1f);
        return newPoint;
    }

    bool CheckSurroundingPoints(Vector2 newPoint, float testRatio=1, bool debug=false){
        int gridX, gridY; GetGridCoords(newPoint, out gridX, out gridY);
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector2>();
        foreach(Vector2 comparePoint in GetSurroundingPoints(gridX, gridY)) {
            Vector2 compareVector = newPoint - comparePoint;
            if (compareVector.magnitude < _dTest*testRatio) {
                return false;
            }
        }
        return true;
    }
    
    List<Vector2> ReturnSurroundingPoints(Vector2 centerPoint, float testRatio=1, bool debug=false) {
        List<Vector2> surroundingPoints = new List<Vector2>();
        int gridX, gridY; GetGridCoords(centerPoint, out gridX, out gridY);
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector2>();
        foreach(Vector2 comparePoint in GetSurroundingPoints(gridX, gridY, (int)(testRatio+0.5f))) {
            Vector2 compareVector = centerPoint - comparePoint;
            if (compareVector.magnitude < _dTest*testRatio) {
                surroundingPoints.Add(comparePoint);
            }
        }
        return surroundingPoints;
    }

    Vector2 FindBestDirection(Vector2 direction, Vector2 samplePoint, float rangeLow, float rangeHigh) {
        float[] cosineDiffs = new float[4];
        
        for (int i = 0; i < 4; i++) {
            int index = i % 2;
            int mult = 1;
            if (i > 1) mult = -1;
            Vector2 newDirection = SamplePixelColorFromTexture(_textures[index], samplePoint) * mult;
            float cosDiff = Vector2.Dot(direction, newDirection);
            cosineDiffs[i] = cosDiff;
            if (cosDiff < rangeHigh && cosDiff > rangeLow) {
                return newDirection;
            }
        }
        //Debug.Log(String.Format("Cosine diffs: {0}, {1}, {2}, {3}",
        //    cosineDiffs[0], cosineDiffs[1], cosineDiffs[2], cosineDiffs[3]));
        return Vector2.zero;
    }

    Vector2 GetIntermediaryPoint(Vector2 first, Vector2 second) {
        //gets last valid point between fist and second points.
        Vector2 direction = (second - first)/2;
        Vector2 lastPointFound = Vector2.zero;
        Vector2 pointToCheck = first + direction;
        
        while (direction.magnitude > 0.01f){
            direction = direction / 2;
            
            if(IsPositionOutTexture(pointToCheck) || IsInvalidColor(pointToCheck, 0)){
                pointToCheck = pointToCheck - direction; // Too far, back
            }
            else {
                lastPointFound = pointToCheck;
                pointToCheck = pointToCheck + direction;
            }
        }
        return lastPointFound;
    }

    List<List<Vector2>> SoftenLine(List<Vector2> line) {
        // Softens the ends of the line by removing segments.
        // Returns a list of the lines created by this process or null if division was not possible.
        int gapSize = 2;
        if (line.Count < 8*gapSize) return null;

        int[] fragmentSizes = new int[] {1, 2, 4, 8};
        List<List<Vector2>> splitLines = new List<List<Vector2>>();
        cutLine(0, fragmentSizes, line, ref splitLines);
        
        
        return splitLines;
    }

    void cutLine(int lineStartOrEnd, int[] fragmentSizes, List<Vector2> line, ref List<List<Vector2>> splitLines) {
        int initialIndex, endIndex, tolerance, mult;
        if (lineStartOrEnd == 0) {
            endIndex = line.Count-1;
            initialIndex = 0;
            tolerance = 3; 
            mult = 1;
        }
        else {
            initialIndex = line.Count;
            endIndex = 0;
            tolerance = 2;
            mult = -1;
        }
        Vector2 lineStart = line[initialIndex];
        int gapSize = fragmentSizes[0];
        bool isOutline = checkSurroundingPixelColors(_outline, lineStart, 3, Vector3.zero);
        if (!isOutline) {
            // This pixel is not at or close to border. soften.
            int startIndex = 0;
            bool lineSoftened = false;
            foreach(int segmentSize in fragmentSizes) {
                int endSegmentIndex = startIndex + segmentSize*mult;
                if (endSegmentIndex * tolerance < line.Count) {
                    lineSoftened = true;
                    splitLines.Add(line.GetRange(startIndex, segmentSize));
                }
                else break;
                startIndex = endSegmentIndex + gapSize*mult;
            }
            if (lineSoftened) splitLines.Add(
                line.GetRange(Math.Min(startIndex, endIndex), Math.Abs(startIndex - endIndex))
                );
        }
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
                bitmap.Mutate(x => x.DrawLines(colors[1], 1, pointFline));
            }
            k=(k+1)%5;
        }

        //DrawDebug(bitmap, DebugPoints, Rgba32.Red);
        //DrawDebug(bitmap, DebugPointsLine, Rgba32.DarkRed);
        Debug.Log(String.Format("Why did it stop: Grid conflict - {0}, InvalidColor - {1}, InvalidNewDirection - {2}",
            stoppedByGridConflict, stoppedByInvalidColor, stoppedByLackOfDirecton));
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