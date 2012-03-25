using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace ys.Web
{
	public class Vol_info_model : Aga.Controls.Tree.ITreeModel
	{
		public Vol_info_model(ObservableCollection<Web_page_info> vol_info_list)
		{
			Vol_info_list = vol_info_list;
		}

		public System.Collections.IEnumerable GetChildren(object parent)
		{
			if (parent == null)
				return Vol_info_list;
			else
				return (parent as Web_page_info).Children;
		}

		public bool HasChildren(object parent)
		{
			var children = (parent as Web_page_info).Children;
			return children != null && children.Count != 0;
		}

		public ObservableCollection<Web_page_info> Vol_info_list { get; private set; }
	}


	public class Web_page_info : Web_file_info, System.ComponentModel.INotifyPropertyChanged
	{
		public Web_page_info()
		{
		}
		public Web_page_info(string url,
			int index,
			Counter count = null,
			string cookie = "",
			string name = null,
			Web_page_info parent = null,
			ObservableCollection<Web_page_info> children = null)
		{
			Url = url;
			Index = index;
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
			private set
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
		public ObservableCollection<Web_page_info> Children { get; private set; }

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
	}

	[Serializable]
	public class Web_file_info
	{
		public Web_file_info()
		{
		}
		public Web_file_info(string url,
			int index,
			Counter count = null,
			string cookie = "",
			string name = null,
			Web_page_info parent = null)
		{
			Url = url;
			Index = index;
			Counter = count;
			Name = name;
			Cookie = cookie;
			Parent = parent;
			Missed = false;
		}

		public string Url { get; protected set; }
		public string Name { get; protected set; }
		public string State { get; set; }
		public int Index { get; protected set; }
		public Counter Counter;
		public string Cookie { get; protected set; }
		public bool Missed { get; set; }
		public Web_page_info Parent { get; protected set; }
	}

	[Serializable]
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
		public int All { get; private set; }
		public int Downloaded
		{
			get { return downloaded; }
			set { downloaded = value; }
		}

		private int downloaded;
	}
}
