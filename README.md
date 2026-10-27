[![.Net40](https://img.shields.io/badge/DotNet-4.0-blue)](https://www.microsoft.com/download/details.aspx?id=17851)
[![LICENSE](https://img.shields.io/badge/License-MIT-green)](https://github.com/DebugST/STNodeEditor/blob/main/LICENSE)

[简体中文](./README.CN.md)

# STTextBox
STTextBox is a pure GDI-drawn WinForm control, which can support transparent colors and Emoji, etc., and can customize the display style of text through the text style monitor.

STTextBox is a WinForm control drawn by me and my friend [tpbeldie](https://github.com/tpbeldie) using GDI.

We adopt the MIT open source license. The code targets .NET Framework 4.0 and builds with VS2017 or newer.
Clone with `git clone --recursive`, or run `git submodule update --init` after cloning.

Need .NET 3.5 or VS2010? Use the [net35](https://github.com/DebugST/STTextBox/tree/net35) branch,
which keeps the original code from before the performance rewrite.

The fast selection highlighting is based on tpbeldie's earlier QuickTextboxPrototype project.

Three open source projects were also generated during the design process:

[emoji-svg-render](https://github.com/DebugST/emoji-svg-render), 
[STGraphemeSplitter](https://github.com/DebugST/STGraphemeSplitter), 
[GraphemeSplitterNET](https://github.com/tpbeldie/GraphemeSplitterNET)
    
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
