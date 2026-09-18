# PRD: Null-Protocol (Tactical De-Rez)

## 0. Mục đích Tài liệu

Tài liệu Yêu cầu Sản phẩm (PRD) này xác định các yêu cầu chức năng và phi chức năng chính thức, hành trình người chơi trọng tâm, phạm vi ranh giới, các ràng buộc kỹ thuật và tiêu chí thành công cho **Null-Protocol (Tactical De-Rez)**. Đây là tài liệu quy chuẩn chính cho các quy trình kỹ thuật (Engineering), thiết kế (Design).

---

## 1. Tầm nhìn Sản phẩm

**Null-Protocol (Tactical De-Rez)** là tựa game bắn súng góc nhìn thứ nhất (FPS) chiến thuật một người chơi có nhịp độ căng thẳng, đặt trong một mô phỏng kỹ thuật số phi-Euclid đang sụp đổ. Kết hợp nhịp độ chiến thuật bài bản và kỹ thuật "cắt góc chữ V" (corner-pieing) nghiêm ngặt của _SWAT 4_ với tính thẩm mỹ tối giản, dễ nhận diện trực quan của _SUPERHOT_, trò chơi bác bỏ lối bắn súng arcade thiên về phản xạ để hướng đến khả năng chọn vị trí có tính toán, kỷ luật kiểm soát âm thanh và khả năng chủ động gia cố phòng thủ trong từng căn phòng.

Người chơi vào vai một đặc vụ đơn độc tinh nhuệ với nhiệm vụ thanh lọc các phân khu bị lỗi/nhiễm độc. Mức độ sát thương cực cao (người chơi hy sinh sau 2–3 phát đạn; kẻ địch bị tiêu diệt chỉ với 1 phát headshot duy nhất) khiến lối chơi lao lên bắn ẩu (rushing) trở thành tự sát. Để sinh tồn trước các lính gác AI phối hợp chặt chẽ, người chơi chủ động tái định hình hình học màn chơi bằng các rào chắn ánh sáng rắn (hard-light barricades) có thể triển khai, khói dữ liệu làm nhiễu cảm biến (data smoke) và bẫy laser logic (logic trip-mines), biến kiến trúc máy chủ biến đổi thành các vùng tiêu diệt (killzones) theo ý muốn.

Sản phẩm mang lại một chiến dịch ngắn nhưng dồn dập, có giá trị chơi lại cao kéo dài 45–60 phút qua 3 màn chơi tăng tiến độ khó, hỗ trợ hồi sinh phòng chơi tức thì dưới 2.0 giây, loại bỏ hoàn toàn các yếu tố HUD phi-nội cảnh (non-diegetic HUD clutter), và phong cách đồ họa cách điệu tối ưu lệnh vẽ (low-draw-call) có khả năng chạy mượt mà trên phần cứng PC phổ thông.

---

## 2. Người dùng Mục tiêu

### 2.1 Chân dung Chính: "Kẻ Đột nhập Bài bản"

- **Nhân khẩu học:** Game thủ cốt lõi của dòng FPS chiến thuật, người đam mê hành động lén lút (tactical stealth) và người hâm mộ các tựa game hành động indie tối giản (độ tuổi 18–35). Đã quen thuộc với các tựa game như _SWAT 4_, _Ready or Not_, _F.E.A.R._, và _SUPERHOT_.
- **Tư duy:** Đặt việc lên kế hoạch cẩn thận, tính nhất quán về mặt cơ chế và khả năng giải quyết bài toán không gian lên trên phản xạ giật chuột hay xả đạn cầu may. Không thích giao diện mô phỏng quân sự rườm rà (menu lệnh xoay tròn chậm chạp), nhưng cũng bài trừ các game bắn súng arcade có cơ chế hồi máu tự động cùng kẻ địch "hút đạn" (bullet-sponge).
- **Phong cách chơi:** Di chuyển chậm rãi, dọn góc cửa từng bước một bằng phím nghiêng người Q/E, đặt trước các thiết bị phòng thủ dọc theo các hướng bọc sườn, và đánh giá thành công dựa trên việc dọn sạch phòng hoàn hảo không nhận một vết sát thương nào.

### 2.2 Mục tiêu Cần Đạt

- **Về chức năng (Functional):**
  - Thực hiện dọn phòng chuẩn xác như phẫu thuật bằng cách nghiêng người vi mô (micro-leaning) và các phát headshot dứt khoát từng viên một.
  - Triển khai vật cản vật lý di động để vô hiệu hóa các làn đạn lộ thiên và góc bọc sườn nguy hiểm.
  - Triệt hạ các phân đội địch phối hợp bài bản bằng cách thao túng tầm nhìn (line-of-sight) và các tiện ích chiến thuật.
- **Về cảm xúc (Emotional):**
  - Cảm nhận sự căng thẳng tột độ khiến nhịp tim tăng nhanh khi tiếp cận những ô cửa khuất tầm nhìn, nơi chỉ một sơ suất nhỏ cũng phải trả giá bằng mạng sống.
  - Tận hưởng cảm giác thỏa mãn đỉnh cao của một chuyên gia khi lên kế hoạch đột nhập đa góc độ và thực thi trơn tru mà không dính đòn.
- **Về ngữ cảnh (Contextual):**
  - Chơi các đợt dọn phòng/màn chơi độc lập ngắn gọn 15–20 phút tôn trọng thời gian của người chơi, hỗ trợ vòng lặp tái thiết lập cực nhanh (<2.0s) khi thất bại.

