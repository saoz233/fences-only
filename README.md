# 桌面栅栏 DesktopFences

一款运行于 **Windows 11**（兼容 Windows 10 x64）的轻量级桌面图标整理工具，形态参考 [Stardock Fences](https://www.stardock.com/products/fences/)。用多个半透明「栅栏（Fence）」容器把杂乱的桌面图标分组归纳，让桌面整洁、简约、可一键管理。

- 界面全简体中文，贴合 Windows 11 设计语言（圆角、亚克力毛玻璃半透明）
- 支持浅色 / 深色主题（可跟随系统）
- 常驻系统托盘，支持开机自启
- 配置本地持久化，重启自动恢复布局

> ⚠️ 本项目使用 **C# + WPF (.NET 8)**，仅能在 **Windows** 上编译、运行与打包。
> 在 macOS / Linux 上可以阅读与编辑源码，但无法构建运行。

---

## 功能一览

| 功能 | 说明 |
| --- | --- |
| 栅栏容器 | 创建 / 删除多个半透明容器，自由拖动、拉伸缩放 |
| 重命名 | 双击标题文字即可重命名 |
| 折叠 (Roll-up) | 双击标题栏空白处将容器折叠为仅标题条，再次双击展开 |
| 图标归纳 | 从桌面 / 资源管理器拖入文件、文件夹、快捷方式；支持容器间互拖 |
| 打开目标 | 双击容器内图标打开对应程序 / 文件 / 文件夹 |
| 一键显隐 | 双击桌面空白处隐藏 / 显示全部容器 |
| 外观 | 调节背景透明度，浅色 / 深色 / 跟随系统主题 |
| 托盘常驻 | 右键菜单：显示隐藏、新建、设置、开机自启、退出 |
| 开机自启 | 通过注册表 `HKCU\...\Run` 实现，可随时开关 |
| 持久化 | 布局与设置保存在 `%AppData%\DesktopFences\config.json` |

---

## 目录结构

```
.
├── DesktopFences.sln              # 解决方案
├── README.md                      # 本文件
├── docs/
│   └── 使用说明.md                 # 功能说明 + 使用指南
├── installer/
│   ├── setup.iss                  # Inno Setup 打包脚本
│   └── build.ps1                  # 一键构建脚本（发布 + 打包）
└── src/DesktopFences/
    ├── DesktopFences.csproj        # 项目文件 (net8.0-windows)
    ├── App.xaml(.cs)               # 启动入口：单实例 / 托盘 / 恢复布局 / 主题 / 钩子
    ├── app.manifest                # DPI 感知与系统兼容性
    ├── Models/                     # 数据模型（可序列化）
    ├── Services/                   # 服务层（配置 / 自启 / 图标 / 快捷方式 / 钩子 / 主题）
    ├── ViewModels/                 # MVVM 视图模型
    ├── Views/                      # 栅栏窗口 / 设置窗口
    ├── Themes/                     # 浅色 / 深色 / 通用样式
    └── Helpers/                    # Win32 互操作 / 托盘 / 转换器
```

---

## 环境要求（Windows）

- Windows 10 / 11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- 打包需要：[Inno Setup 6](https://jrsoftware.org/isdl.php)（可选，仅生成安装包时需要）

---

## 快速开始

### 方式一：直接运行（开发调试）

```powershell
cd src\DesktopFences
dotnet run -c Debug
```

### 方式二：一键构建 + 生成安装包（推荐）

在**仓库根目录**执行：

```powershell
powershell -ExecutionPolicy Bypass -File installer\build.ps1
```

脚本会自动完成：

1. 检查 .NET SDK
2. `dotnet publish`（Release / win-x64 / 自包含 / 单文件）输出到 `publish\`
3. 若检测到 Inno Setup，调用 `ISCC.exe` 生成安装包

生成的安装包位于：

```
installer\Output\DesktopFencesSetup.exe
```

> 若未安装 Inno Setup，脚本仍会完成应用发布，你可以直接运行
> `publish\DesktopFences.exe`。

### 方式三：仅手动发布（不打包）

```powershell
dotnet publish src\DesktopFences\DesktopFences.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true -o publish
```

---

## 安装包说明

`DesktopFencesSetup.exe` 支持：

- 选择安装目录（默认 `Program Files\DesktopFences`）
- 创建开始菜单 / 桌面快捷方式
- 可选「开机自动启动」
- 自带卸载程序（卸载时清理用户配置）

---

## 技术栈

- **C# + WPF (.NET 8，`net8.0-windows`)**
- **MVVM**：CommunityToolkit.Mvvm
- **托盘**：WinForms `NotifyIcon`（框架自带，无第三方依赖）
- **序列化**：System.Text.Json
- **原生能力 (P/Invoke)**：
  - 亚克力毛玻璃：`SetWindowCompositionAttribute` + Win11 圆角 `DwmSetWindowAttribute`
  - 图标提取：`SHGetFileInfo`
  - 桌面双击检测：低级全局鼠标钩子 `WH_MOUSE_LL`
  - 启动目标：`ShellExecute`
- **开机自启**：注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- **单实例**：命名 `Mutex`

---

## 常见问题

**Q：双击桌面没有反应？**
A：请确认鼠标双击的是桌面**空白处**（而非图标）。该功能依赖全局鼠标钩子，部分安全软件可能拦截。

**Q：开机自启没生效？**
A：企业组策略可能限制写入注册表 Run 项。可改用安装包中的「开机自启」任务，或手动将快捷方式放入 `shell:startup`。

**Q：拖图标进容器后，桌面上原图标还在？**
A：本工具默认采用「引用」方式归纳，不会物理移动 / 删除原文件，以避免误删风险。

---

## 许可证

本项目仅供学习与个人使用。
