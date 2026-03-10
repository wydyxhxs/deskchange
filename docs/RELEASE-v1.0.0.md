# DeskChange v1.0.0

DeskChange 的第一个公开版本。

DeskChange 是一个面向 Windows 11 的虚拟桌面管理工具，提供中文设置界面，补齐原生虚拟桌面的日常效率操作：指定桌面快捷键、桌面新建与删除、开机自启，以及切换动画控制。

![DeskChange 主界面](https://raw.githubusercontent.com/wydyxhxs/deskchange/main/docs/screenshots/main-window.png)

## 主要功能

- 支持 1 到 4 个虚拟桌面的快捷键独立配置
- 支持在主界面直接新建桌面、删除当前桌面
- 支持开启或关闭桌面切换动画
- 支持开机自启动
- 支持随系统启动后隐藏到托盘
- 提供安装版和便携版

## 下载说明

- `DeskChange-Setup.exe`
  - 安装版
  - 适合长期使用

- `DeskChange-portable.zip`
  - 便携版
  - 解压即可运行
  - 配置保存在程序目录

## 使用建议

1. 启动程序后，先选择需要启用的桌面数量
2. 为每个桌面设置快捷键
3. 根据习惯选择是否显示切换动画
4. 如需常驻使用，可开启开机自启动

## 系统要求

- Windows 11
- .NET Framework 4.8

## 说明

- DeskChange 基于 Windows 自带虚拟桌面能力工作，并不是另一套桌面系统
- 仓库内打包了开源 `VirtualDesktopHelper.exe`，许可证见 `vendor/LICENSE.MScholtes.txt`
