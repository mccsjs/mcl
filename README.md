# MCL Launcher

## 1. 项目声明 / Project statement

> 本项目为 [zqq-699/BlockHelm-Launcher](https://github.com/zqq-699/BlockHelm-Launcher) 的二次开发自用版，删减、调整或添加了个人喜欢的样式与功能。上游项目及其原作者的版权和许可证信息仍受尊重，详见本仓库的 `LICENSE` 文件。
>
> This project is a personal-use derivative of [zqq-699/BlockHelm-Launcher](https://github.com/zqq-699/BlockHelm-Launcher). Styles and features have been removed, adjusted, or added to suit my preferences. The upstream project and its original authorship and license are acknowledged; see the `LICENSE` file in this repository.

[中文](#中文) | [English](#english)

## 中文

### 简介

`MCL Launcher`（程序集名称：`mccsjs_launcher`）是一款面向 Minecraft Java 版玩家的 Windows 启动器，基于 BlockHelm Launcher 二次开发，提供游戏下载、实例管理、账户登录、资源安装、联机和故障诊断等功能。

本项目是独立的第三方自用版本，并非 Minecraft 官方产品，也未经 Microsoft、Mojang Studios 或其他第三方平台赞助、认可或背书。

### 主要功能

#### 游戏安装与启动

* 安装原版 Minecraft，以及 `Fabric`、`Forge`、`NeoForge` 和 `Quilt` 加载器。
* 管理下载任务，并可配置下载源、并发线程和速度限制。
* 检查并补全缺失或损坏的游戏文件。
* 展示启动进度与状态；启动失败时提供原因分析和诊断信息。

#### 实例管理

* 创建、重命名、删除和启动多个独立游戏实例。
* 为每个实例配置 Java、内存、窗口、启动参数和启动前/退出后命令。
* 备份与恢复实例，并可自定义备份目录。
* 导入本地游戏和整合包，也可将实例导出为整合包。

#### 账户与角色

* 支持 Microsoft 正版账户、离线账户和第三方认证账户。
* 管理多个账户并快速切换当前角色。
* 管理账户名称、头像、皮肤与披风。
* 提供多种离线账户 UUID 生成方式。
* 如果您尚未拥有 Minecraft Java 版，请优先购买正版游戏。

#### 资源中心

* 浏览和搜索 Mod、资源包、光影包、世界与整合包。
* 支持 Modrinth 和 CurseForge，并可按游戏版本、加载器、来源及分类筛选。
* 查看项目详情、可用版本和依赖关系，并将兼容资源安装到指定实例。
* 检查缺失的前置 Mod，并辅助安装依赖。
* 管理本地 Mod、存档、资源包和光影包，支持拖放导入、搜索及批量启用、禁用和删除。

#### 整合包与服务器

* 安装和导入 Modrinth、CurseForge 整合包。
* 导出实例内容，便于分享和迁移游戏配置。
* 将支持的整合包部署为独立 Minecraft 服务端。
* 对受分发限制的文件提供手动补全指引。

#### 联机

* 自动发现本机已开放到局域网的 Minecraft 世界。
* 创建联机房间并生成房间代码，或通过房间代码加入其他玩家。
* 查看房间状态和成员，快速完成跨网络联机。

#### 外观与设置

* 支持深色和浅色主题。
* 本分支使用橙色强调色，并调整首页背景、首页布局、版本选择面板毛玻璃效果和顶部工具栏。
* 自动发现、下载和管理 Java，也可手动导入本地 Java。
* 支持全局及实例独立内存设置、Minecraft 目录管理和启动器更新。

### 系统要求

* Windows 10 / 11（64 位）。
* Minecraft Java Edition；Microsoft 登录需要具有游戏授权的账户。
* 运行已发布版本需要 `.NET 8 Desktop Runtime`；从源码构建需要 `.NET 8 SDK`。

### 许可证与致谢

本项目沿用仓库 `LICENSE` 文件所列的 GNU GPL v3.0-only 许可证。修改或再分发时，请阅读并遵守该许可证，并保留适用的版权和来源声明。

上游项目：[zqq-699/BlockHelm-Launcher](https://github.com/zqq-699/BlockHelm-Launcher)

---

## English

### Overview

`MCL Launcher` (assembly name: `mccsjs_launcher`) is a Windows launcher for Minecraft: Java Edition. It is based on BlockHelm Launcher and provides game installation, instance management, account sign-in, resource installation, multiplayer, and troubleshooting.

This is an independent, personal-use third-party project. It is not an official Minecraft product and is not sponsored, approved, or endorsed by Microsoft, Mojang Studios, or any other third-party platform.

### Features

#### Game Installation and Launching

* Install vanilla Minecraft with `Fabric`, `Forge`, `NeoForge`, or `Quilt`.
* Manage downloads and configure download sources, concurrency, and speed limits.
* Detect and restore missing or damaged game files.
* Follow launch progress and status, with diagnostic information when a launch fails.

#### Instance Management

* Create, rename, delete, and launch multiple independent game instances.
* Configure Java, memory, window options, launch arguments, and pre-launch or post-exit commands per instance.
* Back up and restore instances with a configurable backup location.
* Import local games and modpacks, or export an instance as a modpack.

#### Accounts and Profiles

* Use Microsoft, offline, and third-party authentication accounts.
* Manage multiple accounts and quickly switch the active profile.
* Manage account names, avatars, skins, and capes.
* Choose from multiple offline UUID generation methods.
* If you do not own Minecraft: Java Edition, please consider purchasing a legitimate copy.

#### Resource Center

* Browse and search mods, resource packs, shader packs, worlds, and modpacks.
* Access Modrinth and CurseForge content with filters for game version, loader, source, and category.
* View project details, available versions, and dependencies, then install compatible content into an instance.
* Detect missing mod dependencies and assist with installing them.
* Manage local mods, saves, resource packs, and shader packs with drag-and-drop import, search, and batch actions.

#### Modpacks and Servers

* Install and import Modrinth and CurseForge modpacks.
* Export instance content for sharing or migration.
* Deploy supported modpacks as standalone Minecraft servers.
* Receive instructions when distribution-restricted files must be supplied manually.

#### Multiplayer

* Automatically discover Minecraft worlds opened to LAN on the local computer.
* Create a multiplayer room and share its code, or join another player with a room code.
* View room status and members for cross-network play.

#### Appearance and Settings

* Switch between dark and light themes.
* This fork uses an orange accent and customizes the home background, home layout, frosted-glass version panel, and top toolbar.
* Automatically discover, download, and manage Java, or import a local Java installation manually.
* Configure global and per-instance memory, manage the Minecraft directory, and receive launcher updates.

### Requirements

* Windows 10 / 11 (64-bit).
* Minecraft: Java Edition. Microsoft sign-in requires an account with a game entitlement.
* The published app requires the `.NET 8 Desktop Runtime`; building from source requires the `.NET 8 SDK`.

### License and Attribution

This project retains the GNU GPL v3.0-only license stated in the repository's `LICENSE` file. Read and comply with that license when modifying or redistributing the project, and preserve applicable copyright and attribution notices.

Upstream project: [zqq-699/BlockHelm-Launcher](https://github.com/zqq-699/BlockHelm-Launcher)
