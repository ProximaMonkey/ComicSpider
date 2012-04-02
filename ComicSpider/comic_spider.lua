--[[
Comic Spider controller.
April 2012 y.s.

Most of the .NET assemblies are visible. You can control any detail behavior of the spider.
For more information please see the source code of the CSharp project:
https://github.com/ysmood/ComicSpider

External functions:
	int levenshtein_distance(string s, string t):
		Get the levenshtein distance between two strings.

	object json_decode(string s):
		Decode json string.

	string lc:find(string regex_pattern):
		CSharp Regex.fill_list method. Param pattern will be automatically convert to string.
		Return the match group which is named 'find'.

	void lc:filtrate(string regex_pattern):
		Reassign html with matched string.

	void lc:fill_list(string regex_pattern, function step(int index, GroupCollection gs, MatchCollection mc)):
		Matches all, fill info_list with matched url and name, and loop with a callback function.
		Url match group should named with 'url'.
		Name match group should named with 'name'.
		Varialble url and name are visible in step function.

	void lc:fill_list(JsonArray arr, function step(int index, string str, JsonArray arr)):
		Fill info_list with an array, and loop with a callback function.
		Varialble url and name are visible in step function.

External objects:
	Main_settings settings:
		Main settings of Comic Spider.

	string html:
		Html context of current page.

	Web_src_info src_info:
		Information about current page.

	List<Web_src_info> info_list:
		link information list of current page.
--]]

comic_spider =
{
	file_types = { ".jpg", ".png", ".bmp", ".gif" },

	['default'] =
	{
		charset = 'utf-8',

		get_comic_name = function()
		end,

		get_volume_list = function()
		end,

		get_page_list = function()
		end,

		get_file_list = function()
		end,
	},

	['mangahere.com'] =
	{
		get_comic_name = function()
			src_info.Name = lc:find([[<title>(?<find>.+) Manga - .+?</title>]])
		end,

		get_volume_list = function()
			lc:filtrate([[class="detail_list"[\s\S]+?/ul]])
			lc:fill_list([[<a class="color_0077" href="(?<url>.+?)".*?>(?<name>[\s\S]+?)</a>]])
		end,

		get_page_list = function()
			lc:filtrate([[change_page[\s\S]+?/select>]])
			lc:fill_list([[value="(?<url>.+?)"]])
		end,

		get_file_list = function()
			lc:fill_list([[img src="(?<url>.+?)"[\s\S]+?id="image"[\s\S]+?/>]])
		end,
	},

	['178.com'] =
	{
		get_comic_name = function()
			src_info.Name = lc:find([[<title>(?<find>.+?)-]])
		end,

		get_volume_list = function()
			lc:filtrate([[<div class="cartoon_online_border"[\s\S]+?<div]])
			lc:fill_list([[href="(?<url>.+?)".*?>(?<name>.+?)</a>]])
		end,

		get_page_list = function()
			-- 站点显式使用了负载均衡，利用这点。
			img_hosts = { 'imgd', 'img' }
			-- 此站点使用了ajax，利用这点可以直接在第一页获取所有文件地址。
			list = lc:find([[var pages = '(?<find>.+?)';]])
			lc:fill_list(
				lc:json_decode(list),
				function(i, str, arr)
					n = math.random(#img_hosts)
					url = 'http://' .. img_hosts[n] .. '.manhua.178.com/' .. str
				end
			)
		end,
	},

}
