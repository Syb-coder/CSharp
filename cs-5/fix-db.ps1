# ============================================================
# 智慧图书馆管理系统（cs-5）- SQL Server 连接检测与修复脚本
# 用法：在 cs-5 目录下以 PowerShell 执行 .\fix-db.ps1
# 功能：检测实例→启动服务→建库→补表→补初始数据→修复连接字符串→验证
# ============================================================

$ErrorActionPreference = "Stop"
$AppConfigPath = ".\App.config"

Write-Host "`n========== 智慧图书馆管理系统 - 数据库修复工具 ==========" -ForegroundColor Cyan

# ------------------------------------------------------------
# 步骤 1：检测所有 SQL Server 实例
# ------------------------------------------------------------
Write-Host "`n[1/6] 检测已安装的 SQL Server 实例..." -ForegroundColor Yellow

$instances = @()

$regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
if (Test-Path $regPath) {
    $regInstances = Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue
    if ($regInstances) {
        $regInstances.PSObject.Properties | Where-Object { $_.Name -notmatch "^PS" } | ForEach-Object {
            if ($instances -notcontains $_.Name) { $instances += $_.Name }
        }
    }
}

$expressService = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction SilentlyContinue
if ($expressService -and $instances -notcontains "SQLEXPRESS") { $instances += "SQLEXPRESS" }

$defaultService = Get-Service -Name "MSSQLSERVER" -ErrorAction SilentlyContinue
if ($defaultService -and $instances -notcontains "MSSQLSERVER") { $instances += "MSSQLSERVER" }

if ($instances.Count -eq 0) {
    Write-Host "  [X] 未检测到任何 SQL Server 实例！" -ForegroundColor Red
    Write-Host "  请安装 SQL Server Express：https://www.microsoft.com/zh-cn/sql-server/sql-server-downloads" -ForegroundColor White
    Write-Host "  安装时建议勾选「默认实例」；若选命名实例 SQLEXPRESS 也可，本脚本会自动适配。" -ForegroundColor White
    exit 1
}

Write-Host "  检测到以下实例：" -ForegroundColor Green
foreach ($inst in $instances) {
    $dn = if ($inst -eq "MSSQLSERVER") { "默认实例 (MSSQLSERVER)" } else { "命名实例 ($inst)" }
    Write-Host "    - $dn" -ForegroundColor Green
}

$serverName = if ($instances -contains "MSSQLSERVER") { "localhost" } else { "localhost\$($instances[0])" }
Write-Host "  将使用服务器名：$serverName" -ForegroundColor Cyan

# ------------------------------------------------------------
# 步骤 2：检测 SQL Server Browser 服务（命名实例需要）
# ------------------------------------------------------------
Write-Host "`n[2/6] 检测 SQL Server Browser 服务..." -ForegroundColor Yellow

if ($serverName -ne "localhost") {
    $browserService = Get-Service -Name "SQLBrowser" -ErrorAction SilentlyContinue
    if ($browserService) {
        if ($browserService.Status -ne "Running") {
            Write-Host "  [!] SQL Server Browser 未运行，正在启动..." -ForegroundColor DarkYellow
            try { Start-Service -Name "SQLBrowser" -ErrorAction Stop; Write-Host "  [OK] Browser 已启动" -ForegroundColor Green }
            catch { Write-Host "  [!] 无法启动 Browser（请确保服务已启用）" -ForegroundColor DarkYellow }
        } else {
            Write-Host "  [OK] SQL Server Browser 正在运行" -ForegroundColor Green
        }
    }
} else {
    Write-Host "  [-] 默认实例无需 Browser 服务" -ForegroundColor DarkGray
}

# ------------------------------------------------------------
# 步骤 3：启动目标 SQL Server 实例服务
# ------------------------------------------------------------
Write-Host "`n[3/6] 检测并启动 SQL Server 服务..." -ForegroundColor Yellow

$serviceName = if ($instances[0] -eq "MSSQLSERVER") { "MSSQLSERVER" } else { "MSSQL`$" + $instances[0] }
$sqlService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($sqlService) {
    if ($sqlService.Status -ne "Running") {
        Write-Host "  [!] $serviceName 未运行，正在启动..." -ForegroundColor DarkYellow
        Start-Service -Name $serviceName -ErrorAction Stop
        Start-Sleep -Seconds 3
        Write-Host "  [OK] $serviceName 已启动" -ForegroundColor Green
    } else {
        Write-Host "  [OK] $serviceName 正在运行" -ForegroundColor Green
    }
} else {
    Write-Host "  [X] 未找到服务 $serviceName" -ForegroundColor Red
    exit 1
}

