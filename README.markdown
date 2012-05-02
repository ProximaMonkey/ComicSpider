
# License

[Comic Spider](https://github.com/ysmood/ComicSpider) - a tool for downloading and viewing online images.
The purposes of this software is sharing and learning which means it is for previewing purposes only.
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

[![Main window](https://raw.github.com/ysmood/ComicSpider/master/Documentation/contents/img/snap/main.png)](http://js.tudouui.com/bin/player2/olc_1.swf?iid=126173758&swfPath=http://js.tudouui.com/bin/player2/olm_8.swf&adSourceId=81000&autoPlay=false&listType=0&rurl=&resourceId=110337721_04_05_99&rpid=110337721&autostart=false&snap_pic=http%3A%2F%2Fi1.tdimg.com%2F126%2F173%2F758%2Fw.jpg&code=Cm3deG4DLak&tag=comic+%2Cdemo%2Ctool&title=Comic+Spider+Demonstration&mediaType=vi&totalTime=162160&hdType=1&hasPassword=0&nWidth=800&isOriginal=1&channelId=99&nHeight=450&banPublic=false&uid=110337721&juid=016qgpe8mj2pqm&aopRate=0.001)

### [Video Demo](http://js.tudouui.com/bin/player2/olc_1.swf?iid=126173758&swfPath=http://js.tudouui.com/bin/player2/olm_8.swf&adSourceId=81000&autoPlay=false&listType=0&rurl=&resourceId=110337721_04_05_99&rpid=110337721&autostart=false&snap_pic=http%3A%2F%2Fi1.tdimg.com%2F126%2F173%2F758%2Fw.jpg&code=Cm3deG4DLak&tag=comic+%2Cdemo%2Ctool&title=Comic+Spider+Demonstration&mediaType=vi&totalTime=162160&hdType=1&hasPassword=0&nWidth=800&isOriginal=1&channelId=99&nHeight=450&banPublic=false&uid=110337721&juid=016qgpe8mj2pqm&aopRate=0.001)

### [Download latest Comic Spider bin](https://github.com/downloads/ysmood/ComicSpider/Comic_Spider.zip)

# Features

* Open source, no ads and free forever. 
* Simple drag and drop is all it needs to download. 
* Online auto login service. 
* A better way to browse manga via auto created view pages. 
* Multithread and fast. 
* Has an easy to use Lua programmable interface. 
* Not only comic images, it can be easily modified into a complex downloader. 
* Support remote script controller which means you can always get the latest parser without updating the program.

# FAQ

* ### How to use the program?

   Please watch the [Video Demo](http://js.tudouui.com/bin/player2/olc_1.swf?iid=126173758&swfPath=http://js.tudouui.com/bin/player2/olm_8.swf&adSourceId=81000&autoPlay=false&listType=0&rurl=&resourceId=110337721_04_05_99&rpid=110337721&autostart=false&snap_pic=http%3A%2F%2Fi1.tdimg.com%2F126%2F173%2F758%2Fw.jpg&code=Cm3deG4DLak&tag=comic+%2Cdemo%2Ctool&title=Comic+Spider+Demonstration&mediaType=vi&totalTime=162160&hdType=1&hasPassword=0&nWidth=800&isOriginal=1&channelId=99&nHeight=450&banPublic=false&uid=110337721&juid=016qgpe8mj2pqm&aopRate=0.001)

* ### 中文界面？

   軟體的主要使用無需刻意理解界面中的文字。將想要的資源拖拽到面板中即為全部所需操作。
   複雜的細節設定是對有更高靈活度需求的用戶設計的。

# For developers

Maybe use mono will make the project more open, but now windows only.
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

Most unpredictable part is the producer part. Every site has is way handle information presentation.
But most sites has a same routine, they has a classic tree with depth 3 and with unknown leaves.

1. Volume list
2. Page list
3. File list

By default it won't parse the html tree for it may take up a lot of resources to handle broken tags or file fragment when the network is bad.
Regular expression is a more efficient way to ignore all of these exceptions.
For example if you want select the javascript fragment in the html, it could be embarrassed to use a xml parser.
And it really simple for testing regular expression in tools like [an online tester](http://myregextester.com/), Sublime Text or Expresso.
But still you can use XPath to get info, I implemented a lua api for [HtmlAgilityPack](http://htmlagilitypack.codeplex.com/).
Because some sites need login to download resource, so I implemented a online login service for some common sites.

### Detail work flow of producer

1. Load lua controller (login some sites if needed)
1. Get comic name (create comic folder)
2. Get volume list (create volume folder)
3. Get page list
4. Get file list

### Detail work flow of downloader

1. download file
2. create a presentation page to present images of each volume

### Auto created presentation page

It has many useful functions for browsing images. Such as auto resize large image and auto split wide image.
With html5 an animated UI, it will be great to use it browsing image collections, not only mangas.

![View page](https://raw.github.com/ysmood/ComicSpider/master/Documentation/contents/img/snap/view.png)