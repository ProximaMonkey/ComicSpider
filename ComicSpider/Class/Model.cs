using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace ys.Web
{
	public class Web_src_info : System.ComponentModel.INotifyPropertyChanged
	{
		public Web_src_info()
		{
		}
		public Web_src_info(string url,
			int index,
			string state = "",
			Counter count = null,
			string cookie = "",
			string name = "",
			Web_src_info parent = null,
			List<Web_src_info> children = null)
		{
			Url = url;
			Index = index;
			State = state;
			Counter = count;
			Name = name;
			Cookie = cookie;
			Parent = parent;
			Children = children;
			Missed = false;
		}

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
			get { return state; }
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
		public Counter Counter;
		public int Index { get; set; }
		public string Cookie { get; set; }
		public bool Missed { get; set; }
		public Web_src_info Parent { get; protected set; }

		public event PropertyChangedEventHandler PropertyChanged;

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

	public class Counter
	{
		public Counter(int count)
		{
			All = count;
			Downloaded = 0;
		}
		public int Increment()
		{
			return Interlocked.Increment(ref downloaded);
		}
		public void Increase_all()
		{
			   All++;
		}
		public void Reset()
		{
			Downloaded = 0;
		}
		public int All { get; private set; }
		public int Downloaded
		{
			get { return downloaded; }
			set { downloaded = value; }
		}

		private int downloaded;
	}
}
