--[[
Comic Spider controller
April 2012 y.s.

This doc should be utf-8 encoded only.

Most of the .NET assemblies are visible. You can control most behaviors of the spider.
For more information please see the source code of the CSharp project:
https://github.com/ysmood/ComicSpider
Be careful when you use functions out of the list below,
it will be hard to debug without the Visual Studio :)

******************************************************************************************

External functions:
	int levenshtein_distance(string s, string t):
		Get the levenshtein distance between two strings.

	object json_decode(string s):
		Decode json string.

	string lc:find(string regex_pattern):
		CSharp Regex.fill_list method. Param pattern will be automatically convert to string.
		Return the match group which is named 'find'.

	void lc:fill_list(string regex_pattern, function step(int index, GroupCollection gs, MatchCollection mc) = null):
	void lc:fill_list(LuaTable regex_patterns, function step(int index, GroupCollection gs, MatchCollection mc) = null):
		Matches all, fill info_list with matched url and name, and loop with a callback function.
		Except the last pattern in the table, others are all use to select contents.
		Url match group should named with 'url'.
		Name match group should named with 'name'.
		Variable url and name are visible in step function.

	void lc:fill_list(JsonArray arr, function step(int index, string str, JsonArray arr)):
		No default value for step function.
		Fill info_list with an array, and loop with a callback function.
		Variable url and name are visible in step function.

	void lc:xfill_list(selector, function step(int index, HtmlNode node, HtmlNodeCollection nodes)):
		No default value for step function.
		Fill info_list via XPath selector. For more information, search HtmlAgilityPack.
		Should manually set the url of src_info.

	void lc:add(string url, int index, string name, Web_src_info parent = null):
		Add a new Web_src_info instance to info_list.

******************************************************************************************

External objects:
	Lua_controller lc:
		Current lua controller.

	Dashboard lc.main:
		The main window.

	Dashboard lc.dashboard:
		The main dashboard window.

	Main_settings lc.settings:
		Main settings of Comic Spider.

	Only visible in 'get_volumes', 'get_pages', 'get_files'.
		string html:
			Html context of current page. 

		Web_src_info src_info:
			Information about current page.

		List<Web_src_info> info_list:
			Link information list of current page.

	Only visible in 'handle_file'.
		string dir:
			Current directory path.
		string name:
			Current file name.
		string ext:
			Current file extension.
]]

settings = {
	-- Url list for including remote lua scripts. Be careful, it may be dangerous to use remote script.
	requires = { 'https://raw.github.com/ysmood/ComicSpider/master/comic_spider.lua' },

	-- File type to be downloaded.
	file_types = { '.jpg', '.jpeg', '.png', '.gif', '.bmp', '.zip' },

	-- Http request User-Agent header list. Fake your info here. It will randomly choose one of them.
	user_agents = { 'Mozilla/5.0 (Windows NT 6.1; rv:10.0.2) Gecko/20100101 Firefox/10.0.2' },

	proxy = '',

	-- Default script editor for this lua script.
	script_editor = [[notepad.exe]],
}

