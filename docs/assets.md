# Null-Protocol (Tactical De-Rez) - Danh Mục & Tài Liệu Kỹ Thuật Assets

Tài liệu này tổng hợp toàn bộ **134 assets** sản xuất cho dự án **Null-Protocol (Tactical De-Rez)**.
Toàn bộ tài nguyên đã được tích hợp trực tiếp vào thư mục `Assets/` của dự án Unity 6 (`game-3d`) và liên kết chặt chẽ với các yêu cầu kỹ thuật trong **PRD**, **GDD** và **Game Architecture**.

---

## 1. Tổng quan phân loại Assets (Asset Summary)

| Phân loại (Category)                     | Số lượng | Định dạng chính                   | Thư mục đích trong Unity                       |
| :--------------------------------------- | :------- | :-------------------------------- | :--------------------------------------------- |
| **3D Environment (Modular Kit & Props)** | 19       | `.glb`, Prefabs                   | `Assets/Art/Environment/`                      |
| **3D Weapons & Arsenal**                 | 7        | `.glb`, Prefabs                   | `Assets/Art/Weapons/`                          |
| **3D Gadgets & Deployables**             | 4        | `.glb`, Prefabs                   | `Assets/Art/Gadgets/`                          |
| **3D Characters & Enemies**              | 7        | `.glb`, Rigged Prefabs            | `Assets/Art/Characters/`                       |
| **Materials & Shaders**                  | 10       | `.shader`, `.shadergraph`, `.mat` | `Assets/Art/Materials/`, `Assets/Art/Shaders/` |
| **Visual Effects (VFX)**                 | 14       | `.prefab`, Particle Systems       | `Assets/VFX/`                                  |
| **Audio - SFX**                          | 39       | `.wav` (3D HRTF Spatial)          | `Assets/Audio/SFX/`                            |
| **Audio - Ambience**                     | 3        | `.wav` (Stereo Looping)           | `Assets/Audio/Ambience/`                       |
| **Audio - Voice**                        | 6        | `.wav` (Radio Synthesized)        | `Assets/Audio/Voice/`                          |
| **UI & 2D Art**                          | 7        | `.png`, `.prefab`                 | `Assets/UI/`                                   |
| **Animations**                           | 18       | Keyframe Clamps / glTF Anim       | `Assets/Art/Animations/`                       |
| **Tổng cộng**                            | **134**  | -                                 | **Đã hoàn thành 100% (Completed)**             |

---

## 2. Tiêu chuẩn kỹ thuật (Technical Constraints & Conventions)

1. **Snap Grid 4-Meter:** Mọi mô-đun kiến trúc tuân thủ nghiêm ngặt snap grid 4m x 4m để ghép khớp chính xác với hệ thống NavMesh và che chắn đường ngắm (sightline <= 12m per FR-28).
2. **Định dạng 3D Game-Ready:** Sử dụng định dạng nhị phân `.glb` chuẩn hóa qua package `com.unity.cloud.gltfast` (v6.7.1), tích hợp sẵn Mesh, UV1 (trim-sheet), UV2 (lightmap), Bones/Armature và Vertex Groups.
3. **Hiệu năng & Draw Calls Budget:**
   - Polycount môi trường: < 150 tris / mô-đun.
   - Polycount vũ khí: < 4,500 tris.
   - Polycount nhân vật: < 6,500 tris.
   - Texture budget: Chia sẻ trim-sheet chung (1024x1024 hoặc 2048x2048).
   - Giữ draw call toàn phòng <= 150 qua GPU Instancing và SRP Batcher.
4. **Hệ thống Diegetic UI:** Hiển thị số lượng đạn và thông số trực tiếp trên thân súng và đồng hồ OLED đeo tay qua `MaterialPropertyBlock`, không sử dụng Canvas HUD 2D nổi trên màn hình.
5. **Âm thanh 3D HRTF:** Âm thanh súng, bước chân và radio đều gán thuộc tính `spatialBlend = 1.0` để phục vụ cơ chế định vị âm thanh chiến thuật.

---

## 3. Chi tiết toàn bộ danh mục Assets

### 3.1 3D Environment (19 assets)

