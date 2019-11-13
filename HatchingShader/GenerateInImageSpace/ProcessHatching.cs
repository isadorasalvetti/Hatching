using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.Util;

using Mat = Emgu.CV.Mat;

namespace Hatching.HatchingShader.GenerateInImageSpace
{
    public class ProcessHatching
    {
        private Texture2D _texture;
        private float _dSeparation;
        private float _dTest;
        private int _gridSize = 50;

        private List<List<Vector2>> Lines = new List<List<Vector2>>(); //Stores line points, in order from start to end.
        private List<List<Vector2>> PointGrid = new List<List<Vector2>>(); //Stores points in a grid. Facilitate distance calculations
        

        ProcessHatching(Texture2D texture, float dSeparation = 0.01f, float dTest = 0.9f, int gridSize = 0)
        {
            _texture = texture;
            _dSeparation = dSeparation * _texture.width;
            _dTest = dTest * _dSeparation;
            if (gridSize > 0) _gridSize = gridSize;
        }

        void FindRandomSeed(){
            // Looks for a pixel with valid curvature in image.
            for (int u=0; u < _texture.width; u += _gridSize)
                for (int v = 0; v < _texture.width; v += _gridSize)
                {
                    Color pixelColor = _texture.GetPixels(u, v, 1, 1)[0];
                    if (Mathf.Abs(pixelColor.r) > 0.01 || Mathf.Abs(pixelColor.g) > 0.01)
                        AddLine(new Vector2(u, v), new Vector2(pixelColor.r, pixelColor.g));
                }
        }

        void AddLine(Vector2 seed, Vector2 direction)
        {
            List<Vector2> line = new List<Vector2>();
            line.Add(seed);
            for (int i = 0; i < 1000; i++) //Sanity check, no infinity loops
            {
                Vector2 newPoint = seed + direction * _dTest;
                direction = rg(_texture.GetPixel((int)newPoint.x, (int)newPoint.y));
                if 
            }
            Lines.Append(line);
        }

        Vector2 rg(Color color)
        {
            float r = color.r;
            float g = color.g;
            return new Vector2(r, g);
        }

    }
}
