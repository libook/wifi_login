# 版本管理指南

## 版本号说明

本项目采用 [语义化版本](https://semver.org/lang/zh-CN/) 规范：`主版本号.次版本号.修订号`

- **主版本号（Major）**：不兼容的 API 修改
- **次版本号（Minor）**：向下兼容的功能性新增
- **修订号（Patch）**：向下兼容的问题修正

示例：
- `1.0.0` - 初始版本
- `1.0.1` - 修复了某个 bug
- `1.1.0` - 新增了某个功能
- `2.0.0` - 重大更新，可能不兼容旧版本

## 版本号维护方式

### 1. 项目文件配置（推荐）

版本号在 `WifiAutoLogin.csproj` 文件中维护：

```xml
<PropertyGroup>
  <!-- NuGet 包版本 -->
  <Version>1.0.0</Version>

  <!-- 程序集版本（4位数字） -->
  <AssemblyVersion>1.0.0.0</AssemblyVersion>

  <!-- 文件版本（4位数字） -->
  <FileVersion>1.0.0.0</FileVersion>

  <!-- 信息版本（可包含预发布标签） -->
  <InformationalVersion>1.0.0</InformationalVersion>
</PropertyGroup>
```

### 2. 查看版本信息

构建后，可以通过以下方式查看版本信息：

#### 在代码中获取版本

```csharp
using System.Reflection;

// 获取程序集版本
var version = Assembly.GetExecutingAssembly().GetName().Version;
Console.WriteLine($"Version: {version}");

// 获取信息版本
var infoVersion = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion;
Console.WriteLine($"Informational Version: {infoVersion}");
```

#### 查看文件属性

在 Windows 资源管理器中：
1. 右键点击编译后的 `WifiAutoLogin.exe`
2. 选择"属性"
3. 切换到"详细信息"选项卡
4. 可以看到文件版本、产品版本等信息

### 3. 版本更新流程

#### 发布新版本

1. **更新版本号**：
   编辑 `WifiAutoLogin.csproj`，更新以下字段：
   ```xml
   <Version>1.1.0</Version>
   <AssemblyVersion>1.1.0.0</AssemblyVersion>
   <FileVersion>1.1.0.0</FileVersion>
   <InformationalVersion>1.1.0</InformationalVersion>
   ```

2. **更新变更日志**：
   在 `CHANGELOG.md` 中记录变更内容

3. **构建发布版本**：
   ```powershell
   dotnet build -c Release
   ```

4. **创建 Git 标签**（如果使用 Git）：
   ```powershell
   git tag v1.1.0
   git push origin v1.1.0
   ```

#### 预发布版本

对于测试版本，可以使用预发布标签：

```xml
<Version>1.1.0-beta.1</Version>
<InformationalVersion>1.1.0-beta.1</InformationalVersion>
```

预发布标签示例：
- `1.1.0-alpha.1` - 内部测试版本
- `1.1.0-beta.1` - 公开测试版本
- `1.1.0-rc.1` - 候选发布版本

## 自动化版本管理（可选）

### 使用 MinVer

[MinVer](https://github.com/adamralph/minver) 是一个基于 Git 标签的自动版本管理工具：

1. 安装 NuGet 包：
   ```powershell
   dotnet add package MinVer
   ```

2. 在项目文件中添加：
   ```xml
   <PackageReference Include="MinVer" Version="4.3.0" PrivateAssets="all" />
   ```

3. 创建 Git 标签：
   ```powershell
   git tag v1.0.0
   ```

4. 构建时自动计算版本号

### 使用 GitVersion

[GitVersion](https://gitversion.net/docs/) 是另一个流行的版本管理工具，支持更复杂的版本策略。

## 变更日志

建议维护 `CHANGELOG.md` 文件记录版本变更：

```markdown
# Changelog

## [1.1.0] - 2024-01-15

### Added
- 新增国际化支持（中英文切换）
- 新增日志记录功能

### Changed
- 优化界面布局

### Fixed
- 修复通知级别选项重复的问题

## [1.0.0] - 2024-01-01

### Added
- 初始版本发布
- 基本的自动登录功能
```

## 最佳实践

1. **保持版本号同步**：确保 `Version`、`AssemblyVersion`、`FileVersion` 保持一致
2. **记录变更**：每次发布都更新 `CHANGELOG.md`
3. **使用标签**：为每个发布版本创建 Git 标签
4. **语义化版本**：遵循语义化版本规范，便于用户理解变更类型
5. **预发布测试**：重大更新前发布 beta 或 rc 版本进行测试

## 相关工具

- **dotnet-pack**: 打包 NuGet 包时自动使用版本号
- **dotnet-publish**: 发布应用时包含版本信息
- **Assembly Information**: 可在代码中读取版本信息用于显示