| Mã Asset      | Tên Asset                                             | Thông số kỹ thuật                                                                                                          | Thư mục Unity                                          | PRD / GDD Ref                              |
| :------------ | :---------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------- | :----------------------------------------------------- | :----------------------------------------- |
| `ENV-MOD-01`  | **Modular Floor Tile (4m x 4m)**                      | Dimensions: 4.0m x 4.0m x 0.2m; Polycount: < 40 tris; Shared trim-sheet UVs; Lightmap UV2.                                 | `Assets/Art/Environment/ModularKit_4m`                 | GDD §10, §12; PRD §3, §4.5 (FR-28)         |
| `ENV-MOD-02`  | **Modular Solid Wall Panel (4m x 4m)**                | Dimensions: 4.0m x 0.4m x 4.0m; Polycount: < 60 tris; Shared trim-sheet UVs; Box Collider.                                 | `Assets/Art/Environment/ModularKit_4m`                 | GDD §10, §12; PRD §4.5 (FR-28)             |
| `ENV-MOD-03`  | **Modular Wall with Doorframe (4m x 4m)**             | Dimensions: 4.0m x 0.4m x 4.0m; Opening: 2.2m x 3.0m; Polycount: < 120 tris; Mesh Collider for frame.                      | `Assets/Art/Environment/ModularKit_4m`                 | GDD §6, §10; PRD §2.4 (UJ-1), §4.5 (FR-28) |
| `ENV-MOD-04`  | **Modular 90-Degree Wall Corner Column**              | Dimensions: 0.6m x 0.6m x 4.0m; Polycount: < 50 tris; Shared trim-sheet UVs.                                               | `Assets/Art/Environment/ModularKit_4m`                 | GDD §10; PRD §4.5 (FR-28)                  |
| `ENV-MOD-05`  | **Modular Cover Pillar (1m x 1m x 4m)**               | Dimensions: 1.0m x 1.0m x 4.0m; Polycount: < 60 tris; Box Collider; Pre-baked AI cover node hooks.                         | `Assets/Art/Environment/ModularKit_4m`                 | GDD §10; PRD §4.4 (FR-23), §4.5 (FR-28)    |
| `ENV-MOD-06`  | **Modular Low Cover Half-Wall (4m x 1.1m)**           | Dimensions: 4.0m x 0.4m x 1.1m; Polycount: < 40 tris; Box Collider; AI crouch-cover tag.                                   | `Assets/Art/Environment/ModularKit_4m`                 | GDD §7, §10; PRD §4.1 (FR-2), §4.4 (FR-23) |
| `ENV-MOD-07`  | **Modular Ceiling Tile (4m x 4m)**                    | Dimensions: 4.0m x 4.0m x 0.2m; Polycount: < 40 tris; Inverted normal orientation option.                                  | `Assets/Art/Environment/ModularKit_4m`                 | GDD §10, §12; PRD §4.5 (FR-28)             |
| `ENV-MOD-08`  | **Modular Industrial Catwalk Straight (4m x 2m)**     | Dimensions: 4.0m x 2.0m x 0.3m; Polycount: < 160 tris; Grate alpha-clip / procedural texture; Metallic footstep audio tag. | `Assets/Art/Environment/Subsector02_CorruptedArchives` | GDD §10; PRD §4.5 (FR-29)                  |
| `ENV-MOD-09`  | **Modular Catwalk Stairs / Ramp (4m x 4m x 4m)**      | Dimensions: 4.0m x 4.0m x 4.0m; Polycount: < 280 tris; Continuous NavMesh ramp collider.                                   | `Assets/Art/Environment/Subsector02_CorruptedArchives` | GDD §10; PRD §4.5 (FR-29)                  |
| `ENV-MOD-10`  | **Modular Catwalk Railing (4m x 1.1m)**               | Dimensions: 4.0m x 0.1m x 1.1m; Polycount: < 90 tris; Transparent hitscan penetration; Physical player block.              | `Assets/Art/Environment/Subsector02_CorruptedArchives` | GDD §10; PRD §4.5 (FR-29)                  |
| `ENV-MOD-11`  | **Modular Void Suspension Strut / Pylon**             | Dimensions: 1.0m x 1.0m x 8.0m; Polycount: < 120 tris; LOD support.                                                        | `Assets/Art/Environment/Subsector02_CorruptedArchives` | GDD §10; PRD §4.5 (FR-29)                  |
| `ENV-MOD-12`  | **Modular Server Rack Cabinet Bank (Single 4m Unit)** | Dimensions: 1.2m x 3.8m x 4.0m; Polycount: < 220 tris; Emissive amber strips; Bullet-stopping box collider.                | `Assets/Art/Environment/Subsector02_CorruptedArchives` | GDD §10; PRD §4.5 (FR-29)                  |
| `ENV-MOD-13`  | **Modular Overhead Cable Conduit Bundle (4m)**        | Dimensions: 4.0m x 0.5m x 0.3m; Polycount: < 140 tris; Shared trim-sheet.                                                  | `Assets/Art/Environment/ModularKit_4m`                 | GDD §10, §11; PRD §4.5                     |
| `ENV-MOD-14`  | **Collapsing Arena Hex-Floor Tile (4m x 4m)**         | Dimensions: 4.0m x 4.0m x 0.4m; Polycount: < 160 tris; Rigidbody + NavMeshCarve component for dynamic removal.             | `Assets/Art/Environment/Subsector03_RootCore`          | GDD §10; PRD §2.4 (UJ-4), §4.5 (FR-31)     |
| `ENV-MOD-15`  | **Room Threshold Grid Marker Strip (4m)**             | Dimensions: 4.0m x 0.4m x 0.05m; Polycount: < 30 tris; Emissive trigger volume.                                            | `Assets/Art/Environment/ModularKit_4m`                 | GDD §5, §6; PRD §2.4 (UJ-5), §4.7 (FR-41)  |
| `ENV-PROP-01` | **Data Core Terminal Unit**                           | Dimensions: 1.2m x 0.8m x 1.6m; Polycount: < 450 tris; Diegetic screen mesh with emissive material hook.                   | `Assets/Art/Environment/Props`                         | GDD §6, §7; PRD §3, §4.5 (FR-32)           |
| `ENV-PROP-02` | **Extraction Gateway / Digital Blast Door (4m x 4m)** | Dimensions: 4.0m x 0.6m x 4.0m; Polycount: < 550 tris; Animated sliding double door; Exit trigger volume.                  | `Assets/Art/Environment/Props`                         | GDD §6, §7; PRD §4.5 (FR-32, FR-33)        |
| `ENV-PROP-03` | **Corrupted Data Monolith (Root Core)**               | Dimensions: 2.0m x 2.0m x 6.0m; Polycount: < 380 tris; Animated UV shader ripple.                                          | `Assets/Art/Environment/Subsector03_RootCore`          | GDD §10; PRD §4.5 (FR-29)                  |
| `ENV-PROP-04` | **Damaged Server Chassis Debris**                     | Dimensions: 1.5m x 1.0m x 1.0m; Polycount: < 220 tris; Static box/mesh collider.                                           | `Assets/Art/Environment/Subsector02_CorruptedArchives` | GDD §10; PRD §4.5                          |

### 3.2 3D Weapons & Arsenal (7 assets)

| Mã Asset         | Tên Asset                                  | Thông số kỹ thuật                                                                                              | Thư mục Unity                  | PRD / GDD Ref                                  |
| :--------------- | :----------------------------------------- | :------------------------------------------------------------------------------------------------------------- | :----------------------------- | :--------------------------------------------- |
| `WEP-MOD-01`     | **Vector-9 Silenced Tactical Pistol**      | Dimensions: 0.28m x 0.04m x 0.18m; Polycount: < 2,500 tris; Moving slide, trigger, magazine bone rigging.      | `Assets/Art/Weapons/Vector9`   | GDD §8, §9; PRD §3, §4.2 (FR-10), §4.6 (FR-34) |
| `WEP-MOD-01-MAG` | **Vector-9 12-Round Magazine**             | Dimensions: 0.04m x 0.03m x 0.13m; Polycount: < 180 tris; Physics rigidbody on drop.                           | `Assets/Art/Weapons/Vector9`   | GDD §8; PRD §4.2 (FR-10)                       |
| `WEP-MOD-02`     | **Synapse-AR Marksman Burst Rifle**        | Dimensions: 0.85m x 0.08m x 0.28m; Polycount: < 4,200 tris; Rigged bolt, magazine, selector switch, trigger.   | `Assets/Art/Weapons/SynapseAR` | GDD §8, §9; PRD §3, §4.2 (FR-11), §4.6 (FR-34) |
| `WEP-MOD-02-MAG` | **Synapse-AR 20-Round Magazine**           | Dimensions: 0.07m x 0.03m x 0.22m; Polycount: < 220 tris; Physics rigidbody on reload eject.                   | `Assets/Art/Weapons/SynapseAR` | GDD §8; PRD §4.2 (FR-11)                       |
| `WEP-MOD-03`     | **Phase-Rail Heavy Anti-Material Sidearm** | Dimensions: 0.42m x 0.10m x 0.24m; Polycount: < 3,800 tris; Expanding cooling heat fins, battery chamber door. | `Assets/Art/Weapons/PhaseRail` | GDD §8, §9; PRD §3, §4.2 (FR-12), §4.6 (FR-34) |
| `WEP-MOD-03-BAT` | **Phase-Rail High-Voltage Power Cell**     | Dimensions: 0.05m x 0.05m x 0.16m; Polycount: < 180 tris; Emissive core charge level indicator.                | `Assets/Art/Weapons/PhaseRail` | GDD §8; PRD §4.2 (FR-12)                       |
| `WEP-MOD-CASING` | **Kinetic Spent Cartridge Casing**         | Dimensions: 0.01m x 0.01m x 0.03m; Polycount: < 60 tris; Shared pool / GPU instancing.                         | `Assets/Art/Weapons/Common`    | GDD §11; PRD §4.2 (FR-9)                       |

