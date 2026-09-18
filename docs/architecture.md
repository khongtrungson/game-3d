# Kiến Trúc Game: Null-Protocol (Tactical De-Rez)

---

## Tóm Tắt Dự Án (Executive Summary)

Kiến trúc của **Null-Protocol (Tactical De-Rez)** được xây dựng trên nền tảng **Unity 6 (6000.6.0f1 LTS)** kết hợp Universal Render Pipeline (URP 17.6.0), hướng tới nền tảng **PC (Steam / Windows 64-bit)** với cơ chế điều khiển độ chính xác cao dành riêng cho Chuột & Bàn phím.

### Các Trụ Cột Kiến Trúc Chính:

1. **Lõi Tách Rời Hướng Sự Kiện (Decoupled Event-Driven Core):** Áp dụng kiến trúc ScriptableObject Event Channel và RuntimeSet (mô hình Ryan Hipple), loại bỏ phụ thuộc Singleton cố định, hỗ trợ kiểm thử đơn vị (Unit Test) độc lập và đảm bảo cơ chế đặt lại phòng (Memory-Dump) xác định trong thời gian dưới $2.0\text{s}$.
2. **Độ Sát Thương & Tính Động Xác Định Cao:** Cơ chế đạn đạo hitscan kết hợp kỹ thuật khắc vật cản động NavMesh (`NavMeshObstacle` dynamic carving) $<5\text{ ms}$ khi triển khai Khiên Ánh Sáng Cứng (Hard-Light Barricade), triệt tiêu tầm nhìn thể tích (LOS) đối với Khói Null-Cloud, và bùa đóng băng kẻ địch từ mìn dây bẫy laser.
3. **Hiệu Năng Nghiêm Ngặt & Tương Thích Phần Cứng Tối Ưu:** Giới hạn ngân sách nghiêm ngặt ($<150$ draw call, $<100k$ tam giác/triangles, $<256\text{ MB}$ bộ nhớ texture thông qua kỹ thuật dò biên wireframe Sobel của URP) đảm bảo đạt $\ge 60\text{ FPS}$ trên cấu hình PC phổ thông/giá rẻ (GTX 750 Ti / Intel Iris Xe / UHD 630).
4. **Phân Tách Assembly Definition:** Phân chia thành 8 assembly definition (`.asmdef`) độc lập trong môi trường production, tuân thủ đồ thị phụ thuộc một chiều (DAG), không tham chiếu vòng tròn và cho phép biên dịch thần tốc dưới 1 giây trên các test runner dòng lệnh (Unity CLI headless).

**Sẵn sàng cho:** Triển khai các Epic và sinh mã nguồn tự động với sự hỗ trợ của AI.

---

## Bối Cảnh Dự Án

### Tổng Quan Trò Chơi

**Null-Protocol (Tactical De-Rez)** là tựa game bắn súng góc nhìn thứ nhất (FPS) chiến thuật chơi đơn đầy căng thẳng, lấy bối cảnh trong một mô phỏng kỹ thuật số bất ổn. Kết hợp cơ chế cắt góc (angle-slicing) cẩn trọng và nhịp độ chiến thuật có tính toán của _SWAT 4_ với phong cách đồ họa sắc nét, tối giản của _SUPERHOT_, trò chơi buộc người chơi phải đặc biệt chú trọng vào vị trí đứng, kỷ luật âm thanh và các công sự phòng thủ tự tạo trong môi trường cận chiến tử thần (chỉ chịu được 2–3 phát bắn).

### Phạm Vi Kỹ Thuật

- **Nền tảng:** PC (Steam), Windows 10/11 (64-bit). Phiên bản v1 dành riêng cho Chuột & Bàn phím.
- **Thể loại:** Hardcore Tactical First-Person Shooter (Hành động chiến thuật lén lút / Đột nhập & Dọn dẹp phòng).
- **Quy mô dự án:** Indie kiểm soát phạm vi (3 màn chơi thiết kế thủ công, thời lượng chiến dịch 45–60 phút, mục tiêu 60 FPS trên phần cứng cấu hình thấp).
- **Cấu hình phần cứng tối thiểu:** Intel Core i3-4160 / AMD FX-4350, 4 GB RAM, NVIDIA GeForce GTX 750 Ti (2 GB) / AMD Radeon HD 7850 / Intel Iris Xe / UHD 630, DirectX 11, dung lượng lưu trữ $<2.0\text{ GB}$.

### Các Hệ Thống Cốt Lõi

| Hệ Thống                       | Độ Phức Tạp     | Mô Tả & Ràng Buộc Kỹ Thuật                                                                                                                                                 |
| ------------------------------ | --------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **First-Person Controller**    | Trung bình      | Di chuyển bám sàn, giảm độ giật khi ngồi, độ trễ từ chạy nước rút sang bắn, nghiêng người vi mô phím Q/E (xoay 18°, lệch 0.35m) chống xuyên tường, bán kính phát âm chân.  |
| **Khí Tài & Đạn Đạo**          | Trung bình      | Truy vết tia hitscan tức thì, lò xo giật/lắc súng thủ tục, xuyên tường/vật cản có chọn lọc, kinh tế đạn dược cố định kèm cơ chế hoàn trả gadget sau 3 phát headshot.       |
| **Công Sự Triển Khai**         | Cao             | Khiên ánh sáng Hard-Light (khắc NavMesh $<5\text{ ms}$), Khói Null-Cloud (chắn tầm nhìn LOS thể tích), Mìn Logic-Trip (dây bẫy laser đóng băng wireframe địch trong 5.0s). |
| **AI Lính Canh Phối Hợp**      | Cao             | Tầm nhìn nón + cảm nhận âm thanh, truy vấn điểm ẩn nấp môi trường, giao thức xếp hàng trước cửa (stacking), Shield Breacher và Trùm Null-01 Phản Chiếu.                    |
| **Thế Giới & Hệ Thống De-Rez** | Trung bình      | Kiến trúc mô-đun khớp lưới 4m, cơ chế sụp đổ hư vô chu vi màn 3, hack thiết bị Data Core, cổng an ninh trích xuất.                                                         |
| **Vòng Lặp Memory-Dump**       | Trung bình-Cao  | Khôi phục trạng thái phòng và tái khởi tạo các thực thể tức thì trong $<2.0\text{s}$ mà không cần tải lại Scene.                                                           |
| **Diegetic UI & Shader**       | Thấp-Trung bình | Màn hình đạn tích hợp trực tiếp trên thân súng, đồng hồ trạng thái OLED trên cổ tay, phát hiện biên Sobel wireframe URP, âm thanh không gian 3D HRTF.                      |
| **Lưu Dữ Liệu & Steamworks**   | Thấp-Trung bình | Điểm kiểm tra (checkpoint) tự động lưu vào `profile.json` có mã kiểm tra tính toàn vẹn (checksum), đồng bộ Steam Cloud và 10 thành tựu Steam.                              |

