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
			//File.Copy(@"Asset\layout.js", Path.Combine(parent_dir, "layout.js"), true);
			File.Copy(@"C:\Cradle\CSharp\ComicSpider\ComicSpider\Asset\layout.js", @"Z:\CAPSULE\ACG\Comic\里香\layout.js", true);

			Console.WriteLine();
			Console.ReadLine();
		}
	}
}
