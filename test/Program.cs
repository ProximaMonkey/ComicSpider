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

			List<string> list = new List<string>();
			list.Add("a");
			lua["list"] = new List<string>();
			lua.RegisterFunction("foo", program, program.GetType().GetMethod("foo"));
			lua.DoString(@"
foo({'1','2',3})
");


			Console.WriteLine();
			Console.ReadLine();
		}

		public void foo(LuaTable t)
		{
			foreach (var item in t.Values)
			{
				Console.WriteLine(item);
			}
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