### 2.3 Nhóm Người dùng Loại trừ

- **Người chơi Bắn súng Phản xạ / Arcade:** Những người tìm kiếm cơ chế vừa chạy nhanh vừa bắn (sprint-and-shoot), trượt hủy động tác (slide-canceling), nhảy tự do hoặc khiên bảo vệ hồi phục tự động.
- **Người chơi Tranh đấu Đa người chơi / Xã hội:** Những người tìm kiếm phòng chờ phối hợp (co-op), ghép trận cạnh tranh hoặc chế độ đối kháng PvP.
- **Người chơi Chỉ dùng Tay cầm Phòng khách:** Phiên bản v1 đòi hỏi độ chính xác chuột đến từng pixel phụ để cắt góc nhìn và phím tắt triển khai tiện ích thần tốc.

### 2.4 Hành trình Người chơi Trọng tâm

- **UJ-1: Tiếp cận Chiến thuật & Cắt góc chữ V (Slicing the Pie)**  
  _Luồng:_ Người chơi tiếp cận cửa ra vào 4m $\rightarrow$ giảm từ chạy nhanh sang đi bộ chậm để triệt tiêu tiếng bước chân $\rightarrow$ nghe thấy khẩu lệnh vô tuyến điện tử định hướng $\rightarrow$ thực hiện nghiêng người vi mô Q/E để mở góc tầm nhìn từng độ một $\rightarrow$ triệt hạ một lính gác chưa cảnh giác bằng một phát headshot duy nhất.
- **UJ-2: Phòng thủ Chủ động & Cố thủ Trong phòng**  
  _Luồng:_ Người chơi bước vào trung tâm máy chủ mở và kích hoạt báo động nhiều lính gác $\rightarrow$ dựng Rào chắn Ánh sáng Rắn để chặn một hành lang giao tranh hỏa lực chéo $\rightarrow$ ném Khói Đám Mây Null lên một lối đi trên cao (catwalk) $\rightarrow$ sử dụng rào chắn làm điểm tựa ẩn nấp để tiêu diệt các kẻ địch đang tràn tới bọc sườn bằng những loạt bắn kiểm soát.
- **UJ-3: Phục kích Bằng Bẫy Laser Logic & Bọc Lót Hậu phương**  
  _Luồng:_ Người chơi gắn Bẫy Laser Logic ngang qua ngưỡng cửa phía sau không có người canh chừng $\rightarrow$ giao chiến với các mục tiêu phía trước $\rightarrow$ một lính gác đi vòng bọc sườn cắt qua tia hồng ngoại và bị đóng băng thành khung dây kim loại (wireframe) bất động $\rightarrow$ người chơi bắn vỡ vụn lính gác bị đóng băng chỉ bằng một viên đạn.
- **UJ-4: Đấu trường Sụp đổ Động & Rút quân (Extraction)**  
  _Luồng:_ Tại Level 3 Root Core, người chơi kích hoạt giải mã Lõi Dữ liệu (Data Core) $\rightarrow$ các ô sàn đấu trường dần tan biến (de-rez) rơi vào khoảng không số $\rightarrow$ người chơi di chuyển trên địa hình đi lại được đang ngày càng thu hẹp, chống lại các đợt lính phối hợp cùng Boss Gương Null-01 $\rightarrow$ kích hoạt cổng trích xuất để giành chiến thắng.
- **UJ-5: Hồi phục Tức thì Nhờ Cơ chế Memory-Dump**  
  _Luồng:_ Người chơi chịu loạt sát thương chí mạng (0 HP) $\rightarrow$ màn hình ngay lập tức tan rã thành hiệu ứng nhiễu số (glitch) trong 2.0s $\rightarrow$ người chơi tái sinh liền mạch ngay tại ngưỡng cửa của căn phòng hiện tại với đầy đủ các điểm sạc tiện ích cơ bản $\rightarrow$ duy trì đà chiến thuật mà không bị ngắt quãng bởi màn hình tải cảnh (loading screen).

---

## 3. Thuật ngữ

