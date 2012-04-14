Comic Spider - a tool to download and view online manga.

For further information, visit Project Home https://github.com/ysmood/ComicSpider

April 2012 y.s.

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

**************************************************************************************************************

[Video Demo](http://js.tudouui.com/bin/player2/olc_1.swf?iid=126173758&swfPath=http://js.tudouui.com/bin/player2/olm_8.swf&adSourceId=81000&autoPlay=false&listType=0&rurl=&resourceId=110337721_04_05_99&rpid=110337721&autostart=false&snap_pic=http%3A%2F%2Fi1.tdimg.com%2F126%2F173%2F758%2Fw.jpg&code=Cm3deG4DLak&tag=comic+%2Cdemo%2Ctool&title=Comic+Spider+Demonstration&mediaType=vi&totalTime=162160&hdType=1&hasPassword=0&nWidth=800&isOriginal=1&channelId=99&nHeight=450&banPublic=false&uid=110337721&juid=016qgpe8mj2pqm&aopRate=0.001)

[Download Comic_Spider_1.0.0.0 Bin](https://github.com/downloads/ysmood/ComicSpider/Comic_Spider_1.0.0.0.zip)

Maybe use mono will make the project more open, but now windows only.
I provide a programmable interface to control the search behavior of producer: [Lua Interface](http://luaforge.net/projects/luainterface/).
VC++ runtime is required for Lua, I put it in the ComicSpider/Asset/vcredist_x86.exe

Techniques required

* C#, .NET3.5, WPF, http
* SQLite
* html5, css3, js, jQuery
* Lua

# Spider main work flow
There are two main task queues:

1. Page info queue
2. File info queue

Multiply producer and consumer threads will be created to work with these queues.

Info producer threads will try to use regular expression and Levenshtein distance to find valuable info,
then push file info into file info queue.
File downloader threads will simply download files in the file info queue.

Most unpredictable part is the producer part. Every site has is way handling with info presentation.
But most sites has a same routine:

1. Load lua controller
2. Volume list
3. Page list

They may protect their resource by check request header's Cookie, User-Agent and Referer.


Detail work flow of producer:

1. Get comic name (create comic folder)
2. Get volume list (create volume folder)
3. Get page list
4. Get file list

Detail work flow of downloader:

1. download file
2. create index.html to present each volume