### Yếu Tố Thúc Đẩy Độ Phức Tạp & Yêu Cầu Mới Lạ

1. **Định Hình Lại Chiến Trường Trực Tiếp:** Việc đặt các công sự chắn đạn theo thời gian thực làm thay đổi cấu trúc liên kết NavMesh và việc đánh giá điểm ẩn nấp của AI ngay giữa các cuộc đọ súng.
2. **Khôi Phục Tức Thì Tại Chỗ:** Đưa trạng thái phòng về nguyên bản trong $\le 2.0\text{ giây}$ mà không cần nạp lại Scene, đòi hỏi việc tách rời hoàn toàn giữa các bộ quản lý và các thực thể dùng pooling.
3. **Độ Sát Thương Chuẩn Xác:** Tính chất rủi ro cực cao trong giao tranh (người chơi gục ngã sau 2–3 phát đạn; kẻ địch hạ gục sau 1 phát trúng đầu) đòi hỏi phản hồi thao tác dưới 16ms và cơ chế ghi nhận đạn bắn mang tính xác định tuyệt đối.

### Rủi Ro Kỹ Thuật Đã Nhận Diện

- **Gai Khung Hình Khi Khắc NavMesh:** Quá trình khắc vật cản NavMesh động khi bung khiên chắn có thể gây giật khung hình trên luồng chính vượt quá ngưỡng ngân sách 16ms.
- **Tắc Nghẽn Khi Xếp Đội Hình Cửa:** Các đơn vị AI có nguy cơ kẹt vào nhau hoặc xuyên thấu mô hình khi thực hiện lệnh phối hợp đột nhập qua các cửa mở 4m.
- **Xuyên Thấu Camera Khi Nghiêng Người:** Camera của người chơi có nguy cơ lọt xuyên hình học màn chơi khi thực hiện nghiêng người sát các góc tường hoặc đạo cụ.

---

## Công Cụ & Khung Nền Tảng (Engine & Framework)

### Engine Đã Chọn

**Unity 6 (6000.6.0f1)** kết hợp với **Universal Render Pipeline (URP 17.6.0)**.

**Lý Do Lựa Chọn:**

- Hỗ trợ gốc cho các Renderer Feature tùy biến của URP, cho phép thực hiện trích xuất biên độ sâu/pháp tuyến Sobel toàn màn hình với hiệu năng cao nhằm tái hiện phong cách thẩm mỹ wireframe phát sáng đặc trưng.
- Tính năng GPU instancing và SRP batcher tích hợp sẵn đảm bảo duy trì $<150$ draw call trên các khối lưới mô-đun 4m ở cấu hình phần cứng cơ sở (GTX 750 Ti / Intel Iris Xe).
- Gói `com.unity.ai.navigation` (2.0.14) hỗ trợ cơ chế khắc vật cản động thời gian thực (`NavMeshObstacle`) với chi phí luồng chính cực thấp ($<5\text{ ms}$).
- Hệ thống Input System mới (`com.unity.inputsystem` 1.20) cung cấp khả năng ánh xạ thao tác mượt mà, gán lại phím linh hoạt và độ chính xác của chuột ở mức dưới từng khung hình (sub-frame).

### Thiết Lập Dự Án

Dự án được khởi tạo tại thư mục gốc của repository với các gói thư viện nền tảng:

- `com.unity.render-pipelines.universal` (17.6.0)
- `com.unity.inputsystem` (1.20.0)
- `com.unity.ai.navigation` (2.0.14)
- `com.unity.test-framework` (1.8.0)

### Kiến Trúc Được Cung Cấp Bởi Engine

| Thành Phần                  | Giải Pháp                        | Tác Động Kiến Trúc                                                                       |
| --------------------------- | -------------------------------- | ---------------------------------------------------------------------------------------- |
| **Đồ Họa (Rendering)**      | Bộ dựng URP Forward+             | Cel-shading phát sáng + Sobel wireframe Blit pass; dùng chung trim-sheet vật liệu.       |
| **Vật Lý (Physics)**        | PhysX 3D Raycasting & Collider   | Ghi nhận đạn hitscan, di chuyển của nhân vật, va chạm vật cản động.                      |
| **Điều Khiển Nhập Liệu**    | Unity Input System (Action Maps) | Phân tách rành mạch giữa việc đọc input và logic di chuyển; dễ dàng gán lại phím bấm.    |
| **Tìm Đường (Pathfinding)** | Unity AI Navigation NavMesh      | Khắc động cho công sự triển khai; tích hợp các điểm nấp thông qua truy vấn tọa độ.       |
| **Âm Thanh (Audio)**        | Unity Audio Engine + 3D HRTF     | Định vị không gian binaural cho bán kính âm thanh bước chân và tiếng bộ đàm của lính AI. |

### Tích Hợp Công Cụ AI & Tự Động Hóa

- **Engine MCP:** Cấu hình `CoderGamester/mcp-unity` để trực tiếp kiểm tra Scene, phân tích cây phân cấp GameObject và thực thi test tự động.
- **Unity CLI:** Trình chạy dòng lệnh headless phục vụ kiểm tra biên dịch tự động và chạy các bộ kiểm thử EditMode/PlayMode.
- **Reference MCP:** Sử dụng `context7` để tra cứu nhanh tài liệu API Unity 6 theo thời gian thực.

