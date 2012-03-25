using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;

namespace UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			list = new PersonM();
			view.Model = list;
		}

		public PersonM list;

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			list.list[0].Age++;
		}
	}


	public class PersonM : Aga.Controls.Tree.ITreeModel
	{
		public PersonM()
		{
			ObservableCollection<Person> f = new ObservableCollection<Person>();
			for (int i = 0; i < 3; i++)
			{
				f.Add(new Person(i, "Aasdfasdf"));
			}

			for (int i = 0; i < 3; i++)
			{
				list.Add(new Person(i, "Jack") { Friends = f, Root = true });
			}
		}

		public System.Collections.IEnumerable GetChildren(object parent)
		{
			if (parent == null)
				return list;
			else
				return (parent as Person).Friends;
		}

		public bool HasChildren(object parent)
		{
			return (parent as Person).Friends.Count != 0;
		}

		public ObservableCollection<Person> list = new ObservableCollection<Person>();
	}

	public class Person : INotifyPropertyChanged
	{
		public Person()
		{
		}

		public Person(int age, string name)
		{
			this.age = age;
			Name = name;
			Friends = new ObservableCollection<Person>();
		}

		public int Age
		{
			get { return age; }
			set
			{
				age = value;
				NotifyPropertyChanged("Age");
			}
		}
		public string Name { get; set; }
		public bool Root = false;

		public ObservableCollection<Person> Friends { get; set; }

		private int age;

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
	}


}
