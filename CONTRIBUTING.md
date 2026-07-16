# 贡献指南

感谢你考虑为鼠标连点器项目做出贡献！

## 如何贡献

### 报告 Bug

1. 在 [Issues](https://github.com/your-username/auto-clicker/issues) 页面点击 "New Issue"
2. 选择 "Bug Report" 模板
3. 描述问题：期望行为、实际行为、复现步骤、系统环境（Windows 版本）

### 功能建议

1. 在 Issues 页面选择 "Feature Request" 模板
2. 描述你希望的功能及其使用场景
3. 如果有 UI 设计想法，欢迎附上草图

### 提交代码

1. **Fork** 本仓库
2. 创建特性分支：`git checkout -b feature/my-feature`
3. 遵循现有代码风格（中文注释、英文变量名、MVVM 模式）
4. 确保 `dotnet build -c Release` 零错误
5. 提交 PR 到 `main` 分支
6. 在 PR 描述中说明变更内容和测试情况

### 代码规范

- **语言**: C# 12
- **架构**: MVVM（CommunityToolkit.Mvvm）
- **注释**: 使用中文
- **命名**: PascalCase（类/方法/属性）、`_camelCase`（私有字段）
- **Win32 互操作**: 统一放在 `Interop/` 目录

### 开发环境

- .NET 8 SDK（≥ 8.0.421）
- Windows 10/11 x64
- Visual Studio 2022 或 VS Code + C# Dev Kit

```bash
# 构建
dotnet build -c Release

# 发布
dotnet publish -c Release -o ./publish/

# 打包安装程序（需要 Inno Setup）
ISCC.exe installer.iss
```

## 版本发布流程

1. 更新 `AutoClicker.App.csproj` 中的 `<Version>` 和 `installer.iss` 中的版本号
2. 更新 `CHANGELOG.md`
3. 通过 `dotnet publish` 构建
4. 在 GitHub Releases 页面创建新 Release，上传构建产物