comic_spider = {
	--[[ Default behaviors here. Name is long, but meaningful :)
	['default'] = {
		home = '',

		hosts = { '' },

		charset = 'utf-8',

		is_create_view_page = true,

		indexed_file_name = true,

		get_volumes = function()
			src_info.Name = ''
		end,

		get_pages = function()
		end,

		get_files = function()
		end,

		handle_file = function()
		end,
	},
	]]

	-- Project home
	['-- Comic Spider Project --'] = {
		home = 'https://github.com/ysmood/ComicSpider',
	},

	-- A sample english manga site. You can follow code below to parse another site.
	['* Manga Here'] = {
		home = 'http://www.mangahere.com/',

		hosts = { 'mangahere.com', 'mhcdn.net' },

		get_volumes = function()
			-- First get comic's main name.
			src_info.Name = lc:find([[<title>(?<find>.+) Manga - .+?</title>]])
			-- Get volume list.
			lc:fill_list({
				[[class="detail_list"[\s\S]+?/ul]],
				[[<a class="color_0077" href="(?<url>.+?)".*?>(?<name>[\s\S]+?)</a>]]
			})
			-- If can't find volume list, then treat it as a volume page, not the index of the comic.
			if info_list.Count == 0 then
				src_info.Name = lc:find([[class="readpage_top"[\s\S]+?>(?<find>[^>]+) Manga</a>]])
				vol_name = lc:find([[<title>(?<find>.+?) - Read]])
				lc:add(src_info.Url, 0, vol_name, src_info)
			end
			info_list:Reverse()
		end,

		get_pages = function()
			html = lc:find([[change_page(?<find>[\s\S]+?)/select>]])
			lc:fill_list([[value="(?<url>.+?)"]])
		end,

		get_files = function()
			lc:fill_list([[img src="(?<url>.+?)"[\s\S]+?id="image"[\s\S]+?/>]])
		end,
	},

	-- 这是个具有代表意义的中文漫画站点。以下为示例(事件驱动)：
	['* 178 漫画频道'] = {
		home = 'http://manhua.178.com/',

		hosts = { '178.com' },

		get_volumes = function()
			-- 首先获取漫画名
			src_info.Name = lc:find([[var g_comic_name = "(?<find>.+?)"]])
			-- 获取卷列表
			lc:fill_list({
				[[<div class="cartoon_online_border"[\s\S]+?<div]],
				[[href="(?<url>.+?)".*?>(?<name>.+?)</a>]]
			})
			-- 如果没有发现列表，则认为这个url指向的是卷地址，而不是主目录地址。
			if info_list.Count == 0 then
				vol_name = lc:find([[var g_chapter_name = "(?<find>.+?)"]])
				lc:add(src_info.Url, 0, vol_name, src_info)
			end
		end,

		get_pages = function()
			-- 站点显式使用了负载均衡，利用这点。
			img_hosts = { 'imgd', 'img', 'imgfast' }
			-- 此站点使用了ajax，利用这点可以直接在第一页获取所有文件地址。
			list = lc:find([[var pages = '(?<find>.+?)']])
			lc:fill_list(
				lc:json_decode(list),
				-- 这里演示了step的应用，类似jQuery中animate的step函数。注意变量url和name是引用。
				function(i, str, arr)
					n = math.random(#img_hosts)
					url = 'http://' .. img_hosts[n] .. '.manhua.178.com/' .. str
				end
			)
		end,
	},

	['* Pixiv'] = {
		home = 'http://www.pixiv.net',

		hosts = { 'pixiv.net' },

		is_create_view_page = false,
		indexed_file_name = false,

		get_volumes = function()
			src_info.Name = 'Pixiv'
			lc:fill_list(
				[[member_illust.php\?mode=medium&illust_id=\d+]],
				function(i, gs, mc)
					url = 'http://www.pixiv.net/' .. gs[0].Value
				end
			)
		end,

		get_files = function()
			lc:xfill_list(
				"//div[@class=works_display]/a/img",
				function(i, node, nodes)
					name = node.Attributes["alt"].Value
					url = node.Attributes["src"].Value.Replace('_m.', '.')
				end
			)
		end,
	},

	-- Example for Danbooru like none comic websites.
	['* Moe imouto'] = {
		home = 'https://yande.re/post?tags=rating%3Asafe',

		hosts = { 'yande.re', 'konachan.com', 'donmai.us', 'behoimi.org', 'nekobooru.net', 'sankakucomplex.com', 'sankakustatic.com' },

		is_create_view_page  = false,
		is_indexed_file_name = false,

		-- Example for usage of XPath. Slower but easier than regex.
		get_volumes = function(self)
			src_info.Name = src_info.Url:find('yande.re') and 'Moe imouto' or 'Danbooru sites'
			one_page = src_info.Url:find('/post/show')
			lc:fill_list(
				[[\{[^\}]+?file_url[^}]+?\}]],
				function(i, gs, mc)
					if one_page and info_list.Count > 0 then return end
					url = gs[0].Value:match([["file_url":"(.-)"]])
					r = gs[0].Value:match([["rating":"(.)"]])
					if r == 's' then
						name = 'Safe'
					elseif r == 'q' then
						name = 'Questionable'
					else
						name = 'Explicit'
					end
				end
			)
		end,
	},
}

-- Example: clone Danbooru websites.
for n, h in pairs {
	['Konachan']               = 'http://konachan.com'           ,
	['Donmai']                 = 'http://donmai.us'              ,
	['Behoimi']                = 'http://behoimi.org'            ,
	['Nekobooru']              = 'http://nekobooru.net'          ,
	['Sankakucomplex Idol']    = 'http://idol.sankakucomplex.com',
	['Sankakucomplex Channel'] = 'http://chan.sankakucomplex.com',
} do
	comic_spider[n] = { home = h .. '/post?tags=rating%3Asafe' }
end
