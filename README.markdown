Just a personal tool.

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