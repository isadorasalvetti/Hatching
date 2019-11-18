using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using UnityEngine;

using Color = UnityEngine.Color;
using DwColor = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;

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
        
        public ProcessHatching(Texture2D texture, float dSeparation = 0.01f, float dTest = 0.9f,
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
                    if (Mathf.Abs(pixelColor.r) > 0.01 || Mathf.Abs(pixelColor.g) > 0.01)
                    {
                        AddLine(new Vector2(u, v), new Vector2(pixelColor.r, pixelColor.g));
                        //u = _texture.width; v = _texture.height;
                    }
                }
        }

        void AddLine(Vector2 seed, Vector2 direction)
        {
            Debug.Log("AddingLine for " + seed.ToString());
            List<Vector2> line = new List<Vector2>();
            Vector2 newPoint = seed;
            foreach (int mult in new int[2]{1, -1})
                for (int i = 0; i < 500; i++)
                {
                    if (newPoint == Vector2.zero) break;
                    
                    if (mult > 0) line.Add(newPoint);
                    else line.Insert(0, newPoint);

                    newPoint = GetNextPoint(newPoint, direction, mult:mult);
                }
            Lines.Add(line);
        }

        Vector2 GetNextPoint(Vector2 previousPoint, Vector2 direction, float mult = 1)
        {
            Vector2 newPoint = previousPoint + _dTest * mult * direction;
            direction = rg(_texture.GetPixel((int)newPoint.x, (int)newPoint.y));
            if (Mathf.Abs(direction.x) > 0.01f || Mathf.Abs(direction.y) > 0.01f) return newPoint;
            return Vector2.zero;
        }

        void DrawHatchings()
        {
            Debug.Log("Drawing Lines");
            Bitmap bitmap = new Bitmap(_texture.width, _texture.height);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(DwColor.White);
            foreach (List<Vector2> line in Lines)
            {
                PointF[] pointFline = new PointF[line.Count];
                for (int v = 0; v < line.Count; v++) pointFline[v] = new PointF(line[v].x, line[v].y);
                graphics.DrawCurve(new Pen(DwColor.Black), pointFline);
            }
            bitmap.Save("C:\\Users\\isadora.albrecht\\Documents\\Downloads\\test.png", ImageFormat.Png);
        }

        Vector2 rg(Color color)
        {
            float r = color.r;
            float g = color.g;
            return new Vector2(r, g);
        }

    }
}
