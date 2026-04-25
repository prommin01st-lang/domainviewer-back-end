# DomainViewer API

Backend API สำหรับระบบจัดการ Domain แบบครบวงจร

---

## 🛠 Tech Stack

| ส่วนประกอบ | เทคโนโลยี |
|-----------|-----------|
| Framework | .NET 10 Web API |
| Database | PostgreSQL 15+ |
| ORM | Entity Framework Core 9 |
| Authentication | JWT Bearer |
| Validation | FluentValidation |
| Background Jobs | Quartz.NET |
| Email | SMTP (Gmail) |

---

## 📁 โครงสร้างโปรเจค

```
back-end/
├── DomainViewer.API/           # Web API Project (Entry Point)
│   ├── Common/                 # Middleware, Helpers, Base Classes
│   ├── Controllers/            # API Endpoints
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Validators/             # FluentValidation Rules
│   ├── appsettings.json        # ⚠️ ไม่รวมใน Git (สร้างเอง)
│   └── appsettings.Example.json # ตัวอย่าง config
├── DomainViewer.Core/          # Entities, Enums, Interfaces
├── DomainViewer.Infrastructure/ # DbContext, Services, Jobs
├── DomainViewer.Tests/         # Unit Tests
└── bruno/                      # API Collection สำหรับทดสอบ
```

---

## ⚙️ ความต้องการเบื้องต้น

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL](https://www.postgresql.org/download/)
- Git

---

## 🚀 ติดตั้งและรันบนเครื่องพัฒนา

### 1. Clone Repo

```bash
git clone https://github.com/USERNAME/domainviewer-api.git
cd domainviewer-api
```

### 2. สร้าง Config

```bash
cp DomainViewer.API/appsettings.Example.json DomainViewer.API/appsettings.json
```

แก้ไขค่าใน `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=domainViewer;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Key": "YOUR_VERY_LONG_SECRET_KEY_AT_LEAST_32_CHARS",
    "Issuer": "DomainViewer",
    "Audience": "DomainViewer",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 30
  },
  "Smtp": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Domain Viewer"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://domainviewer-font-end.vercel.app"
    ]
  }
}
```

### 3. รัน Database Migration

```bash
dotnet ef database update --project DomainViewer.Infrastructure --startup-project DomainViewer.API
```

### 4. รัน API

```bash
cd DomainViewer.API
dotnet run
```

API จะรันที่ `http://localhost:5000`

---

## 🐧 Deploy บน Linux Server

### 1. ติดตั้ง .NET 10 Runtime

```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

### 2. Clone และ Build

```bash
git clone https://github.com/USERNAME/domainviewer-api.git /opt/domainviewer-api
cd /opt/domainviewer-api

# สร้าง config
cp DomainViewer.API/appsettings.Example.json DomainViewer.API/appsettings.json
nano DomainViewer.API/appsettings.json  # แก้ค่าจริง

# Build และ Publish
dotnet publish DomainViewer.API/DomainViewer.API.csproj -c Release -o /opt/domainviewer
```

### 3. ตั้งค่า systemd Service

```bash
sudo cp /opt/domainviewer/domainviewer.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable domainviewer
sudo systemctl start domainviewer
```

### 4. เช็คสถานะ

```bash
sudo systemctl status domainviewer
sudo journalctl -u domainviewer -f
```

---

## 🔗 เชื่อมต่อกับ Frontend (BFF Pattern)

ตอนนี้ Frontend ใช้ **BFF (Backend for Frontend)** pattern — ไม่ได้เรียก Backend ตรงจาก Browser อีกต่อไป แต่เรียกผ่าน `/api/*` บน Next.js (Vercel) แล้ว Vercel Server จะ forward ไปหา Backend

### วิธีเชื่อมต่อ

**1. เปิด ngrok (ถ้ายังไม่มี Public IP)**

```bash
ngrok http 5000
```

**2. เอา ngrok URL ไปใส่ใน Vercel Environment Variables**

| Variable | Value | Example |
|----------|-------|---------|
| `API_PROXY_URL` | URL ของ backend | `https://xxx.ngrok-free.app` |

> ⚠️ อย่าใส่ `/api` ต่อท้าย ตัว BFF route จัดการให้เอง

**3. อัปเดต `Cors:AllowedOrigins` ใน `appsettings.json`**

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://your-vercel-app.vercel.app"
    ]
  }
}
```

**4. Restart backend**

```bash
sudo systemctl restart domainviewer
```

> 💡 **ถ้า server มี Public IP/Domain จริง**: ไม่ต้องใช้ ngrok เลย ตั้ง Nginx + SSL แล้วเอา `https://your-domain.com` ไปใส่ใน `API_PROXY_URL` แทน

---

## 📚 API Endpoints

| Endpoint | Method | คำอธิบาย |
|----------|--------|---------|
| `/api/auth/login` | POST | Login |
| `/api/auth/register` | POST | Register |
| `/api/auth/refresh-token` | POST | Refresh JWT Token |
| `/api/auth/me` | GET | ข้อมูลผู้ใช้ปัจจุบัน |
| `/api/domains` | GET | รายการ Domain |
| `/api/domains` | POST | สร้าง Domain |
| `/api/domains/{id}` | PUT | แก้ไข Domain |
| `/api/domains/{id}` | DELETE | ลบ Domain |
| `/api/users` | GET | รายการผู้ใช้ |
| `/api/dashboard/stats` | GET | สถิติ Dashboard |
| `/api/alertsettings` | GET/PUT | ตั้งค่าการแจ้งเตือน |
| `/api/notificationlogs` | GET | ประวัติการแจ้งเตือน |

---

## 🔐 ความปลอดภัย

- **JWT Secret** ต้องยาวอย่างน้อย 32 ตัวอักษร
- **appsettings.json** ไม่รวมใน Git (มีไฟล์ `.gitignore`)
- ใช้ **Gmail App Password** แทนรหัสผ่านปกติสำหรับ SMTP
- หมั่น **rotate secrets** เป็นระยะ

---

## 🧪 ทดสอบ API

ใช้ [Bruno](https://www.usebruno.com/) เปิดโฟลเดอร์ `bruno/` แล้วรัน collection ได้เลย

---

## 📝 License

Private Project
