using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public static class HashCodeCalculator
{
  /// <summary>
  /// Flattens a 2D int grid into bytes, hashes it with MD5, and returns a hex string.
  /// </summary>
  public static string CalculateHashCode(int[][] grid)
  {
    // a) Flatten: compute total byte length and copy all ints into one byte[]
    int rows = grid.Length;
    int cols = grid.FirstOrDefault()?.Length ?? 0;
    var buffer = new byte[rows * cols * sizeof(int)];
    int offset = 0;

    foreach (var row in grid)
    {
      // Copy entire row in one go
      Buffer.BlockCopy(row, 0, buffer, offset, row.Length * sizeof(int));
      offset += row.Length * sizeof(int);
    }

    // b) Compute MD5
    using (var md5 = MD5.Create())
    {
      var hash = md5.ComputeHash(buffer);
      // c) To hex
      var sb = new StringBuilder(hash.Length * 2);
      foreach (var b in hash)
        sb.Append(b.ToString("X2"));
      return sb.ToString();
    }
  }
}



// using System;
// using System.Security.Cryptography;
// using System.Linq;
// using System.Text;

// public static class HashCodeCalculator
// {
//     /// <summary>
//     /// Transformă matricea 2D de întregi într-un singur byte[].
//     /// Hash-uiește-o cu MD5(16 octeți).
//     ///Întoarce un string hexazecimal reprezentând acel hash.
//     public static string CalculateHashCode(int[][] grid)
//     {
//         // a) "Flatten" – transformă fiecare rând int[] în octeți și alipeste-i într-un singur byte[]
//         byte[] tmpSource = grid
//           .SelectMany(row => GetByteArrayFromIntArray(row))  // LINQ SelectMany 
//           .ToArray();

//         // b) Calculează MD5 peste toți acei octeți
//         byte[] tmpHash = new MD5CryptoServiceProvider()
//           .ComputeHash(tmpSource);

//         // c) Transformă hash-ul binar într-un string hexazecimal (ex: "AF12C3...")
//         return ByteArrayToString(tmpHash);
//     }

//     private static byte[] GetByteArrayFromIntArray(int[] intArray)
//     {
//         // Fiecare int ocupă 4 octeți
//         byte[] data = new byte[intArray.Length * 4];

//         for (int i = 0; i < intArray.Length; i++)
//         {
//             // BitConverter.GetBytes(int) → byte[4] little-endian
//             // Array.Copy(..., destOffset: i*4, count: 4)
//             Array.Copy(
//               BitConverter.GetBytes(intArray[i]),
//               0,
//               data,
//               i * 4,
//               4
//             );
//         }

//         return data;
//     }

//     private static string ByteArrayToString(byte[] arrInput)
//     {
//         var sOutput = new StringBuilder(arrInput.Length * 2);

//         for (int i = 0; i < arrInput.Length; i++)
//         {
//             // "X2" → format hex cu două cifre (e.g. 0x0F devine "0F")
//             sOutput.Append(arrInput[i].ToString("X2"));
//         }

//         return sOutput.ToString();
//     }
// }
