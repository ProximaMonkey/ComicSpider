/** Javascript doc
	Comic layout controller
	April 2012 y.s.
*/

// Default settings
var is_auto_split_page = true;
var img_load_step = 1;

/**************** Main *******************/

var win = $(window);
var doc;
var volume_list = [];

win.load(function()
{
	if ($.browser.webkit)
		doc = $('body');
	else
		doc = $('html');

	append_img(2);

	init_css();
	init_navigator();
	init_page_waterfall();
});

/**************** Subfunction *******************/

function init_page_waterfall()
{
	win.scroll(function()
	{
		var top = last_page.offset().top - win.height() - doc.scrollTop();
		if(top < last_page.height() * 0.5)
			append_img(img_load_step);
	});
}

function init_css()
{
	if($.browser.msie && $.browser.version < 9)
	{
		$('html').addClass('ie-shadow-fix');
	}

	$('#navibar').hover(
		function ()
		{
			$(this).stop().animate({ right: 5 }, 'fast');
		},
		function ()
		{
			$(this).stop().animate({ right: -380 });
		}
	);
	
	win.one('scroll', function()
	{
		$('#navibar').animate({ right: -380 });
	});
}

function init_navigator()
{
	$('.btn.auto-split').click(function(){
		$this = $(this);
		if(is_auto_split_page)
		{
			is_auto_split_page = false;
			$this.text('on');
			$('.btn.full-page').click();
		}
		else
		{
			is_auto_split_page = true;
			$this.text('off');
			$('.btn.split-page').click();
		}
	});

	$('.btn.effect').click(function(){
		$.fx.off = !$.fx.off;
		if($.fx.off)
			$(this).text('on');
		else
			$(this).text('off');
	});

	var path = decodeURIComponent(location);
	volume_list.alphanumSort(false);
	var i  = 0;
	for (; i < volume_list.length; i++)
	{
		if(path.indexOf(volume_list[i]) >= 0)
			break;
	}

	$('.btn.previous').attr('href',
		i > 0 ? volume_list[i - 1] : 'javascript:alert("This is the first volume.")'
	);
	$('.btn.next').attr('href',
		i < volume_list.length - 1 ? volume_list[i + 1] : 'javascript:alert("This is the last volume.")'
	);
}

var img_count = 0;
var last_page = null;
function append_img(count)
{
	for(var i = 0; (i < count) && (img_count < img_list.length); i++)
	{
		var frame = $(
			'<div class="img_frame">' +
				'<div><span class="page_num">' + (img_count + 1) + ' / ' + img_list.length + '</span></div>' +
			'</div>'
		);

		var page = $('<img class="page" src="' + img_list[img_count] + '"/>');

		if(is_auto_split_page)
			page.load(auto_split_page);
		else
			page.load(auto_resize);

		frame.append(page);

		$('#container').append(frame).append('<hr />');

		img_count++;

		last_page = page;
	}
}

function auto_split_page(page)
{
	if(!(page instanceof $))
		page = $(this);

	var window_width = win.width();
	var width;

	if (page.attr('original_width'))
	{
		width = parseInt(page.attr('original_width'), 10);
		page.width(width);
	}
	else
		width = page.width();

	if (width > window_width)
	{
		if (width > page.height())
		{
			var frame = page.parent();

			var new_frame = $('<div class="img_frame">' + frame.html() + '</div>');
			var new_page = new_frame.find('.page');

			frame.css(
				{
					width: width / 2,
					overflow: 'hidden'
				}
			);
			new_frame.css(
				{
					width: width / 2,
					overflow: 'hidden'
				}
			);

			var page_num = frame.find('.page_num');

			var span_right = $('<span> - right - </span>');
			var span_left = $('<span> - left - </span>');
			var span_original = $('<span> - original - </span>');

			page_num.append(span_right);

			var btn_full_page = $('<a class="btn full-page" href="#!" title="view full page">full</a>');
			var btn_split_page = $('<a class="btn split-page" href="#!" title="auto split page">split</a>');
			btn_split_page.click(function ()
			{
				span_original.remove();
				btn_split_page.remove();
				new_frame.find('.resize').remove();
				auto_split_page(new_page);
			});
			btn_full_page.click(function (e)
			{
				frame.remove();
				new_frame.removeAttr('style');
				span_left.remove();

				page_num.append(span_original);
				page_num.append(btn_split_page);
				auto_resize(new_page);
			});


			page_num.append(btn_full_page);

			page_num = new_frame.find('.page_num');
			page_num.append(span_left);

			frame.after(new_frame);
			page.css(
				{
					position: 'relative',
					left: -width / 2
				}
			);

			page_control(new_page);
		}
		else
		{
			auto_resize(page);
		}
	}

	page_control(page);
}

