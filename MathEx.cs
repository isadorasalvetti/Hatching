using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MathExNamespace
{
    public class Vector4Ex{
        public static Vector4 FromArray(float[] a){
            return new Vector4(a[0], a[1], a[2], a[3]);
        }
    }

    public class Math2{
        public static void Swap<T>(ref T a1, ref T a2){
            T tmp = a1;
            a1 = a2;
            a2 = tmp;
        }
    }
    public class ArrayEx{
        public static T[] Clone<T> (T[] a){
            return (T[]) a.Clone();
        }
    }
}
