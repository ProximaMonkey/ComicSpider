using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Linq;

namespace ys.Web
{
	public class Web_src_info : System.ComponentModel.INotifyPropertyChanged
	{
		public Web_src_info()
		{
			Url = string.Empty;
			Index = 0;
			Name = string.Empty;
			state = string.Empty;
			Cookie = string.Empty;
		}
		public Web_src_info(
			string url,
			int index,
			string name,
			Web_src_info parent = null)
		{
			Url = url;
			Index = index;
			Name = name;
			Parent = parent;
			state = "";
			Cookie = "";
		}

		public const string State_downloaded = "OK";
		public const string State_missed = "X";

		public string Url
		{
			get { return url; }
			private set
			{
				url = value;
				NotifyPropertyChanged("Url");
			}
		}
		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				NotifyPropertyChanged("Name");
			}
		}
		public string State
		{
			get
			{
				return state;
			}
			set
			{
				state = value;
				NotifyPropertyChanged("State");
			}
		}
		public List<Web_src_info> Children
		{
			get { return children; }
			set
			{
				children = value;
				NotifyPropertyChanged("Children");
			}
		}
		public int Index { get; set; }
		public string Cookie { get; set; }
		public Web_src_info Parent { get; protected set; }

		public int Count
		{
			get
			{
				if (children == null)
					return 0;
				else
					return children.Count;
			}
		}
		public int Downloaded
		{
			get
			{
				return children.Count(c => c.State == Web_src_info.State_downloaded);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public class Comparer : IEqualityComparer<Web_src_info>
		{
			public bool Equals(Web_src_info x, Web_src_info y)
			{
				if (x.Name == y.Name)
				{
					y.Name = x.Name + "'";
				}
				return x.Url == y.Url;
			}
			public int GetHashCode(Web_src_info src_info)
			{
				if (Object.ReferenceEquals(src_info, null)) return 0;

				return src_info.Url.GetHashCode();
			}
		}

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		private string url;
		private string name;
		private string state;
		private List<Web_src_info> children;
	}

	public class Website_info
	{
		public Website_info(string name, string home)
		{
			Name = name;
			Home = home;
		}

		public string Name { get; set; }
		public string Home { get; set; }
	}
}
