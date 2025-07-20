// MIT License
// 
// Copyright (c) 2020 Sunny Valley Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// Modified by: Anghel Ana-Maria, iulie 2025 

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
