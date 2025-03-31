using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// Extensions for Array.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Set values of elements in an array.
        /// </summary>
        /// <param name="index"> Start index. </param>
        /// <param name="count"> A negative or zero value means all elements after start index. </param>
        public static void SetValues<T>(
            this T[] array,
            T value = default,
            int index = 0,
            int count = 0)
        {
            int lastIndex = count > 0 ? (index + count) : array.Length;
            while(index < lastIndex) array[index++] = value;
        }


        /// <summary>
        /// Set values of elements in a 2d array.
        /// </summary>
        /// <param name="beginRowIndex">  Start row index </param>
        /// <param name="beginColIndex"> Start col index </param>
        /// <param name="endRowIndex"> A negative or zero value means all elements after start index. </param>
        /// <param name="endColIndex"> A negative or zero value means all elements after start index. </param>
        public static void SetValues<T>(
            this T[,] array,
            T value = default,
            int beginRowIndex = 0,
            int beginColIndex = 0,
            int endRowIndex = 0,
            int endColIndex = 0)
        {
            if (endRowIndex <= 0) endRowIndex = array.GetLength(0)-1;
            if (endColIndex <= 0) endColIndex = array.GetLength(1)-1;

            for (int i = beginRowIndex; i <= endRowIndex; i++)
            {
                for (int j = beginColIndex; j <= endColIndex; j++)
                {
                    array[i,j] = value;
                }
            }
        }


        /// <summary>
        /// Find the nearest element with specific value in an array.
        /// </summary>
        public static int FindNearestIndex(this float[] items, float value)
        {
            if (RuntimeUtilities.IsNullOrEmpty(items)) return -1;

            int result = 0;
            float minError = Mathf.Abs(value - items[0]);
            float error;

            for (int i = 1; i < items.Length; i++)
            {
                error = Mathf.Abs(value - items[i]);
                if (error < minError)
                {
                    minError = error;
                    result = i;
                }
            }

            return result;
        }


        /// <summary>
        /// Change the size of the list.
        /// </summary>
        public static void Resize<T>(this List<T> list, int newSize, T newValue = default)
        {
            if (list.Count != newSize)
            {
                if (list.Count > newSize)
                {
                    list.RemoveRange(newSize, list.Count - newSize);
                }
                else
                {
                    int addCount = newSize - list.Count;

                    while (addCount > 0)
                    {
                        list.Add(newValue);
                        addCount--;
                    }
                }
            }
        }


        /// <summary>
        /// Change the size of the list.
        /// </summary>
        public static void Resize(this IList list, int newSize, object newValue = null)
        {
            if (list.Count != newSize)
            {
                if (list.Count > newSize)
                {
                    for (int i = list.Count - 1; i >= newSize; i--)
                    {
                        list.RemoveAt(i);
                    }
                }
                else
                {
                    int addCount = newSize - list.Count;

                    while (addCount > 0)
                    {
                        list.Add(newValue);
                        addCount--;
                    }
                }
            }
        }


        /// <summary>
        /// Sort elements part of the list.
        /// </summary>
        /// <param name="index"> Start index </param>
        /// <param name="count"> A negative or zero value means all elements after start index. </param>
        public static void Sort<T>(this IList<T> list, Comparison<T> compare, int index = 0, int count = 0)
        {
            if (count <= 0) count = list.Count - index;
            int lastIndex = index + count - 1;
            T temp;
            bool changed;

            for (int i = 0; i < count - 1; i++)
            {
                changed = false;
                for (int j = index; j < lastIndex; j++)
                {
                    if (compare(list[j], list[j+1]) > 0)
                    {
                        temp = list[j];
                        list[j] = list[j+1];
                        list[j+1] = temp;
                        changed = true;
                    }
                }
                if (!changed) break;

                lastIndex--;
            }
        }


        public static T Last<T>(this IList<T> list)
        {
            return list[list.Count - 1];
        }


        public static void Swap<T>(this IList<T> list, int firstIndex, int secondIndex)
        {
            var first = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = first;
        }


        /// <summary>
        /// Traverse any array.
        /// </summary>
        /// <param name="onElement"> param1 is dimension index, param2 is element indexes in every dimension </param>
        /// <param name="beginDimension"> param1 is dimension index, param2 is indexes in every dimension before this dimension </param>
        /// <param name="endDimension"> param1 is dimension index, param2 is indexes in every dimension before this dimension </param>
        public static void Traverse(
            this Array array,
            Action<int, int[]> onElement,
            Action<int, int[]> beginDimension = null,
            Action<int, int[]> endDimension = null)
        {
            if (array.Length != 0)
            {
                TraverseArrayDimension(0, new int[array.Rank]);
            }

            void TraverseArrayDimension(int dimension, int[] indices)
            {
                int size = array.GetLength(dimension);
                bool isFinal = (dimension + 1 == array.Rank);

                beginDimension?.Invoke(dimension, indices);

                for (int i = 0; i < size; i++)
                {
                    indices[dimension] = i;
                    if (isFinal)
                    {
                        onElement?.Invoke(dimension, indices);
                    }
                    else TraverseArrayDimension(dimension + 1, indices);
                }

                endDimension?.Invoke(dimension, indices);
            }
        }


        /// <summary>
        /// Get the text description of the array, the text is similar to C# code.
        /// </summary>
        public static string ToCodeString(this Array array, Func<object, string> elementToString = null)
        {
            if (array == null) return "Null Array";

            if (elementToString == null)
            {
                elementToString = obj =>
                {
                    if (ReferenceEquals(obj, null))
                    {
                        return "null";
                    }
                    if (obj.GetType() == typeof(string))
                    {
                        return string.Format("\"{0}\"", obj);
                    }
                    return obj.ToString();
                };
            }

            var builder = new System.Text.StringBuilder(array.Length * 4);

            Traverse(array,
                (d, i) =>
                {
                    if (i[d] != 0) builder.Append(',');
                    builder.Append(' ');
                    object obj = array.GetValue(i);
                    builder.Append(elementToString(obj));
                },

                (d, i) =>
                {
                    if (d != 0)
                    {
                        if(i[d - 1] != 0) builder.Append(',');
                        builder.Append('\n');
                        while(d != 0)
                        {
                            builder.Append('\t');
                            d--;
                        }
                    }
                    builder.Append('{');
                },

                (d, i) =>
                {
                    if (d + 1 == array.Rank) builder.Append(" }");
                    else
                    {
                        builder.Append('\n');
                        while (d != 0)
                        {
                            builder.Append('\t');
                            d--;
                        }
                        builder.Append('}');
                    }
                });

            return builder.ToString();
        }

    } // class Extensions

} // namespace UnityExtensions