# Smart Hostel Management System 🏠

Hệ thống quản lý nhà trọ thông minh được phát triển trên nền tảng **ASP.NET Core 8.0 MVC** và **Entity Framework Core**. Dự án hỗ trợ linh hoạt cả môi trường phát triển thuần Docker (VS Code/Linux) lẫn môi trường máy thật (Visual Studio/Windows).

---

## Công nghệ sử dụng
* **Backend:** ASP.NET Core 8.0 MVC (C#)
* **ORM:** Entity Framework Core (SQL Server Provider)
* **Database:** Microsoft SQL Server 2022
* **Môi trường ảo hóa:** Docker & Docker Compose

---

## Hướng dẫn khởi chạy dự án (Dành cho từng môi trường)

Dự án sử dụng cơ chế đa cấu hình thông qua `appsettings.json` nên mọi thành viên đều có thể chạy database thông qua Docker mà không cần cài đặt SQL Server vào máy thật.

### 🔹 Trường hợp 1: Bạn dùng Visual Studio (Windows)
Sử dụng Visual Studio sẽ chạy Web trên máy thật và dùng Docker để chạy riêng Database ngầm.

1. **Khởi động Database:** Mở Terminal tại thư mục gốc và chạy lệnh:
   ```bash
   docker compose up -d db
2. **Cấu hình kết nối**: Đảm bảo file appsettings.Development.json của bạn đang trỏ chuỗi kết nối về Server=localhost;.
3. **Chạy ứng dụng**: Mở file .sln hoặc thư mục dự án bằng Visual Studio, nhấn nút Play (F5) hoặc chuột phải vào dự án chọn Start Debugging.

### 🔹 Trường hợp 2: Bạn dùng VS Code + Docker (Ubuntu/Linux)
Dành cho việc chạy toàn bộ hệ thống (cả Web và DB) gói gọn trong môi trường Docker container.
1. **Khởi động toàn bộ hệ thống:** Mở Terminal tại thư mục gốc và chạy lệnh:
    **docker compose up -d**
Ứng dụng Web sẽ tự động được biên dịch và chạy tại địa chỉ: http://localhost:5000
2. **Cập nhật code C# khi chỉnh sửa:** Mỗi khi bạn sửa đổi logic file .cs (Controllers, Models, Data), chạy lệnh sau để ép Docker biên dịch lại container Web:
    **docker compose up --build -d webmvc**
### 🔹 Quy trình cập nhật Cơ sở dữ liệu (Migrations)
Khi có bất kỳ sự thay đổi nào trong thư mục Models cấu trúc dữ liệu, hãy tuân theo quy trình 4 bước sau để đồng bộ bảng:
1. Mở file appsettings.json, tạm thời đổi Server=db; thành Server=localhost;.
2. Chạy lệnh tạo file Migration mới trên máy thật:
    **dotnet ef migrations add <Ten_Migration_Viet_Lien>**
3. Đẩy thay đổi cấu trúc vào SQL Server trong Docker:
    **dotnet ef database update**
4. Đổi ngược lại Server=localhost; về lại Server=db; trong file appsettings.json để container Web nhận diện được DB, sau đó build lại web:
    **docker compose up --build -d webmvc**

### 🔹 Cấu hình thanh toán sandbox MoMo/VNPay
Không commit khóa merchant vào Git. Cấp cấu hình bằng biến môi trường:

```text
PaymentGateways__PublicBaseUrl=https://your-public-https-domain
PaymentGateways__Momo__PartnerCode=...
PaymentGateways__Momo__AccessKey=...
PaymentGateways__Momo__SecretKey=...
PaymentGateways__VnPay__TmnCode=...
PaymentGateways__VnPay__HashSecret=...
```

MoMo IPN được gửi tới `/Payments/MomoIpn`. Trong trang quản trị sandbox VNPay,
cấu hình IPN URL là `https://your-public-https-domain/Payments/VnPayIpn`.
`PublicBaseUrl` phải là HTTPS URL mà MoMo/VNPay có thể truy cập; `localhost`
chỉ phù hợp kiểm tra giao diện và không nhận được webhook từ cổng thanh toán.

### 🔹 Dọn dẹp cuối buổi làm việc
Để tắt toàn bộ hệ thống và giải phóng tài nguyên RAM/CPU cho máy tính mà không làm mất dữ liệu đã lưu trong Database:
    **docker compose down**
Lệnh cứu hộ (Khi Docker bị lỗi cache hoặc ngáo link)
    docker compose down
    rm -rf bin obj
    docker compose build --no-cache
    docker compose up -d
