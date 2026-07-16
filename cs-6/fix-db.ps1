# ============================================================
# 智慧酒店管理系统（cs-6）- SQL Server 连接检测与修复脚本
# 用法：在 cs-6 目录下以 PowerShell 执行 .\fix-db.ps1
# 功能：检测实例→启动服务→建库→补表→补初始数据→修复连接字符串→验证
# 区别于 cs-5：HotelDB 数据库、SHA-256 密码哈希、T_RoomType 种子表
# ============================================================

$ErrorActionPreference = "Stop"
$AppConfigPath = ".\App.config"

Write-Host "`n========== 智慧酒店管理系统 - 数据库修复工具 ==========" -ForegroundColor Cyan

# ------------------------------------------------------------
# 辅助函数：计算 SHA-256 哈希（与 SecurityUtil 保持一致）
# ------------------------------------------------------------
function Get-Sha256Hash {
    param([string]$Input)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Input)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $hashBytes = $sha.ComputeHash($bytes)
    return [BitConverter]::ToString($hashBytes).Replace("-", "").ToUpper()
}

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

# 4.1 确保 HotelDB 存在
$dbExists = [int](Invoke-SqlCmdScalar -Server $serverName -Db "master" -Sql "SELECT COUNT(*) FROM sys.databases WHERE name='HotelDB'")
if ($dbExists -eq 0) {
    Write-Host "  [!] HotelDB 不存在，正在创建..." -ForegroundColor DarkYellow
    sqlcmd -S $serverName -E -Q "CREATE DATABASE HotelDB;" -f 65001 2>&1 | Out-Null
    Write-Host "  [OK] HotelDB 已创建" -ForegroundColor Green
} else {
    Write-Host "  [OK] HotelDB 数据库已存在" -ForegroundColor Green
}

# 4.2 检查 T_User 表是否存在（作为9张表是否完整的代表）
$userTableExists = [int](Invoke-SqlCmdScalar -Server $serverName -Db "HotelDB" -Sql "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='T_User'")

if ($userTableExists -eq 0) {
    Write-Host "  [!] 数据表缺失，正在执行 init.sql 建表..." -ForegroundColor DarkYellow
    $initSql = ".\Database\init.sql"
    if (-not (Test-Path $initSql)) { Write-Host "  [X] 未找到 $initSql" -ForegroundColor Red; exit 1 }
    $r = sqlcmd -S $serverName -d HotelDB -E -i $initSql -f 65001 2>&1
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
$adminCount = [int](Invoke-SqlCmdScalar -Server $serverName -Db "HotelDB" -Sql "SELECT COUNT(*) FROM T_User WHERE userName='admin'")
$frontCount = [int](Invoke-SqlCmdScalar -Server $serverName -Db "HotelDB" -Sql "SELECT COUNT(*) FROM T_User WHERE userName='front01'")

# 动态计算 SHA-256 哈希（与 SecurityUtil.ComputeSha256Hash 保持一致）
$adminPwdHash = Get-Sha256Hash "admin123"
$frontPwdHash = Get-Sha256Hash "front123"

# 验证哈希值长度，防止因脚本编码问题导致变量为空
if ($adminPwdHash.Length -ne 64) {
    Write-Host "  [X] admin 密码哈希计算异常，长度=$($adminPwdHash.Length)（应为64），请检查脚本编码" -ForegroundColor Red
    exit 1
}
if ($frontPwdHash.Length -ne 64) {
    Write-Host "  [X] front01 密码哈希计算异常，长度=$($frontPwdHash.Length)（应为64），请检查脚本编码" -ForegroundColor Red
    exit 1
}

if ($adminCount -eq 0 -or $frontCount -eq 0) {
    Write-Host "  [!] 默认用户缺失（admin/front01），正在补插入..." -ForegroundColor DarkYellow
    # 使用 .NET SqlClient 参数化查询，避免 here-string 拼接中因编码问题导致变量为空
    $connStr = "Server=$serverName;Database=HotelDB;Integrated Security=True;TrustServerCertificate=True;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    try {
        $conn.Open()
        if ($adminCount -eq 0) {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "INSERT INTO T_User (userName, userPassword, userPurview, realName) VALUES (@name, @pwd, @purview, @realName)"
            [void]$cmd.Parameters.AddWithValue("@name", "admin")
            [void]$cmd.Parameters.AddWithValue("@pwd", $adminPwdHash)
            [void]$cmd.Parameters.AddWithValue("@purview", "管理员")
            [void]$cmd.Parameters.AddWithValue("@realName", "系统管理员")
            [void]$cmd.ExecuteNonQuery()
        }
        if ($frontCount -eq 0) {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = "INSERT INTO T_User (userName, userPassword, userPurview, realName) VALUES (@name, @pwd, @purview, @realName)"
            [void]$cmd.Parameters.AddWithValue("@name", "front01")
            [void]$cmd.Parameters.AddWithValue("@pwd", $frontPwdHash)
            [void]$cmd.Parameters.AddWithValue("@purview", "前台")
            [void]$cmd.Parameters.AddWithValue("@realName", "前台操作员")
            [void]$cmd.ExecuteNonQuery()
        }
        Write-Host "  [OK] 默认用户已补全：admin/admin123（管理员）、front01/front123（前台）" -ForegroundColor Green
    } finally {
        $conn.Close()
    }
} else {
    Write-Host "  [OK] 默认用户 admin 和 front01 均已存在" -ForegroundColor Green
}

