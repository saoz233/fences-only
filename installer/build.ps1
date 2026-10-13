# ============================================================
#  桌面栅栏 DesktopFences 一键构建脚本
#  用途：在 Windows 上发布应用并（可选）用 Inno Setup 生成安装包
#  用法：在仓库根目录执行
#     powershell -ExecutionPolicy Bypass -File installer\build.ps1
# ============================================================

$ErrorActionPreference = "Stop"

# 路径定位（脚本位于 installer\ 下，上一级为仓库根目录）
$Root       = Split-Path -Parent $PSScriptRoot
$Project    = Join-Path $Root "src\DesktopFences\DesktopFences.csproj"
$PublishDir = Join-Path $Root "publish"
$OutputDir  = Join-Path $PSScriptRoot "Output"
$Iss        = Join-Path $PSScriptRoot "setup.iss"

Write-Host "==> 仓库根目录: $Root"

# 1) 检查 .NET SDK
Write-Host "==> 检查 .NET SDK ..."
try {
    $dotnetVersion = dotnet --version
    Write-Host "    .NET SDK 版本: $dotnetVersion"
} catch {
    Write-Error "未检测到 .NET SDK，请先安装 .NET 8 SDK: https://dotnet.microsoft.com/download"
    exit 1
}

# 2) 清理旧的发布目录
if (Test-Path $PublishDir) {
    Write-Host "==> 清理旧发布目录 ..."
    Remove-Item $PublishDir -Recurse -Force
}

# 3) 发布：Release / win-x64 / 自包含 / 单文件
Write-Host "==> 正在发布应用 (Release, win-x64, 自包含, 单文件) ..."
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish 失败。"
    exit 1
}
Write-Host "==> 发布完成，输出目录: $PublishDir"

# 4) 查找 Inno Setup 编译器 ISCC.exe
Write-Host "==> 查找 Inno Setup (ISCC.exe) ..."
$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    # 尝试从 PATH 中查找
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}

if (-not $iscc) {
    Write-Warning "未找到 Inno Setup 6 (ISCC.exe)。已完成应用发布，但未生成安装包。"
    Write-Warning "请安装 Inno Setup 6: https://jrsoftware.org/isdl.php 后重新运行本脚本。"
    Write-Host   "你也可以直接运行发布目录下的 DesktopFences.exe 使用程序。"
    exit 0
}

Write-Host "    使用 ISCC: $iscc"

# 5) 生成安装包
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Host "==> 正在生成安装包 ..."
& $iscc $Iss
if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup 生成安装包失败。"
    exit 1
}

Write-Host ""
Write-Host "========================================================"
Write-Host " 构建成功！"
Write-Host " 安装包位置: $OutputDir\DesktopFencesSetup.exe"
Write-Host "========================================================"