### 3.3 3D Gadgets & Deployables (4 assets)

| Mã Asset              | Tên Asset                                     | Thông số kỹ thuật                                                                                   | Thư mục Unity                           | PRD / GDD Ref                                   |
| :-------------------- | :-------------------------------------------- | :-------------------------------------------------------------------------------------------------- | :-------------------------------------- | :---------------------------------------------- |
| `GAD-MOD-01-PUCK`     | **Hard-Light Barricade Deployer Puck**        | Dimensions: 0.25m dia x 0.06m; Polycount: < 320 tris; Rigged expanding anchoring legs.              | `Assets/Art/Gadgets/HardLightBarricade` | GDD §8; PRD §3, §4.3 (FR-16)                    |
| `GAD-MOD-01-BARRIER`  | **Hard-Light Ballistic Barricade (Deployed)** | Dimensions: 1.8m x 0.25m x 1.1m; Polycount: < 580 tris; Box Collider; NavMeshObstacle (Carve=True). | `Assets/Art/Gadgets/HardLightBarricade` | GDD §8; PRD §3, §4.3 (FR-16, FR-17), §8 (NFR-5) |
| `GAD-MOD-02-CANISTER` | **Null-Cloud Smoke Grenade Canister**         | Dimensions: 0.08m dia x 0.22m; Polycount: < 280 tris; Impact detonator fuse.                        | `Assets/Art/Gadgets/NullCloudSmoke`     | GDD §8; PRD §3, §4.3 (FR-18, FR-19)             |
| `GAD-MOD-03-MINE`     | **Logic-Trip Mine Device Unit**               | Dimensions: 0.15m x 0.10m x 0.08m; Polycount: < 260 tris; Surface snapping raycast script anchor.   | `Assets/Art/Gadgets/LogicTripMine`      | GDD §8; PRD §3, §4.3 (FR-20, FR-21)             |

### 3.4 3D Characters & Enemies (7 assets)

| Mã Asset                 | Tên Asset                                        | Thông số kỹ thuật                                                                                              | Thư mục Unity                                  | PRD / GDD Ref                                |
| :----------------------- | :----------------------------------------------- | :------------------------------------------------------------------------------------------------------------- | :--------------------------------------------- | :------------------------------------------- |
| `CHR-FP-ARMS`            | **FP Tactical Commando Arms & Hands**            | Polycount: < 6,500 tris; Humanoid standard arm rig with 24 custom finger bones and wrist mount socket.         | `Assets/Art/Characters/FirstPersonViewmodel`   | GDD §7, §11; PRD §4.1 (FR-1-5), §4.6 (FR-35) |
| `CHR-OLED-WATCH`         | **Diegetic Forearm OLED Status Monitor**         | Dimensions: 0.09m x 0.06m x 0.02m; Polycount: < 750 tris; Render-to-texture / emissive display material plane. | `Assets/Art/Characters`                        | GDD §5, §13 (EP-08); PRD §3, §4.6 (FR-35)    |
| `CHR-AI-SENTINEL`        | **Corrupted Sentinel (Standard Unit)**           | Height: 1.85m; Polycount: < 5,500 tris; Standard humanoid Mecanim/Unity rig; Head/Torso/Limb hitboxes.         | `Assets/Art/Characters/Enemies/Sentinel`       | GDD §8; PRD §3, §4.4 (FR-22, FR-23, FR-24)   |
| `CHR-AI-BREACHER`        | **Shield Breacher (Elite Shock Unit)**           | Height: 1.95m; Polycount: < 6,200 tris; Reinforced shoulder and arm joints for heavy shield bracing.           | `Assets/Art/Characters/Enemies/ShieldBreacher` | GDD §8; PRD §3, §4.4 (FR-25)                 |
| `CHR-AI-BREACHER-SHIELD` | **Frontal Hard-Light Ballistic Shield**          | Dimensions: 0.85m x 0.15m x 1.45m; Polycount: < 480 tris; Discrete 300 HP damage receiver collider.            | `Assets/Art/Characters/Enemies/ShieldBreacher` | GDD §8; PRD §3, §4.4 (FR-25)                 |
| `CHR-AI-NULL01`          | **Null-01 (The Mirror Operative - Climax Boss)** | Height: 1.85m; Polycount: < 6,800 tris; Inverted dark wireframe aesthetic; Dual weapon attachment points.      | `Assets/Art/Characters/Enemies/Null01_Boss`    | GDD §8, §10; PRD §3, §4.4 (FR-26)            |
| `CHR-VOXEL-DEBRIS`       | **De-Rez Low-Poly Physics Voxel Shards**         | Dimensions: 0.08m to 0.18m cubes/shards; Polycount: < 24 tris per mesh; Physics convex hull; GPU instancing.   | `Assets/Art/Characters/Enemies/Common`         | GDD §11; PRD §3, §4.6 (FR-38)                |

### 3.5 Materials & Shaders (10 assets)

