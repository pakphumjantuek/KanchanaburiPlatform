# Kanchanaburi Platform

แพลตฟอร์มดิจิทัลเพื่อยกระดับคอนเทนต์ท้องถิ่นและเศรษฐกิจสร้างสรรค์ของจังหวัดกาญจนบุรี รวมตลาดออนไลน์สำหรับร้านค้าและสินค้าชุมชน ระบบสั่งซื้อและชำระเงิน เข้ากับเนื้อหาท้องถิ่นพร้อมแผนที่ ให้ผู้ซื้อ ร้านค้า และผู้ดูแลระบบใช้งานได้ในที่เดียว

<!-- TODO: ใส่ภาพหน้าจอ 3–4 ภาพที่นี่ เช่น หน้าหลัก หน้าร้านค้า หน้าตะกร้า และแดชบอร์ดแอดมิน
![หน้าหลัก](docs/screenshots/home.png)
-->

## ระบบนี้ทำอะไรได้

| กลุ่มงาน | สิ่งที่ทำได้ |
|---|---|
| **ร้านค้าและสินค้า** | เปิดร้าน จัดการหมวดหมู่ร้านและสินค้า ดีลส่วนลดสินค้า (เช่น Flash Deal) และรีวิวทั้งสินค้าและร้านค้า |
| **ตะกร้าและคำสั่งซื้อ** | ใส่ตะกร้า สั่งซื้อ จัดการที่อยู่จัดส่ง อัปเดตสถานะการจัดส่ง และคำสั่งซื้อที่ค้างชำระเกินเวลาจะหมดอายุอัตโนมัติ |
| **การชำระเงิน** | ชำระผ่านบัตรด้วย Stripe (PaymentIntent และ Webhook) หรือโอนเงินแล้วอัปโหลดสลิป ให้แอดมินตรวจสอบ |
| **เนื้อหาท้องถิ่น** | เนื้อหา หมวดหมู่ แท็ก กำหนดการ ลิงก์วิดีโอ และแผนที่ตำแหน่งด้วย Leaflet |
| **ฝั่งร้านค้า** | ดูคำสั่งซื้อของร้านและส่งออกข้อมูลเป็น CSV และ PDF |
| **ฝั่งผู้ดูแลระบบ** | แดชบอร์ดสรุปยอดขายและสถานะออเดอร์ ตรวจสลิป จัดการข้อมูลทั้งระบบ ติดตามการจ่ายเงินให้ร้านค้า (payout) และส่งออกรายงาน CSV/PDF |

รายการ API ทั้งหมดอยู่ใน [backend/README.md](backend/README.md)

## เทคโนโลยีที่ใช้

| ส่วน | เทคโนโลยี |
|---|---|
| **Backend** | ASP.NET Core Web API (.NET 9), Entity Framework Core 9, SQL Server, ASP.NET Core Identity (bearer token), Stripe.net, QuestPDF, Swagger |
| **Frontend** | Vue 3, TypeScript, Vite, Pinia, Vue Router, Vuetify, Tailwind CSS 4, Leaflet, Chart.js, VeeValidate, Stripe.js |

## จุดเด่นทางเทคนิค

- **Clean Architecture** แยกเป็น `API` → `Application` → `Domain` และ `Infrastructure` ใช้ Repository, Unit of Work และ Specification Pattern
- **รับชำระเงินแบบ Webhook** ยืนยันผลการชำระจาก Stripe ฝั่งเซิร์ฟเวอร์ ไม่เชื่อผลจากฝั่งผู้ใช้
- **งานเบื้องหลังตรวจออเดอร์หมดเวลา** Worker รันทุก 1 นาที เพื่อปิดคำสั่งซื้อที่ยังไม่ชำระเกินกำหนดให้เป็นหมดอายุ
- **แดชบอร์ดที่คำนวณใน SQL** สรุปยอดฝั่งฐานข้อมูล พร้อม composite index บนตารางคำสั่งซื้อเพื่อลดภาระการดึงข้อมูล
- **ส่งออกรายงาน** CSV และ PDF แนวนอน (QuestPDF) ทั้งฝั่งร้านค้าและแอดมิน
- **สิทธิ์ตามบทบาทและความเป็นเจ้าของ** เช่น เฉพาะผู้เขียนหรือแอดมินที่จัดการเนื้อหาได้ มีโปรแกรมตรวจกฎความเป็นเจ้าของเนื้อหา (`backend/tests/ContentOwnershipChecks`)

## โครงสร้างโปรเจกต์

```
KanchanaburiPlatform/
├── backend/
│   ├── KanchanaburiPlatform.API/             Controllers, Program.cs, Worker
│   ├── KanchanaburiPlatform.Application/     Specifications และกฎของระบบ
│   ├── KanchanaburiPlatform.Domain/          Entities
│   ├── KanchanaburiPlatform.Infrastructure/  EF Core, Services, Migrations
│   └── tests/ContentOwnershipChecks/         โปรแกรมตรวจกฎสิทธิ์เนื้อหา
└── frontend/                                 Vue 3 + TypeScript (Vite)
```

## วิธีรันบนเครื่อง

**สิ่งที่ต้องมี:** .NET 9 SDK, Node.js, SQL Server (หรือ SQL Server Express) และ `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

### 1) Backend

```powershell
cd backend\KanchanaburiPlatform.API

dotnet user-secrets init

# ฐานข้อมูล (ไม่เก็บไว้ในโค้ด)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=KanchanaburiPlatform;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"

# Stripe (ใช้คีย์ทดสอบ sk_test_... และ whsec_... จากบัญชี Stripe ของคุณ)
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."

# สร้างตารางในฐานข้อมูล
dotnet ef database update --project ..\KanchanaburiPlatform.Infrastructure --startup-project .

dotnet run
```

API อยู่ที่ `https://localhost:7289` (ตั้งไว้ใน `Properties/launchSettings.json`) และมี Swagger ให้ลองเรียก

> ในโค้ดยังไม่มีการสร้างผู้ดูแลระบบอัตโนมัติ การให้บทบาท `Admin` กับบัญชีใดบัญชีหนึ่งต้องเพิ่มเองในฐานข้อมูล (ตาราง `AspNetRoles` และ `AspNetUserRoles`)

### 2) Frontend

```powershell
cd frontend
copy .env.example .env
```

แก้ `.env` ให้ `VITE_API_URL` ชี้ไปที่ API และใส่ `VITE_STRIPE_PUBLISHABLE_KEY` (คีย์แบบเปิดเผยได้ `pk_test_...` เท่านั้น ห้ามใส่ Secret Key) แล้วรัน:

```powershell
npm install
npm run dev
```

เปิด `http://localhost:5173` (backend อนุญาต CORS จากพอร์ตนี้ไว้แล้ว)

## ผู้พัฒนา

**ภาคภูมิ จันทร์ทึก** · นักพัฒนาเว็บแอปพลิเคชัน

- GitHub: [github.com/pakphumjantuek](https://github.com/pakphumjantuek)
- อีเมล: _(ใส่อีเมลที่ต้องการให้ติดต่อ)_

> โปรเจกต์นี้พัฒนาเพื่อการเรียนรู้และแสดงผลงาน ข้อมูลตัวอย่างทั้งหมด รวมถึงข้อมูลการชำระเงินในหน้าจอ เป็นข้อมูลทดสอบ ไม่ใช่ข้อมูลจริง
