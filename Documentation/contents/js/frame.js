/* JavaScript Doc Version 0.1 y.s. */

function frame()
{
	$('#header').prepend(
		'<img style="float: left; margin: 4px 4px;" src="img/ys_16.png"/>' +
		'<a href="0-comic-spider.html">Comic Spider Documentation</a> &gt;&gt; '
	);
	$('#footer').append(
		'April 2012 y.s.<br>'
	);

	var index = location.href.substr(location.href.lastIndexOf('/') + 1,1);
	$('h1').prepend(index + '.');
	$('h2').each(
		function (i)
		{
			$(this).prepend(index + '.' + i + '.');
		}
	);
}