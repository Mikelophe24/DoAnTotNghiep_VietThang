# setup.ps1 - Chuan bi may moi de chay do an Viet Thang (chay 1 lan sau khi clone).
# Dung: powershell -ExecutionPolicy Bypass -File .\setup.ps1 [-SkipNpm] [-RunTests]
# (Script co y khong dung tieng Viet co dau de khong bi loi font tren PowerShell 5.1)
[CmdletBinding()]
param(
    [switch]$SkipNpm,    # bo qua npm install (khi da cai roi)
    [switch]$RunTests    # chay them 93 unit test sau khi build
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$webDir = Join-Path $root 'src\VietThang.Web'
$ngDir  = Join-Path $root 'src\vietthang-web'
$testDir = Join-Path $root 'tests\VietThang.Tests'
$hasError = $false

function Step($msg) { Write-Host "`n== $msg" -ForegroundColor Cyan }
function Ok($msg)   { Write-Host "   [OK]  $msg" -ForegroundColor Green }
function Warn($msg) { Write-Host "   [!]   $msg" -ForegroundColor Yellow }
function Bad($msg)  { Write-Host "   [LOI] $msg" -ForegroundColor Red; $script:hasError = $true }

# ---------- 1. .NET ----------
Step "Kiem tra .NET"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Bad "Chua cai .NET SDK. Tai .NET 8 SDK tai https://dotnet.microsoft.com/download/dotnet/8.0"
} else {
    $sdks = dotnet --list-sdks
    $runtimes = dotnet --list-runtimes
    Ok ("SDK: " + (($sdks | ForEach-Object { ($_ -split ' ')[0] }) -join ', '))
    if (-not ($runtimes -match 'Microsoft\.AspNetCore\.App 8\.')) {
        Bad "Thieu ASP.NET Core Runtime 8.x (project dung net8.0). Cai .NET 8 SDK se co san runtime nay."
    } else {
        Ok "Co ASP.NET Core Runtime 8.x"
    }
}

# ---------- 2. Node.js ----------
Step "Kiem tra Node.js / npm"
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Bad "Chua cai Node.js. Tai ban 20 LTS hoac 22 LTS tai https://nodejs.org"
} else {
    $nodeVer = (node --version).TrimStart('v')
    $major = [int]($nodeVer.Split('.')[0])
    if ($major -lt 18) { Bad "Node $nodeVer qua cu, Angular 18 can Node 18.19+ (khuyen dung 20 LTS)" }
    else { Ok "Node $nodeVer, npm $(npm --version)" }
}

# ---------- 3. SQL Server ----------
Step "Kiem tra SQL Server"
$appsettings = Get-Content (Join-Path $webDir 'appsettings.json') -Raw | ConvertFrom-Json
$conn = $appsettings.ConnectionStrings.DefaultConnection
Write-Host "   Chuoi ket noi: $conn"
if ($conn -match 'localdb') {
    if (-not (Get-Command sqllocaldb -ErrorAction SilentlyContinue)) {
        Bad "Chua co SQL Server LocalDB. Cai Visual Studio (workload ASP.NET) hoac bo cai SQL Server Express -> chon LocalDB."
        Warn "Hoac doi ConnectionStrings:DefaultConnection trong appsettings.json sang SQL Server Express: Server=.\SQLEXPRESS;Database=VietThangFashion;Trusted_Connection=True;TrustServerCertificate=True"
    } else {
        $info = sqllocaldb info
        if ($info -notcontains 'MSSQLLocalDB') {
            Warn "Chua co instance MSSQLLocalDB, dang tao..."
            sqllocaldb create MSSQLLocalDB | Out-Null
        }
        sqllocaldb start MSSQLLocalDB | Out-Null
        Ok "LocalDB MSSQLLocalDB dang chay"
    }
} else {
    Warn "Khong dung LocalDB. Hay chac chan SQL Server trong chuoi ket noi dang chay."
}

if ($hasError) {
    Write-Host "`nCon thieu phan mem o tren. Cai xong roi chay lai setup.ps1." -ForegroundColor Red
    exit 1
}

# ---------- 4. Build backend ----------
Step "Restore + build backend (src\VietThang.Web)"
dotnet build (Join-Path $webDir 'VietThang.Web.csproj') -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) { Bad "Build backend that bai"; exit 1 }
Ok "Build backend xong"

# ---------- 5. Unit test (tuy chon) ----------
if ($RunTests) {
    Step "Chay unit test (tests\VietThang.Tests)"
    dotnet test (Join-Path $testDir 'VietThang.Tests.csproj') --nologo -v q
    if ($LASTEXITCODE -ne 0) { Bad "Co test that bai"; exit 1 }
    Ok "Tat ca test dat"
}

# ---------- 6. npm install ----------
if (-not $SkipNpm) {
    Step "npm install cho Angular (src\vietthang-web) - lan dau mat vai phut"
    Push-Location $ngDir
    try {
        if (Test-Path 'package-lock.json') { npm ci } else { npm install }
        if ($LASTEXITCODE -ne 0) { Bad "npm install that bai"; exit 1 }
    } finally { Pop-Location }
    Ok "npm install xong"
}

# ---------- 7. Huong dan chay ----------
Write-Host ""
Write-Host "=============================================================" -ForegroundColor Green
Write-Host " SAN SANG. Mo 2 cua so terminal:" -ForegroundColor Green
Write-Host ""
Write-Host "  [1] Backend (API + Admin) - http://localhost:5292"
Write-Host "      cd src\VietThang.Web"
Write-Host "      dotnet run -- --seed-demo     # lan dau: tao CSDL + du lieu demo"
Write-Host "      dotnet run                    # cac lan sau"
Write-Host ""
Write-Host "  [2] Storefront Angular - http://localhost:4200"
Write-Host "      cd src\vietthang-web"
Write-Host "      npm start"
Write-Host ""
Write-Host "  Admin:  http://localhost:5292/Admin   admin@thoitrangvietthang.vn / Admin@123"
Write-Host "  Khach:  http://localhost:4200          khachhang@gmail.com / KhachHang@123"
Write-Host "  Chi tiet: docs\06-huong-dan-cai-dat.md"
Write-Host "=============================================================" -ForegroundColor Green
