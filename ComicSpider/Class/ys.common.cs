using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;

namespace ys
{
	public class Common
	{
		/// <summary>
		/// Function to get byte array from adpter object
		/// </summary>
		/// <param name="web_base_info">object to get byte array</param>
		/// <returns>Byte Array</returns>
		public static byte[] ObjectToByteArray(object obj)
		{
			if (obj == null) return null;

			// create new memory stream
			System.IO.MemoryStream _MemoryStream = new System.IO.MemoryStream();

			// create new BinaryFormatter
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter _BinaryFormatter
						= new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

			// Serializes an object, or graph of connected objects, to the given stream.
			_BinaryFormatter.Serialize(_MemoryStream, obj);

			// convert stream to byte array and return
			return _MemoryStream.ToArray();
		}

		/// <summary>
		/// Function to get object from byte array
		/// </summary>
		/// <param name="_ByteArray">byte array to get object</param>
		/// <returns>object</returns>
		public static object ByteArrayToObject(byte[] _ByteArray)
		{
			if (_ByteArray == null) return null;

			// convert byte array to memory stream
			System.IO.MemoryStream _MemoryStream = new System.IO.MemoryStream(_ByteArray);

			// create new BinaryFormatter
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter _BinaryFormatter
						= new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

			// set memory stream position to starting point
			_MemoryStream.Position = 0;

			// Deserializes adpter stream into an object graph and return as adpter object.
			return _BinaryFormatter.Deserialize(_MemoryStream);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static string Format_for_number_sort(string str, int length = 3)
		{
			Regex reg = new Regex(@"(?<int>\d+)(?<float>\.?\d*)");
			str = reg.Replace(str, (m) =>
				{
					int num;
					string int_part = string.Empty;
					if(int.TryParse(m.Groups["int"].Value, out num))
						int_part = string.Format("{0:D3}", num);
					string float_part = m.Groups["float"].Value;
					return int_part + float_part;
				}
			);
			return str;
		}

		public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj != null)
			{
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
				{
					DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
					if (child != null && child is T)
					{
						yield return (T)child;
					}

					foreach (T childOfChild in FindVisualChildren<T>(child))
					{
						yield return childOfChild;
					}
				}
			}
		}

		public static int LevenshteinDistance(string s, string t)
		{
			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1, m + 1];

			// Step 1
			if (n == 0)
			{
				return m;
			}

			if (m == 0)
			{
				return n;
			}

			// Step 2
			for (int i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (int j = 0; j <= m; d[0, j] = j++)
			{
			}

			// Step 3
			for (int i = 1; i <= n; i++)
			{
				//Step 4
				for (int j = 1; j <= m; j++)
				{
					// Step 5
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

					// Step 6
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}
			// Step 7
			return d[n, m];
		}
		private const int FO_DELETE = 3;
		private const int FOF_ALLOWUNDO = 0x40;
		private const int FOF_NOCONFIRMATION = 0x0010;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
		public struct SHFILEOPSTRUCT
		{
			public IntPtr hwnd;
			[MarshalAs(UnmanagedType.U4)]
			public int wFunc;
			public string pFrom;
			public string pTo;
			public short fFlags;
			[MarshalAs(UnmanagedType.Bool)]
			public bool fAnyOperationsAborted;
			public IntPtr hNameMappings;
			public string lpszProgressTitle;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

		public static void DeleteFileOperation(string filePath)
		{
			SHFILEOPSTRUCT fileop = new SHFILEOPSTRUCT();
			fileop.wFunc = FO_DELETE;
			fileop.pFrom = filePath + '\0' + '\0';
			fileop.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION;

			SHFileOperation(ref fileop);
		}

		public static string Combine_path(string root, params string[]paths)
		{
			foreach (var path in paths)
			{
				root = Path.Combine(root, path);
			}

			return root;
		}
		public static string Get_web_src_extension(string url)
		{
			int i = url.LastIndexOf('?');
			if (i >= 0)
				return Path.GetExtension(url.Remove(i));
			else
				return Path.GetExtension(url);
		}
	}
}