| Mã Asset               | Tên Asset                                      | Thông số kỹ thuật                                                                                                      | Thư mục Unity                       | PRD / GDD Ref                                          |
| :--------------------- | :--------------------------------------------- | :--------------------------------------------------------------------------------------------------------------------- | :---------------------------------- | :----------------------------------------------------- |
| `MAT-CEL-WIREFRAME`    | **Cel-Shaded Glowing Wireframe Master Shader** | Custom URP Shader Graph / HLSL; Supports Cyan/Orange, Yellow/Blue, and Colorblind themes (FR-46); Draw call instanced. | `Assets/Art/Shaders`                | GDD §11, §12; PRD §4.6 (FR-37), §4.8 (FR-46)           |
| `MAT-BRUTALIST-WHITE`  | **Sterile Brutalist White/Grey Material**      | Albedo: Flat Off-White/Grey (#E5E9F0); Emissive: Cyan (#00E5FF); Roughness: 0.8; Zero texture map overhead.            | `Assets/Art/Materials`              | GDD §10, §11; PRD §4.5 (FR-29)                         |
| `MAT-VOID-SERVER-DARK` | **Dark Void Server Chassis Material**          | Albedo: Dark Carbon (#1E222A); Emissive: Amber/Gold (#FF9100); Roughness: 0.7.                                         | `Assets/Art/Materials`              | GDD §10, §11; PRD §4.5 (FR-29)                         |
| `MAT-HARDLIGHT-ENERGY` | **Hard-Light Barrier Lattice Shader**          | URP Unlit Additive/Alpha; Exposed parameters: Health_Fraction (1.0 to 0.0), Impact_Pos, Pulse_Speed.                   | `Assets/Art/Shaders`                | GDD §8, §11; PRD §4.3 (FR-16)                          |
| `MAT-NULLCLOUD-VOLUME` | **Null-Cloud Volumetric Static Shader**        | Volumetric / particle additive shader; Procedural Simplex noise + UV scanline jitter; Low fill-rate overhead.          | `Assets/Art/Shaders`                | GDD §8; PRD §4.3 (FR-18, FR-19)                        |
| `MAT-LASER-BEAM`       | **Logic-Trip Laser Ribbon Shader**             | Unlit Additive; Emissive Color: Crimson Red / Neon Orange; Falloff edge glow; Pulse frequency 2.0 Hz.                  | `Assets/Art/Shaders`                | GDD §8; PRD §4.3 (FR-20)                               |
| `MAT-DIEGETIC-OLED`    | **Diegetic High-Contrast OLED Display Shader** | Unlit Emissive; Pixel-grid mask; Color: Cyan/Amber; Visible in extreme low-light environments.                         | `Assets/Art/Shaders`                | GDD §5, §11; PRD §4.6 (FR-34, FR-35)                   |
| `MAT-DEREZ-DISSOLVE`   | **De-Rez Voxel Dissolve Master Shader**        | URP Shader Graph; 3D Voronoi noise clipping; Dissolve_Amount parameter (0.0 to 1.0) over 2.0s duration.                | `Assets/Art/Shaders`                | GDD §11; PRD §3, §4.6 (FR-38)                          |
| `MAT-GLITCH-POST`      | **Memory-Dump Digital Glitch Post-Process**    | URP Full Screen Render Pass; Execution time < 2.0s; Triggered on Player 0 HP.                                          | `Assets/Art/Shaders/PostProcessing` | GDD §5, §6; PRD §2.4 (UJ-5), §4.1 (FR-8), §4.5 (FR-30) |
| `TEX-TRIM-MODULAR`     | **Environment Modular Master Trim Sheet**      | Resolution: 2048 x 2048 PNG (BC7 compressed); Keeps total VRAM strictly < 256 MB.                                      | `Assets/Art/Textures/TrimSheets`    | GDD §12; PRD §8 (NFR-4)                                |

### 3.6 Visual Effects (VFX) (14 assets)

| Mã Asset                     | Tên Asset                                            | Thông số kỹ thuật                                                                                    | Thư mục Unity            | PRD / GDD Ref                                          |
| :--------------------------- | :--------------------------------------------------- | :--------------------------------------------------------------------------------------------------- | :----------------------- | :----------------------------------------------------- |
| `VFX-MUZZLE-PISTOL`          | **Vector-9 Suppressed Digital Muzzle Flash**         | Unity Particle System; Duration: 0.05s; Max particles: 15; Emissive cyan/white.                      | `Assets/VFX/Weapons`     | GDD §8, §11; PRD §4.2 (FR-9)                           |
| `VFX-MUZZLE-RIFLE`           | **Synapse-AR Geometric Muzzle Burst**                | Unity Particle System; Duration: 0.06s; Max particles: 25; Point light intensity 2.5.                | `Assets/VFX/Weapons`     | GDD §8, §11; PRD §4.2 (FR-9)                           |
| `VFX-MUZZLE-RAIL`            | **Phase-Rail Electric Shockwave Muzzle Burst**       | Unity Particle System + Mesh Ring; Duration: 0.25s; Max particles: 50; Screen shake hook.            | `Assets/VFX/Weapons`     | GDD §8, §11; PRD §4.2 (FR-9, FR-12)                    |
| `VFX-TRACER-BEAM`            | **Hitscan Neon Tracer Line**                         | LineRenderer component; Width: 0.02m; Additive emissive material; Fade time: 0.08s.                  | `Assets/VFX/Weapons`     | GDD §8, §11; PRD §4.2 (FR-9)                           |
| `VFX-IMPACT-SPARKS`          | **Surface Bullet Impact Digital Sparks & UV Ripple** | Unity Particle System + Decal/Ripple projector; Max particles: 35; Lifespan: 0.3s.                   | `Assets/VFX/Systems`     | GDD §11; PRD §4.2 (FR-9)                               |
| `VFX-BARRICADE-DEPLOY`       | **Hard-Light Barricade Deployment Lattice Growth**   | Procedural lattice animation; Duration: 0.8s; Emissive bloom flash upon locking.                     | `Assets/VFX/Gadgets`     | GDD §8; PRD §4.3 (FR-16)                               |
| `VFX-BARRICADE-SHATTER`      | **Hard-Light Barricade Depletion Shatter**           | Unity Particle System; Max particles: 80; Dissolve lifespan: 1.2s.                                   | `Assets/VFX/Gadgets`     | GDD §8; PRD §4.3 (FR-16)                               |
| `VFX-NULLCLOUD-BURST`        | **Null-Cloud Volumetric Static Particle Field**      | Particle System (Volumetric simulation); Radius: 4.0m; Duration: 8.0s; Blocks AI LineOfSight checks. | `Assets/VFX/Gadgets`     | GDD §8; PRD §4.3 (FR-18, FR-19)                        |
| `VFX-LOGICTRIP-LASER`        | **Logic-Trip Laser Tripwire Ribbon**                 | LineRenderer / Quad strip; Length: Up to 3.0m (raycast clamped); Pulsing opacity.                    | `Assets/VFX/Gadgets`     | GDD §8; PRD §4.3 (FR-20)                               |
| `VFX-LOGICTRIP-FREEZE`       | **Logic-Trip Stasis Lockdown Freeze Effect**         | Mesh overlay / material swap; Duration: 5.0s; Visual freeze indicator.                               | `Assets/VFX/Gadgets`     | GDD §8; PRD §4.3 (FR-21)                               |
| `VFX-ENEMY-DEREZ`            | **Enemy Neutralization De-Rez Voxel Burst**          | Pre-fractured mesh spawner; 30-50 active rigidbodies; Dissolve shader over 2.0s.                     | `Assets/VFX/Characters`  | GDD §11; PRD §3, §4.6 (FR-38)                          |
| `VFX-PHASERAIL-DISINTEGRATE` | **Phase-Rail Target Disintegration Effect**          | Unity Particle System; Max particles: 120; Upward velocity; Alpha fade 1.0s.                         | `Assets/VFX/Weapons`     | GDD §8; PRD §4.2 (FR-12)                               |
| `VFX-TILE-DEREF`             | **Root Core Floor Tile De-Reference & Void Plunge**  | Custom particle seam burst + gravity physics drop; De-spawns at Y < -30m.                            | `Assets/VFX/Environment` | GDD §10; PRD §2.4 (UJ-4), §4.5 (FR-31)                 |
| `VFX-MEMORYDUMP-RESET`       | **Memory-Dump Fast Reset Transition Glitch**         | Post-process animation; Duration: 2.0s strict budget; Zero scene unload.                             | `Assets/VFX/Systems`     | GDD §5, §6; PRD §2.4 (UJ-5), §4.1 (FR-8), §4.5 (FR-30) |

### 3.7 Audio - SFX (39 assets)

| Mã Asset                      | Tên Asset                                         | Thông số kỹ thuật                                                                                 | Thư mục Unity                  | PRD / GDD Ref                                          |
| :---------------------------- | :------------------------------------------------ | :------------------------------------------------------------------------------------------------ | :----------------------------- | :----------------------------------------------------- |
| `SFX-WEP-VEC9-FIRE`           | **Vector-9 Silenced Shot**                        | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: ~0.4s; 4 variations.                 | `Assets/Audio/SFX/Weapons`     | GDD §8, §11; PRD §4.2 (FR-10)                          |
| `SFX-WEP-VEC9-RELOAD`         | **Vector-9 Tactical Reload Sequence**             | Format: WAV 48kHz / 24-bit; Stereo (2D viewmodel); Duration: 1.2s calibrated.                     | `Assets/Audio/SFX/Weapons`     | GDD §8; PRD §4.2 (FR-10)                               |
| `SFX-WEP-SYN-FIRE`            | **Synapse-AR Rifle Burst Shot**                   | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: ~0.5s; 4 variations.                 | `Assets/Audio/SFX/Weapons`     | GDD §8, §11; PRD §4.2 (FR-11)                          |
| `SFX-WEP-SYN-RELOAD`          | **Synapse-AR Tactical Reload Sequence**           | Format: WAV 48kHz / 24-bit; Stereo; Duration: 1.9s calibrated.                                    | `Assets/Audio/SFX/Weapons`     | GDD §8; PRD §4.2 (FR-11)                               |
| `SFX-WEP-RAIL-CHARGE`         | **Phase-Rail Capacitor Charge Hum**               | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.6s; Acoustic trigger hook.         | `Assets/Audio/SFX/Weapons`     | GDD §8; PRD §4.2 (FR-12), §9 (Question 2)              |
| `SFX-WEP-RAIL-FIRE`           | **Phase-Rail Kinetic Shockwave Discharge**        | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: ~1.2s; Heavy sub-bass component.     | `Assets/Audio/SFX/Weapons`     | GDD §8, §11; PRD §4.2 (FR-12)                          |
| `SFX-WEP-RAIL-CYCLE`          | **Phase-Rail Battery Eject & Chamber Lock**       | Format: WAV 48kHz / 24-bit; Stereo; Duration: 1.8s calibrated.                                    | `Assets/Audio/SFX/Weapons`     | GDD §8; PRD §4.2 (FR-12)                               |
| `SFX-WEP-DRYFIRE`             | **Weapon Dry Fire Mechanical Click**              | Format: WAV 48kHz / 24-bit; Stereo; Duration: 0.15s; 3 variations.                                | `Assets/Audio/SFX/Weapons`     | GDD §11; PRD §4.2                                      |
| `SFX-WEP-CASING-DROP`         | **Bullet Shell Casing Floor Clatter**             | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.3s; 6 randomized pitch variations. | `Assets/Audio/SFX/Weapons`     | GDD §11; PRD §4.2 (FR-9)                               |
| `SFX-BULLET-IMPACT-SURFACE`   | **Bullet Impact on Solid Wireframe Wall**         | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.25s; 5 variations.                 | `Assets/Audio/SFX/Impacts`     | GDD §11; PRD §4.2 (FR-9)                               |
| `SFX-BULLET-IMPACT-BARRIER`   | **Bullet Impact on Hard-Light Barricade**         | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.3s; 4 variations.                  | `Assets/Audio/SFX/Impacts`     | GDD §8, §11; PRD §4.3 (FR-16)                          |
| `SFX-BULLET-HEADSHOT-CONFIRM` | **Lethal Headshot Hit Confirmation Chirp**        | Format: WAV 48kHz / 24-bit; 2D Audio; Duration: 0.2s; Clean non-intrusive feedback.               | `Assets/Audio/SFX/UI`          | GDD §6, §8; PRD §4.2 (FR-13)                           |
| `SFX-BULLET-WHIZ`             | **Near-Miss Bullet Whiz & Doppler Zip**           | Format: WAV 48kHz / 24-bit; Binaural HRTF; Duration: 0.2s; 4 variations.                          | `Assets/Audio/SFX/Impacts`     | GDD §11; PRD §4.6 (FR-39)                              |
| `SFX-PLY-STEP-WALK`           | **Player Footstep (Walk on Composite Floor)**     | Format: WAV 48kHz / 24-bit; Binaural 3D; Duration: 0.2s; 8 step variations.                       | `Assets/Audio/SFX/Locomotion`  | GDD §7; PRD §4.1 (FR-1, FR-7)                          |
| `SFX-PLY-STEP-CROUCH`         | **Player Footstep (Crouch Creep)**                | Format: WAV 48kHz / 24-bit; Binaural 3D; Duration: 0.25s; 6 variations.                           | `Assets/Audio/SFX/Locomotion`  | GDD §7; PRD §4.1 (FR-2, FR-7)                          |
| `SFX-PLY-STEP-SPRINT`         | **Player Footstep (Tactical Sprint)**             | Format: WAV 48kHz / 24-bit; Binaural 3D; Duration: 0.25s; 8 variations.                           | `Assets/Audio/SFX/Locomotion`  | GDD §7; PRD §4.1 (FR-3, FR-7)                          |
| `SFX-PLY-STEP-CATWALK`        | **Player Footstep (Metal Grate Catwalk)**         | Format: WAV 48kHz / 24-bit; Binaural 3D; Duration: 0.3s; 6 variations.                            | `Assets/Audio/SFX/Locomotion`  | GDD §7, §10; PRD §4.1 (FR-1), §4.5 (FR-29)             |
| `SFX-PLY-LEAN`                | **Player Tactical Lean Shift Rustle**             | Format: WAV 48kHz / 24-bit; Stereo; Duration: 0.15s; 4 variations.                                | `Assets/Audio/SFX/Locomotion`  | GDD §7; PRD §4.1 (FR-5)                                |
| `SFX-PLY-HURT`                | **Player Damage Sustained Glitch Shock**          | Format: WAV 48kHz / 24-bit; 2D Audio; Duration: 0.4s; 3 variations.                               | `Assets/Audio/SFX/Player`      | GDD §6, §7; PRD §4.1 (FR-8)                            |
| `SFX-PLY-DEATH`               | **Player Fatal Memory-Dump De-Rez Stinger**       | Format: WAV 48kHz / 24-bit; 2D Audio; Duration: 1.8s; Seamless silence cutoff.                    | `Assets/Audio/SFX/Player`      | GDD §5, §6; PRD §2.4 (UJ-5), §4.1 (FR-8), §4.5 (FR-30) |
| `SFX-GAD-THROW`               | **Tactical Gadget Toss Swoosh**                   | Format: WAV 48kHz / 24-bit; Stereo; Duration: 0.25s; 3 variations.                                | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-16, FR-18)                        |
| `SFX-GAD-PUCK-BOUNCE`         | **Barricade Puck Ground Impact**                  | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.2s; 3 variations.                  | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-16)                               |
| `SFX-GAD-BARRIER-EXPAND`      | **Hard-Light Barricade Expansion Hum**            | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.8s calibrated.                     | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-16)                               |
| `SFX-GAD-BARRIER-SHATTER`     | **Hard-Light Barricade Destruction Crash**        | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 1.1s.                                | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-16)                               |
| `SFX-GAD-SMOKE-BURST`         | **Null-Cloud Smoke Detonation & Static Hiss**     | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Initial burst (0.4s) + 8.0s looping hiss.      | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-18)                               |
| `SFX-GAD-MINE-PLANT`          | **Logic-Trip Mine Surface Mount & Arm**           | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.5s.                                | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-20)                               |
| `SFX-GAD-MINE-TRIGGER`        | **Logic-Trip Laser Tripwire Breach Alarm**        | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.3s.                                | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-21)                               |
| `SFX-GAD-ENEMY-FREEZE`        | **Hostile Logic-Trip Freeze Lockdown Stinger**    | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.6s.                                | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-21)                               |
| `SFX-GAD-FROZEN-SHATTER`      | **Frozen Hostile One-Shot Shatter Pop**           | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 0.5s.                                | `Assets/Audio/SFX/Gadgets`     | GDD §8; PRD §4.3 (FR-21)                               |
| `SFX-ENV-DATACORE-IDLE`       | **Data Core Terminal Idle Pulsing Hum**           | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 4.0s seamless loop.                  | `Assets/Audio/SFX/Environment` | GDD §6, §10; PRD §4.5 (FR-32)                          |
| `SFX-ENV-DATACORE-HACK`       | **Data Core Terminal Purge Hacking Chirps**       | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 1.5s sequence.                       | `Assets/Audio/SFX/Environment` | GDD §6, §7; PRD §4.5 (FR-32)                           |
| `SFX-ENV-GATEWAY-UNLOCK`      | **Extraction Gateway Unseal & Depressurize**      | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 2.0s.                                | `Assets/Audio/SFX/Environment` | GDD §6, §7; PRD §4.5 (FR-32)                           |
| `SFX-ENV-TILE-COLLAPSE`       | **Root Core Arena Tile De-Referencing Rumble**    | Format: WAV 48kHz / 24-bit; Mono (3D Spatialized); Duration: 2.5s.                                | `Assets/Audio/SFX/Environment` | GDD §10; PRD §2.4 (UJ-4), §4.5 (FR-31)                 |
| `SFX-UI-NAV-CLICK`            | **Terminal Menu Navigation Click**                | Format: WAV 48kHz / 24-bit; 2D Stereo; Duration: 0.08s; Crisp low-latency.                        | `Assets/Audio/SFX/UI`          | PRD §4.8 (FR-44, FR-46)                                |
| `SFX-UI-NAV-HOVER`            | **Terminal Menu Item Hover Blip**                 | Format: WAV 48kHz / 24-bit; 2D Stereo; Duration: 0.05s; Soft high-pass tone.                      | `Assets/Audio/SFX/UI`          | PRD §4.8 (FR-46)                                       |
| `SFX-UI-GADGET-REFUND`        | **Precision Headshot Streak Gadget Refund Chime** | Format: WAV 48kHz / 24-bit; 2D Stereo; Duration: 0.45s; Distinct high-reward sound.               | `Assets/Audio/SFX/UI`          | GDD §9; PRD §4.2 (FR-15)                               |
| `SFX-UI-DEBRIEF-TALLY`        | **Tactical Debrief Score Counter Tally**          | Format: WAV 48kHz / 24-bit; 2D Stereo; Duration: 0.04s per click; Looped sequence.                | `Assets/Audio/SFX/UI`          | GDD §3; PRD §3, §4.5 (FR-33)                           |
| `SFX-UI-DEBRIEF-GRADE-S`      | **Tactical Debrief Grade S Confirmation Chord**   | Format: WAV 48kHz / 24-bit; 2D Stereo; Duration: 1.5s.                                            | `Assets/Audio/SFX/UI`          | GDD §3; PRD §4.5 (FR-33)                               |
| `SFX-UI-ACHIEVE-UNLOCK`       | **Steam Achievement Milestone Notification**      | Format: WAV 48kHz / 24-bit; 2D Stereo; Duration: 0.6s.                                            | `Assets/Audio/SFX/UI`          | PRD §4.7 (FR-43)                                       |

