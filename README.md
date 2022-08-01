[![.Net35](https://img.shields.io/badge/DotNet-3.5-blue)](https://www.microsoft.com/zh-cn/download/details.aspx?id=25150)
[![LICENSE](https://img.shields.io/badge/License-MIT-green)](https://github.com/DebugST/STNodeEditor/blob/main/LICENSE)

[简体中文](./README.CN.md)

# STTextBox
STTextBox is a pure GDI-drawn WinForm control, which can support transparent colors and Emoji, etc., and can customize the display style of text through the text style monitor.

STTextBox is a WinForm control drawn by me and my friend [netero](https://github.com/0x54164) using GDI.

We adopt the MIT open source license, and the code is built with .Net3.5 + vs2010, 
so it is compatible with almost all VS versions.
Two open source projects were also generated during the design process:

[emoji-svg-render](https://github.com/DebugST/emoji-svg-render), 
[STGraphemeSplitter](https://github.com/DebugST/STGraphemeSplitter)
    
Because of our limited capabilities, many functions are not well implemented, 
so we modified the core functions into independent interfaces,
So that other developers can implement it independently 
without having to find and modify the code in the entire control.

![Emoji](https://s3.bmp.ovh/imgs/2022/08/01/870c128600fcaf5b.png)

You can use `Emoji` in `STTextBox` in the `demos_bin` we used [twemoji](https://github.com/twitter/twemoji).
And for more info, you can see: [emoji-svg-render](https://github.com/DebugST/emoji-svg-render)

![Alpha](https://s3.bmp.ovh/imgs/2022/08/01/9adb88ed6966ba5b.png)

In `STTextBox` all the color you can use `alpha`, Of course his efficiency is also very slow. We can't solve it well for now.

![Style](https://s3.bmp.ovh/imgs/2022/08/01/d18e93176e4a4e48.png)

You can also customize text styles, by implementing the `ITextStyleMonitor` interface, we have four realities built in.
`KeyWorldStyleMonitor` `CSharpStyleMonitor` `LinkStyleMonitor` `SelectionStyleMonitor`.

Note: The rendering speed of `STTextBox` has nothing to do with the content, 
but is related to the number of characters that need to be drawn in the current UI. 
For some reasons, we use character-by-character drawing.
In order for the colors to use the `Alpha` channel, we don't cache what's being drawn, 
since the background may change. And doing caching is also a hassle.
So we have not considered the rendering speed of extreme cases for the time being.

# About Author
* Github: [DebugST](https://github.com/DebugST/)
* Blog: [Crystal_lz](http://st233.com)
* Mail: (2212233137@qq.com)
* TG: [DebugST](t.me/DebugST)