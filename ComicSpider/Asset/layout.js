/** Javascript doc
	Comic layout controller
	April 2012 y.s.
*/
// Default settings
var effect_on = false;
var resize = true;

/**************** Main *******************/

$(window).load(function ()
{
	init_css();
	init_animation();
	init_ui_control();
	$(window).scroll();
	init_navigator();
}
);

/**************** Subfunction *******************/

function init_css()
{
	$('#navibar').css('right', -310).hover(
		function ()
		{
			$(this).stop().animate({ right: 5 });
		},
		function ()
		{
			$(this).stop().animate({ right: -310 });
		}
	);

	var btn_page_mode = $('.page-mode');
	var btn_auto_size = $('.auto-size');
	var window_width = $(window).width();

	btn_page_mode.click(function ()
	{
		location.hash = btn_page_mode.attr('href');
		location.reload();
	});

	btn_auto_size.click(function ()
	{
		if (btn_auto_size.text() == 'on')
		{
			btn_auto_size.text('off');
			$('.page[original_width]').each(function ()
			{
				var page = $(this);
				page.parent().find('a').click();
			});
		}
		else
		{
			btn_auto_size.text('on');
			$('.page').each(function ()
			{
				var page = $(this);
				if (page.width() > window_width)
					auto_resize(page);
			});
		}
	});

	// Split page into two parts.
	if (location.hash == '#!full-page')
	{
		btn_page_mode.text('full');
		btn_page_mode.attr('href', '#!');

		$('.page').each(function ()
		{
			var page = $(this);
			if (page.width() > window_width)
				auto_resize(page);
		});
	}
	else
	{
		btn_page_mode.text('split');
		btn_page_mode.attr('href', '#!full-page');

		// Split page.
		$('.page').each(function ()
		{
			split_page($(this));
		});
	}

	function split_page(page)
	{
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
				frame.css(
					{
						width: width / 2,
						overflow: 'hidden'
					}
				);

				var new_frame = frame.clone();

				var page_num = frame.find('.page_num');

				var span_right = $('<span> - right - </span>');
				var span_left = $('<span> - left - </span>');
				var span_original = $('<span> - original - </span>');

				page_num.append(span_right);

				var btn_full_page = $('<a href="#!" title="view full page">full</a>');
				var btn_split_page = $('<a href="#!" title="auto split page">split</a>');
				btn_split_page.click(function ()
				{
					span_original.remove();
					btn_split_page.remove();
					new_frame.find('.resize').remove();
					split_page(new_frame.find('.page'));
					init_ui_control();
				});
				btn_full_page.click(function (e)
				{
					frame.remove();
					new_frame.removeAttr('style');
					span_left.remove();

					page_num.append(span_original);
					page_num.append(btn_split_page);
					auto_resize(new_frame.find('.page'));
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
			}
			else
			{
				auto_resize(page);
			}
		}
	}

	function auto_resize(page)
	{
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

	}
}

function init_animation()
{
	if (!effect_on) return;

	$('.page:gt(0)').css('opacity', 0);
	$('.page').attr('_hidden', '1');
	$(window).scroll(
		function ()
		{
			var page = $('.page[_hidden]:eq(0)');

			if (page.length > 0 &&
				page[0].offsetTop - $(window).scrollTop() < 200)
			{
				// Show page animation.
				page.animate({ opacity: 1 }, 500);
				page.removeAttr('_hidden');
			}
		}
	);
}

function init_ui_control()
{
	document.onmousedown = function () { return true; };
	document.oncontextmenu = function () { return true; };

	var pos;
	var scroll = { x: 0, y: 0 };
	var distance = 0;
	var isDraging = false;
	var doc;
	var page;

	if ($.browser.webkit)
		doc = $('body');
	else
		doc = $('html');

	page = $('.page');

	page.css('cursor', 'move');

	page.mousedown(
		function (e)
		{
			if (e.button == 2)
			{
				return;
			}

			scroll.x = doc.scrollLeft();
			scroll.y = doc.scrollTop();

			pos = e;

			isDraging = true;

			if (!$.browser.msie)
				e.preventDefault();
		}
	);

	doc.mousemove(
		function (e)
		{
			if (!isDraging)
				return;

			doc.scrollTop(doc.scrollTop() + pos.pageY - e.pageY);
			doc.scrollLeft(doc.scrollLeft() + pos.pageX - e.pageX);

			e.preventDefault();
		}
	);

	var n = 0;
	page.mouseup(
		function (e)
		{
			isDraging = false;

			distance = 0;
			try
			{
				distance = Math.pow(scroll.x - doc.scrollLeft(), 2) + Math.pow(scroll.y - doc.scrollTop(), 2);
			}
			catch (ex) { }

			if (isNaN(distance) || distance < 9)
			{
				$this = $(this);
				var bottom = $this.height() + $this.offset().top - $(window).height() - doc.scrollTop();
				if (bottom > 0)
				{
					doc.stop().animate({ scrollTop: doc.scrollTop() + bottom + 20 });
				}
				else
				{
					var top = bottom + $(window).height();
					doc.stop().animate({ scrollTop: doc.scrollTop() + top + 50 });
				}
			}
		}
	);
	doc.keyup(
		function (e)
		{
			switch (e.keyCode)
			{
				case 16:
					doc.stop().animate({ scrollTop: doc.scrollTop() - $(window).height() });
					break;

				case 17:
					page.each(function ()
					{
						$this = $(this);
						var window_height = $(window).height();
						var bottom = $this.height() + $this.offset().top - window_height - doc.scrollTop();
						var top = bottom + window_height;

						if (Math.abs(bottom) < window_height ||
							Math.abs(top) < window_height)
						{
							if (bottom > 0)
							{
								doc.stop().animate({ scrollTop: doc.scrollTop() + bottom + 20 });
							}
							else
							{
								doc.stop().animate({ scrollTop: doc.scrollTop() + top + 50 });
							}
						}
					});
					break;
			}
		}
	);
}

function init_navigator()
{
	var path = decodeURIComponent(location);
	var m = path.match(/(?:(\d+)[^\d]+?)$/);

	if (m === null) return;

	var num_len = m[1].length;
	var pre_num = pad(parseInt(m[1], 10) - 1, num_len);
	var next_num = pad(parseInt(m[1], 10) + 1, num_len);

	// If path is like "vol 10.5"
	if (path.indexOf('.' + m[1]) > 0)
	{
		m = path.match(/(?:(\d+)\.(\d+)[^\d]+?)$/);
		num_len = m[1].length + m[2].length + 1;
		if (m[2] == '5')
		{
			pre_num = m[1];
			next_num = pad(parseInt(m[1], 10) + 1, m[1].length);
		}
		else
		{
			alert('Auto navigation failed.');
			return;
		}
	}

	$('.previous').attr('href',
		path.slice(0, m.index) + pre_num + path.slice(m.index + num_len)
	);
	$('.next').attr('href',
		path.slice(0, m.index) + next_num + path.slice(m.index + num_len)
	);

	function pad(num, n)
	{
		var len = num.toString().length;
		while (len < n)
		{
			num = '0' + num;
			len++;
		}
		return num;
	}
}