- **Lưới 4 mét (4-Meter Grid):** Kiến trúc lưới ghép nối (snap-grid) nền tảng quản lý toàn bộ hình học màn chơi dạng module, kích thước cửa đi, chiều cao vật cản và tầm nhìn.
- **Trạm Lõi Dữ liệu (Data Core Terminal):** Vật thể tương tác mục tiêu chính (phím `F`) bắt buộc phải dùng để thanh lọc phân khu và mở khóa cổng rút quân.
- **De-Rez (De-Referencing - Hủy tham chiếu / Tan biến số):** Hiện tượng sụp đổ thực tại mô phỏng, thể hiện qua các khối hình học hòa tan, các khối voxel phân rã và hiệu ứng vỡ vụn khung lưới kim loại (wireframe) vật lý.
- **UI Nội cảnh (Diegetic UI):** Các hệ thống giao diện trong thế giới game, được nhúng trực tiếp vào mô hình 3D (bộ đếm đạn trên thân súng, đồng hồ hiển thị trạng thái màn hình OLED trên cẳng tay) thay vì các lớp giao diện 2D nổi trên màn hình.
- **Rào chắn Ánh sáng Rắn (Hard-Light Barricade):** Một đĩa thiết bị công sự chiến thuật có thể ném ra, mở rộng thành một rào cản vật lý kích thước $1.8\text{m} \times 1.1\text{m}$ với 400 HP, đồng thời chạm khắc biên cản điều hướng AI (navigation obstacle) theo thời gian thực.
- **Bẫy Laser Logic (Logic-Trip Mine):** Thiết bị gắn trên khung cửa phát ra một dây bẫy laser hồng ngoại dài 3.0m; kích hoạt trạng thái đóng băng khung lưới kim loại trong 5.0s đối với kẻ địch đi qua.
- **Vòng lặp Memory-Dump (Memory-Dump Loop):** Cơ chế phục hồi sau thất bại dưới 2.0 giây giúp thiết lập lại trạng thái căn phòng hiện tại mà không phải tải lại toàn bộ cảnh.
- **Null-01 (Đặc vụ Bản sao - Mirror Operative):** Trùm AI ở cao trào chiến dịch sở hữu các năng lực tương đương người chơi (nghiêng người, dựng rào chắn, ném khói và bọc sườn).
- **Khói Đám Mây Null (Null-Cloud Smoke):** Lựu đạn tạo ra vùng nhiễu kỹ thuật số thể tích 4.0m kéo dài 8.0s, làm mù hoàn toàn tầm nhìn AI và ngăn chặn kẻ địch nổ súng.
- **Phase-Rail:** Súng phụ bắn tia năng lượng chống vật liệu tiêu thụ pin (`FR-12`), có khả năng làm tan biến mục tiêu và xuyên thủng một bức tường khung lưới đặc.
- **Kẻ Phá Khiên (Shield Breacher):** Biến thể kẻ địch tinh nhuệ trang bị khiên ánh sáng rắn 300 HP phía trước, chuyên dùng để càn quét ép người chơi ra khỏi các vị trí cố thủ.
- **Cắt góc chữ V (Slicing the Pie):** Kỹ thuật chiến thuật dọn dẹp góc tường hoặc cửa mở từng chút một thông qua các bước di chuyển vi mô và nghiêng người Q/E để lộ diện tích cơ thể ở mức tối thiểu nhất.
- **Lính gác Tiêu chuẩn (Standard Sentinel):** Kẻ địch AI dạng người cơ bản trang bị súng trường bắn loạt ngắn, biết tự kiểm tra điểm ẩn nấp và phối hợp ập vào cửa phòng.
- **Synapse-AR:** Súng trường thiện xạ bắn loạt chính xác (`FR-11`) sử dụng hộp tiếp đạn 20 viên, sát thương headshot cao và có thể bắn xuyên rào chắn dựng.
- **Báo cáo Chiến thuật (Tactical Debrief):** Màn hình đánh giá sau khi hoàn thành màn chơi, hiển thị thời gian hoàn thành, tỉ lệ bắn trúng, lượng sát thương phải nhận và xếp hạng chiến thuật (S, A, B, C).
- **Vector-9:** Súng ngắn chiến thuật có giảm thanh (`FR-10`) sử dụng hộp tiếp đạn 12 viên, độ ồn âm thanh gần như triệt tiêu và có hệ số headshot cao để đột nhập tĩnh lặng.

---

## 4. Các Tính năng Chi tiết

### 4.1 Hệ thống Di chuyển & Bộ điều khiển Chiến thuật Góc nhìn Thứ nhất

_Hiện thực hóa UJ-1, UJ-2 và Trụ cột Cốt lõi 1 (Hành động Cân nhắc Chí mạng)._
**Mô tả:** Bộ điều khiển nhân vật bám sàn, phản hồi nhanh, đề cao di chuyển chiến thuật có tính toán mà không tạo cảm giác trôi trượt hay quán tính nặng nề.

- **FR-1:** Nhân vật người chơi phải hỗ trợ di chuyển 8 hướng bằng phím WASD với tốc độ đi bộ cơ bản là `4.0 m/s`.
- **FR-2:** Nhân vật người chơi phải hỗ trợ ngồi bằng phím `Ctrl / C`, giảm tốc độ di chuyển xuống `2.2 m/s`, hạ tầm mắt camera xuống `0.7m`, và giảm độ giật của vũ khí đi `30%`.
- **FR-3:** Nhân vật người chơi phải hỗ trợ chạy nước rút chiến thuật bằng phím `Left Shift` với tốc độ `6.2 m/s`. Chạy nhanh sẽ vô hiệu hóa nhắm qua đầu ruồi (ADS), vô hiệu hóa bắn từ hông (hip-fire), và áp dụng thời gian trễ chuyển từ chạy sang bắn là `0.25s`.
- **FR-4:** Cơ chế di chuyển của người chơi tuyệt đối không có tính năng nhảy, nhằm giữ vững phong cách điều hướng chiến thuật áp sát mặt đất.
- **FR-5:** Nhân vật người chơi phải hỗ trợ phím nghiêng người Q/E (tín hiệu analog hoặc nút nhấn), nghiêng góc camera `18°` và dịch chuyển vị trí camera sang bên `0.35m` trong khoảng thời gian chuyển tiếp mượt mà `0.15s`.
- **FR-6:** Hệ thống nghiêng người phải liên tục kiểm tra va chạm bằng sphere-cast để ngăn camera xuyên thấu qua các bức tường hoặc chướng ngại vật liền kề.
- **FR-7:** Việc di chuyển của người chơi phải tạo ra các vùng kích thích âm thanh hình cầu để AI phát hiện: Ngồi (`bán kính 1.5m`), Đi bộ (`bán kính 6.0m`), Chạy nhanh (`bán kính 18.0m`).
- **FR-8:** Nhân vật người chơi sở hữu lượng máu cố định `100 HP` không tự hồi phục. Khi lượng máu về `0 HP`, hệ thống phải kích hoạt ngay lập tức Vòng lặp Memory-Dump trong 2.0s (`UJ-5`).