### 3.8 Audio - Ambience (3 assets)

| Mã Asset                 | Tên Asset                                      | Thông số kỹ thuật                                                                   | Thư mục Unity           | PRD / GDD Ref                         |
| :----------------------- | :--------------------------------------------- | :---------------------------------------------------------------------------------- | :---------------------- | :------------------------------------ |
| `SFX-AMB-SUB01-DRONE`    | **Subsector 01 Sterile Corridor Server Drone** | Format: WAV 48kHz / 24-bit; Stereo Loop; Seamless loop; Integrated low-pass filter. | `Assets/Audio/Ambience` | GDD §10, §11; PRD §4.5 (FR-29)        |
| `SFX-AMB-SUB02-VOID`     | **Subsector 02 Endless Digital Void Hum**      | Format: WAV 48kHz / 24-bit; Stereo Loop; Seamless loop; Sub-bass emphasis.          | `Assets/Audio/Ambience` | GDD §10, §11; PRD §4.5 (FR-29)        |
| `SFX-AMB-SUB03-COLLAPSE` | **Subsector 03 Reality De-Rez Glitch Hum**     | Format: WAV 48kHz / 24-bit; Stereo Loop; Dynamic tension escalation layers.         | `Assets/Audio/Ambience` | GDD §10, §11; PRD §4.5 (FR-29, FR-31) |

