using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ComicSpider;
using ys.Web;
using ys;
using Jint;
using LuaInterface;

namespace test
{
	class Program
	{
		static void Main(string[] args)
		{
			Run r = new Run();
		}

		public class Run
		{
			public Run()
			{
				Lua lua = new Lua();
				var ret = lua.DoString(@"
a = 10
a");
				Console.WriteLine(ret[0]);
				Console.ReadLine();
			}
		}
	}
}