---

### 4.2 Hệ thống Đường đạn & Xạ kích Chuẩn xác

_Hiện thực hóa UJ-1, UJ-2 và Trụ cột Cốt lõi 1 (Hành động Cân nhắc Chí mạng)._
**Mô tả:** Hệ thống đường đạn dạng tia quét (hitscan) có mức sát thương cao, sở hữu các vũ khí chiến thuật đặc trưng, đường giật hạt nhân và phần thưởng xứng đáng cho các pha bắn trúng đầu.

- **FR-9:** Đường đạn phải sử dụng cơ chế raycast tức thời kèm theo hiệu ứng chớp lửa đầu nòng theo thuật toán, vệt đạn (tracers), tia lửa va chạm vật lý và các gợn sóng hủy tham chiếu UV trên bề mặt trúng đạn.
- **FR-10:** Game phải cung cấp khẩu **Vector-9** (Súng ngắn Chiến thuật Giảm thanh): Hộp đạn 12 viên, bán tự động (tối đa 400 RPM), sát thương 30 vào thân / 75 vào đầu, độ ồn âm thanh triệt tiêu, thời gian nạp đạn `1.2s`, không xuyên tường.
- **FR-11:** Game phải cung cấp khẩu **Synapse-AR** (Súng trường Thiện xạ): Hộp đạn 20 viên, chế độ bắn tùy chọn từng viên/loạt 3 viên (550 RPM), sát thương 45 vào thân / 112 vào đầu, độ giật nảy dọc (`2.2°`), thời gian nạp đạn `1.9s`, bắn xuyên rào chắn ánh sáng có thể dựng.
- **FR-12:** Game phải cung cấp khẩu **Phase-Rail** (Súng phụ Công phá Hạng nặng): 1 viên trong buồng đạn (sức chứa tối đa 3 cell pin), chu kỳ nạp điện `0.6s`, sát thương 150 vào thân, headshot làm bốc hơi mục tiêu tức thì, sóng chấn động giật lùi `6.0°`, chu kỳ nạp pin `1.8s`, bắn xuyên qua 1 bức tường khung lưới đặc.
- **FR-13:** Sát thương giao tranh phải tuân thủ nghiêm ngặt quy tắc người chơi chết sau 2–3 phát đạn (`35 HP` mỗi viên đạn từ kẻ địch) và Lính gác Tiêu chuẩn chết sau 1 phát đạn headshot.
- **FR-14:** Nhắm qua đầu ruồi (ADS bằng `Chuột phải / RMB`) phải thu hẹp trường nhìn (FOV) lại 15%, giảm 80% độ rung lắc vũ khí theo thuật toán và căn chỉnh hồng tâm quang học.
- **FR-15:** Lượng đạn dược tuân theo nền kinh tế cố định của màn chơi, không rơi ra các hộp đạn ngẫu nhiên; khi thực hiện được 3 pha headshot liên tiếp mà không nhận sát thương, người chơi được hoàn trả 1 lượt sạc cho một thiết bị tiện ích ngẫu nhiên.

---

### 4.3 Các Tiện ích Công sự Triển khai

_Hiện thực hóa UJ-2, UJ-3 và Trụ cột Cốt lõi 2 (Động lực Ẩn nấp do Người chơi Tạo dựng)._
**Mô tả:** Các công cụ người chơi có thể triển khai cho phép chủ động tái định hình chiến trường và thiết lập phòng thủ.

- **FR-16:** Người chơi được trang bị **Rào chắn Ánh sáng Rắn** (2 lượt dùng mỗi màn qua phím `1` hoặc `G`), mở ra một tấm khiên $1.8\text{m} \times 1.1\text{m}$ với `400 HP` trong vòng `0.8s` kể từ khi đĩa tiếp đất.
- **FR-17:** Khi Rào chắn Ánh sáng Rắn được dựng, hệ thống phải cắt (carve) chướng ngại vật điều hướng vào bản đồ điều hướng runtime trong vòng `5 ms` để ngăn AI tìm đường đi xuyên qua rào chắn.
- **FR-18:** Người chơi được trang bị **Khói Đám Mây Null** (2 lượt dùng mỗi màn qua phím `2` hoặc `F`), nổ ngay khi va chạm tạo thành một vùng tĩnh kỹ thuật số thể tích `4.0m` tồn tại trong `8.0s`.
- **FR-19:** Khói Đám Mây Null phải chặn đứng các tia raycast tầm nhìn của AI, khiến các lính gác bị vướng vào hoặc bị che mắt ngừng bắn và tự tìm nơi ẩn nấp phòng thủ.
- **FR-20:** Người chơi được trang bị **Bẫy Laser Logic** (1 lượt dùng mỗi màn qua phím `3` hoặc `X`), gắn lên các bề mặt khung cửa và chiếu ra một tia laser bẫy ngang dài `3.0m`.
- **FR-21:** Khi kích hoạt Bẫy Laser Logic, kẻ địch cắt qua sẽ bị đóng băng thành trạng thái khung lưới cố định trong `5.0s`, cho phép kết liễu ngay lập tức từ bất kỳ va chạm vũ khí nào.