---

## Các Quyết Định Kiến Trúc

### Tổng Hợp Quyết Định

| Danh Mục                     | Quyết Định                                                | Phiên Bản / Chuẩn                   | Cơ Sở Lý Luận                                                                                                     |
| ---------------------------- | --------------------------------------------------------- | ----------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| **Mô Hình Kiến Trúc**        | ScriptableObject Event Channels & Runtime Sets            | Mô hình Ryan Hipple                 | Tách biệt hoàn toàn giữa controller, vũ khí, AI và HUD; loại bỏ singleton; tinh gọn quy trình test.               |
| **Kiến Trúc Reset Phòng**    | Memento Pattern + Object Pools `IResettable`              | C# Tùy biến                         | Đảm bảo reset phòng lập tức dưới 2.0s (`NFR-3`) mà không phải chịu độ trễ nạp lại Scene của Unity.                |
| **Kiến Trúc AI**             | Máy Trạng Thái Hữu Hạn Phân Cấp (HFSM)                    | C# Xác Định                         | Độ tin cậy và kiểm soát cao cho tác vụ xếp hàng đột nhập, kiểm tra điểm nấp và các pha phản xạ của trùm Null-01.  |
| **NavMesh & Điểm Nấp Động**  | Khắc `NavMeshObstacle` Thời Gian Thực + Nút Nấp Nướng Sẵn | `com.unity.ai.navigation` 2.0.14    | Khắc vật cản tức thì $<5\text{ ms}$ khi thả khiên chắn, tuyệt đối không gây khựng luồng chính.                    |
| **Quy Trình Đạn Đạo**        | Hitscan LayerMask Raycasting với Lò Xo Giật Thủ Tục       | PhysX 3D                            | Ghi nhận va chạm đạn tức thì; phân tầng xuyên thấu (Synapse-AR xuyên khiên; Phase-Rail xuyên 1 lớp tường).        |
| **Lưu Dữ Liệu & Tiến Trình** | JSON Cục Bộ (`profile.json`) + Đồng Bộ Steam Cloud        | `System.Text.Json` + Steamworks.NET | Nhẹ, có mã kiểm tra chống can thiệp (checksum) và đồng bộ tự động đa thiết bị.                                    |
| **Liên Kết Diegetic UI**     | Material Property Blocks trên Lưới Thực Thể Thế Giới      | Unity URP                           | Gán trực tiếp giá trị SO lên số đạn phát sáng trên súng và đồng hồ OLED mà không nhân bản Material gây tràn VRAM. |
| **Công Cụ Tự Động Hóa**      | Unity CLI                                                 | Unity 6000.6 Batchmode              | Chạy kiểm thử headless, kiểm tra biên dịch và đóng gói tài nguyên qua command line.                               |

---

### 1. Quản Lý Trạng Thái & Kiến Trúc Sự Kiện

- **Kênh Sự Kiện (Event Channels):** Các hệ thống trao đổi thông điệp thông qua các kênh ScriptableObject chuyên dụng (`VoidEventChannelSO`, `DamageEventChannelSO`, `GadgetDeployedEventChannelSO`).
- **Tập Hợp Thời Gian Thực (Runtime Sets):** Các thực thể hoạt động (kẻ địch trong phòng, khiên chắn kích hoạt, các điểm ẩn nấp) tự ghi danh vào các ScriptableObject dạng `RuntimeSet<T>` khi xuất hiện và hủy ghi danh khi bị tiêu diệt, triệt tiêu các lệnh quét `FindObjectsByType` tốn kém.
- **Biến Dùng Chung (Shared Variables):** Các trạng thái toàn cục (ví dụ: `PlayerHealthSO`, `PlayerAmmoSO`) là các biến ScriptableObject có phát ra sự kiện `OnValueChanged`.

---

### 2. Trạng Thái Phòng & Vòng Lặp Khôi Phục Dữ Liệu (Memory-Dump Reset $\le 2.0\text{s}$)

- **Mô hình:** Bản chụp nhanh `Memento` kết hợp cơ chế Object Pool có cài đặt interface `IResettable`.
- **Giao thức:**
  1. Khi người chơi bước qua collider ngưỡng cửa phòng, `RoomController` sẽ lưu lại lượng đạn, số lượng gadget và máu ban đầu của người chơi.
  2. Khi máu người chơi giảm về 0, sự kiện `PlayerDeathEventSO` kích hoạt hiệu ứng biến dạng quang sai và phân rã kỹ thuật số trong 0.2s.
  3. `RoomController` duyệt qua và thực thi `IResettable.ResetState()` trên toàn bộ lính gác phòng, vết đạn và các khiên chắn đã dựng.
  4. Thành phần `CharacterController` của người chơi được tắt tạm thời, dịch chuyển về vị trí cửa phòng, hồi phục các chỉ số sinh tồn và kích hoạt lại.
  5. Toàn bộ quá trình thực thi mất $<100\text{ ms}$, thỏa mãn chỉ số `NFR-3` mà không cần nạp lại Scene.

---

### 3. Kiến Trúc AI Lính Canh Phối Hợp (HFSM)

- **Cấu trúc phân cấp:**
  - **Trạng thái Gốc (Root State):** `Unaware` (Tuần tra / Nhàn rỗi) đối đầu với `Combat` (Báo động / Giao tranh).
  - **Trạng thái Con Giao Tranh (Combat Sub-States):**
    - `SeekCover`: Đánh giá các điểm ẩn nấp đã nướng sẵn trong bán kính 12m, sàng lọc bằng tích vô hướng (dot-product) so với hướng nhìn của người chơi.
    - `PeekAndFire`: Nhô người qua mép tường/vật cản, bắn loạt 3 viên và thụt lại nạp đạn.
    - `DoorwayStack`: Khi một phòng liền kề phát báo động, các thành viên trong tiểu đội sẽ xếp hàng hai bên khung cửa trước khi đồng loạt đột kích.
    - `Flank`: Kích hoạt khi người chơi bị áp chế bởi lính Shield Breacher hoặc đang cố thủ sau công sự.
