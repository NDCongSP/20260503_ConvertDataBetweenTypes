# 🔁 Data Type Converter – Hướng dẫn đầy đủ

Ứng dụng **Blazor Server** dùng framework **Radzen.Blazor** để chuyển đổi 2 chiều giữa các kiểu dữ liệu:
**DEC ↔ HEX ↔ BIN ↔ OCT ↔ WORD ↔ BYTE ↔ ASCII**

---

## 📁 Cấu trúc dự án

```
ConvertDataBetweenDataType/
├── ConvertDataBetweenDataType.csproj   ← Project file (.NET 8)
├── Program.cs                           ← Cấu hình DI, Middleware
├── App.razor                            ← Router gốc
├── _Imports.razor                       ← Global using
├── appsettings.json
├── appsettings.Development.json
├── web.config                           ← Cấu hình IIS (ANCM)
│
├── Models/
│   └── ConversionModels.cs              ← DataType enum, ConversionResult, ConversionStep
│
├── Services/
│   └── ConversionService.cs             ← Toàn bộ logic chuyển đổi + diễn giải
│
├── Pages/
│   ├── _Layout.cshtml                   ← HTML shell (Radzen CSS/JS)
│   ├── _Host.cshtml                     ← Blazor Server entry point
│   ├── Index.razor                      ← Trang chuyển đổi chính
│   ├── Reference.razor                  ← Bảng tham khảo DEC/HEX/BIN/OCT
│   └── Error.cshtml
│
├── Shared/
│   └── MainLayout.razor                 ← Layout với RadzenLayout + Sidebar
│
├── wwwroot/
│   └── css/app.css                      ← Custom styles
│
└── Properties/
    ├── launchSettings.json              ← VS debug profiles
    └── PublishProfiles/
        ├── FolderProfile.pubxml         ← Publish ra folder
        └── IISProfile.pubxml            ← Deploy trực tiếp Web Deploy
```

---

## 🖥️ BƯỚC 1 – Yêu cầu cài đặt

| Phần mềm | Phiên bản | Link tải |
|---|---|---|
| **.NET SDK** | 8.0 trở lên | https://dotnet.microsoft.com/download |
| **Visual Studio** | 2022 (17.8+) | https://visualstudio.microsoft.com |
| **ASP.NET workload** | Bắt buộc | Chọn khi cài VS |
| **IIS** (chỉ để deploy) | Windows Feature | Bật qua "Turn Windows features on/off" |
| **ASP.NET Core Module** | Bắt buộc cho IIS | Cài .NET Hosting Bundle |

---

## 🚀 BƯỚC 2 – Chạy Debug trên Visual Studio

### 2.1. Mở dự án
```
File → Open → Project/Solution
→ Chọn: ConvertDataBetweenDataType.csproj
```

### 2.2. Restore NuGet packages
Visual Studio tự động restore khi mở. Nếu không:
```
Tools → NuGet Package Manager → Manage NuGet Packages for Solution
→ Click "Restore"
```
Hoặc dùng terminal:
```bash
cd đường_dẫn_tới_thư_mục_project
dotnet restore
```

### 2.3. Chọn profile debug
Thanh toolbar → dropdown bên cạnh nút ▶ → chọn một trong:

| Profile | Mô tả |
|---|---|
| **ConvertDataBetweenDataType** | Chạy Kestrel (khuyên dùng), mở https://localhost:7200 |
| **IIS Express** | Chạy qua IIS Express, mở http://localhost:5200 |

### 2.4. Chạy debug
Nhấn **F5** (với breakpoint) hoặc **Ctrl+F5** (không breakpoint).

Trình duyệt tự mở trang chủ ứng dụng.

### 2.5. Sửa lỗi certificate (nếu có)
Nếu trình duyệt báo "Your connection is not private":
```bash
dotnet dev-certs https --trust
```
→ Nhấn "Yes" khi Windows hỏi trust certificate.

---

## 📦 BƯỚC 3 – Publish ra thư mục