---

### 4.4 Trí tuệ Nhân tạo Kẻ địch Phối hợp

_Hiện thực hóa UJ-1, UJ-2 và UJ-4._
**Mô tả:** Các đơn vị chiến đấu phối hợp theo nhóm sử dụng hình nón tầm nhìn, các điểm ẩn nấp (cover nodes), cơ động bọc sườn và kỹ thuật xếp hàng công phá cửa.

- **FR-22:** Lính gác AI phải vận hành một mô hình nhận thức tính toán cả tia raycast thị giác (góc nón `110°`, phạm vi `18m`) lẫn các vùng cầu kích thích âm thanh.
- **FR-23:** Lính gác AI phải truy vấn các điểm nấp xung quanh, thò người ra bắn từng loạt 3 viên súng trường, và ngồi thụp sau vật cản để nạp đạn.
- **FR-24:** Các lính gác bị báo động ở phòng kế bên phải thực hiện quy trình xếp hàng ngoài cửa trước khi bắt đầu đột kích đồng bộ vào phòng.
- **FR-25:** Hệ thống AI phải hỗ trợ chủng loại **Kẻ Phá Khiên** (`120 HP`, khiên ánh sáng rắn phía trước `300 HP`), từ từ tiến lên vị trí người chơi trong khi các Lính gác Tiêu chuẩn di chuyển theo các lộ trình bọc sườn.
- **FR-26:** Hệ thống AI phải hỗ trợ trùm **Đặc vụ Bản sao Null-01** (`250 HP`), có khả năng thò người nhòm góc, ném Khói Null phòng thủ, dựng rào chắn ánh sáng khi HP giảm xuống dưới 50%, và thực hiện các pha đánh bọc sườn quyết liệt.
- **FR-27:** Các đơn vị AI phải phát ra tín hiệu điện đàm tổng hợp âm thanh không gian 3D để báo hiệu việc thay đổi trạng thái cảnh báo, bị áp chế hỏa lực hoặc ý định bọc sườn.

---

### 4.5 Cấu trúc Màn chơi Dạng Module & Sự Sụp đổ Môi trường

_Hiện thực hóa UJ-4, UJ-5 và Tiến trình Nhiệm vụ._
**Mô tả:** Các màn chơi tuân thủ hệ thống lưới nghiêm ngặt, tích hợp hoàn thành mục tiêu, đấu trường sụp đổ theo thời gian thực và vòng lặp khôi phục tức thì.

- **FR-28:** Hình học màn chơi phải tuân thủ nghiêm ngặt lưới module 4 mét, với tầm nhìn giao tranh trong phòng được hiệu chỉnh tối đa là 12 mét.
- **FR-29:** Chiến dịch bao gồm 3 màn chơi khác biệt: Phân khu 01 (Hành lang Kiến trúc Thô mộc / Định hướng Chiến thuật), Phân khu 02 (Cầu cạn Khoảng không / Lính bắn tỉa tầm cao), và Root Core (Đấu trường Sụp đổ / Màn đấu tay đôi với Null-01). [GIẢ ĐỊNH: Luồng Chọn Phân khu]
- **FR-30:** Cái chết của người chơi sẽ kích hoạt Vòng lặp Memory-Dump, khôi phục lại trạng thái phòng hiện tại trong thời gian $\le 2.0\text{ giây}$ mà không hiện màn hình tải cảnh (`UJ-5`).
- **FR-31:** Màn 3 Root Core phải thực thi hủy tham chiếu (de-rez) các ô gạch chu vi theo thời gian qua 3 đợt lính, làm rơi các ô sàn ngoài rìa vào khoảng không vô tận và thu hẹp 40% diện tích có thể di chuyển.
- **FR-32:** Các màn chơi đòi hỏi tương tác với các trạm Lõi Dữ liệu (phím `F`) để mở khóa cổng rút lui.
- **FR-33:** Hoàn thành màn chơi phải hiển thị màn hình Báo cáo Chiến thuật (Tactical Debrief) cho biết thời gian đã trôi qua, phần trăm độ chính xác, sát thương phải gánh chịu và bậc xếp hạng (S, A, B, C).

---

### 4.6 Giao diện Nội cảnh & Hệ thống Nghe - Nhìn

_Hiện thực hóa Trụ cột Cốt lõi 3 (Độ rõ ràng Tuyệt đối từ Giao diện Nội cảnh) và UJ-1._
**Mô tả:** Phong cách nghệ thuật cách điệu dễ nhìn với 100% giao diện hiển thị trong thế giới và âm thanh không gian hai tai (binaural spatial audio).

- **FR-34:** Số lượng đạn trong hộp tiếp đạn và chế độ bắn phải được hiển thị trực tiếp trên lưới 3D của thân súng thông qua các chữ số phát sáng nổi bật.
- **FR-35:** Lượng máu của người chơi cùng các lượt dùng tiện ích có sẵn (Rào chắn, Khói, Bẫy) phải được hiển thị trên màn hình OLED gắn ở cổ tay trái của nhân vật.
- **FR-36:** Khung nhìn màn hình không được chứa các thành phần HUD 2D thông thường (không có bản đồ nhỏ trôi nổi, không có thanh máu ở mép màn hình, không có số đạn nổi).
- **FR-37:** Hình ảnh sử dụng phong cách đổ bóng phẳng (flat-shaded), shader đường viền khung kim loại phát sáng có độ tương phản cao, cùng các hiệu ứng quang sai màu (chromatic glitch) cục bộ.
- **FR-38:** Các lính gác bị triệt hạ sẽ đứng khựng lại trong `0.15s` trước khi vỡ tan thành 30–50 mảnh voxel khung lưới vật lý và tan biến dần trong `2.0s`.
- **FR-39:** Đường truyền xử lý âm thanh phải triển khai hệ thống định hướng không gian 3D HRTF cho tiếng bước chân, tiếng súng, tiếng đạn nảy và giọng nói nhân tạo.

