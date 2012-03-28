using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace test
{
	class Program
	{
		static void Main(string[] args)
		{
			string str = "111abcabd111";
			Regex reg = new Regex(@"(a.)");
			Match mc = reg.Match(str);
			Console.WriteLine(mc.Groups["test"]);
		}
	}
}
