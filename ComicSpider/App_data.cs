namespace ComicSpider
{


	public partial class App_data
	{
		public static void CheckAndFix()
		{
			App_dataTableAdapters.Key_valueTableAdapter da = new App_dataTableAdapters.Key_valueTableAdapter();

			da.Adapter.SelectCommand = da.Connection.CreateCommand();
			da.Adapter.SelectCommand.CommandText =
@"PRAGMA foreign_keys = OFF;

create table if not exists [Key_value] (
	[Key] nvarchar(50) PRIMARY KEY NOT NULL,
	[Value] image
);
create table if not exists [Error_log] (
	[Date_time] datetime NOT NULL,
	[Url] nvarchar(200) PRIMARY KEY NOT NULL,
	[Title] nvarchar(100) NOT NULL,
	[Detail] text
);
create table if not exists [Vol_info] (
	[Url] nvarchar(200) PRIMARY KEY NOT NULL,
	[Name] nvarchar(100),
	[State] nvarchar(50),
	[Parent_url] nvarchar(200),
	[Parent_name] nvarchar(100),
	[Index] int,
	[Cookie] text,
	[Data_time] datetime NOT NULL
);
create table if not exists [Page_info] (
	[Url] nvarchar(200) PRIMARY KEY NOT NULL,
	[Name] nvarchar(100),
	[State] nvarchar(50),
	[Parent_url] nvarchar(200),
	[Parent_name] nvarchar(100),
	[Index] int,
	[Cookie] text,
	[Data_time] datetime NOT NULL
);
insert or ignore into [Key_value] ([Key], [Value]) values ('Settings', NULL);";
			da.Connection.Open();
			da.Adapter.SelectCommand.ExecuteNonQuery();
			da.Connection.Close();
		}
	}
}
