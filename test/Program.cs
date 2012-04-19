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
			Console.WriteLine(string.Format("{0:000.0}", 45.123));
			Console.ReadLine();
		}
	}
}
