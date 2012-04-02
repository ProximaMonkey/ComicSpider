var auto_split_page = true;

$(window).load(function()
	{
		InitAnimation();
		InitUIControl();
		$(window).scroll();
		InitCSS();
	}
);
		
function InitCSS()
{
	// Split page into two parts.
	if(auto_split_page) $('img').each(
		function()
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
				frame.after(frame.clone());
				img.css(
					{
						position: 'relative',
						left: -img.width() / 2
					}
				);
			}
		}
	);
}

function InitAnimation()
{
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

function InitUIControl()
{
	document.onmousedown = function(){ return true; };
	document.oncontextmenu = function(){ return true; };
			
	var pos;
	var scroll = { x: 0, y: 0 };
	var distance = 0;
	var isDraging = false;
	var doc;
	var container;
	
	if($.browser.webkit)
		doc = $('body');
	else
		doc = $('html');

	container = $('#container');
			
	container.mousedown(
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
	container.mouseup(
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
				var frame_list = $('.img_frame');
				var index = 0;
				for(var i = 0; i < frame_list.length; i++)
				{
					var top = $(frame_list[i]).offset().top - doc.scrollTop();
					if(top >= 0)
					{
						index = i;
						break;
					}
				}
						
				var offset = $(frame_list[index]).height();
				var scrollTo = 0;
				if(offset > $(window).height())
					scrollTo = doc.scrollTop() + (n++ % 2 === 0 ? (offset - $(window).height()) : $(window).height());
				else
					scrollTo = doc.scrollTop() + offset;
				doc.stop().animate({ scrollTop: scrollTo });
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
					e.preventDefault();
					break;
						
				case 17:
					doc.mouseup();
					e.preventDefault();
					break;
			}
		}
	);
}

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