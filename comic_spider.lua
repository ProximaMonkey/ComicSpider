--[[
Comic Spider server side controller
April 2012 y.s.
]]

app_info =
{
	version = '1.0.0.3',
	notice = 'A newer version of Comic Spider has been found, click here to download.',
	url = 'https://github.com/downloads/ysmood/ComicSpider/Comic_Spider.zip'
}

comic_spider['* Manga Here'] = {
	home = 'http://www.mangahere.com/',

	hosts = { 'mangahere.com', 'mhcdn.net' },

	get_volumes = function()
		-- First get comic's main name.
		src_info.Name = lc:find([[<title>(?<find>.+) Manga - .+?</title>]])
		-- Get volume list.
		lc:fill_list(
			{
				[[class="detail_list"[\s\S]+?/ul]],
				[[<a class="color_0077" href="(?<url>.+?)".*?>(?<name>[\s\S]+?)</a>]]
			}
		)
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
}

comic_spider['* 新动漫'] = {
	home = 'http://www.xindm.cn/',

	hosts = { 'xindm.cn' },

	charset = 'gb2312',

	get_volumes = function()
		src_info.Name = lc:find([[<title>.+? >> (?<find>.+?)\[.+?</title>]])
		lc:xfill_list(
			"//table/tr/td/tr/td/a[1]",
			function(i, node)
				url = node.Attributes['href'].Value
				name = node.InnerText
			end
		)
		if info_list.Count == 0 then
			src_info.Name = lc:find([[<title>.+? >> .*? >> .*? >> (?<find>.+?)\[.+? >> .+?</title>]])
			vol_name = lc:find([[<title>.+? >> .*? >> .*? >> .+? >> (?<find>.+?)</title>]])
			lc:add(src_info.Url, 0, vol_name, src_info)
		end
		info_list:Reverse()
	end,

	get_pages = function()
		lc:xfill_list(
			"//select[@name='page']/option",
			function(i, node)
				url = src_info.Url .. '&page=' .. node.Attributes['value'].Value
			end
		)
	end,

	get_files = function()
		img_hosts = { 'mh', 'mh2' }
		lc:fill_list(
			[[id="next".+?src="\n?(?<url>../book.+?)".+?</]],
			function(i, gs)
				n = math.random(#img_hosts)
				url = 'http://' .. img_hosts[n] .. '.xindm.cn' .. gs['url'].Value:sub(3)
			end
		)
	end,
}
