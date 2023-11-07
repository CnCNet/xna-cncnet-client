using System.Collections.Generic;
using System.Linq;

namespace ClientCore.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Converts an enumerable to a matrix of items with a max number of items per column.
    /// The matrix is built column by column, left to right.
    /// </summary>
    /// <param name="enumerable">the enumerable to convert</param>
    /// <param name="maxPerColumn">the max number of items per column</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<List<T>> ToMatrix<T>(this IEnumerable<T> enumerable, int maxPerColumn)
    {
        var list = enumerable.ToList();
        return list.Aggregate(new List<List<T>>(), (matrix, item) =>
        {
            int index = list.IndexOf(item);
            int column = (index / maxPerColumn);
            List<T> columnList = matrix.Count <= column ? new List<T>() : matrix[column];
            if (columnList.Count == 0)
                matrix.Add(columnList);

            columnList.Add(item);
            return matrix;
        });
    }
}