---

### 4.7 Lưu Trữ Phiên Chơi, Hồ Sơ & Tích hợp Steamworks

_Hiện thực hóa Lưu trữ Phiên Chiến dịch, Đồng bộ Hóa Đám mây và Chỉ số Duy trì Người chơi._
**Mô tả:** Lưu tiến trình mượt mà, đồng bộ hóa đám mây và theo dõi thành tựu.

- **FR-40:** Trò chơi phải tuần tự hóa tiến trình vào một tệp JSON cục bộ duy nhất (`profile.json`) nằm trong vùng lưu trữ dữ liệu ứng dụng chuẩn của máy tính. [GIẢ ĐỊNH: Đường dẫn Lưu Cục bộ]
- **FR-41:** Trạng thái điểm kiểm tra (checkpoint) phải được tự động ghi vào ổ đĩa ngay khi bước vào bất kỳ phòng nào chưa được dọn sạch và sau khi hoàn thành màn chơi.
- **FR-42:** Trò chơi phải cung cấp tính năng đồng bộ hóa Steam Cloud cho tệp `profile.json`. [GIẢ ĐỊNH: Wrapper Steamworks SDK]
- **FR-43:** Trò chơi phải cung cấp 10 Thành tựu Steam (Steam Achievements) cốt lõi ghi nhận các mốc chiến dịch (ví dụ: "Subsector Purged"), trình độ chiến thuật (ví dụ: "Clean Sweep" cho việc dọn sạch phòng mà không dính đòn, "Surgical Execution" khi đạt tỉ lệ headshot trên 80%), và mức độ thành thạo các thiết bị tiện ích.

---

### 4.8 Cài đặt, Điều khiển & Khả năng Tiếp cận

_Hiện thực hóa Khả năng Tiếp cận cho Người chơi, Tùy biến Phím bấm và Mức độ Tương thích Phần cứng._
**Mô tả:** Tùy biến phím điều khiển, các tùy chọn hiệu năng và thiết lập hỗ trợ hiển thị hình ảnh.

- **FR-44:** Trò chơi phải cho phép gán lại phím (remap) toàn bộ bàn phím và chuột cho tất cả các thao tác di chuyển, sử dụng vũ khí và tiện ích.
- **FR-45:** Trò chơi phải xử lý tín hiệu chuột thô (raw mouse input), có thanh trượt độ nhạy chuột (0.1 đến 10.0), nút bật/tắt gia tốc chuột (mặc định TẮT), và nút đảo trục Y.
- **FR-46:** Trò chơi phải cung cấp thanh trượt Góc nhìn (FOV) từ `80°` đến `110°` (mặc định `90°`), các thanh âm lượng độc lập (Tổng, Hiệu ứng âm thanh, Giọng vô tuyến, UI), và các tùy chọn bảng màu khung lưới (Mặc định Xanh lơ/Cam, Tương phản cao Vàng/Xanh dương, Chế độ Mù màu Đỏ/Xanh lá).

---

## 5. Các Mục tiêu Loại trừ

Những tính năng dưới đây được xác định rõ ràng là nằm ngoài phạm vi phát triển của bản phát hành tối thiểu (v1 MVP):

- **Nhiều người chơi / Co-op / PvP:** Trải nghiệm hoàn toàn tập trung vào chơi đơn chiến thuật; không xây dựng bất kỳ kiến trúc mạng chơi nhiều người nào. `[NGOÀI MỤC TIÊU MVP]`
- **Hỗ trợ Tay cầm Nguyên bản:** Được hiệu chuẩn riêng biệt cho Chuột & Bàn phím; các tính năng gán nút cho tay cầm và hỗ trợ ngắm (aim-assist) hoãn sang phiên bản v2. `[v2 — ngoài phạm vi MVP]`
- **Tạo Màn chơi Ngẫu nhiên / Phong cách Roguelite:** Toàn bộ 3 màn chơi đều được thiết kế thủ công 100% nhằm đảm bảo tính chuẩn xác tuyệt đối về hình học chiến thuật. `[NGOÀI MỤC TIÊU MVP]`
- **Phụ kiện Vũ khí, Tùy biến Súng & Nhặt Đồ:** Mỗi loại súng đều có thông số chiến thuật cố định, khóa cứng; không có hệ thống tùy biến phụ kiện hay rơi đồ. `[NGOÀI MỤC TIÊU MVP]`
- **Radar & Bản đồ nhỏ Dạng HUD Trôi nổi:** Cấm hoàn toàn các lớp phủ phi-nội cảnh nhằm duy trì tối đa sự căng thẳng thị giác và nhận biết không gian. `[NGOÀI MỤC TIÊU MVP]`
- **Hồi máu Tự động & Hộp Cứu thương Nhặt được:** Máu người chơi cố định ở mức 100 HP mỗi phòng; không có bất kỳ vật phẩm hồi máu nào trong màn chơi. `[NGOÀI MỤC TIÊU MVP]`
- **Đoạn cắt cảnh Điện ảnh & Diễn viên Lồng tiếng:** Cốt truyện được truyền tải trọn vẹn thông qua các bản ghi văn bản trên màn hình máy tính và giọng điện đàm AI tổng hợp. `[NGOÀI MỤC TIÊU MVP]`

