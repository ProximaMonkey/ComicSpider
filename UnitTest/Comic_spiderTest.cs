using ys.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace UnitTest
{
	
	
	/// <summary>
	///This is adpter test class for Comic_spiderTest and is intended
	///to contain all Comic_spiderTest Unit Tests
	///</summary>
	[TestClass()]
	public class Comic_spiderTest
	{


		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in adpter class have run
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion


		/// <summary>
		///A test for Get_info_list_from_html
		///</summary>
		[TestMethod()]
		[DeploymentItem("Comic Spider.exe")]
		public void Get_info_list_from_htmlTest()
		{
			Comic_spider_Accessor target = new Comic_spider_Accessor();
			Web_src_info src_info = new Web_src_info("http://www.mangahere.com/manga/nononono/v09/c087/", 0);
			string url_pattern = @"src=""(?<url>http://c.mhcdn.net/store/manga/.+?((jpg)|(png)|(gif)|(bmp)))""";
			int expected = 1;
			List<Web_src_info> actual;
			actual = target.Get_info_list_from_html(src_info, url_pattern);
			Assert.AreEqual(expected, actual.Count);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}
	}
}
