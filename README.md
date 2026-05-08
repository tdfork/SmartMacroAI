<p align="center">
  <img src="Assets/logo.ico" alt="SmartMacroAI Logo" width="80" height="80">
</p>

<h1 align="center">SmartMacroAI</h1>

<p align="center">
  <strong>Smart RPA Automation for Windows</strong>
</p>

<p align="center">
  <a href="https://github.com/TroniePh/SmartMacroAI/releases/latest"><img src="https://img.shields.io/badge/version-1.5.7-0078D4?style=flat-square" alt="Version"></a>
  <img src="https://img.shields.io/badge/platform-Windows%2010%2B-0078D4?style=flat-square&logo=windows" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet" alt=".NET">
  <img src="https://img.shields.io/badge/language-VI%20%7C%20EN-F7931E?style=flat-square" alt="Language">
  <a href="https://github.com/TroniePh/SmartMacroAI/releases/latest"><img src="https://img.shields.io/github/downloads/TroniePh/SmartMacroAI/total?style=flat-square&color=success" alt="Downloads"></a>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#download">Download</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#driver-level-mode">Driver Level</a> •
  <a href="#build-from-source">Build</a> •
  <a href="#tiếng-việt">Tiếng Việt</a>
</p>

---

## Overview

SmartMacroAI is a professional-grade Windows macro automation tool with AI integration. It automates repetitive tasks — game farming, web form filling, data entry, UI testing — with support for anti-cheat games via kernel-level Interception driver.

> *Created by Phạm Duy – Giải pháp tự động hóa thông minh.*

---

## Features

### Input Automation
| Mode | Click | Key Press | Description |
|------|:-----:|:---------:|-------------|
| **Stealth** | ✅ | ✅ | Background PostMessage — window doesn't need focus |
| **Raw Input** | ✅ | ✅ | SendInput with hardware scan codes |
| **Hardware** | ✅ | ✅ | SetCursorPos + mouse_event |
| **Driver Level** | ✅ | ✅ | Kernel Interception driver — bypasses anti-cheat |

### Core Capabilities

- **Image Recognition** — Find image on screen with confidence threshold, auto-click at found position
- **OCR Text Detection** — Read text from screen regions via Tesseract 5.2
- **If/Else Branching** — Conditional logic with nested Then/Else action branches
- **Loop Control** — Count-based, infinite, and break-condition loops
- **Variables** — Set, read, and branch on macro-scoped variables
- **CSV Auto Fill** — Loop CSV rows into web/desktop forms automatically
- **AI Integration** — OpenAI & Gemini for smart decision-making within macros
- **Web Automation** — Playwright-based browser control for web tasks

### Productivity

- **Multi Dashboard** — Monitor and control multiple macros simultaneously
- **Macro Recording** — Record mouse/keyboard actions in real-time
- **Scheduler** — Time-based, interval, or startup-triggered execution
- **Undo/Redo** — Full Ctrl+Z / Ctrl+Y support in macro editor
- **Copy/Paste** — Duplicate actions with Ctrl+C / Ctrl+V
- **Drag & Drop** — Reorder actions, move in/out of If/Else branches
- **6 Templates** — Pre-built macro templates ready to customize
- **Run History** — Execution logs with filtering and statistics

### Security & Distribution

- **Macro Lock** — Password-protect individual macros
- **Script Sharing** — Share macros via encoded strings
- **Anti-Detection** — Bézier mouse curves + randomized delays
- **Telegram Alerts** — Receive notifications on macro completion
- **Bilingual UI** — Full English & Vietnamese (826 localization keys)

---

## Download