---

## 6. Phạm vi MVP

### 6.1 Thuộc Phạm vi

- Chuỗi chiến dịch 3 màn chơi hoàn chỉnh (Phân khu 01, Phân khu 02, Root Core) với tổng thời lượng chơi từ 45–60 phút.
- 3 loại vũ khí chiến thuật riêng biệt: Vector-9 (Súng ngắn), Synapse-AR (Súng trường bắn loạt), Phase-Rail (Súng phụ hạng nặng).
- 3 công cụ phòng thủ chiến thuật: Rào chắn Ánh sáng Rắn, Khói Đám Mây Null, Bẫy Laser Logic.
- 3 biến thể kẻ địch: Lính gác Tiêu chuẩn, Kẻ Phá Khiên, Boss Gương Null-01.
- Cơ chế di chuyển chiến thuật bám đất với thao tác nghiêng người vi mô Q/E và các bán kính kích thích âm thanh.
- Giao diện 100% nội cảnh (bộ đếm đạn trên thân súng, đồng hồ đo trạng thái OLED ở cẳng tay).
- Vòng lặp hồi sinh và đặt lại phòng cực nhanh Memory-Dump ($\le 2.0\text{ giây}$).
- Tệp lưu JSON cục bộ lưu theo từng phòng và tích hợp Steam Cloud.
- 10 Thành tựu Steam cốt lõi.
- Đầy đủ khả năng cài đặt lại phím bấm, tinh chỉnh độ nhạy chuột, thanh trượt FOV (`80°`–`110°`), và các chủ đề màu sắc tương phản cao.

### 6.2 Nằm ngoài Phạm vi MVP

- Hỗ trợ tay cầm chơi game và chuyển hệ sang console. _(Dời sang v2)_
- Chế độ sinh tồn quái bất tận hoặc các bản đồ thử thách tính thời gian. _(Dời sang v2)_
- Trình tạo màn chơi (Level editor) và tích hợp Steam Workshop. _(Dời sang v2)_
- Lồng tiếng đa ngôn ngữ (chỉ hỗ trợ văn bản/phụ đề cho các ngôn ngữ ngoài tiếng Anh). _(Dời sang v2)_

---

## 7. Các Chỉ số Thành công

### Chỉ số Chính

- **Độ trễ Thiết lập lại Phòng (Room Reset Latency):** $< 2.0\text{ giây}$ từ khi người chơi nhận sát thương chí mạng cho đến khi phòng được khôi phục hoàn toàn trên cấu hình phần cứng tối thiểu mục tiêu.
- **Tốc độ Khung hình Chiến đấu:** Đạt ổn định $\ge 60\text{ FPS}$ ở độ phân giải 720p/1080p trên cấu hình PC tối thiểu cơ sở.
- **Tỉ lệ Hoàn thành Chiến dịch:** $\ge 65\%$ người chơi vượt qua Phân khu 01 sẽ hoàn thành toàn bộ chiến dịch 3 màn chơi.
- **Sự Chủ động Chiến thuật của Người chơi:** $\ge 80\%$ các lần dọn phòng thành công đều có sử dụng ít nhất 1 thiết bị công sự chiến thuật.

### Chỉ số Phụ

- **Thời gian Vượt màn Lần đầu:** Phân khu 01 (10–12 phút), Phân khu 02 (15–18 phút), Root Core (18–22 phút).
- **Điểm Đánh giá Người dùng trên Steam:** $\ge 85\%$ đánh giá Tích cực trong vòng 60 ngày kể từ khi ra mắt, đặc biệt ghi nhận lối bắn súng chiến thuật rành mạch và đồ họa rõ ràng.

### Chỉ số Chống Tối ưu

- **KHÔNG tối ưu hóa cho tốc độ Vừa Chạy Vừa Bắn (Run-and-Gun):** Nếu thời gian hoàn thành phòng trung bình giảm xuống dưới 30 giây do người chơi lao lên bắn ẩu, cần phải nâng cao độ sát thương của kẻ địch.
- **KHÔNG tối ưu hóa cho số lượng Vũ khí Ồ ạt:** Không thêm vũ khí mới làm loãng vai trò chiến thuật khác biệt vốn có của Vector-9, Synapse-AR và Phase-Rail.
- **KHÔNG tối ưu hóa cho độ sắc nét vân bề mặt Siêu thực tế:** Không thay thế phong cách đồ họa khung lưới kim loại cách điệu bằng các bộ texture PBR 4K nặng nề làm ảnh hưởng đến ngân sách draw-call và khả năng nhận diện hình ảnh.

---

## 8. Yêu cầu Phi Chức năng (NFRs) & Phần cứng Cơ sở