# 4.4 检查其他基础数据表是否有数据（T_RoomType 作为代表，因为种子数据前置依赖）
$roomTypeCount = [int](Invoke-SqlCmdScalar -Server $serverName -Db "HotelDB" -Sql "SELECT COUNT(*) FROM T_RoomType")
if ($roomTypeCount -eq 0) {
    Write-Host "  [!] 基础数据缺失，正在执行 seed_data.sql 填充测试数据..." -ForegroundColor DarkYellow
    $seedSql = ".\Database\seed_data.sql"
    if (Test-Path $seedSql) {
        $r = sqlcmd -S $serverName -d HotelDB -E -i $seedSql -f 65001 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  [OK] seed_data.sql 执行完成（测试数据已填充）" -ForegroundColor Green
        } else {
            Write-Host "  [!] seed_data.sql 部分失败（可能因为外键依赖顺序问题，基础用户已可用，不影响登录）" -ForegroundColor DarkYellow
        }
    }
} else {
    Write-Host "  [OK] 基础数据已存在（客房类型 $roomTypeCount 条）" -ForegroundColor Green
}

# ------------------------------------------------------------
# 步骤 5：修复 App.config 和 bin 目录下的连接字符串
# ------------------------------------------------------------
Write-Host "`n[5/6] 修复连接字符串配置..." -ForegroundColor Yellow

if (-not (Test-Path $AppConfigPath)) {
    Write-Host "  [X] 未找到 $AppConfigPath" -ForegroundColor Red; exit 1
}

[xml]$config = Get-Content $AppConfigPath -Encoding UTF8
$addNode = $config.configuration.connectionStrings.add | Where-Object { $_.name -eq "HotelDB" }
if (-not $addNode) { Write-Host "  [X] App.config 中未找到 HotelDB 连接字符串" -ForegroundColor Red; exit 1 }

$expectedConnStr = "Server=$serverName;Database=HotelDB;Integrated Security=True;TrustServerCertificate=True;"
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
    ".\bin\Debug\net8.0-windows\HotelSys.dll.config",
    ".\bin\Release\net8.0-windows\HotelSys.dll.config"
)
foreach ($bc in $binConfigs) {
    if (Test-Path $bc) {
        try {
            [xml]$bx = Get-Content $bc -Encoding UTF8
            $ba = $bx.configuration.connectionStrings.add | Where-Object { $_.name -eq "HotelDB" }
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
    $cmd.CommandText = "SELECT COUNT(*) FROM T_User; SELECT COUNT(*) FROM T_RoomType; SELECT COUNT(*) FROM T_Room;"
    $da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $da.Fill($ds) | Out-Null
    $userCnt = [int]$ds.Tables[0].Rows[0][0]
    $typeCnt = [int]$ds.Tables[1].Rows[0][0]
    $roomCnt = [int]$ds.Tables[2].Rows[0][0]
    $c.Close()

    Write-Host "  [OK] 数据库连接成功！T_User 共 $userCnt 个用户，T_RoomType 共 $typeCnt 种类型，T_Room 共 $roomCnt 间客房。" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  修复完成！默认登录账号：" -ForegroundColor White
    Write-Host "    管理员：admin    /  admin123" -ForegroundColor Cyan
    Write-Host "    前台：  front01  /  front123" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Green
} catch {
    Write-Host "  [X] 连接验证失败：$($_.Exception.Message)" -ForegroundColor Red
    exit 1
}