- **Thực Thể Gương Null-01:** Sử dụng chung cấu trúc HFSM nhưng bổ sung ngưỡng kích hoạt trang bị (dựng khiên nếu Máu $<50\%$, ném bom khói khi bị bắn rát).

---

### 4. Khắc NavMesh Động & Hệ Thống Công Sự

- **Khiên Ánh Sáng Cứng (Hard-Light Barricade):** Khởi tạo từ Object Pool. Ổn định vị trí tĩnh trong vòng `0.05s`, bật thành phần `NavMeshObstacle` với tùy chọn `carveOnlyStationary = true`.
- **Ngân Sách Hiệu Năng:** Chi phí khắc động trên luồng chính được giới hạn nghiêm ngặt ở mức $<5\text{ ms}$ (`NFR-5`).
- **Khói Null-Cloud:** Không khắc NavMesh; thay vào đó tạo ra một khối thể tích trigger kích thước `4.0m` mang thành phần `ILOSBlocker`, làm gián đoạn các tia raycast nón tầm nhìn của AI.

---

### 5. Quy Trình Đạn Đạo & Cơ Chế Chiến Đấu

- **Xử Lý Raycast:** Truy vấn tia raycast phát xuất từ tâm camera đối chiếu với LayerMask: `Layers.HitscanTargets` (`Wall`, `Barricade`, `EnemyHead`, `EnemyBody`).
- **Quy Tắc Xuyên Thấu:**
  - _Vector-9:_ Bị chặn đứng bởi mọi loại collider.
  - _Synapse-AR:_ Tia bắn tiếp tục xuyên qua `DeployableBarricade` nhưng chịu tổn hao 25% sát thương.
  - _Phase-Rail:_ Bắn xuyên qua đúng 1 khối va chạm `GridWall`, sinh ra tia raycast thứ hai tại điểm thoát với sát thương giữ nguyên 100%.
- **Mô Hình Lò Xo Giật Của Camera:** Sử dụng cơ chế mô phỏng lò xo góc (`dao động điều hòa có cản`) được cập nhật trong hàm `LateUpdate()`, đảm bảo camera hồi về tâm mượt mà sau xung lực giật của súng.

---

### 6. Bộ Công Cụ Lập Trình Viên & Quy Trình Tự Động Hóa

#### Tích Hợp Unity CLI

Thực thi dòng lệnh không giao diện đồ họa phục vụ phát triển, kiểm thử và tích hợp liên tục:

```bash
# Chạy bộ unit test EditMode và PlayMode ở chế độ headless
Unity -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testResults Logs/editmode-results.xml -logFile Logs/unity-cli.log -quit

# Xác minh lỗi biên dịch và recompile toàn bộ script
Unity -batchmode -nographics -projectPath . -quit -logFile Logs/compile-check.log
```

---

## Các Vấn Đề Xuyên Suốt (Cross-cutting Concerns)

Các quy chuẩn dưới đây áp dụng cho TẤT CẢ các hệ thống và bắt buộc phải được tuân thủ nghiêm ngặt bởi mọi kỹ sư và agent AI.

### Xử Lý Ngoại Lệ & Lỗi

**Chiến lược:** Sử dụng Guard Clause và struct Result cho các hành động thời gian thực; thiết lập ranh giới an toàn cho tác vụ lưu trữ màn chơi.

**Nguyên tắc:**

1. Tuyệt đối không ném exception trong các vòng lặp khung hình (`Update`, `FixedUpdate`).
2. Kiểm tra tính hợp lệ của tham chiếu thực thể và tham số đầu vào ngay tại đầu hàm bằng guard clause.
3. Xử lý các lỗi triển khai trang bị một cách an toàn thông qua hàm kiểm tra `CanDeploy(out string reason)`.

**Ví dụ Minh Họa:**

```csharp
public struct Result<T>
{
    public bool Success { get; }
    public T Value { get; }
    public string Error { get; }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);

    private Result(bool success, T value, string error)
    {
        Success = success;
        Value = value;
        Error = error;
    }
}

public class DeployableBarricade : MonoBehaviour
{
    public bool TryDeploy(Vector3 position, Quaternion rotation, out string failureReason)
    {
        if (!Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out var hit, 1.0f, Layers.Environment))
        {
            failureReason = "Bề mặt sàn không hợp lệ";
            return false;
        }

        transform.SetPositionAndRotation(hit.point, rotation);
        failureReason = null;
        return true;
    }
}
```

---

### Chiến Lược Ghi Log (Logging Strategy)

**Định dạng:** `[PhânLoại] Nội dung`  
**Không Cấp Phát Bộ Nhớ Khi Phát Hành:** Sử dụng các thuộc tính biên dịch có điều kiện nhằm đảm bảo loại bỏ hoàn toàn các chuỗi cấp phát bộ nhớ rác trên bản build chính thức.

**Ví dụ Minh Họa:**

```csharp
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class NullLog
{
    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Info(string category, string message) =>
        Debug.Log($"[{category}] {message}");

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    public static void Warn(string category, string message) =>
        Debug.LogWarning($"[{category}] {message}");

    public static void Error(string category, string message) =>
        Debug.LogError($"[{category}] {message}");
}
```

---

### Quản Lý Cấu Hình (Configuration Management)

**Phương pháp:**

- **Cân Bằng & Hằng Số:** Tinh chỉnh trực tiếp trên các tệp ScriptableObject (`WeaponConfigSO`, `AIConfigSO`, `LevelConfigSO`).
- **Thiết Lập Người Chơi:** Tuần tự hóa ra tệp `profile.json` (thông qua struct `SettingsData`).

**Cấu Trúc Thư Mục:**

