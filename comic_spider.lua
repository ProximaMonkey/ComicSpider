--[[
Comic Spider server side controller
April 2012 y.s.
]]

app_info =
{
	version = '1.0.0.1',
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
