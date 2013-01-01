using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LuaInterface;
using System.IO;

namespace test
{
	public class Program
	{
		static void Main(string[] args)
		{
			run_js("[1,'2',3]");
		}

		public static string run_js(string s)
		{
			Noesis.Javascript.JavascriptContext context = new Noesis.Javascript.JavascriptContext();
			var ret = context.Run(s);
			return ret.ToString();
		}
	}
}