# ------------------------------------------------------------
# 辅助函数：通过 sqlcmd 执行 SQL，返回 stdout
# ------------------------------------------------------------
function Invoke-SqlCmdScalar {
    param(
        [string]$Server,
        [string]$Db,
        [string]$Sql
    )
    $result = sqlcmd -S $Server -d $Db -E -Q "SET NOCOUNT ON; $Sql" -h -1 -W 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SQL 执行失败: $result"
    }
    return ($result | Select-Object -First 1).ToString().Trim()
}

# ------------------------------------------------------------
# 步骤 4：创建数据库、补建缺失表、补全初始数据
# ------------------------------------------------------------
Write-Host "`n[4/6] 检测数据库与表结构完整性..." -ForegroundColor Yellow

$sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmdPath) {
    Write-Host "  [X] 未找到 sqlcmd 命令！" -ForegroundColor Red
    Write-Host "  请安装 SQL Server 命令行工具，或安装 SSMS（自带 sqlcmd）。" -ForegroundColor White
    exit 1
}

# 4.1 确保 LibraryDB 存在
$dbExists = [int](Invoke-SqlCmdScalar -Server $serverName -Db "master" -Sql "SELECT COUNT(*) FROM sys.databases WHERE name='LibraryDB'")
if ($dbExists -eq 0) {
    Write-Host "  [!] LibraryDB 不存在，正在创建..." -ForegroundColor DarkYellow
    sqlcmd -S $serverName -E -Q "CREATE DATABASE LibraryDB;" -f 65001 2>&1 | Out-Null
    Write-Host "  [OK] LibraryDB 已创建" -ForegroundColor Green
} else {
    Write-Host "  [OK] LibraryDB 数据库已存在" -ForegroundColor Green
}

# 4.2 检查 T_User 表是否存在（作为8张表是否完整的代表）
$userTableExists = [int](Invoke-SqlCmdScalar -Server $serverName -Db "LibraryDB" -Sql "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='T_User'")

if ($userTableExists -eq 0) {
    Write-Host "  [!] 数据表缺失，正在执行 init.sql 建表..." -ForegroundColor DarkYellow
    $initSql = ".\Database\init.sql"
    if (-not (Test-Path $initSql)) { Write-Host "  [X] 未找到 $initSql" -ForegroundColor Red; exit 1 }
    $r = sqlcmd -S $serverName -d LibraryDB -E -i $initSql -f 65001 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] init.sql 执行完成（表结构 + 初始数据）" -ForegroundColor Green
    } else {
        Write-Host "  [X] init.sql 执行失败：$r" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  [OK] 数据表结构已存在" -ForegroundColor Green
}

# 4.3 关键检查：T_User 中是否有默认用户（防止编码问题导致 INSERT 失败）
Write-Host "`n  检查初始用户数据..." -ForegroundColor Yellow
$adminCount = [int](Invoke-SqlCmdScalar -Server $serverName -Db "LibraryDB" -Sql "SELECT COUNT(*) FROM T_User WHERE userName='admin'")
$user01Count = [int](Invoke-SqlCmdScalar -Server $serverName -Db "LibraryDB" -Sql "SELECT COUNT(*) FROM T_User WHERE userName='user01'")

if ($adminCount -eq 0 -or $user01Count -eq 0) {
    Write-Host "  [!] 默认用户缺失（admin/user01），正在补插入..." -ForegroundColor DarkYellow

    # admin123 -> MD5 = 0192023A7BBD73250516F069DF18B500
    # user123  -> MD5 = 6AD14BA9986E3615423DFCA256D04E3F
    $fixUserSql = @"
IF NOT EXISTS (SELECT 1 FROM T_User WHERE userName='admin')
    INSERT INTO T_User (userName, userPassword, userPurview) VALUES (N'admin', N'0192023A7BBD73250516F069DF18B500', N'管理员');
IF NOT EXISTS (SELECT 1 FROM T_User WHERE userName='user01')
    INSERT INTO T_User (userName, userPassword, userPurview) VALUES (N'user01', N'6AD14BA9986E3615423DFCA256D04E3F', N'普通用户');
"@
    sqlcmd -S $serverName -d LibraryDB -E -Q $fixUserSql -f 65001 2>&1 | Out-Null
    Write-Host "  [OK] 默认用户已补全：admin/admin123（管理员）、user01/user123（普通用户）" -ForegroundColor Green
} else {
    Write-Host "  [OK] 默认用户 admin 和 user01 均已存在" -ForegroundColor Green
}

