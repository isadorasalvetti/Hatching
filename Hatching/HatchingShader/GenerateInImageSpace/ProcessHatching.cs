using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using UnityEngine;

using Color = UnityEngine.Color;
using Image = SixLabors.ImageSharp.Image;

namespace Hatching.HatchingShader.GenerateInImageSpace
{
    public class ProcessHatching
    {
        private Texture2D _texture;
        private float _dSeparation;
        private float _dTest;
        private int _gridSize = 50;
        private int _width = 3;

        private List<List<Vector2>> Lines = new List<List<Vector2>>(); //Stores line points, in order from start to end.
        private List<List<Vector2>> PointGrid = new List<List<Vector2>>(); //Stores points in a grid. Facilitate distance calculations
        
        public ProcessHatching(Texture2D texture, float dSeparation = 0.001f, float dTest = 0.9f,
            int gridSize = 0, int width = 0)
        {
            _texture = texture;
            _dSeparation = dSeparation * _texture.width;
            _dTest = dTest * _dSeparation;
            if (gridSize > 0) _gridSize = gridSize;
            if (width > 0) _width = width;
            
            StartRandomSeed();
            DrawHatchings();
        }

        void StartRandomSeed(){
            // Looks for a pixel with valid curvature in image.
            Debug.Log("Looking for seeds");
            for (int u=0; u < _texture.width; u += _gridSize)
                for (int v = 0; v < _texture.height; v += _gridSize)
                {
                    Color pixelColor = _texture.GetPixel(u, v);
                    if (Mathf.Abs(pixelColor.b) < 0.5f)
                    {
                        AddLine(new Vector2(u, v), new Vector2(pixelColor.r, pixelColor.g));
                    }
                }
        }

        void AddLine(Vector2 seed, Vector2 initialDirection)
        {
            List<Vector2> line = new List<Vector2>();
            foreach (int mult in new int[2]{1, -1}){
                Vector2 newPoint = seed;
                Vector2 direction = initialDirection*mult;
                for (int i = 0; i < 1000; i++)
                {
                    if (newPoint == Vector2.zero) break;
                    
                    if (mult > 0) line.Add(newPoint);
                    else line.Insert(0, newPoint);
                    newPoint = GetNextPoint(newPoint, ref direction, mult:mult);
                }
            }
            Lines.Add(line);
        }

        Vector2 GetNextPoint(Vector2 previousPoint, ref Vector2 direction, float mult = 1)
        {
            Vector2 newPoint = previousPoint + _dTest * direction;
            Color pixelColor = _texture.GetPixel((int)newPoint.x, -(int)newPoint.y);
            Vector2 newDirection = rg(pixelColor);
            if (Mathf.Abs(pixelColor.b) < 0.5f)
            {
                newDirection = newDirection * 2 - Vector2.one;
                if(Vector3.Dot(direction, newDirection) > 0) direction = -newDirection;
                else direction = newDirection;
                return newPoint;
            }
            return Vector2.zero;
        }

        void DrawHatchings()
        {
            Debug.Log("Drawing Lines");
            Image bitmap = new Image<Rgba32>(_texture.width, _texture.height);
            foreach (List<Vector2> line in Lines)
            {
                PointF[] pointFline = new PointF[line.Count];
                for (int v = 0; v < line.Count; v++) pointFline[v] = new PointF(line[v].x, line[v].y);
                Debug.Log("Line: " + string.Join(", ", new List<PointF>(pointFline).ConvertAll(j => j.ToString()).ToArray()));
                bitmap.Mutate(x => x.DrawLines(Rgba32.Black, 1, pointFline));
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
}