### 3.9 Audio - Voice (6 assets)

| Mã Asset               | Tên Asset                                       | Thông số kỹ thuật                                                                      | Thư mục Unity                       | PRD / GDD Ref                          |
| :--------------------- | :---------------------------------------------- | :------------------------------------------------------------------------------------- | :---------------------------------- | :------------------------------------- |
| `VO-AI-ALERT-01`       | **Sentinel Radio: Target Detected Callout**     | Format: WAV 48kHz / 24-bit; Mono (3D HRTF); Bandpass radio filter + synthetic vocoder. | `Assets/Audio/Voice/Sentinel_Radio` | GDD §8, §11; PRD §4.4 (FR-22, FR-27)   |
| `VO-AI-COVER-01`       | **Sentinel Radio: Hard Cover Callout**          | Format: WAV 48kHz / 24-bit; Mono (3D HRTF); Duration: ~1.2s; 3 variations.             | `Assets/Audio/Voice/Sentinel_Radio` | GDD §8, §11; PRD §4.4 (FR-23, FR-27)   |
| `VO-AI-FLANK-01`       | **Sentinel Radio: Flanking Intent Callout**     | Format: WAV 48kHz / 24-bit; Mono (3D HRTF); Duration: ~1.4s; 3 variations.             | `Assets/Audio/Voice/Sentinel_Radio` | GDD §8, §11; PRD §4.4 (FR-24, FR-27)   |
| `VO-AI-STACK-01`       | **Sentinel Radio: Doorway Stack Callout**       | Format: WAV 48kHz / 24-bit; Mono (3D HRTF); Duration: ~1.8s; 2 variations.             | `Assets/Audio/Voice/Sentinel_Radio` | GDD §8, §11; PRD §4.4 (FR-24, FR-27)   |
| `VO-AI-SMOKE-BLIND`    | **Sentinel Radio: Sensors Compromised Callout** | Format: WAV 48kHz / 24-bit; Mono (3D HRTF); Duration: ~1.3s; 2 variations.             | `Assets/Audio/Voice/Sentinel_Radio` | GDD §8; PRD §4.3 (FR-19), §4.4 (FR-27) |
| `VO-BOSS-NULL01-INTRO` | **Null-01 Mirror Operative: Encounter Voice**   | Format: WAV 48kHz / 24-bit; Stereo with wide spatial reverb; Duration: ~2.2s.          | `Assets/Audio/Voice/Boss_Null01`    | GDD §8; PRD §4.4 (FR-26)               |

