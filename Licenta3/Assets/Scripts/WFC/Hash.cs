using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public static class Hash
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
