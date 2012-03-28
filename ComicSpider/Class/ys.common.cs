using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

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
					string int_part = string.Format("{0:D3}", int.Parse(m.Groups["int"].Value));
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
	}
}