### 3.1. Dùng Visual Studio
```
Build → Publish ConvertDataBetweenDataType
→ Chọn profile: FolderProfile
→ Click "Publish"
```
Output mặc định: `bin\Release\net8.0\publish\`

### 3.2. Dùng dotnet CLI (nhanh hơn)
```bash
dotnet publish -c Release -r win-x64 --no-self-contained -o ./publish
```

---

## 🌐 BƯỚC 4 – Deploy lên IIS

### 4.1. Cài .NET Hosting Bundle trên máy chủ IIS
Tải về: https://dotnet.microsoft.com/download/dotnet/8.0
→ Chọn: **"Hosting Bundle"** (không phải SDK)
→ Cài và **restart IIS**:
```cmd
net stop was /y
net start w3svc
```

### 4.2. Tạo Application Pool trên IIS
```
IIS Manager → Application Pools → Add Application Pool
    Name: ConvertDataAppPool
    .NET CLR Version: No Managed Code   ← QUAN TRỌNG
    Managed Pipeline Mode: Integrated
```

### 4.3. Tạo Website / Application trên IIS
```
IIS Manager → Sites → Add Website
    Site name: ConvertDataBetweenDataType
    Application pool: ConvertDataAppPool
    Physical path: C:\inetpub\wwwroot\ConvertDataBetweenDataType
    Port: 80 (hoặc tuỳ chọn)
```

### 4.4. Copy file publish lên thư mục IIS
```cmd
xcopy /E /I /Y publish\* "C:\inetpub\wwwroot\ConvertDataBetweenDataType\"
```

### 4.5. Cấp quyền cho IIS
```cmd
icacls "C:\inetpub\wwwroot\ConvertDataBetweenDataType" /grant "IIS AppPool\ConvertDataAppPool:(OI)(CI)F"
```

### 4.6. Truy cập ứng dụng
Mở trình duyệt: `http://localhost` (hoặc cổng bạn chọn).

---

## 🔧 BƯỚC 5 – Troubleshooting IIS phổ biến

| Lỗi | Nguyên nhân | Cách fix |
|---|---|---|
| **HTTP 500.19** | web.config sai cú pháp | Kiểm tra web.config |
| **HTTP 500.30** | App không start | Bật stdout log, xem log |
| **HTTP 403** | Thiếu quyền thư mục | Chạy lại lệnh icacls |
| **HTTP 502.5** | .NET Runtime không tìm thấy | Cài lại .NET Hosting Bundle |
| **Blank page** | JavaScript Blazor lỗi | Mở F12, xem Console |

### Bật stdout log để debug IIS:
Mở `web.config`, đổi:
```xml
stdoutLogEnabled="true"
```
Tạo thư mục `logs\` trong thư mục app, rồi xem file `logs\stdout_*.log`.

---

## 🔄 Chức năng ứng dụng

### Trang chính (/)
- Chọn **Kiểu dữ liệu nguồn** (From): DEC, HEX, BIN, OCT, WORD, BYTE, ASCII
- Nhập **Giá trị** (có validation real-time)
- Chọn **Kiểu dữ liệu đích** (To)
- Nhấn **Chuyển đổi** → Xem kết quả + diễn giải từng bước
- Nhấn **Hoán đổi** → đổi from/to tự động
- Xem **Bảng tất cả kiểu** cùng lúc

### Trang tham khảo (/reference)
- Bảng so sánh DEC/HEX/OCT/BIN/WORD từ 0–255
- Bảng mã ASCII chuẩn (32–126)

---

## 📌 Các cặp chuyển đổi hỗ trợ

| From → To | Thuật toán |
|---|---|
| DEC → HEX | Chia liên tiếp cho 16, lấy dư đọc ngược |
| DEC → BIN | Chia liên tiếp cho 2, lấy dư đọc ngược |
| DEC → OCT | Chia liên tiếp cho 8, lấy dư đọc ngược |
| HEX → DEC | Mỗi chữ số × 16^vị_trí, cộng lại |
| BIN → DEC | Mỗi bit × 2^vị_trí, cộng lại |
| OCT → DEC | Mỗi chữ số × 8^vị_trí, cộng lại |
| DEC → WORD | Biểu diễn 16-bit, chia High/Low byte |
| DEC → BYTE | Biểu diễn 8-bit |
| DEC → ASCII | Tra bảng ASCII[value] |
| ASCII → DEC | Lấy mã ASCII của ký tự |
| Tất cả ↔ Tất cả | Qua số nguyên trung gian (long) |