function auto_resize(page)
{
	if(!(page instanceof $))
		page = $(this);

	var window_width = win.width();

	if(page.width() < window_width) return;

	var frame = page.parent();
	var page_num = frame.find('.page_num');
	var span_fit = $('<span class="resize"> - fit width - </span>');
	var span_original = $('<span class="resize"> - original - </span>');

	page_num.append(span_fit);

	var btn_origin = $('<a class="resize" href="#!" title="view original page">original</a>');
	var btn_resize = $('<a class="resize" href="#!" title="resize page to fit window">resize</a>');
	btn_resize.click(function ()
	{
		span_original.remove();
		btn_resize.remove();
		auto_resize(page);
	});

	btn_origin.click(function (e)
	{
		page.width(page.attr('original_width'));
		btn_origin.remove();
		span_fit.remove();

		page_num.append(span_original);
		page_num.append(btn_resize);
	});
	page_num.append(btn_origin);

	page.attr('original_width', page.width()).width(window_width - 40);

	if(!is_auto_split_page)
		page_control(page);
}

var is_page_draging = false;
var page_pos;
var page_scroll = { x: 0, y: 0 };
var page_distance = 0;
var ui_control_inited = false;
function page_control(page)
{
	page.css('cursor', 'move');

	page.mousedown(
		function (e)
		{
			if (e.button == 2)
			{
				return;
			}

			page_scroll.x = doc.scrollLeft();
			page_scroll.y = doc.scrollTop();

			page_pos = e;

			is_page_draging = true;

			if (!$.browser.msie)
				e.preventDefault();
		}
	);
	
	if(!ui_control_inited) doc.mousemove(
		function (e)
		{
			if (!is_page_draging)
				return;

			doc.scrollTop(doc.scrollTop() + page_pos.pageY - e.pageY);
			doc.scrollLeft(doc.scrollLeft() + page_pos.pageX - e.pageX);

			e.preventDefault();
		}
	);

	page.mouseup(
		function (e)
		{
			is_page_draging = false;

			page_distance = 0;
			try
			{
				page_distance = Math.pow(page_scroll.x - doc.scrollLeft(), 2) + Math.pow(page_scroll.y - doc.scrollTop(), 2);
			}
			catch (ex) { }

			if (isNaN(page_distance) || page_distance < 9)
			{
				$this = $(this);
				var bottom = $this.height() + $this.offset().top - win.height() - doc.scrollTop();
				if (bottom > 0)
				{
					doc.stop().animate({ scrollTop: doc.scrollTop() + bottom + 20 });
				}
				else
				{
					var top = bottom + win.height();
					doc.stop().animate({ scrollTop: doc.scrollTop() + top + 50 });
				}
			}
		}
	);

	if(!ui_control_inited) doc.keyup(
		function (e)
		{
			if(e.ctrlKey)
				return;

			switch (e.keyCode)
			{
				// Q previous volume
				case 81:
					location.href = $('.btn.previous').attr('href');
					break;

				// W next volume
				case 87:
					location.href = $('.btn.next').attr('href');
					break;

				// A auto split switch
				case 65:
					$('.btn.auto-split').click();
					break;

				// S animation switch
				case 83:
					$('.btn.effect').click();
					break;

				// previous page
				case 90:
					var distance = 100000;
					var img_frame;
					var frames = $('.img_frame');
					for(var i = 0; i < frames.length; i++)
					{
						$this = $(frames[i]);
						var d = Math.abs($this.offset().top - doc.scrollTop());
						if(d < distance)
						{
							distance = d;
							img_frame = $this;
						}
						else
						{
							var top = doc.scrollTop() - img_frame.offset().top;
							if (top > 0)
							{
								doc.stop().animate({ scrollTop: doc.scrollTop() - top - 10 });
							}
							else
							{
								var bottom = win.height() + top;
								doc.stop().animate({ scrollTop: doc.scrollTop() - bottom - 40 });
							}
							break;
						}
					}
					break;

				// next page
				case 88:
					var distance = 100000;
					var img_frame;
					var frames = $('.img_frame');
					for(var i = 0; i < frames.length; i++)
					{
						$this = $(frames[i]);
						var d = Math.abs($this.offset().top - doc.scrollTop());
						if(d < distance)
						{
							distance = d;
							img_frame = $this;
						}
						else
						{
							var bottom = img_frame.height() + img_frame.offset().top - win.height() - doc.scrollTop();
							if (bottom > 0)
							{
								doc.stop().animate({ scrollTop: doc.scrollTop() + bottom + 20 });
							}
							else
							{
								var top = bottom + win.height();
								doc.stop().animate({ scrollTop: doc.scrollTop() + top + 50 });
							}
							break;
						}
					}
					break;
			}
		}
	);

	ui_control_inited = true;
}
