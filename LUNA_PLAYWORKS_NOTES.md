# Luna Playworks Plugin — Ghi chú khi code Playable Ad (Unity)

> Tổng hợp từ tài liệu chính thức Unity Playworks Developer Docs (docs.lunalabs.io).
> Cập nhật theo docs tại thời điểm: 09/2026. Plugin mới nhất tham chiếu: **v7.2.0 (27/04/2026)**.

Playworks Plugin (trước đây gọi là Luna Playable) chuyển đổi (transpile) code C# của bạn sang JavaScript và chạy trên engine web riêng của họ (WebGL). Nó **không** dùng build target WebGL của Unity. Vì vậy có nhiều giới hạn cần lưu ý khi code.

---

## 1. Phiên bản & môi trường

| Hạng mục | Ghi chú |
|---|---|
| Unity | Hỗ trợ **2021.3 → 6000.0** (Personal/Pro, không tính alpha/beta). Dưới 2021.3 đã deprecated. |
| Unity 6 | Chỉ hỗ trợ **WebGL 2** (WebGL 1 không dùng được trên Unity 6). |
| WebGL | Mặc định WebGL 1.0. WebGL 2 hỗ trợ từ **Plugin 6.0.0** (bật ở `Settings > Advanced`). |
| C# (Compiler v1) | Đầy đủ **C# 7.0**, một phần 7.1–7.3. |
| C# (Compiler v2) | Hỗ trợ tới **C# 8.0 / 9.0** (có giới hạn). Bật khi cần feature mới. |
| .NET | Cần **.NET 4.7+** cài trên máy (để convert C# → JS). |
| MSBuild | Bắt buộc — thường ở `C:\Program Files (x86)\Microsoft Visual Studio\<ver>\...\MSBuild\Bin\MSBuild.exe`. Cài qua VS Build Tools (workload ".NET desktop development"). |
| OS phát triển | Windows và macOS. |
| Engine size | Base ~650kb nén, giảm được nhờ Runtime Analysis (code stripping). |

---

## 2. Plugin / thư viện đã được validate

Chỉ dùng plugin có **source code C#**. **KHÔNG hỗ trợ DLL** (kể cả .NET DLL; C++ plugin cũng không).

| Plugin | Version hỗ trợ |
|---|---|
| TextMeshPro | 3.0.6 (cần import TMP Essential Resources) |
| Spine | tới 4.2 beta |
| DOTween | tới 1.2.705 (DOTween & DOTween Pro tích hợp sẵn, không cần thay DLL) |
| Zenject | 8.0.0 |
| Cinemachine | tới 2.10.7 |
| LeanTween | ✔ |
| A* Pathfinding | ✔ (thay thế cho NavMesh) |
| Newtonsoft.Json | Hỗ trợ hạn chế |

> Nhiều plugin khác có thể chạy được nếu là source C# và không dùng API/feature chưa hỗ trợ.

---

## 3. Những thứ KHÔNG hỗ trợ (tránh dùng)

- **DLL / native plugin (C++)** → thay bằng source C#.
- **Ads SDK & Analytics SDK** → gỡ bỏ, hoặc dùng **Automatic Stubbing**, hoặc bọc bằng `#if !UNITY_LUNA`.
- **NavMesh** → dùng A* asset, tự viết logic C#, hoặc node-based movement.
- **HDRP** → dùng **URP** hoặc **Built-in (Default) render pipeline**.
- **DOTS / ECS** → không hỗ trợ.
- **Unity GUI cũ (IMGUI/OnGUI)** → dùng **uGUI**.
- **NGUI** → chuyển sang uGUI.
- **Deferred rendering** → chỉ hỗ trợ **Forward path** của built-in pipeline.
- **HDR / float textures**, **native texture compression**, **instancing/cbuffers/vertex buffer partial update**.
- **Precompiled shaders** (Compiled code: none) → gây lỗi `Cannot read property val of null`.
- **Shader target 3.5+** (chỉ từ Plugin 6.0.0 mới lên tới shader model 4.0). Dùng shader hỗ trợ **Shader Target 3.0** và tương thích **GLES2**.
- **Linear color space**: chỉ với WebGL2; và không hỗ trợ Texture3D, Texture2DArray, Cubemap, runtime texture creation trong linear.
- **NavMesh, WebGL uniform locations / uniform buffers**.

---

## 4. Giới hạn khi transpile C# (Bridge.NET — Compiler v1)

Bridge chỉ hỗ trợ tới **C# 7.0**. Feature từ 7.1+ dễ fail. Các trường hợp cần tránh + workaround:

- **Destructor `~ClassName()`** → dùng `OnDestroy()`.
- **`goto` statement (kể cả `goto case`)** → tách ra hàm riêng hoặc dùng đệ quy.
- **Circular inheritance giữa các interface** → viết lại, bỏ vòng lặp kế thừa.
- **Inline cast** → khai báo biến trước rồi mới cast; tốt nhất tránh inline cast hoàn toàn.

> Cần C# 8.0/9.0 → bật **Compiler V2** (hoạt động như bước tiền xử lý cho Compiler v1).

---

## 5. Rendering / Shader — lưu ý

**Hỗ trợ:** Standard & custom shader (1:1), Forward path, Normal maps, Light/Reflection/Ambient probes, Unity Fog, Baked lightmaps, MaterialPropertyBlock, Dynamic batching, Static batching, Realtime Shadows, Linear color space (WebGL2).

**Cẩn thận:**
- **Fog qua script** gây lỗi shader → chỉnh Fog qua cửa sổ **Lighting settings** thay vì script.
- **Realtime Shadows:** chỉ hỗ trợ **1 nguồn sáng**.
- **Lighting/WebGL1:** nhiều đèn intensity cao bị cộng dồn → cảnh bị "over-lit". Giảm số đèn / intensity.
- Shader phải bật **GLES20** trong Compile settings, nếu không sẽ không grab được variant.
- ShaderLab không hỗ trợ: `PackageRequirements`, `RenderPipeline` subshader tag, `DisableBatching`, `CanUseSpriteAtlas`, `PreviewType`, `AlphaToMask`, `Conservative`, `PassFlags`, `RequireOptions`.
- Màu từ fragment shader bị **clamp 0..1** trên WebGL1.

---

## 6. Animation (Mecanim) — lưu ý

- Hỗ trợ **Generic** & **Humanoid (Experimental)** — chỉ **playback cơ bản**.
- Hỗ trợ: Animator Layer (Weight, Mask, Blending, Sync, Timing), State (Motion, SpeedMultiplier, Mirror, CycleOffset, WriteDefault), Transition (Has/No exit time, Fixed duration, Transition duration).
- Hỗ trợ **RootMotion cơ bản** (`OnAnimatorMove`, `OnStateMove`, `Animator.ApplyBuiltinRootMotion()`).
- **BlendTree**: hỗ trợ từ Plugin 6.1.0 (trước đó không).
- **KHÔNG** hỗ trợ: **Sub/Nested StateMachine**, **Inverse Kinematics (IK)**. Humanoid chỉ tới Forward Kinematics.
- Mecanim tốn hiệu năng trên máy yếu → giảm số nhân vật humanoid active.
- Lưu ý: gọi `Animator.Play()` trên GameObject inactive sẽ cảnh báo/không chạy đúng.

---

## 7. Playable API bắt buộc (code cần có)

```csharp
// Điều hướng về store (CTA / end card)
Luna.Unity.Playable.InstallFullGame();

// Lifecycle pause/resume (khi app vào background hoặc click store)
Luna.Unity.LifeCycle.OnPause  += PauseGameplay;
Luna.Unity.LifeCycle.OnResume += ResumeGameplay;

// Bắt buộc với một số network (Mintegral, Vungle): báo kết thúc
Luna.Unity.LifeCycle.GameEnded();
```

**Xử lý orientation (responsive):** không có `Input.deviceOrientation`. Dùng `Screen.width` / `Screen.height`:

```csharp
float screenRatio = (float)Screen.width / Screen.height;
if (screenRatio >= 1) { /* Landscape */ }
else                  { /* Portrait  */ }
```

**Playground Fields:** dùng attribute `[LunaPlaygroundField]` để expose biến tạo nhiều biến thể. Docs khuyến nghị **tối thiểu 3** (cảnh báo `LP3011/LP3012` nếu thiếu).

---

## 8. Lỗi thường gặp & cách xử lý

### Export / build fails
- **"Assets processing failed"** → xem log `Project/LunaTemp/luna.log`. Nguyên nhân: MSBuild không tìm thấy / sai path; custom script lỗi; Editor script xung đột; hoặc bug plugin.
- **MSBuild failed** → cài MSBuild + .NET 4.7+ (kèm các targeting pack 4.x).
- **Scene thừa trong Build Settings** → xóa mọi scene không export khỏi Build Settings (kể cả khi chưa tick).
- **PNGQuant Access Restriction (macOS)** → `chmod 777 pngquant` trong thư mục `tools` của package (và `tools/pngquant/mac64|win64`).
- **Stubs sai path (khi làm nhóm)** → sửa path ở `Plugin UI > Code > External Sources` ("External C# sources folders").
- **GUI/Text Shader Error khi export** → thường bỏ qua được (shader IMGUI mặc định bị auto-export lỗi, hầu như không dùng).

### Object có trong Unity nhưng KHÔNG hiện trong build
- Chưa rebuild → bấm **Build Develop** lại.
- Chưa bật **Disable cache** trong browser (Plugin 3.8.0+ local server đã không cache).
- Sai scene chọn export.
- Mesh/sprite/shader bị **exclude** (kiểm tra tab Exclusions), hoặc mesh rỗng, hoặc culling sai (kiểm tra bằng BabylonJS Spector).
- **Duplicate sprite draw order** → set Sprite layer cụ thể.

### Runtime C# / API
- **`Type error: Cannot read property val of null`** → do precompiled shader.
- **Dùng API không hỗ trợ** → báo qua error codes (xem dưới). Cân nhắc bỏ feature, tìm workaround, hoặc email `playworks@unity3d.com`.
- **Async/Await**: hỗ trợ, nhưng **không đa luồng** (giới hạn web platform).
- **Audio không tự phát** cho tới khi user click → giới hạn autoplay của trình duyệt (khi chạy qua ad network SDK có thể khác).
- Playable đứng máy touchscreen laptop → tắt touchscreen trong Device Manager rồi restart.

### Error codes (Project Diagnostics / Playable Health Check)
Double-click message để nhảy tới code; click mã lỗi để mở docs. Một số mã hay gặp:
- **LP1015** – TMP version không hỗ trợ (có nút autofix).
- **LP1028** – Unity version không đúng.
- **LP1034** – Mesh > 65k triangles.
- **LP3009** – Dùng hàm/component không được hỗ trợ trong Plugin.
- **LP3011 / LP3012** – Chưa đủ `LunaPlaygroundField` (khuyến nghị ≥ 3).
- **LP3013** – Dùng `JsonUtility` feature không hỗ trợ.
- **LP3014** – Dùng sai API `Application.*`.

---

## 9. Checklist khi bắt đầu code playable

- [ ] Tạo **scene standalone** riêng cho playable, chỉ giữ asset/code cần thiết (build size có giới hạn theo network).
- [ ] Gỡ/stub toàn bộ Ads & Analytics SDK; bọc code bằng `#if !UNITY_LUNA` nếu cần.
- [ ] Dùng **URP hoặc Built-in**, KHÔNG HDRP; tránh DOTS/ECS, NavMesh, IMGUI, NGUI.
- [ ] Chỉ dùng plugin có source C#, đúng version đã validate.
- [ ] Viết code trong phạm vi **C# 7.0** (hoặc bật Compiler V2 nếu cần 8/9); tránh destructor, `goto`, inline cast.
- [ ] Thiết kế **responsive** bằng `Screen.width/height`.
- [ ] Implement Playable API: `InstallFullGame()`, `OnPause/OnResume`, `GameEnded()` (nếu network yêu cầu).
- [ ] Thêm ≥ 3 `[LunaPlaygroundField]`.
- [ ] Cập nhật plugin lên bản mới nhất trước khi debug (nhiều bug được fix theo version).
- [ ] Test trên **Chrome incognito**, dùng Develop build (`LunaTemp/Stage4/develop`).

---

## 10. Ad networks được hỗ trợ (tham khảo)

Aarki, AdColony, Adikteev, AppLovin, Appreciate, BigaBid, Display & Video 360, Facebook Gaming, Google Ad Manager, Google Ads, InMobi, Kayzen, Liftoff, Meta, Mintegral, Moloco, MRAID (generic), MRAID host, Remerge, Snapchat, Tencent, The Trade Desk, TikTok, Unity Ads, Vungle, YouAppi.

> Mỗi network có giới hạn build size & yêu cầu riêng — kiểm tra trước khi upload.

---

## Nguồn tham khảo

Nguồn: Unity Playworks Developer Docs — `docs.lunalabs.io` (các trang: overview, getting-started/limitations, faq, setup/dependencies, setup/export-failures, supported-features/supported-rendering-features, supported-features/mecanim, code/unity-plugins, playable-setup/playable-api, common-issues, release-notes).
