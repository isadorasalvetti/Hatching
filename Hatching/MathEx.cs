using System;
using UnityEngine;

namespace Hatching
{
    public class Vector4Ex{
        public static Vector4 FromArray(float[] a){
            return new Vector4(a[0], a[1], a[2], a[3]);
        }
    }

    public class Math2
    {
        private static float PI = 3.14159265359f;
        
        public static void Swap<T>(ref T a1, ref T a2){
            T tmp = a1;
            a1 = a2;
            a2 = tmp;
        }

        public static Vector3 Cross(ref Vector3 a, ref Vector3 b)
        {
            Vector3 cross_P = new Vector3();
            cross_P.x = a.y * b.z - a.z * b.y; 
            cross_P.y = a.x * b.z - a.z * b.x; 
            cross_P.z = a.x * b.y - a.y * b.x;
            return cross_P;
        }

        public static float radToDegree(float rad) { return rad * (180.0f / PI); }

    }

    public class ArrayEx{
        public static T[] Clone<T> (T[] a){
            return (T[]) a.Clone();
        }
    }
}
