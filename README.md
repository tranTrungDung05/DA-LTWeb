2. **Chưa bọc khối code cho lệnh Docker:** Các lệnh ở trường hợp 2, Migrations và Dọn dẹp đang được viết bằng chữ bôi đậm (`**...**`). Viết dạng khối block code (` ```bash ... ``` `) sẽ giúp đồng nghiệp dễ bấm nút **Copy** nhanh trên giao diện GitHub hơn.
3. **Đồng bộ các thẻ tiêu đề (`###`):** Phần Migrations và Dọn dẹp nên để cùng cấp `###` với các trường hợp chạy máy để thanh mục lục trực quan hơn.

---

Dưới đây là bản **văn bản chuẩn cuối cùng** sau khi đã vá các lỗi hiển thị trên. Bạn chỉ cần copy đè đoạn này vào file `README.md` là xong:

```markdown
# Smart Hostel Management System 🏠

Hệ thống quản lý nhà trọ thông minh được phát triển trên nền tảng **ASP.NET Core 8.0 MVC** và **Entity Framework Core**. Dự án hỗ trợ linh hoạt cả môi trường phát triển thuần Docker (VS Code/Linux) lẫn môi trường máy thật (Visual Studio/Windows).

---

## 🛠️ Công nghệ sử dụng
* **Backend:** ASP.NET Core 8.0 MVC (C#)
* **ORM:** Entity Framework Core (SQL Server Provider)
* **Database:** Microsoft SQL Server 2022
* **Môi trường ảo hóa:** Docker & Docker Compose

---

## 💻 Hướng dẫn khởi chạy dự án (Dành cho từng môi trường)

Dự án sử dụng cơ chế đa cấu hình thông qua `appsettings.json` nên mọi thành viên đều có thể chạy database thông qua Docker mà không cần cài đặt SQL Server vào máy thật.

### 🔹 Trường hợp 1: Sử dụng Visual Studio (Windows)
Cách này sẽ chạy Web trên máy thật và dùng Docker để chạy riêng Database ngầm.

1. **Khởi động Database:** Mở Terminal tại thư mục gốc và chạy lệnh:
   ```bash
   docker compose up -d db
   ```
2. **Cấu hình kết nối:** Đảm bảo file `appsettings.Development.json` của bạn đang trỏ chuỗi kết nối về `Server=localhost;`.
3. **Chạy ứng dụng:** Mở file `.sln` hoặc thư mục dự án bằng Visual Studio, nhấn nút **Play (F5)** hoặc chuột phải vào dự án chọn **Start Debugging**.

---

### 🔹 Trường hợp 2: Sử dụng VS Code + Docker (Ubuntu/Linux)
Dành cho việc chạy toàn bộ hệ thống (cả Web và DB) gói gọn trong môi trường Docker container.

1. **Khởi động toàn bộ hệ thống:** Mở Terminal tại thư mục gốc và chạy lệnh:
   ```bash
   docker compose up -d
   ```
   *Ứng dụng Web sẽ tự động được biên dịch và chạy tại địa chỉ: `http://localhost:5000`*

2. **Cập nhật code C# khi chỉnh sửa:** Mỗi khi bạn sửa đổi logic file `.cs` (Controllers, Models, Data), chạy lệnh sau để ép Docker biên dịch lại container Web:
   ```bash
   docker compose up --build -d webmvc
   ```

---

### 🔹 Quy trình cập nhật Cơ sở dữ liệu (Migrations)

Khi có bất kỳ sự thay đổi nào trong thư mục `Models` cấu trúc dữ liệu, hãy tuân theo quy trình 4 bước sau để đồng bộ bảng:

1. Mở file `appsettings.json`, tạm thời đổi `Server=db;` thành `Server=localhost;`.
2. Chạy lệnh tạo file Migration mới trên máy thật:
   ```bash
   dotnet ef migrations add <Ten_Migration_Viet_Lien>
   ```
3. Đẩy thay đổi cấu trúc vào SQL Server trong Docker:
   ```bash
   dotnet ef database update
   ```
4. Đổi ngược lại `Server=localhost;` về lại `Server=db;` trong file `appsettings.json` để container Web nhận diện được DB, sau đó build lại web:
   ```bash
   docker compose up --build -d webmvc
   ```

---

### 🔹 Dọn dẹp cuối buổi làm việc

Để tắt toàn bộ hệ thống và giải phóng tài nguyên RAM/CPU cho máy tính mà không làm mất dữ liệu đã lưu trong Database:
```bash
docker compose down
```

### ⚠️ Lệnh cứu hộ (Khi Docker bị lỗi cache hoặc ngáo link)
```bash
docker compose down
rm -rf bin obj
docker compose build --no-cache
docker compose up -d