- **NFR-1 (Tốc độ Khung hình & Độ Ổn định Dựng hình):** Duy trì tối thiểu `60 FPS` trên Cấu hình Tối thiểu ở mức 720p (hoặc 1080p Low), và `60+ FPS` ở mức 1080p High trên Cấu hình Khuyến nghị.
- **NFR-2 (Độ trễ Đầu vào):** Độ trễ từ khi nhận thao tác đến khi hiển thị điểm ảnh (input-to-photon) $\le 16\text{ ms}$ (phản hồi trong phạm vi một khung hình ở 60 Hz).
- **NFR-3 (Thời gian Đặt lại Memory-Dump):** Quá trình tái lập trạng thái phòng và định vị lại các đối tượng phải hoàn thành trong $\le 2.0\text{ giây}$ mà không qua màn hình tải cảnh.
- **NFR-4 (Ngân sách Dựng hình Runtime):**
  - Số lệnh vẽ (Draw Calls): $< 150$ draw calls cho mỗi căn phòng đang hoạt động.
  - Số đa giác hoạt động: $< 100,000$ tam giác (triangles) mỗi khung hình.
  - Tiêu thụ VRAM: Dung lượng bộ nhớ đồ họa cho texture $< 256\text{ MB}$ nhờ tận dụng triệt để shader thuật toán và trim-sheets.
- **NFR-5 (Hiệu năng Cắt đường Bản đồ Động):** Thời gian thực thi việc khắc vật cản lên bản đồ điều hướng của Rào chắn Ánh sáng không được vượt quá `5 ms` trên luồng xử lý chính (main thread).
- **NFR-6 (Tải Xử lý Âm thanh):** Bộ đệm xử lý âm thanh định hướng không gian 3D HRTF có độ trễ $\le 10\text{ ms}$.
- **NFR-7 (Dung lượng Ổ đĩa):** Tổng dung lượng game sau khi cài đặt $\le 2.0\text{ GB}$.

### Cấu hình Phần cứng Tiêu chuẩn

_Cấu hình được tính toán nhằm đảm bảo trò chơi tiếp cận được đông đảo người chơi thông qua nền tảng đồ họa đổ bóng phẳng, ít lệnh vẽ._ [GIẢ ĐỊNH: Thông số Máy Thấp Đã Hiệu Chuẩn]

#### Cấu hình PC Tối thiểu (720p 60 FPS / 1080p 30-60 FPS Low)

- **Hệ điều hành:** Windows 10 (64-bit)
- **Bộ vi xử lý:** Intel Core i3-4160 (3.6 GHz) / AMD FX-4350 hoặc tương đương (2 nhân 4 luồng)
- **Bộ nhớ RAM:** 4 GB RAM
- **Card đồ họa:** NVIDIA GeForce GTX 750 Ti (2 GB) / AMD Radeon HD 7850 (2 GB) / Intel Iris Xe / Intel UHD 630
- **DirectX:** Phiên bản 11
- **Dung lượng ổ đĩa:** 2.0 GB dung lượng trống

#### Cấu hình PC Khuyến nghị (1080p 60+ FPS High)

- **Hệ điều hành:** Windows 10/11 (64-bit)
- **Bộ vi xử lý:** Intel Core i5-4460 (3.2 GHz) / AMD Ryzen 3 1200
- **Bộ nhớ RAM:** 8 GB RAM
- **Card đồ họa:** NVIDIA GeForce GTX 1050 Ti (4 GB) / AMD Radeon RX 560 (4 GB)
- **DirectX:** Phiên bản 11 hoặc 12
- **Dung lượng ổ đĩa:** 2.0 GB dung lượng trống (khuyến khích dùng SSD)

---

## 9. Các Câu hỏi Còn để ngỏ (Open Questions)

1. **Giao diện Chọn Màn chơi Phân khu:** Khi hoàn thành Phân khu 01, trò chơi sẽ tự động tải tiếp Phân khu 02 hay chuyển người chơi về màn hình chọn phân khu kiểu giao diện terminal? _(Khuyến nghị: Tự động tiếp tục ở lần đầu vượt màn, đồng thời mở khóa khả năng chọn phân khu từ Menu Chính)._
2. **Âm thanh Tích điện của Súng Phase-Rail:** Chu kỳ tích điện của Phase-Rail có nên phát ra âm thanh kích thích AI trong bán kính xung quanh trước khi khai hỏa không? _(Khuyến nghị: Có, tạo vùng kích thích âm thanh bán kính 6.0m khi sạc được 0.4s để khuyến khích việc chủ động sạc đạn trước khi nghiêng người nhòm góc)._

---

## 10. Danh mục Giả định (Assumptions Index)

- `[GIẢ ĐỊNH: Luồng Chọn Phân khu]` — Giả định tiến trình chiến dịch là tuần tự, với tính năng chọn các chương đã mở khóa xuất hiện tại menu máy trạm chính (§4.5, FR-29).
- `[GIẢ ĐỊNH: Đường dẫn Lưu Cục bộ]` — Giả định sử dụng đường dẫn lưu trữ ứng dụng chuẩn cục bộ (`Application.persistentDataPath`) cho tệp `profile.json` (§4.7, FR-40).
- `[GIẢ ĐỊNH: Wrapper Steamworks SDK]` — Giả định thư viện Steamworks.NET sẽ được sử dụng để tích hợp các tính năng Steam Cloud và đồng bộ Thành tựu vào Unity (§4.7, FR-42).
- `[GIẢ ĐỊNH: Thông số Máy Thấp Đã Hiệu Chuẩn]` — Giả định phong cách đồ họa khung lưới low-poly cho phép chạy mượt mà 60 FPS trên card GTX 750 Ti / Intel Iris Xe với lượng draw call $<150$ (§8).
