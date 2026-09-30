# SPEC.md — Đặc tả dự án

## 1. Tổng quan
- **Tên dự án (tạm)**: Prehistoric Tribe (đặt tên chính thức sau)
- **Thể loại**: Survival + City-Builder + Base Management, bối cảnh thời tiền sử
- **Nền tảng**: Desktop (Windows/Mac/Linux), có thể phát hành Steam sau này; định hướng phát hành thêm trên **Mobile (Android qua Google Play, iOS qua App Store)** ở giai đoạn sau. Kéo theo yêu cầu thiết kế thêm: UI/input hỗ trợ cảm ứng (touch) song song với chuột/bàn phím, tối ưu hiệu năng/kích thước build cho thiết bị di động, và build iOS bắt buộc cần máy Mac + Xcode.
- **Góc nhìn**: 2.5D — thế giới và model 3D (low-poly), camera phối cảnh nhìn nghiêng từ trên xuống, bám theo nhân vật, zoom được; nhân vật chỉ di chuyển trên mặt đất (mặt phẳng XZ). Chuyển từ 2D top-down ngày 2026-09-30 (xem Decision Log trong PROGRESS.md).
- **Engine**: Unity (C#), IDE: VS Code
- **Người phát triển**: 1 người (solo dev), nền tảng web dev, chưa có kinh nghiệm Unity
- **Tham khảo (reference game)**: Banished, Frostpunk (xây dựng/quản lý), Rimworld/Ark (thuần hóa, sinh tồn), Stardew Valley (trồng trọt)

## 2. Ý tưởng cốt lõi (Core concept)
Người chơi dẫn dắt một bộ lạc thời tiền sử, từ vài người sống sót ban đầu, phát triển thành một cộng đồng có nhà cửa, nông nghiệp, chăn nuôi, công nghệ và khả năng phòng thủ trước thiên tai và kẻ thù.

**Vòng lặp gameplay chính (core loop)**:
1. Thu thập tài nguyên (gỗ, đá, thức ăn, nước)
2. Xây dựng nơi trú ẩn / công trình
3. Trồng trọt & chăn nuôi để có nguồn thức ăn ổn định
4. Nghiên cứu/mở khóa công nghệ mới (tech tree tiền sử)
5. Phòng thủ trước thiên tai và các mối đe dọa (thú dữ, bộ lạc khác)
6. Mở rộng lãnh thổ, dân số, tiếp tục vòng lặp

## 3. Các hệ thống gameplay chính

### 3.1 Xây dựng (Building)
- Đặt công trình trên bản đồ dạng grid hoặc free-placement (quyết định ở giai đoạn prototype).
- Loại công trình ban đầu: lều trú ẩn, kho chứa, hàng rào, chuồng nuôi, khu trồng trọt, đống lửa.
- Yêu cầu tài nguyên để xây (ví dụ: 1 lều = 10 gỗ + 5 da thú).
- Công trình có thể bị phá hủy bởi thiên tai hoặc tấn công.

### 3.2 Trồng trọt (Farming)
- Chọn loại cây trồng theo mùa/khu vực.
- Vòng đời cây: Hạt giống → Nảy mầm → Trưởng thành → Thu hoạch → (héo nếu không thu hoạch kịp).
- Cần chăm sóc: tưới nước, đất tốt/xấu ảnh hưởng năng suất.

### 3.3 Chăn nuôi (Animal husbandry)
- Thuần hóa động vật hoang dã (giai đoạn đầu game).
- Vật nuôi có nhu cầu: đói, sinh sản, sức khỏe.
- Sản phẩm từ vật nuôi: thịt, da, sữa (tùy loại).

### 3.4 Phát triển / Cây công nghệ (Tech tree / Progression)
- Tiến trình gợi ý: Đá thô → Lửa → Công cụ đá mài → Nông nghiệp sơ khai → Đồ gốm → Luyện kim đồng → Làng mạc quy mô lớn.
- Mỗi công nghệ mở khóa: công trình mới, công cụ mới, hoặc buff (tăng năng suất, tăng phòng thủ...).
- Có thể gắn với tài nguyên nghiên cứu riêng (ví dụ: "tri thức" tích lũy qua thời gian hoặc qua hành động cụ thể).

### 3.5 Chiến đấu (Combat)
- Đối tượng: thú dữ tấn công, bộ lạc/kẻ thù khác đột kích.
- Chế độ: điều khiển từng nhân vật (giống RPG nhẹ) hoặc chỉ huy nhóm (RTS nhẹ) — **cần quyết định ở milestone Combat**, không cố định ngay từ đầu.
- Vũ khí tiền sử: giáo, đá ném, cung tên (mở khóa qua tech tree).

### 3.6 Thiên tai (Disasters)
- Sự kiện ngẫu nhiên: lũ lụt, cháy rừng, động đất, bão, hạn hán, mùa đông khắc nghiệt.
- Ảnh hưởng: phá hủy công trình, giảm tài nguyên, giảm sức khỏe dân làng.
- Có cảnh báo trước (early warning) để người chơi kịp phản ứng — mức độ cảnh báo tùy độ khó.

## 4. Nhân vật & Bối cảnh
- **NPC dân làng**: có nhu cầu cơ bản (đói, ngủ, ấm), có thể gán công việc (thu thập, xây dựng, canh gác).
- **Phong cách hình ảnh**: 3D low-poly flat-shading (asset miễn phí kiểu Kenney/Quaternius), tông màu đất/tự nhiên (nâu, xanh lá, xám đá).
- **Bối cảnh thế giới**: vùng đất hoang sơ, rừng, sông, núi đá, thay đổi theo mùa (season system — có thể để phiên bản sau).

## 5. Phạm vi cho bản demo/prototype đầu tiên (MVP)
Để tránh ôm đồm, MVP chỉ gồm:
- [ ] Nhân vật di chuyển trên map 2D
- [ ] Thu thập 1 loại tài nguyên (gỗ)
- [ ] Đặt 1 loại công trình (lều)
- [ ] UI hiển thị số lượng tài nguyên
- [ ] Lưu/tải game đơn giản (save/load)

Các hệ thống còn lại (farming, chăn nuôi, tech tree, combat, thiên tai) được thêm dần theo từng milestone — xem file PROGRESS.md.

## 6. Ngoài phạm vi (Out of scope) cho giai đoạn đầu
- Multiplayer
- 3D đồ họa cao cấp
- Cốt truyện thoại nhánh phức tạp
- Hệ thống thời tiết/mùa chi tiết
(Có thể xem xét lại sau khi MVP hoàn thành và ổn định.)