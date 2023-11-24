[![.Net35](https://img.shields.io/badge/DotNet-3.5-blue)](https://www.microsoft.com/zh-cn/download/details.aspx?id=25150)
[![LICENSE](https://img.shields.io/badge/License-MIT-green)](https://github.com/DebugST/STNodeEditor/blob/main/LICENSE)

# STTextBox
`STTextBox`是我和我的朋友[tpbeldie](https://github.com/tpbeldie)使用`GDI`构建的一个`WinForm`控件，可以支持`Emoji`

我们采用MIT开源协议，使用`.Net3.5` + `vs2010`构建的项目，所以几乎可以兼容任何VS版本，
在开发的过程中我们还衍生出两个额外的开源项目：

[emoji-svg-render](https://github.com/DebugST/emoji-svg-render), 
[STGraphemeSplitter](https://github.com/DebugST/STGraphemeSplitter)

因为能力有限，很多功能我们都无法很好的实现，所以将一些有代表性的功能独立成了接口。
如果有能力且愿意去修改它们的开发者可以直接继承接口实现，不用在项目中到处找代码了。

![Emoji](https://s3.bmp.ovh/imgs/2022/08/01/870c128600fcaf5b.png)

你可以在`STTextBox`中使用`Emoji`,在`demos_bin`的样例我们使用的是推特的表情符[twemoji](https://github.com/twitter/twemoji).
更多信息可以查看: [emoji-svg-render](https://github.com/DebugST/emoji-svg-render)

![Alpha](https://s3.bmp.ovh/imgs/2022/08/01/9adb88ed6966ba5b.png)

在`STTextBox`中的所有颜色你都可以使用`alpha`通道，当然代价就是渲染的速度会变慢。

![Style](https://s3.bmp.ovh/imgs/2022/08/01/d18e93176e4a4e48.png)

你可以自定义文本样式，通过实现`ITextStyleMonitor`接口，在`STTextBox`中内置了4个样式监视器。
`KeyWorldStyleMonitor` `CSharpStyleMonitor` `LinkStyleMonitor` `SelectionStyleMonitor`。

注意：`STTextBox`的渲染速度和内容没太大关系，而是和当前UI中需要绘制的字符个数有关，因为一些原因我们采用的是逐个字符绘制。
为了让颜色可以使用`Alpha`通道，所以我们并没有对绘制的内容做缓存处理，因为背景可能是变化的。而且要做缓存也是一个麻烦事情。
所以我们暂时没有考虑极端情况的渲染速度。

# 关于作者
* Github: [DebugST](https://github.com/DebugST/)
* Blog: [Crystal_lz](http://st233.com)
* Mail: (2212233137@qq.com)
* TG: [DebugST](t.me/DebugST)