# 4.4 检查其他基础数据表是否有数据（T_BookType 作为代表，因为种子数据前置依赖）
$bookTypeCount = [int](Invoke-SqlCmdScalar -Server $serverName -Db "LibraryDB" -Sql "SELECT COUNT(*) FROM T_BookType")
if ($bookTypeCount -eq 0) {
    Write-Host "  [!] 基础数据缺失，正在执行 seed_data.sql 填充测试数据..." -ForegroundColor DarkYellow
    $seedSql = ".\Database\seed_data.sql"
    if (Test-Path $seedSql) {
        $r = sqlcmd -S $serverName -d LibraryDB -E -i $seedSql -f 65001 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [OK] seed_data.sql 执行完成（测试数据已填充）" -ForegroundColor Green
        } else {
            Write-Host "  [!] seed_data.sql 部分失败（可能因为外键依赖顺序问题，基础用户已可用，不影响登录）" -ForegroundColor DarkYellow
        }
    }
} else {
    Write-Host "  [OK] 基础数据已存在（图书类型 $bookTypeCount 条）" -ForegroundColor Green
}

# ------------------------------------------------------------
# 步骤 5：修复 App.config 和 bin 目录下的连接字符串
# ------------------------------------------------------------
Write-Host "`n[5/6] 修复连接字符串配置..." -ForegroundColor Yellow

if (-not (Test-Path $AppConfigPath)) {
    Write-Host "  [X] 未找到 $AppConfigPath" -ForegroundColor Red; exit 1
}

[xml]$config = Get-Content $AppConfigPath -Encoding UTF8
$addNode = $config.configuration.connectionStrings.add | Where-Object { $_.name -eq "LibraryDB" }
if (-not $addNode) { Write-Host "  [X] App.config 中未找到 LibraryDB 连接字符串" -ForegroundColor Red; exit 1 }

$expectedConnStr = "Server=$serverName;Database=LibraryDB;Integrated Security=True;TrustServerCertificate=True;"
$currentServer = ""
if ($addNode.connectionString -match "Server=([^;]+)") { $currentServer = $matches[1] }

if ($currentServer -ne $serverName) {
    Write-Host "  [!] 修改 Server：$currentServer -> $serverName" -ForegroundColor DarkYellow
    $addNode.connectionString = $expectedConnStr
    $config.Save($AppConfigPath)
    Write-Host "  [OK] App.config 已更新" -ForegroundColor Green
} else {
    Write-Host "  [OK] App.config 连接字符串正确" -ForegroundColor Green
}

$binConfigs = @(
    ".\bin\Debug\net8.0-windows\LibrarySys.dll.config",
    ".\bin\Release\net8.0-windows\LibrarySys.dll.config"
)
foreach ($bc in $binConfigs) {
    if (Test-Path $bc) {
        try {
            [xml]$bx = Get-Content $bc -Encoding UTF8
            $ba = $bx.configuration.connectionStrings.add | Where-Object { $_.name -eq "LibraryDB" }
            if ($ba -and $ba.connectionString -ne $expectedConnStr) {
                $ba.connectionString = $expectedConnStr
                $bx.Save($bc)
                Write-Host "  [OK] 已同步：$bc" -ForegroundColor Green
            }
        } catch {
            Write-Host "  [-] 跳过 $bc" -ForegroundColor DarkGray
        }
    }
}

# ------------------------------------------------------------
# 步骤 6：最终验证
# ------------------------------------------------------------
Write-Host "`n[6/6] 最终验证..." -ForegroundColor Yellow

try {
    Add-Type -AssemblyName "System.Data" -ErrorAction SilentlyContinue
    $c = New-Object System.Data.SqlClient.SqlConnection
    $c.ConnectionString = "$expectedConnStr Connection Timeout=10;"
    $c.Open()
    $cmd = $c.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM T_User; SELECT COUNT(*) FROM T_BookType;"
    $da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $da.Fill($ds) | Out-Null
    $userCnt = [int]$ds.Tables[0].Rows[0][0]
    $typeCnt = [int]$ds.Tables[1].Rows[0][0]
    $c.Close()

    Write-Host "  [OK] 数据库连接成功！T_User 共 $userCnt 个用户，T_BookType 共 $typeCnt 种类型。" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  修复完成！默认登录账号：" -ForegroundColor White
    Write-Host "    管理员：admin  /  admin123" -ForegroundColor Cyan
    Write-Host "    普通用户：user01 / user123" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Green
} catch {
    Write-Host "  [X] 连接验证失败：$($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
