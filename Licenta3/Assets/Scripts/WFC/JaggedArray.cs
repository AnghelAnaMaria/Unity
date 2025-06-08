using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Helpers
{//construiesc generice n-dimensionale de jagged arrays
    public static class JaggedArray
    {
        public static T CreateJaggedArray<T>(params int[] lengths)//exemplu: T=TileBaseValue[][];
        {
            return (T)InitializeJaggedArray(typeof(T).GetElementType(), 0, lengths);//typeof(T) returnează un obiect System.Type care reprezintă tipul generic T în întregime, adica TileBaseValue[][] din exemplu
        }                                            //GetElementType() îmi spune: „dă-mi tipul primului nivel de array” ca să știu ce să aloc pentru array[0], array[1], …

        static object InitializeJaggedArray(Type type, int index, int[] lengths)//index= nivelul de recursiune;  type= tipul elementului pe care vreau să-l creez (ex: int[], int[][])
        {
            Array array = Array.CreateInstance(type, lengths[index]);//creeaza liniile de array
            //Vedem dacă «type» e la rândul lui array (GetElementType()!=null)
            Type elementType = type.GetElementType();
            if (elementType != null)
            {
                //Dacă da, pentru fiecare poziție construim recursiv sub-array-ul:
                for (int i = 0; i < lengths[index]; i++)
                {
                    object child = InitializeJaggedArray(elementType, index + 1, lengths);
                    array.SetValue(child, i);
                }
            }
            return array;
        }

        public static bool CheckJaggedArray2dIndexIsValid<T>(this T[][] array, int x, int y)
        {
            if (array == null)
            {
                return false;
            }
            return ValidateCoordinates(x, y, array[0].Length, array.Length);
        }

        public static bool ValidateCoordinates(int x, int y, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;
            return true;
        }
    }

}
