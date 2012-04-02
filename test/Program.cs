using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LuaInterface;

namespace test
{
	public class Program
	{
		static void Main(string[] args)
		{
			Program program = new Program();
			Lua lua = new Lua();

			lua["lua"] = lua;

			Persion jack = new Persion();
			jack.lua = lua;
			lua["cs"] = jack;

			lua["v"] = 10;
			lua.DoString(@"
cs:m()
");

			Console.WriteLine();
			Console.ReadLine();
		}


		private class Persion
		{
			public string name = "Jack";
			public Lua lua;

			public void m(string m = "")
			{
				m += "OK ";
				Console.WriteLine(lua["v"]);
			}
		}
	}
}
