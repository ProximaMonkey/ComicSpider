[![Comic Spider](https://raw.github.com/ysmood/ComicSpider/master/Documentation/contents/img/splash_screen.png)](https://github.com/downloads/ysmood/ComicSpider/Comic_Spider.zip)

# License

[Comic Spider](https://github.com/ysmood/ComicSpider) - a tool for downloading and viewing online images.

The purpose of this software is sharing and learning. It is for previewing only.
If you like the mangas/illustrations please support the author by purchasing them.

This program is free software: you can redistribute it and/or modify 
it under the terms of the GNU General Public License as published by 
the Free Software Foundation, either version 3 of the License, or 
(at your option) any later version. 

This program is distributed in the hope that it will be useful, 
but WITHOUT ANY WARRANTY; without even the implied warranty of 
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
GNU General Public License for more details. 

You should have received a copy of the GNU General Public License 
along with this program. If not, see http://www.gnu.org/licenses/.

April 2012 y.s.

**************************************************************************************************************

# Overview

[![Main window](https://raw.github.com/ysmood/ComicSpider/master/Documentation/contents/img/snap/main.png)](http://www.tudou.com/v/Cm3deG4DLak/&rpid=2572312&resourceId=2572312_04_05_99/v.swf)

### [Video Demo](http://www.tudou.com/v/Cm3deG4DLak/&rpid=2572312&resourceId=2572312_04_05_99/v.swf)

### [Download the latest Comic Spider](http://ysmood.org/upload/comicspider/Comic_Spider.zip)

# Features

* Open source, no ads and free forever. 
* All you need are only dragging and dropping.
* Auto-login service.
* A better way to view manga via auto-created display pages.
* Multithreaded and parsing fast.
* An easy to use Lua programmable interface.
* Not only downloading comic images.
* It can be easily modified to a complex downloader.
* Support remote script controller.
* Always get the latest parser without updates.

# FAQ

* ### How to use the program?

   Please watch the [Video Demo](http://www.tudou.com/v/Cm3deG4DLak/&rpid=2572312&resourceId=2572312_04_05_99/v.swf)

* ### Could not load file or assembly?
   Please install the "vcredist_x86.exe" in the "Asset" folder, then try again.
   Or install this pack [vcredist_x86.exe](http://www.microsoft.com/en-us/download/details.aspx?id=26368).

* ### 中文界面？

   軟體的主要使用無需刻意理解界面中的文字。將想要的資源拖拽到面板中即為全部所需操作。
   複雜的細節設定是對有更高靈活度需求的用戶設計的。

# For developers

Maybe using Mono will make the project more open, but now for Windows only.
I provide a programmable interface to control the search behavior of producer: [Lua Interface](http://luaforge.net/projects/luainterface/).
VC++ runtime is required for Lua, I put it in the ComicSpider/Asset/vcredist_x86.exe

Techniques required

* C#, .NET3.5, WPF, http protocol, regular expression, XPath
* SQLite
* html5, css3, js, jQuery
* Lua

![Dashboard](https://raw.github.com/ysmood/ComicSpider/master/Documentation/contents/img/snap/dashboard.png)

### Spider main work flow

There are three main task queues:

1. Volume info queue
1. Page info queue
2. File info queue

Multiply producer and consumer threads will be created to work with these queues(linked lists but behave like queues).

Producer threads will try to search pieces of valuable information, then push them into the queues.
File downloader threads will simply download files via the information queues.
The spider will act like a normal browser and handle all the Cookies,
Referer and other basic header information automatically.

Most unpredictable part is the producer part. Every site has its way to handle information presentation.
But most sites has a same routine, they all have a classic tree with depth 3 and with unknown leaves.

1. Volume list
2. Page list
3. File list

By default it won't parse the html tree for it may take up a lot of resources to handle broken tags or file fragment when the quality of network connection is poor.
Regular expression is a more efficient way to ignore all these exceptions.
For example if you want select the javascript fragment in the html, it could be embarrassed to use a xml parser.
And it's really simple to test regular expression in tools like [an online tester](http://myregextester.com/), Sublime Text or Expresso.

But still you can use XPath to get information, I implemented a lua api for [HtmlAgilityPack](http://htmlagilitypack.codeplex.com/).

Since login is required to download in some sites, I implemented a login web service for some famous sites.

### Detail work flow of producer

1. Load lua controller (login some sites if needed)
1. Get comic name (create comic folder)
2. Get volume list (create volume folder)
3. Get page list
4. Get file list

### Detail work flow of downloader

1. download file
2. create a presentation page to display images of each volume

### Auto created presentation page

It has many useful functions for browsing images. Such as auto resize large image and auto split wide image.
With html5 animation, it will be great to use it browsing image collections, not only mangas.

![View page](https://raw.github.com/ysmood/ComicSpider/master/Documentation/contents/img/snap/view.png)
