using ys;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTest
{
	
	
	/// <summary>
	///This is a test class for CommonTest and is intended
	///to contain all CommonTest Unit Tests
	///</summary>
	[TestClass()]
	public class CommonTest
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
		//Use ClassCleanup to run code after all tests in a class have run
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
		///A test for Format_for_number_sort
		///</summary>
		[TestMethod()]
		public void Format_for_number_sortTest()
		{
			string str = "asdf 01.12 sdf"; // TODO: Initialize to an appropriate value
			int length = 3; // TODO: Initialize to an appropriate value
			string expected = "asdf 001.12 sdf"; // TODO: Initialize to an appropriate value
			string actual;
			actual = Common.Format_for_number_sort(str, length);
			Assert.AreEqual(expected, actual);
		}
	}
}