| File | Description |
|------|-------------|
| [**SmartMacroAI-v1.5.7-Setup.exe**](https://github.com/TroniePh/SmartMacroAI/releases/latest) | Windows installer (recommended) |
| [**SmartMacroAI-v1.5.7-portable-win-x64.zip**](https://github.com/TroniePh/SmartMacroAI/releases/latest) | Portable — extract and run |
| [**SmartMacroAI-v1.5.7-win-x64.zip**](https://github.com/TroniePh/SmartMacroAI/releases/latest) | Full package with web automation |

---

## System Requirements

| Component | Requirement |
|-----------|-------------|
| OS | Windows 10 x64 (build 19041+) or later |
| Runtime | .NET 8.0 (bundled in self-contained builds) |
| RAM | 4 GB minimum |
| Storage | 300 MB (core) / 600 MB (with web automation) |
| Admin | Required for Driver Level mode installation |

---

## Quick Start

1. Download the latest installer from [Releases](https://github.com/TroniePh/SmartMacroAI/releases/latest)
2. Run `SmartMacroAI-v1.5.7-Setup.exe` as Administrator
3. Launch SmartMacroAI
4. Click **+ New Macro** — choose a template or start blank
5. Select your target window → add actions → press **Run** (F5)

---

## Driver Level Mode

> Enables macros inside games protected by anti-cheat systems (MapleStory, Cabal Online, etc.)

```
Settings → Driver Level → Install Now → Reboot → Select "Driver Level" in actions
```

1. Navigate to **Settings → Driver Level**
2. Click **Install Now** — approve the UAC prompt
3. **Restart your PC** (required for kernel driver)
4. Open SmartMacroAI → select **Driver Level** mode in any Click/Key action
5. Click **Test Driver** to verify

**Supported:** MapleStory, Cabal Online, MU Online, and most DirectX/OpenGL games with kernel anti-cheat.

---

## Templates

| Template | Actions | Use Case |
|----------|:-------:|----------|
| Auto Login Website | 7 | Web authentication |
| Auto Fill CSV Data | 9 (loop) | Bulk form submission |
| Auto Repeat Task | 3 (loop) | General automation |
| Image Detection Loop | 4 (loop) | Visual condition loops |
| Hotkey Automation | 3 (loop) | Key sequence macros |
| Game Farming Loop | 6 (loop) | Game skill rotation |

---

## Build from Source

**Prerequisites:** .NET 8 SDK, Windows 10+

```powershell
git clone https://github.com/TroniePh/SmartMacroAI.git
cd SmartMacroAI
dotnet restore
dotnet build
dotnet run
```

### Publish Release

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish/SmartMacroAI-v1.5.7
```

### Build Installer

```powershell
# Requires Inno Setup 6
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
```

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for full version history.

### v1.5.7 (2026-05-08)
- Full project audit with auto-fix across 6 phases
- Undo/Redo (Ctrl+Z/Y) in macro editor
- Copy/Paste actions (Ctrl+C/V)
- Fixed GDI resource leaks in screenshot capture
- Fixed CancellationToken handling in scheduler
- DLL whitelist expanded — eliminates false-positive security warnings
- 826 localization keys fully synchronized (EN/VI)
- 0 build warnings, 0 errors

---

## Contributing

Pull requests are welcome. Please open an issue first to discuss proposed changes.

---

## Support the Developer

If SmartMacroAI helps your workflow, consider supporting continued development:

<p align="center">
  <a href="https://www.paypal.com/paypalme/nhocbobi22">
    <img src="https://img.shields.io/badge/Donate-PayPal-00457C?style=for-the-badge&logo=paypal" alt="Donate via PayPal">
  </a>
</p>

- **PayPal:** nhocbobi22@gmail.com
- **Bank QR:** Available in the app's About section

---

## Contact

- **Issues:** [github.com/TroniePh/SmartMacroAI/issues](https://github.com/TroniePh/SmartMacroAI/issues)
- **Website:** [tronieph.github.io/SmartMacroAI-Website](https://tronieph.github.io/SmartMacroAI-Website/)

---

## Tiếng Việt

### SmartMacroAI là gì?

SmartMacroAI là công cụ tự động hóa macro chuyên nghiệp cho Windows, tích hợp AI. Tự động hóa tác vụ lặp lại — farm game, điền form web, nhập liệu, kiểm thử UI — hỗ trợ game anti-cheat qua driver Interception cấp nhân.

### Tính năng chính

- **4 chế độ input** — Stealth, Raw Input, Hardware, Driver Level (vượt anti-cheat)
- **Nhận diện hình ảnh** — Tìm ảnh trên màn hình, tự click vào vị trí
- **OCR đọc chữ** — Nhận dạng text từ vùng màn hình (Tesseract 5.2)
- **Logic If/Else** — Phân nhánh điều kiện với Then/Else lồng nhau
- **Vòng lặp** — Lặp theo số lần, vô hạn, hoặc điều kiện dừng
- **Biến số** — Đặt, đọc, và phân nhánh theo biến macro
- **Tự động điền CSV** — Lặp dữ liệu CSV vào form web/desktop
- **Tích hợp AI** — OpenAI & Gemini hỗ trợ quyết định thông minh
- **Web Automation** — Điều khiển trình duyệt qua Playwright
- **Multi Dashboard** — Quản lý nhiều macro cùng lúc
- **Ghi macro** — Thu hành động chuột/phím thời gian thực
- **Lịch chạy** — Chạy theo giờ, khoảng cách, hoặc khởi động Windows
- **Undo/Redo** — Ctrl+Z / Ctrl+Y đầy đủ trong editor
- **Copy/Paste** — Nhân bản action bằng Ctrl+C / Ctrl+V
- **Kéo thả** — Sắp xếp action, di chuyển vào/ra nhánh If/Else
- **6 mẫu có sẵn** — Template macro sẵn sàng tùy chỉnh
- **Khóa macro** — Bảo vệ bằng mật khẩu
- **Chống phát hiện** — Chuột Bézier + delay ngẫu nhiên
- **Thông báo Telegram** — Nhận tin khi macro hoàn thành
- **Giao diện song ngữ** — 826 khóa ngôn ngữ Anh/Việt

### Bắt đầu nhanh

1. Tải installer mới nhất tại [Releases](https://github.com/TroniePh/SmartMacroAI/releases/latest)
2. Chạy `SmartMacroAI-v1.5.7-Setup.exe` với quyền Administrator
3. Mở SmartMacroAI
4. Bấm **+ Macro mới** → chọn mẫu hoặc tạo từ đầu
5. Chọn cửa sổ mục tiêu → thêm action → bấm **Chạy** (F5)

### Driver Level (Game Anti-Cheat)

```
Cài đặt → Driver Level → Cài đặt ngay → Khởi động lại → Chọn "Driver Level" trong action
```

**Game hỗ trợ:** MapleStory, Cabal Online, MU Online và hầu hết game DirectX/OpenGL có anti-cheat.

### Ủng hộ tác giả

<p align="center">
  <a href="https://www.paypal.com/paypalme/nhocbobi22">
    <img src="https://img.shields.io/badge/Ủng_hộ-PayPal-00457C?style=for-the-badge&logo=paypal" alt="Donate via PayPal">
  </a>
</p>

- **PayPal:** nhocbobi22@gmail.com
- **QR Bank:** Xem trong phần **About** của ứng dụng

---

<p align="center">
  <sub>Made with ❤️ in Vietnam</sub><br>
  <em>Created by Phạm Duy – Giải pháp tự động hóa thông minh.</em>
</p>
