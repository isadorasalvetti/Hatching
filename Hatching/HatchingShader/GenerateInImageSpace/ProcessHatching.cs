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
    private Texture2D _texture;
    private float _dSeparation;
    private float _dTest;
    private int _gridSize = 50;
    private int _width = 3;

    private List<List<Vector2>> Lines = new List<List<Vector2>>(); //Stores line points, in order from start to end.
    private List<Vector3>[,] PointGrid; //Stores points in a grid. Facilitate distance calculations
    
    public ProcessHatching(Texture2D texture, float dSeparation = 0.01f, float dTest = 0.9f,
        int gridSize = 0, int width = 0)
    {
        _texture = texture;
        _dSeparation = (int)(dSeparation * _texture.width);
        _dSeparation = Mathf.Max(6, _dSeparation);
        _dTest = (int)(dTest * _dSeparation);
        _dTest = Mathf.Max(4, _dTest);
        if (gridSize > 0) _gridSize = gridSize;
        if (width > 0) _width = width;
        if (width > 0) _width = width;
        
        PointGrid = new List<Vector3>[(int)(_texture.width/(_dSeparation))+1, (int)(_texture.height/(_dSeparation))+1];
        int testX, testY; getGridCoords(_texture.width, _texture.height, out testX, out testY);
        
        StartRandomSeed();
        DrawHatchings();
    }

    bool isInvalidColor(Vector2 newPoint) {
        Color col = _texture.GetPixel((int)newPoint.x, -(int)newPoint.y);
        return Mathf.Abs(col.r) < Single.Epsilon && Mathf.Abs(col.g) < Single.Epsilon;
    }

    bool isPositionOutTexture(Vector2 newPoint) {
        return newPoint.x < 0 || newPoint.y < 0 || newPoint.x > _texture.width || newPoint.y > _texture.height;
    }

    void addPointToGrid(int gridX, int gridY, Vector2 point, float depth){
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector3>();
        PointGrid[gridX, gridY].Add(new Vector3(point.x, point.y, depth));
    }
    
    void addPointToGrid(int gridX, int gridY, float x, float y, float depth){
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector3>();
        PointGrid[gridX, gridY].Add(new Vector3(x, y, depth));
    }
    
    void addPointToGrid(int gridX, int gridY, Vector3 point){
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector3>();
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

    List<Vector3> getSurroudingPoints(int gridX, int gridY)
    {
        List<Vector3> combinedList = new List<Vector3>();
        for (int i = -1; i <= 1; i++)
        for (int j = -1; j <= 1; j++){
            if (PointGrid[gridX+i, gridY+j] != null) combinedList.AddRange(PointGrid[gridX+i, gridY+j]);
        }
        return combinedList;
    }

    void StartRandomSeed() {
        // Looks for a pixel with valid curvature in image.

        Debug.Log("Looking for seeds");
        for (int u = 0; u < _texture.width; u += _gridSize)
        for (int v = 0; v < _texture.height; v += _gridSize)
        {
            Color pixelColor = _texture.GetPixel(u, -v);
            if (!isInvalidColor(new Vector2(u, v)))
            {
                Vector2 point = new Vector2(u, v);
                Vector2 direction = rg(pixelColor);
                float depth = pixelColor.b;
                
                direction = direction * 2 - Vector2.one;
                
                int gridX, gridY; getGridCoords(u, v, out gridX, out gridY);
                addPointToGrid(gridX, gridY, u, v, depth);
                AddLine(new Vector2(u, v), direction);
                break;
            }
        }
    }

    void GetNextSeed(List<Vector2> line)
    {
        if (Lines.Count > 500) Debug.Log("About to crash");
        if (Lines.Count > 501) throw new Exception("Max number of lines reached");
        
        int[] mults = {1, -1};
        foreach(int mult in mults)
        foreach(Vector2 point in line){
            Vector2 testPoint = point + new Vector2(_dSeparation, _dSeparation)*mult;
            if (testPoint.x < 0 || testPoint.y < 0 || testPoint.x > _texture.width || testPoint.y > _texture.height) continue;
            Color pixelColor = _texture.GetPixel((int)testPoint.x, -(int)testPoint.y);
            float depth = pixelColor.b;
            
            Vector3 testPoint3 = new Vector3(testPoint.x, testPoint.y, depth); 
            
            bool validGrid = true;
            int gridX, gridY; getGridCoords(testPoint, out gridX, out gridY);
            if (PointGrid[gridX, gridY] != null)
                foreach(Vector3 comparePoint in getSurroudingPoints(gridX, gridY))
                    if ((testPoint3 - comparePoint).magnitude < _dTest) {
                        validGrid = false;
                        break;
                    }

            if (!isInvalidColor(testPoint) && validGrid)
            {
                Vector2 direction = rg(pixelColor);
                direction = direction * 2 - Vector2.one;
                addPointToGrid(gridX, gridY, testPoint, depth);
                AddLine(testPoint, direction);
                break;
            }
        }
    }

    void AddLine(Vector2 seed, Vector2 initialDirection)
    {
        //Creates new line starting at seed.
        List<Vector2> line = new List<Vector2>();
        foreach (int mult in new int[2]{1, -1}){
            Vector2 direction = initialDirection*mult;
            Vector2 newPoint = seed;
            for (int i = 0; i < 2000; i++)
            {
                if (newPoint == Vector2.zero) break;

                if (mult > 0) line.Add(newPoint);
                else line.Insert(0, newPoint);
                newPoint = GetNextPoint(newPoint, ref direction);
            }
        }
        if(line.Count > 2) Lines.Add(line);
        //GetNextSeed(line);
    }

    Vector2 GetNextPoint(Vector2 previousPoint, ref Vector2 direction)
    {
        //Gets next valid point for a line
        Vector2 newPoint = previousPoint + _dSeparation * direction;

        //Reject points outside of image and finds adequate point in between.
        if (isPositionOutTexture(newPoint)) newPoint = GetIntermediaryPoint(previousPoint, newPoint);
        Color pixelColor = _texture.GetPixel((int)newPoint.x, -(int)newPoint.y);
        if (isInvalidColor(newPoint)) newPoint = GetIntermediaryPoint(previousPoint, newPoint);

        // Stop and return if no valid point was found
        if(newPoint == Vector2.zero) return newPoint;
        
        float depth = pixelColor.b;
        Vector3 newPoint3 = new Vector3(newPoint.x, newPoint.y, depth);
        
        int gridX, gridY; getGridCoords(newPoint, out gridX, out gridY);
        if (PointGrid[gridX, gridY] == null) PointGrid[gridX, gridY] = new List<Vector3>();
        foreach(Vector3 comparePoint in getSurroudingPoints(gridX, gridY)){
            if ((newPoint3 - comparePoint).magnitude < _dTest) return new Vector2();
        }
        addPointToGrid(gridX, gridY, newPoint3);

        Vector2 newDirection = rg(pixelColor);
        newDirection = newDirection * 2 - Vector2.one;
        return newPoint;
    }

    Vector2 GetIntermediaryPoint(Vector2 first, Vector2 second){
        //gets last valid point between fist and second points.
        Vector2 direction = second - first;
        Vector2 point_found = Vector2.zero;
        Vector2 pointToCheck = first + direction/2;
        Vector2 lastPointFound = Vector2.zero;
        for (int i = 2; i < 6 ; i++){
            if(isPositionOutTexture(pointToCheck) || isInvalidColor(pointToCheck)){
                pointToCheck = pointToCheck - direction/i/2;
            }
            else{
                pointToCheck = pointToCheck + direction/i/2;
                lastPointFound = pointToCheck;
            }
        }
        Debug.Log(string.Format("Intermediary point found: {0}, between {1} and {2}", lastPointFound, first, second));
        //Debug.Log(string.Format("Point color: {0}", _texture.GetPixel((int)lastPointFound.x, -(int)lastPointFound.y)));
        return lastPointFound;
    }


    Rgba32[] colors = {Rgba32.Black, Rgba32.Blue, Rgba32.Red, Rgba32.Yellow, Rgba32.Green};
    void DrawHatchings()
    {
        Debug.Log("Drawing Lines");
        Image bitmap = new Image<Rgba32>(_texture.width, _texture.height);
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

        //bitmap.Save("C:\\Users\\isadora.albrecht\\Documents\\Downloads\\test.png", new PngEncoder());
        bitmap.Save("C:\\Users\\Isadora\\Documents\\_MyWork\\Papers\\Thesis\\test.png", new PngEncoder());
    }

    Vector2 rg(Color color)
    {
        float r = color.r;
        float g = color.g;
        return new Vector2(r, g);
    }

}