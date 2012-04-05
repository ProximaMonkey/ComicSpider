/* JavaScript Doc Version 0.1 y.s. */

$(window).ready(
	function ()
	{
		frame();
		checkDotNet();
		initSnap();
	}
);

function checkDotNet()
{
	try
	{
		var key = ['.NET CLR 2.0', '.NET CLR 3.5', '.NET4.0C'];
		if (navigator.userAgent.indexOf(key[0]) != -1)
			$('#dotnet_lnk')[0].innerText += '(You have installed)';
	}
	catch(ex){}
}

function initSnap()
{
	$('.snap').each(
		function()
		{
			var w = $(this).width();
			if(w > 600)
			{
				$(this)
					.css({ cursor: 'pointer', width: 600 })
					.attr('title', 'Click to see the original image')
					.click(
						function()
						{
							var current_w = $(this).width();
							if(current_w > 600)
							{
								$(this).animate({ width: 600 }, 300);
							}
							else if(current_w == 600)
							{
								$(this).animate({ width: w }, 300);
							}
						}
					);
			}
		}
	);
}