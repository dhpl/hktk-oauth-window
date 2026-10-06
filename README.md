# HKTK OAuth SDK for Windows

SDK OAuth cho game Windows, Cocos, Unity, Java và libGDX. Phiên bản hiện tại là `1.3.12`, hỗ trợ Windows x64.

## C++/Cocos với CMake

```bash
git submodule add https://github.com/dhpl/hktk-oauth-window.git third_party/hktk-oauth-window
```

```cmake
add_subdirectory(third_party/hktk-oauth-window)
target_link_libraries(your_game PRIVATE HKTKSDK::HKTKSDK)
```

Khi đóng gói game, copy `native/bin/HKTKSDK.dll` cạnh file `.exe`.

```cpp
#include <HKTKSDK.hpp>

hktk::Config config;
config.environment = hktk::Environment::Production;
config.clientId = "YOUR_CLIENT_ID";

hktk::Client client(config);
auto login = client.loginAsync();
auto result = login.get(); // Chờ trên worker thread.
```

## C#/.NET

Thêm `csharp/HKTKSDK.csproj` vào solution hoặc tham chiếu trực tiếp
`csharp/HKTKSDK.Managed.dll`, rồi đặt `native/bin/HKTKSDK.dll` cạnh executable.

```csharp
using HKTK.Windows;

using var client = new HKTKClient(new HKTKConfig
{
    Environment = HKTKEnvironment.Production,
    ClientId = "YOUR_CLIENT_ID",
});

HKTKLoginResult result = await client.LoginAsync();
```

## Unity

Trong Unity Package Manager, chọn **Add package from disk** và mở:

```text
unity/com.hktk.sdk/package.json
```

Package đã chứa C# wrapper và Windows x86_64 plugin.

## Java

Thêm `java/hktk-sdk-windows-java-1.3.12.jar` vào classpath. Đặt `java/HKTKSDK.dll` và `java/HKTKSDKJNI.dll` trong `java.library.path`.

```java
HKTKConfig config = HKTKConfig.builder("YOUR_CLIENT_ID").build();
try (HKTKClient client = new HKTKClient(config)) {
    HKTKLoginResult result = client.login();
}
```

libGDX dùng thêm `gdx/hktk-sdk-windows-gdx-1.3.12.jar`. Callback được chuyển về libGDX application thread.

SDK mở trình duyệt mặc định, nhận OAuth callback qua loopback và trả `code`, `state`, `redirectUri`. Backend game dùng nguyên văn `redirectUri` để đổi code lấy token. Không đặt `clientSecret` trong game.

Đăng ký redirect URI sau trong Partner App:

```text
http://127.0.0.1/hktk-callback
```

Tài liệu API: [docs.hktk.vn/oauth/hktk-auth-api.html](https://docs.hktk.vn/oauth/hktk-auth-api.html)
