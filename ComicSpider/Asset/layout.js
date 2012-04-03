/** Javascript doc
	Comic layout controller
	April 2012 y.s.
*/
// Default settings
var effect_on = false;

/**************** Main *******************/

$(window).load(function()
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
	var btn_page_mode = $('.page-mode');

	btn_page_mode.click(function(){
		location.hash = btn_page_mode.attr('href');
		location.reload();
	});

	// Split page into two parts.
	if(location.hash == '#!full-page')
	{
		btn_page_mode.text('Full');
		btn_page_mode.attr('href', '#!');
		return;
	}
	else
	{
		btn_page_mode.text('Split');
		btn_page_mode.attr('href', '#!full-page');
	}

	// Split page.
	$('img').each(function()
	{
		var img = $(this);
		if(img.width() > img.height())
		{
			var frame = $('.img_frame[index="' + img.attr('index') + '"]');
			frame.css(
				{
					width: img.width() / 2,
					overflow: 'hidden'
				}
			);

			var new_frame = frame.clone();

			var page_num = frame.find('.page_num');
			page_num.append('<span class="page-side"> - right </span>');

			var btn_full_page = $('<a href="#!">Full page</a>').mousedown(function(e) {
				frame.remove();
				new_frame.removeAttr('style');
				new_frame.find('.page-side').remove();
			});


			page_num.append(btn_full_page);

			page_num = new_frame.find('.page_num');
			page_num.append('<span class="page-side"> - left  </span>');

			frame.after(new_frame);
			img.css(
				{
					position: 'relative',
					left: -img.width() / 2
				}
			);
		}
		else
		{

		}
	}
);
}

function init_animation()
{
	if(!effect_on) return;

	$('img:gt(0)').css('opacity', 0);
	$('img').attr('_hidden', '1');
	$(window).scroll(
		function()
		{
			var img = $('img[_hidden]:eq(0)');
					
			if(img.length > 0 &&
				img[0].offsetTop - $(window).scrollTop() < 200)
			{
				// Show page animation.
				img.animate({ opacity: 1 }, 500);
				img.removeAttr('_hidden');
			}
		}
	);
}

function init_ui_control()
{
	document.onmousedown = function(){ return true; };
	document.oncontextmenu = function(){ return true; };
			
	var pos;
	var scroll = { x: 0, y: 0 };
	var distance = 0;
	var isDraging = false;
	var doc;
	var page;
	
	if($.browser.webkit)
		doc = $('body');
	else
		doc = $('html');

	page = $('.page');
			
	page.mousedown(
		function(e)
		{
			if(e.button == 2)
			{
				return;
			}

			scroll.x = doc.scrollLeft();
			scroll.y = doc.scrollTop();

			pos = e;

			isDraging = true;

			if(!$.browser.msie)
				e.preventDefault();
		}
	);
			
	doc.mousemove(
		function(e)
		{
			if(!isDraging)
				return;
					
			doc.scrollTop(doc.scrollTop() + pos.pageY - e.pageY);
			doc.scrollLeft(doc.scrollLeft() + pos.pageX - e.pageX);

			e.preventDefault();
		}
	);
			
	var img = $('img:eq(0)');
	var n = 0;
	page.mouseup(
		function(e)
		{
			isDraging = false;
					
			distance = 0;
			try
			{
				distance = Math.pow(scroll.x - doc.scrollLeft(), 2) + Math.pow(scroll.y - doc.scrollTop(), 2);
			}
			catch(ex){}
					
			if(isNaN(distance) || distance < 9)
			{
				$this = $(this);
				var bottom = $this.height() + $this.offset().top - $(window).height() - doc.scrollTop();
				if(bottom > 0)
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
		function(e)
		{
			switch(e.keyCode)
			{
				case 16:
					doc.stop().animate({ scrollTop: doc.scrollTop() - $(window).height() });
					break;
						
				case 17:
					doc.stop().animate({ scrollTop: doc.scrollTop() + $(window).height() * 0.9 });
					break;
			}
		}
	);
}

function init_navigator()
{
	var path = decodeURIComponent(location);
	var m = path.match(/(?:(\d+)[^\d]+?)$/);
	var num_len = m[1].length;
	var pre_num = pad(parseInt(m[1], 10) - 1, num_len);
	var next_num = pad(parseInt(m[1], 10) + 1, num_len);

	// If path is like "vol 10.5"
	if(path.indexOf('.' + m[1]) > 0)
	{
		m = path.match(/(?:(\d+)\.(\d+)[^\d]+?)$/);
		num_len = m[1].length + m[2].length + 1;
		if(m[2] == '5')
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
		while(len < n)
		{
			num = '0' + num;
			len++;
		}
		return num;
	}
}