### 3.10 UI & 2D Art (7 assets)

| Mã Asset                 | Tên Asset                                      | Thông số kỹ thuật                                                                                 | Thư mục Unity           | PRD / GDD Ref                              |
| :----------------------- | :--------------------------------------------- | :------------------------------------------------------------------------------------------------ | :---------------------- | :----------------------------------------- |
| `UI-SPR-AMMO-FONT`       | **Receiver Emissive Ammo Counter Font Atlas**  | Resolution: 512 x 512 PNG; Alpha transparency; Emissive channel map.                              | `Assets/UI/Diegetic`    | GDD §5, §13 (EP-08); PRD §4.6 (FR-34)      |
| `UI-SPR-OLED-WATCH`      | **Forearm OLED Watch Interface Graphic Atlas** | Resolution: 1024 x 1024 PNG; High-contrast unlit cyan/amber palette; Crisp sub-pixel readability. | `Assets/UI/Diegetic`    | GDD §5, §13 (EP-08); PRD §3, §4.6 (FR-35)  |
| `UI-SPR-TERMINAL-SCREEN` | **Data Core Hacking Terminal Screen UI**       | Resolution: 1024 x 1024 PNG; Pixel grid overlay; CRT scanline mask.                               | `Assets/UI/Diegetic`    | GDD §7; PRD §3, §4.5 (FR-32)               |
| `UI-SPR-CROSSHAIR`       | **Minimalist Dynamic Crosshair Reticle**       | Resolution: 256 x 256 PNG; Alpha transparency; Vector SVG master available.                       | `Assets/UI/HUD_Screens` | GDD §7, §8; PRD §4.2 (FR-14), §4.6 (FR-36) |
| `UI-TEX-DEBRIEF-BG`      | **Tactical Debrief Assessment Layout Canvas**  | Resolution: 1920 x 1080 PNG; Minimalist brutalist wireframe layout; Unity UI Canvas prefab.       | `Assets/UI/HUD_Screens` | GDD §3; PRD §3, §4.5 (FR-33)               |
| `UI-ICO-ACHIEVE-PACK`    | **Steam Achievements Icon Set (10 Badges)**    | Resolution: 256 x 256 PNG (each); 10 discrete icons; High-contrast cyan/grey/amber.               | `Assets/UI/Steamworks`  | PRD §4.7 (FR-43)                           |
| `UI-TEX-TITLE-LOGO`      | **Null-Protocol Game Title Logo**              | Resolution: 2048 x 1024 PNG with alpha transparency; 4K master available.                         | `Assets/UI/Branding`    | GDD §1; PRD §1                             |

### 3.11 Animations (18 assets)

