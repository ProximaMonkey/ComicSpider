using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ys;
using System.Threading;
using System.Collections.ObjectModel;

namespace ComicSpider
{
	class Task_manager
	{
		public Task_manager()
		{
			Root.Children = Volumes;
		}

		public readonly Web_resource_info Root = new Web_resource_info("web", 0, "web", "", null);

		public readonly List<Web_resource_info> Volumes = new List<Web_resource_info>();

		public Web_resource_info Volumes_dequeue()
		{
			lock (volume_lock)
			{
				return Dequeue(Root);
			}
		}
		public Web_resource_info Pages_dequeue()
		{
			lock (page_lock)
			{
				var volume = Peek_downloading(Root);
				return Dequeue(volume);
			}
		}
		public Web_resource_info Files_dequeue()
		{
			lock (file_lock)
			{
				var volume = Peek_downloading(Root);
				var page = Peek_downloading(volume);
				return Dequeue(page);
			}
		}

		/***************************** Private ********************************/

		private object volume_lock = new object();
		private object page_lock = new object();
		private object file_lock = new object();

		private Web_resource_info Dequeue(Web_resource_info parent)
		{
			var item = Peek(parent);

			if (item == null) return null;

			item.State = Web_resource_state.Downloading;
			return item;
		}

		private Web_resource_info Peek(Web_resource_info parent)
		{
			if (parent == null || parent.Count == 0) return null;

			try
			{
				return parent.Children.First(v =>
				{
					return (v.State == Web_resource_state.Wait) ||
						(v.State == Web_resource_state.Failed);
				});
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		private Web_resource_info Peek_downloading(Web_resource_info parent)
		{
			if (parent == null || parent.Count == 0) return null;

			try
			{
				return parent.Children.First(v => v.State == Web_resource_state.Downloading);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}
	}
}
