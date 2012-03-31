/** External functions:
		int levenshtein_distance(string s, string t):
			Get the levenshtein distance between two strings.

		string matches(string str, object pattern):
			CSharp Regex.Matches method. Param pattern will be automatically convert to string.

		string tostr(string s):
			Convert a js string to CSharp string type.

		void report(string info):
			Report information to console.

		void web_src_info(string url, int index, string name, Web_src_info parent)
			create a Web_src_info object.

	External objects:
		MainSettings settings:
			Main settings of Comic Spider.

		string html:
			Html context of current page.

		Web_src_info src_info:
			Information about current page.

		List<Web_src_info> info_list:
			link information list of current page.
*/
var mangahere_com =
{
	get_comic_name: function()
	{
		src_info.Name = html.match(/<title>(.+) Manga - .+?<\/title>/)[1];
	},
	get_volume_list: function()
	{
		html = html.match(/class="detail_list"[\s\S]+?\/ul/)[0];
		var mc = matches(html, /<a class="color_0077" href="(.+?)".*?>([\s\S]+?)<\/a>/);
		for(var i = 0; i < mc.Count; i++)
		{
			info_list.Add(
				web_src_info(
					mc[i].Groups[1].Value,
					i,
					mc[i].Groups[2].Value,
					src_info
				)
			);
		}
	},
	get_page_list: function()
	{
		html = html.match(/change_page[\s\S]+?\/select>/)[0];
		var mc = matches(html, /value="(.+?)"/);
		for(var i = 0; i < mc.Count; i++)
		{
			info_list.Add(
				web_src_info(
					mc[i].Groups[1].Value,
					i,
					i + '',
					src_info
				)
			);
		}
	},
	get_file_list: function()
	{
		var url = html.match(/img src="(.+?)"[\s\S]+?id="image"[\s\S]+?\/>/)[1];
		info_list.Add(
			web_src_info(
				url,
				0,
				'',
				src_info
			)
		);
	}
};