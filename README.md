# 简贴 JianClip

Windows 10/11 剪贴板历史工具，使用 C# / Windows Forms，无第三方依赖。需要 .NET Framework 4.8（多数 Windows 10/11 已安装）。

## 使用

1. 将 dist/JianClip.exe 放在固定目录后双击启动，无需管理员权限。
2. 正常 Ctrl+C / Ctrl+X 复制文本或图片，软件自动记录。
3. 在需要粘贴的输入框按 Ctrl+Alt+V，搜索并选中历史，双击或 Enter 粘贴。
4. “仅复制”将选中项放回剪贴板，随后可以手动 Ctrl+V。
5. 勾选“开机自启”，下次登录 Windows 后在托盘后台启动。移动 EXE 后需关闭再开启自启，以更新路径。
6. 关闭窗口只隐藏到托盘。托盘右键“退出”才会结束软件。

## 功能与边界

- 文本、图片历史，搜索文本，收藏、删除、清空、暂停记录。
- 最多 200 条，总内容约 1600 万字符；优先清理最旧的非收藏条目。全部收藏达到限制后不再加入新记录。
- 单张 PNG 上限 4 MB，文本上限 50 万字符。图片支持预览，不含 OCR 搜索。
- 历史文件位于 %LOCALAPPDATA%/JianClip/history.dat，使用 Windows DPAPI 当前用户加密，断网可用，无网络请求。
- 尊重 ExcludeClipboardContentFromMonitorProcessing 以及 CanIncludeInClipboardHistory=0 标记。没有标记的密码、令牌仍可能被记录，处理此类内容时请暂停记录或删除历史。
- 当前保存纯文本与图片，不保存 HTML/RTF 样式或资源管理器文件复制内容。
- 自动粘贴依赖目标窗口允许前台切换和模拟 Ctrl+V；管理员窗口、部分游戏或特殊编辑器可能需要手动粘贴。快捷键冲突时会提示，可从托盘打开。
- 从托盘打开时，推荐用“仅复制”，或先回到目标输入框再用快捷键呼出。
- 取消开机自启后退出软件、删除 EXE 即可移除；清空历史可在界面操作。数据目录可手动删除。

## 构建

在本目录运行 powershell -ExecutionPolicy Bypass -File build.ps1，输出 dist/JianClip.exe。
源代码：src/JianClip.cs。

## 验证

已通过编译、进程启动与响应检查；tests/SmokeTests.cs 验证了 DPAPI 加密读写、中文内容、收藏状态和剪贴板隐私标记。自动粘贴与重启登录后的自启尚需真实交互验收。

## 调研参考

独立实现，没有复制下列项目代码：
- Ditto：https://github.com/sabrogden/Ditto
- CopyQ：https://github.com/hluk/CopyQ
- Windows 剪贴板监听：https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener
- 隐私标记：https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats
