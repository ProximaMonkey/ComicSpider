using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	}
}
