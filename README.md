# 🧿 Lucky Dangle — Free Windows Screen Charm

**Lucky Dangle** is a free, lightweight, open-source Windows application that hangs an interactive, swaying charm (like the classic Mac Emoji Evil Eye 🧿) from the top corner of your screen. 

It sways gently while you work, stays out of your way, reacts naturally to your mouse cursor, and uses virtually zero battery or system resources.

---

## ✨ Highlights & Features

- 🆓 **100% Free & Open Source**: No subscriptions, no ads, no tracking, and no paid unlockables.
- 🧿 **Mac Emoji (🧿) Styling**: Designed to match Apple's clean Mac Emoji aesthetic with crisp Cobalt Royal Blue, White, Sky Blue, and a pure white thread.
- 💎 **Crisp WPF Rendering**: Built using WPF 32-bit ARGB per-pixel hardware transparency—guaranteeing **zero purple or magenta border halos**.
- 📏 **Tiny & Unobtrusive**: Compact default size (24px) or Micro size (16px) that hangs neatly in the corner without blocking your workspace.
- 🖱️ **Cursor Proximity Reaction**: Moving your mouse near the charm creates a gentle air breeze that makes it sway dynamically!
- 🔒 **Click-Through Mode**: Optional toggle that makes all mouse clicks pass directly through to whatever window is beneath the charm.
- ⚡ **Battery & Eco-Friendly**: Uses less than 15 MB of RAM and ~0.05% CPU. Automatically enters low-power idle mode when motionless.
- 🚀 **Windows Auto-Start**: Built-in system tray setting to automatically launch whenever your PC powers on.

---

## 🎨 Included Charm Styles

- 🧿 **Mac Emoji Evil Eye** *(Default)* — Classic Nazar amulet with glossy Apple specular catchlight.
- 🌟 **Golden Star** — Shimmering gold star & crescent moon motif.
- 💎 **Mystic Crystal Gem** — Faceted violet crystal quartz.
- 🐾 **Lucky Cat Paw** — Cute kawaii Maneki-Neko paw.
- 🪙 **Ancient Gold Coin** — Traditional lucky coin.

---

## 🚀 How to Run & Control

### 1. Launching
- Double-click the **`Lucky Dangle`** icon on your Desktop, or run `LuckyDangle.exe`.

### 2. System Tray Menu (Near the Clock 🕒)
Right-click the mini 🧿 **Evil Eye** icon in the bottom-right corner of your taskbar to access controls:

| Option | Description |
| :--- | :--- |
| **🧿 Select Charm Style** | Switch between Evil Eye, Star, Crystal, Paw, or Coin. |
| **📏 Charm Size** | Choose Micro (16px), Tiny (24px), Small (32px), or Medium (48px). |
| **📍 Screen Position** | Anchor to Top-Right Corner, Top-Left Corner, or Top-Center. |
| **🔒 Click-Through Mode** | Pass all mouse clicks through the charm to underlying apps. |
| **🚀 Windows Auto-Start** | Enable/disable launching automatically when Windows boots. |
| **❌ Exit** | Close the application. |

---

## 🛠️ Building from Source

If you wish to modify or build the project yourself:

1. Clone or copy the project files.
2. Open PowerShell in the project directory.
3. Run the build script:
   ```powershell
   .\Build-LuckyDangle.ps1
   ```
4. The compiled `LuckyDangle.exe` binary will be created instantly using native Windows `.NET Framework` compilers (`csc.exe`).

---

## 📜 License

Distributed under the **MIT License**. Free for personal and commercial use forever.