```
Assets/Data/
├── Weapons/       (Vector9_Data.asset, SynapseAR_Data.asset, PhaseRail_Data.asset)
├── Gadgets/       (Barricade_Data.asset, Smoke_Data.asset, TripMine_Data.asset)
└── AI/            (StandardSentinel_Data.asset, ShieldBreacher_Data.asset, Null01_Data.asset)
```

---

### Hệ Thống Sự Kiện

**Mô hình:** Kênh sự kiện ScriptableObject với kiểu dữ liệu định danh chặt chẽ (Strongly Typed).

**Ví dụ Minh Họa:**

```csharp
[CreateAssetMenu(fileName = "DamageEventChannel", menuName = "Events/Damage Event Channel")]
public class DamageEventChannelSO : ScriptableObject
{
    public event System.Action<int, Vector3, bool> OnEventRaised;

    public void RaiseEvent(int damageAmount, Vector3 hitPoint, bool isHeadshot)
    {
        OnEventRaised?.Invoke(damageAmount, hitPoint, isHeadshot);
    }
}
```

---

### Công Cụ Debug & Giao Diện Đo Lường Đo Kiểm (Telemetry)

**Phím Tắt Kích Hoạt:**

- **F1:** Bật/Tắt chế độ Bất tử (Khóa 100 HP).
- **F2:** Nạp đầy toàn bộ số lượng trang bị (2 Khiên, 2 Quả Khói, 1 Mìn).
- **F3:** Kích hoạt đặt lại phòng ngay lập tức (Memory-Dump Reset).
- **Phím phẩy ngược (`):** Bật/Tắt các Gizmo debug chiến thuật (nón tầm nhìn AI, bán kính phát âm thanh, trạng thái chiếm giữ điểm nấp).

**Điều kiện tồn tại:** Đặt bên trong khối tiền xử lý `#if DEVELOPMENT_BUILD || UNITY_EDITOR`.

---

## Cấu Trúc Dự Án & Ranh Giới Module

### Mô Hình Tổ Chức

**Kiến Trúc Tên Miền Theo Assembly Definition**: Mã nguồn được phân chia thành các Assembly Definition (`.asmdef`) độc lập đi kèm đồ thị quan hệ phụ thuộc tường minh. Mọi tài nguyên thuộc dự án đều được đặt trong thư mục `Assets/_Project/` nhằm cách ly hoàn toàn khỏi các thư viện bên thứ ba và các gói Unity gốc.

### Cây Thư Mục Dự Án

```
game-3d/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Core/                         # NullProtocol.Core.asmdef (Sự kiện gốc, interface, tiện ích log)
│   │   │   │   ├── Events/                   # Các kênh sự kiện ScriptableObject
│   │   │   │   ├── RuntimeSets/              # RuntimeSet<T> ScriptableObjects
│   │   │   │   ├── Interfaces/               # IDamageable, IResettable, ILOSBlocker
│   │   │   │   └── Utilities/                # Result<T>, NullLog, Layers
│   │   │   ├── Controller/                   # NullProtocol.Controller.asmdef (Phụ thuộc: Core, InputSystem)
│   │   │   │   ├── Locomotion/               # Di chuyển bám sàn WASD, Ngồi, Chạy nhanh
│   │   │   │   ├── Camera/                   # Góc nhìn chuột, nghiêng người Q/E, chống xuyên tường
│   │   │   │   └── Acoustics/                # Vùng phát âm thanh bước chân
│   │   │   ├── Combat/                       # NullProtocol.Combat.asmdef (Phụ thuộc: Core)
│   │   │   │   ├── Weapons/                  # BaseWeapon, Vector9, SynapseAR, PhaseRail
│   │   │   │   ├── Ballistics/               # Hitscan raycaster, logic xuyên tường
│   │   │   │   └── Recoil/                   # Lò xo điều hòa giảm chấn cho độ giật camera
│   │   │   ├── Gadgets/                      # NullProtocol.Gadgets.asmdef (Phụ thuộc: Core, AI.Navigation)
│   │   │   │   ├── Barricade/                # DeployableBarricade, khắc NavMeshObstacle
│   │   │   │   ├── Smoke/                    # NullCloudSmoke, thể tích cản tầm nhìn
│   │   │   │   └── TripMine/                 # LogicTripMine, tia quét laser, hiệu ứng đóng băng
│   │   │   ├── AI/                           # NullProtocol.AI.asmdef (Phụ thuộc: Core, AI.Navigation, Combat)
│   │   │   │   ├── HFSM/                     # Máy trạng thái gốc & các trạng thái phụ
│   │   │   │   ├── Perception/               # Nón tầm nhìn, cảm biến âm thanh
│   │   │   │   ├── Cover/                    # Đánh giá truy vấn CoverNode
│   │   │   │   ├── Squad/                    # Phối hợp xếp hàng trước cửa (DoorwayStacking)
│   │   │   │   └── Archetypes/               # StandardSentinel, ShieldBreacher, Null01Boss
│   │   │   ├── World/                        # NullProtocol.World.asmdef (Phụ thuộc: Core, AI, Controller)
│   │   │   │   ├── Grid/                     # Xác thực đặt lưới snapping 4m
│   │   │   │   ├── Rooms/                    # RoomController, trigger ngưỡng cửa
│   │   │   │   ├── Reset/                    # MemoryDumpManager, gọi pool IResettable
│   │   │   │   └── Objectives/               # DataCoreTerminal, ExtractionGateway
│   │   │   ├── UI/                           # NullProtocol.UI.asmdef (Phụ thuộc: Core, Combat)
│   │   │   │   ├── Diegetic/                 # Hiển thị trên thân súng, đồng hồ đeo tay
│   │   │   │   └── Menus/                    # PauseMenuController, SettingsUI, TacticalDebriefUI
│   │   │   └── Persistence/                  # NullProtocol.Persistence.asmdef (Phụ thuộc: Core)
│   │   │       ├── SaveSystem/               # Tuần tự hóa ProfileData, lưu Checkpoint
│   │   │       └── Steam/                    # Tích hợp Steamworks, AchievementManager
│   │   ├── Data/                             # Tài nguyên ScriptableObject
│   │   │   ├── Weapons/                      # Cấu hình súng (sát thương, độ giật, tốc độ bắn)
│   │   │   ├── Gadgets/                      # Lượng trang bị phòng thủ và thời gian tồn tại
│   │   │   ├── AI/                           # Tinh chỉnh thông số và ngưỡng phản xạ của lính
│   │   │   └── Events/                       # Các tài nguyên kênh sự kiện dùng chung
│   │   ├── Art/
│   │   │   ├── Modular/                      # Mô hình FBX/glTF mô-đun 4m xuất từ Blender
│   │   │   │   ├── Walls/
│   │   │   │   ├── Doors/
│   │   │   │   ├── Floors/
│   │   │   │   └── CoverProps/
│   │   │   ├── Materials/                    # Vật liệu trim-sheet dùng chung, shader phát sáng
│   │   │   └── Shaders/                      # Renderer Feature phát hiện biên Sobel trong URP
│   │   ├── Prefabs/
│   │   │   ├── Player/                       # P_PlayerCharacter
│   │   │   ├── Weapons/                      # P_Vector9, P_SynapseAR, P_PhaseRail
│   │   │   ├── Gadgets/                      # P_HardLightBarricade, P_NullSmoke, P_TripMine
│   │   │   ├── Enemies/                      # P_StandardSentinel, P_ShieldBreacher, P_Null01
│   │   │   └── ModularGrid/                  # P_Tile_4m_Floor, P_Tile_4m_Wall, P_Tile_4m_Door
│   │   ├── Audio/
│   │   │   ├── SFX/                          # Tiếng súng, nạp đạn, đặt trang bị
│   │   │   ├── Radio/                        # Lời thoại radio tổng hợp
│   │   │   └── Ambience/                     # Tiếng vo ve mô phỏng số hóa, âm thanh hư vô
│   │   └── Scenes/
│   │       ├── Boot.unity                    # Cảnh khởi động & nạp hồ sơ người chơi
│   │       ├── Subsector_01.unity            # Màn 1 (Hành lang Brutalist)
│   │       ├── Subsector_02.unity            # Màn 2 (Cầu treo hư vô)
│   │       └── Root_Core.unity               # Màn 3 (Đấu trường sụp đổ)
│   └── Tests/
│       ├── EditMode/                         # EditModeTests.asmdef
│       └── PlayMode/                         # PlayModeTests.asmdef
```

---

### Ánh Xạ Vị Trí Hệ Thống

| Hệ Thống                      | Thư Mục Chính          | Tên Assembly Definition    | Trách Nhiệm Kỹ Thuật                                                |
| ----------------------------- | ---------------------- | -------------------------- | ------------------------------------------------------------------- |
| **Di Chuyển & Nghiêng Người** | `Scripts/Controller/`  | `NullProtocol.Controller`  | Đi bộ bám sàn, góc nghiêng Q/E, kiểm tra va chạm camera, tiếng ồn.  |
| **Khí Tài & Đạn Đạo**         | `Scripts/Combat/`      | `NullProtocol.Combat`      | Bắn tia raycast, lò xo giật súng, logic xuyên thấu, đếm đạn.        |
| **Công Sự Triển Khai**        | `Scripts/Gadgets/`     | `NullProtocol.Gadgets`     | Khắc NavMesh khiên chắn, khói chặn tầm nhìn, mìn bẫy đóng băng.     |
| **AI Lính Canh Canh Gác**     | `Scripts/AI/`          | `NullProtocol.AI`          | Cảm nhận thị giác/thính giác, truy vấn điểm nấp, xếp hàng đột nhập. |
| **Phòng Chơi & Reset**        | `Scripts/World/`       | `NullProtocol.World`       | Kiểm tra ranh giới, chụp trạng thái phòng, reset nhanh dưới 2.0s.   |
| **Diegetic UI**               | `Scripts/UI/`          | `NullProtocol.UI`          | Cập nhật Material Property Block cho màn hình súng & đồng hồ tay.   |
| **Lưu File & Steamworks**     | `Scripts/Persistence/` | `NullProtocol.Persistence` | Tuần tự hóa file `profile.json`, tích hợp Steam Cloud/Achievement.  |

---

### Quy Ước Đặt Tên (Naming Conventions)

#### 1. Phần Tử Code

| Phần Tử                     | Quy Ước           | Ví Dụ Minh Họa                               |
| --------------------------- | ----------------- | -------------------------------------------- |
| **Class & Struct**          | PascalCase        | `PlayerController`, `HardLightBarricade`     |
| **Interface**               | `I` + PascalCase  | `IDamageable`, `IResettable`, `ILOSBlocker`  |
| **Hàm & Thuộc Tính**        | PascalCase        | `ExecuteShot()`, `CurrentHealth`             |
| **Biến Thành Viên Private** | `_` + camelCase   | `_characterController`, `_currentRecoil`     |
| **Tài Nguyên SO**           | PascalCase + Type | `Vector9_ConfigSO`, `DamageEventChannelSO`   |
| **Hằng Số**                 | UPPER_SNAKE_CASE  | `MAX_ENGAGEMENT_DISTANCE`, `LEAN_ROLL_ANGLE` |

#### 2. Tiền Tố Đặt Tên Asset

- **Prefab:** `P_` (ví dụ: `P_Player`, `P_HardLightBarricade`, `P_StandardSentinel`)
- **Material:** `M_` (ví dụ: `M_ModularTrim_01`, `M_Emissive_Cyan`)
- **Texture / Trim-Sheet:** `T_` (ví dụ: `T_ModularGrid_Albedo`, `T_ModularGrid_Normal`)
- **Audio Clip:** `S_` (ví dụ: `S_Vector9_Fire`, `S_Barricade_Deploy`)
- **Scene:** PascalCase (ví dụ: `Subsector_01`, `Root_Core`)

---

### Ranh Giới & Quy Tắc Kiến Trúc

1. **Tính Độc Lập Tuyệt Đối Của Core:** `NullProtocol.Core` không bao giờ được phép tham chiếu tới bất kỳ assembly gameplay nào khác.
2. **Không Gọi Trực Tiếp Qua Singleton:** Các hệ thống thuộc các assembly khác nhau bắt buộc phải giao tiếp thông qua ScriptableObject Event Channel hoặc Interface (`IDamageable`), không được gọi trực tiếp qua các lớp static xuyên module.
3. **Phân Tách Scene Rành Mạch:** Các thành phần xử lý logic gameplay không được trực tiếp can thiệp vào các thành phần UI Canvas; các bảng hiển thị diegetic phải đăng ký nhận thay đổi dữ liệu thông qua kênh sự kiện hoặc biến runtime.

---

## Các Mẫu Thiết Kế Áp Dụng (Implementation Patterns)

Các mẫu thiết kế này đảm bảo việc viết mã diễn ra đồng nhất giữa mọi AI agent và lập trình viên, hạn chế tối đa xung đột phong cách.

### Mẫu Thiết Kế Mới

#### 1. Dynamic Fortification Carver (Bộ Khắc Công Sự Động)

**Mục đích:** Triển khai vật che chắn vật lý và thực hiện khắc vật cản NavMesh động trong thời gian $<5\text{ ms}$ mà không gây khựng khung hình (`FR-16`, `FR-17`, `NFR-5`).

**Các thành phần:**

- `BarricadePuckLauncher`: Tính toán quỹ đạo ném và khởi tạo đĩa puck khiên chắn.
- `DeployableBarricade`: Quản lý việc bám sàn khi tiếp đất, bung nở lưới chắn (`0.8s`), và điều khiển thành phần `NavMeshObstacle`.
- `BarricadeRuntimeSet`: Theo dõi các khiên chắn đang kích hoạt trong từng phòng.

**Hướng Dẫn Triển Khai:**

```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle), typeof(BoxCollider))]
public class DeployableBarricade : MonoBehaviour, IResettable
{
    [SerializeField] private NavMeshObstacle _navObstacle;
    [SerializeField] private int _maxHealth = 400;
    private int _currentHealth;

    private void Awake()
    {
        _navObstacle.carving = true;
        _navObstacle.carveOnlyStationary = true;
    }

    public void Deploy(Vector3 hitPoint, Vector3 normal)
    {
        transform.position = hitPoint;
        transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(normal, Vector3.up));
        _currentHealth = _maxHealth;
        gameObject.SetActive(true);
        _navObstacle.enabled = true; // Kích hoạt khắc vật cản NavMesh
    }

    public void ResetState()
    {
        _navObstacle.enabled = false;
        gameObject.SetActive(false);
    }
}
```

---

#### 2. Memory-Dump Room Memento (Bản Ghi Nhớ Phòng)

**Mục đích:** Khôi phục trạng thái phòng trong vòng $\le 2.0\text{ giây}$ mà không cần nạp lại Scene trong Unity (`FR-30`, `NFR-3`).

**Các thành phần:**

- `RoomController`: Ghi nhận người chơi bước vào phòng, lưu bản chụp trạng thái ban đầu và điều phối việc reset.
- `IResettable`: Interface bắt buộc cài đặt trên mọi thực thể biến động trong phòng (kẻ địch, trang bị, vết đạn).
- `MemoryDumpManager`: Điều phối hiệu ứng camera phân rã quang sai và dịch chuyển người chơi về vị trí ban đầu.

**Luồng Dữ Liệu:**

```
Người Chơi Chết (0 HP)
  --> MemoryDumpManager.TriggerGlitchFX() [0.2s]
  --> RoomController.ExecuteRoomReset()
      --> Lặp qua toàn bộ IResettable trong phòng (Kẻ địch, Khiên chắn, Decal)
      --> Gọi IResettable.ResetState()
  --> Player.Reposition(Tọa độ ngưỡng cửa phòng)
  --> Kết thúc hiệu ứng Glitch [0.1s] -> Người chơi nhận lại quyền điều khiển
```

**Hướng Dẫn Triển Khai:**

```csharp
public interface IResettable
{
    void ResetState();
}

public class RoomController : MonoBehaviour
{
    [SerializeField] private Transform _spawnPoint;
    private readonly List<IResettable> _roomResettables = new();

    public void RegisterResettable(IResettable item) => _roomResettables.Add(item);

    public void ResetRoom(CharacterController playerController)
    {
        // 1. Reset toàn bộ thực thể biến động đã ghi danh
        foreach (var resettable in _roomResettables)
        {
            resettable.ResetState();
        }

        // 2. Dịch chuyển nhân vật an toàn
        playerController.enabled = false;
        playerController.transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);
        playerController.enabled = true;
    }
}
```

---

### Mẫu Thiết Kế Hệ Thống Chuẩn

#### 1. Entity Pooling Pattern (Bể Chứa Thực Thể)

Áp dụng cho vệt đạn hitscan, tia lửa va chạm và các mảnh vỡ de-rez vật lý:

```csharp
using UnityEngine.Pool;

public class VoxelDeRezPool : MonoBehaviour
{
    private IObjectPool<GameObject> _pool;
    [SerializeField] private GameObject _voxelPrefab;

    private void Awake()
    {
        _pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(_voxelPrefab, transform),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            defaultCapacity: 50,
            maxSize: 200
        );
    }

    public GameObject Get() => _pool.Get();
    public void Release(GameObject obj) => _pool.Release(obj);
}
```

---

#### 2. Máy Trạng Thái Phân Cấp (HFSM)

Áp dụng cho tất cả các nhóm đơn vị Sentinel AI:

```csharp
public interface IState
{
    void Enter();
    void Update();
    void FixedUpdate();
    void Exit();
}

public class SentinelStateMachine
{
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void Update() => CurrentState?.Update();
    public void FixedUpdate() => CurrentState?.FixedUpdate();
}
```

---

### Quy Tắc Đồng Nhất Dành Cho Agent AI

| Vấn Đề Quan Tâm            | Quy Tắc Bắt Buộc                                                                                                                                             | Phương Thức Kiểm Tra     |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------ |
| **Thu Dọn Rác (GC)**       | Không cấp phát bộ nhớ heap trong `Update()`, `FixedUpdate()` hoặc `LateUpdate()`. Lưu trữ tham chiếu, cấp phát mảng tĩnh cho raycast (`RaycastNonAlloc`).    | Profiler GC Alloc = 0 B. |
| **Truy Cập Component**     | Tuyệt đối không gọi `GetComponent<T>()` trong vòng lặp từng khung hình. Lưu cache tại hàm `Awake()`.                                                         | Kiểm tra tĩnh mã nguồn.  |
| **Hủy Đăng Ký Sự Kiện**    | Mọi hàm lắng nghe sự kiện đăng ký trong `OnEnable()` BẮT BUỘC phải được hủy đăng ký trong `OnDisable()`.                                                     | Kiểm tra rò rỉ bộ nhớ.   |
| **Tham Chiếu Layer & Tag** | Tuyệt đối không sử dụng chuỗi tự do (magic string) cho tag hoặc layer. Phải gọi thông qua lớp hằng số `Layers` và `Tags` trong assembly `NullProtocol.Core`. | Kiểm tra lỗi biên dịch.  |
| **Cập Nhật Diegetic UI**   | Bắt buộc sử dụng `MaterialPropertyBlock` để cập nhật hiển thị số đạn trên vỏ súng; TUYỆT ĐỐI KHÔNG gán thẳng vào `renderer.material`.                        | Kiểm tra rò rỉ VRAM.     |

---

## Đánh Giá & Xác Nhận Kiến Trúc (Architecture Validation)

### Bảng Đánh Giá Tổng Hợp

| Hạng Mục Kiểm Tra         | Kết Quả | Ghi Chú                                                                                                      |
| ------------------------- | ------- | ------------------------------------------------------------------------------------------------------------ |
| **Tính Tương Thích**      | **ĐẠT** | Engine, render pipeline và các mẫu kiến trúc hướng ScriptableObject đồng bộ hoàn toàn, không phụ thuộc chéo. |
| **Bao Quát GDD & PRD**    | **ĐẠT** | Đáp ứng 100% toàn bộ 46 Yêu Cầu Chức Năng (FR) và 7 Epic lớn.                                                |
| **Độ Hoàn Thiện Của Mẫu** | **ĐẠT** | Cung cấp code C# minh họa cụ thể cho cả mẫu thiết kế thông thường lẫn các giải pháp kỹ thuật đặc thù.        |
| **Ánh Xạ Epic**           | **ĐẠT** | Tất cả 7 Epic phát triển đều được phân định vào các Assembly Definition cụ thể.                              |
| **Tính Toàn Vẹn Văn Bản** | **ĐẠT** | Diễn giải chi tiết, không còn các đoạn giữ chỗ trống (placeholder) hoặc các điểm mơ hồ chưa chốt.            |

### Các Chỉ Số Độ Bao Phủ

- **Số Hệ Thống Cốt Lõi Được Thiết Kế:** 8 / 8 (Di chuyển, Chiến đấu, Trang bị, AI, Thế giới/Reset, Diegetic UI, Lưu dữ liệu, Tự động hóa).
- **Số Mẫu Thiết Kế Mới Được Ghi Nhận:** 2 (Dynamic Fortification Carver, Memory-Dump Room Memento).
- **Số Lượng Assembly Definition:** 8 domain production + 2 domain test.
- **Ràng Buộc Kiến Trúc:** Không cấp phát heap mỗi khung hình; $<150$ draw call mỗi phòng.

### Ngày Phê Duyệt

2026-09-17

---

## Môi Trường Phát Triển & Thiết Lập

### Yêu Cầu Cài Đặt Ban Đầu

- **Unity Hub & Unity Editor:** 6000.6.0f1 (Unity 6 LTS / Update) đi kèm Universal Windows Build Support.
- **IDE:** Visual Studio / JetBrains Rider / VS Code với bộ mở rộng C# Dev Kit.
- **Git:** Hệ thống quản lý phiên bản có hỗ trợ Git LFS cho các tệp `.unitypackage`, `.fbx`, và tài nguyên âm thanh.
- **Blender (Tùy chọn cho Artist):** Blender 4.x để thiết kế các khối lưới mô-đun 4m và căn chỉnh UV.

### Chạy Kiểm Thử & Xác Minh Bằng CLI

Thực thi kiểm thử ở chế độ headless từ thư mục gốc của repository:

```bash
# Chạy bộ unit test EditMode ở chế độ headless
Unity -batchmode -nographics -projectPath . -runTests -testPlatform editmode -testResults Logs/editmode-results.xml -logFile Logs/unity-cli.log -quit

# Kiểm tra biên dịch mã nguồn C# ở chế độ headless
Unity -batchmode -nographics -projectPath . -quit -logFile Logs/compile-check.log
```

### Trình Tự Triển Khai Ban Đầu

1. Khởi tạo cấu trúc các Assembly Definition trong thư mục `Assets/_Project/Scripts/` (`Core`, `Controller`, `Combat`, `Gadgets`, `AI`, `World`, `UI`, `Persistence`).
2. Xây dựng các kênh sự kiện nền tảng thuộc `Core` (`VoidEventChannelSO`, `DamageEventChannelSO`) và các interface (`IDamageable`, `IResettable`).
3. Triển khai `NullProtocol.Controller` phụ trách di chuyển bám sàn và nghiêng người Q/E có kiểm tra va chạm bằng sphere-cast.
4. Triển khai `NullProtocol.Combat` phụ trách cơ chế bắn tia hitscan và lò xo độ giật procedural của camera.
5. Triển khai `NullProtocol.Gadgets` phụ trách Khiên Ánh Sáng Cứng (Hard-Light Barricade) tích hợp khắc `NavMeshObstacle` thời gian thực.
