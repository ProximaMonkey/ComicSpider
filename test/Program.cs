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
			lua.DoString(@"cs = {}");

			lua["this"] = program;
			lua["cs.html"] = "asl12dfsdfasdf";

			Func<string, string> match = new Func<string, string>((pattern) =>
			{
				return Regex.Match(lua.GetString("cs.html"), pattern).Groups[0].Value;
			});
			lua.RegisterFunction("cs.match", match.Target, match.Method);


			lua.DoString(@"
cs.html = 'ssdf55sf'
print(cs.match([[\d\d]]))
");

			Console.WriteLine();
			Console.ReadLine();
		}

		public void run(string name)
		{
		}

		public void run(string name, LuaFunction step = null)
		{
			for (int i = 0; i < 10; i++)
			{
				step.Call(name, i);
			}
		}
	}
}
