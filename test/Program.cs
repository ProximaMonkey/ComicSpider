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
			//HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
			//doc.Load("test.html");
			//foreach (var item in doc.DocumentNode.SelectNodes("//a[@id='highres']"))
			//{
			//    Console.WriteLine(item.Attributes["href"].Value);
			//}

			Lua lua = new Lua();
			Regex reg = new Regex(@".+");
			lua["s"] =  reg.Match("asdfasdf").Groups;
			lua.DoString(@"
print(s[0].Value:match('a(.)d'))
");

			Console.WriteLine();
			Console.ReadLine();
		}
	}
}
