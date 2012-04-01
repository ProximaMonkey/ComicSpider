--[[
Comic Spider controller.
April 2012 y.s.

Most of the .NET assemblies are visible. You can control any detail behavior of the spider.
For more information please see the source code of the CSharp project:
https://github.com/ysmood/ComicSpider

External functions:
	int cs.levenshtein_distance(string s, string t):
		Get the levenshtein distance between two strings.

	void cs.find(string regex_pattern):
		CSharp Regex.Matches method. Param pattern will be automatically convert to string.

	void cs.filtrate(string regex_pattern):
		Reassign cs.html with matched string.

	string cs.match(string regex_pattern):
		Return first matched string.

	void cs.matches(string regex_pattern, function step(index, match_group, match_collection)):
		Matches all, and loop with a callback function.

External objects:
	Main_settings cs.settings:
		Main settings of Comic Spider.

	string cs.html:
		Html context of current page.

	Web_src_info cs.src_info:
		Information about current page.

	List<Web_src_info> cs.info_list:
		link information list of current page.
--]]

cs =
{
	['www.mangahere.com'] =
	{
		get_comic_name = function()
			cs.src_info.Name = cs.find([[<title>(?<find>.+) Manga - .+?</title>]])
		end,

		get_volume_list = function()
			cs.filtrate([[class="detail_list"[\s\S]+?/ul]])
			cs.matches([[<a class="color_0077" href="(?<url>.+?)".*?>(?<name>[\s\S]+?)</a>]], function() end)
		end,

		get_page_list = function()
			cs.filtrate([[change_page[\s\S]+?/select>]])
			cs.matches([[value="(?<url>.+?)"]], function() end)
		end,

		get_file_list = function()
			cs.matches([[img src="(?<url>.+?)"[\s\S]+?id="image"[\s\S]+?/>]], function() end)
		end,
	},
}