| Mã Asset                   | Tên Asset                                           | Thông số kỹ thuật                                                                             | Thư mục Unity                               | PRD / GDD Ref                       |
| :------------------------- | :-------------------------------------------------- | :-------------------------------------------------------------------------------------------- | :------------------------------------------ | :---------------------------------- |
| `ANIM-FP-IDLE`             | **FP Commando Weapon Idle Pose**                    | Frame rate: 60 FPS; Duration: 2.0s loop; Calibrated for Vector-9, Synapse-AR, and Phase-Rail. | `Assets/Art/Animations/FirstPerson`         | GDD §7; PRD §4.1 (FR-1)             |
| `ANIM-FP-WALK`             | **FP Commando Walk Cycle Bob**                      | Frame rate: 60 FPS; Duration: 1.0s loop; Smooth figure-eight procedural weapon bob.           | `Assets/Art/Animations/FirstPerson`         | GDD §7; PRD §4.1 (FR-1)             |
| `ANIM-FP-CROUCH-WALK`      | **FP Commando Crouch Walk Stabilized Cycle**        | Frame rate: 60 FPS; Duration: 1.2s loop; Eye height reduced by 0.7m.                          | `Assets/Art/Animations/FirstPerson`         | GDD §7; PRD §4.1 (FR-2)             |
| `ANIM-FP-SPRINT`           | **FP Commando Tactical Sprint Dash**                | Frame rate: 60 FPS; Duration: 0.6s loop; Sprint-to-fire transition blend tree.                | `Assets/Art/Animations/FirstPerson`         | GDD §7; PRD §4.1 (FR-3)             |
| `ANIM-FP-ADS-TRANSITION`   | **FP Aim Down Sights (ADS In / Out)**               | Frame rate: 60 FPS; Duration: 0.15s transition; Dampens sway by 80%.                          | `Assets/Art/Animations/FirstPerson`         | GDD §7, §8; PRD §4.2 (FR-14)        |
| `ANIM-FP-VEC9-FIRE`        | **Vector-9 Fire & Recoil Kick**                     | Frame rate: 60 FPS; Duration: 0.15s; Procedural camera kick link.                             | `Assets/Art/Animations/FirstPerson`         | GDD §8; PRD §4.2 (FR-10)            |
| `ANIM-FP-VEC9-RELOAD`      | **Vector-9 Tactical Reload Sequence**               | Frame rate: 60 FPS; Duration: 1.2s strict timing; Animated mag bone + left hand.              | `Assets/Art/Animations/FirstPerson`         | GDD §8; PRD §4.2 (FR-10)            |
| `ANIM-FP-SYN-FIRE`         | **Synapse-AR Burst Fire & Climb Recoil**            | Frame rate: 60 FPS; Duration: 0.32s (3-shot cycle); Camera shake hook.                        | `Assets/Art/Animations/FirstPerson`         | GDD §8; PRD §4.2 (FR-11)            |
| `ANIM-FP-SYN-RELOAD`       | **Synapse-AR Tactical Reload Sequence**             | Frame rate: 60 FPS; Duration: 1.9s strict timing; Left hand IK pass.                          | `Assets/Art/Animations/FirstPerson`         | GDD §8; PRD §4.2 (FR-11)            |
| `ANIM-FP-RAIL-CHARGE-FIRE` | **Phase-Rail Charge & Discharge Shockwave**         | Frame rate: 60 FPS; Duration: 0.6s charge + 0.4s recovery; Heavy camera punch.                | `Assets/Art/Animations/FirstPerson`         | GDD §8; PRD §4.2 (FR-12)            |
| `ANIM-FP-RAIL-RELOAD`      | **Phase-Rail Power Cell Replacement**               | Frame rate: 60 FPS; Duration: 1.8s strict timing; Battery mesh swap keyframe.                 | `Assets/Art/Animations/FirstPerson`         | GDD §8; PRD §4.2 (FR-12)            |
| `ANIM-FP-GADGET-THROW`     | **FP Gadget Toss (Barricade Puck / Smoke)**         | Frame rate: 60 FPS; Duration: 0.45s; Discrete projectile spawn event at frame 14.             | `Assets/Art/Animations/FirstPerson/Gadgets` | GDD §7, §8; PRD §4.3 (FR-16, FR-18) |
| `ANIM-FP-MINE-PLANT`       | **FP Logic-Trip Mine Surface Affix**                | Frame rate: 60 FPS; Duration: 0.5s; Surface raycast anchor event.                             | `Assets/Art/Animations/FirstPerson/Gadgets` | GDD §7, §8; PRD §4.3 (FR-20)        |
| `ANIM-AI-SENTINEL-LOCO`    | **Sentinel Tactical BlendTree Locomotion**          | Frame rate: 60 FPS; 2D BlendTree; Root motion enabled for clean grid navigation.              | `Assets/Art/Animations/AI`                  | GDD §8; PRD §4.4 (FR-22, FR-23)     |
| `ANIM-AI-COVER-PEEK`       | **Sentinel Cover Peek & Burst Fire (Left / Right)** | Frame rate: 60 FPS; Left and Right variants; Duration: 1.2s total cycle.                      | `Assets/Art/Animations/AI`                  | GDD §8; PRD §4.4 (FR-23)            |
| `ANIM-AI-DOOR-STACK`       | **Sentinel Doorway Stacking & Breach Slice**        | Frame rate: 60 FPS; Pointman and Rearguard synchronized animation clips.                      | `Assets/Art/Animations/AI`                  | GDD §8; PRD §4.4 (FR-24)            |
| `ANIM-AI-BREACHER-ADVANCE` | **Shield Breacher Deliberate March**                | Frame rate: 60 FPS; Duration: 1.2s loop; Grounded deliberate stride; Shield hitbox anchored.  | `Assets/Art/Animations/AI`                  | GDD §8; PRD §4.4 (FR-25)            |
| `ANIM-AI-FROZEN-STASIS`    | **Hostile Logic-Trip Frozen Wireframe Pose**        | Single static frame pose applied to animator; Duration: 5.0s timer freeze.                    | `Assets/Art/Animations/AI`                  | GDD §8; PRD §4.3 (FR-21)            |

---

## 4. Hướng dẫn tích hợp & Sử dụng trong Unity

### 4.1 Tạo màn chơi tự động (Scene Generation)

Tất cả các mô-đun và assets đã được lập trình sẵn trong công cụ Editor:

- **Script Builder:** `Assets/_Project/Editor/ProductionSceneBuilder.cs`
- **Menu Unity:** Chọn `NullProtocol > Build Production Subsector 01 Scene` để tự động:
  1. Xếp đặt các phòng 4m grid (`Room_01` đến `Room_04`).
  2. Gắn vật cản chiến thuật (`CoverNode` Low & High).
  3. Cài đặt Data Core Terminal & Cổng trích xuất Gateway.
  4. Lắp ráp First-Person Player Rig đầy đủ cánh tay, đồng hồ OLED, súng Synapse-AR/Vector-9 và bộ Gadget.
  5. Thiết lập AI Sentinel tuần tra, hitbox đầu 1-tap kill và hiệu ứng vỡ voxel.
  6. Nướng NavMesh tự động bằng `NavMeshSurface`.
