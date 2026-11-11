# 🕷️ SpiderDesk

A small, friendly desktop spider that hangs out on your Windows desktop.

<p align="center">
  <img src="docs/demo.gif" alt="The spider catches a bug, wraps it in silk and hangs the cocoon in its web" width="600">
</p>

It wanders around your screen, weaves webs in the corners, catches passing bugs, shakes off raindrops, and takes naps when there's nothing to do.

It runs quietly in the background without getting in your way or eating up your battery.

---

## 🌿 What it does

- **🕷️ Procedural walking:** Crawls around naturally with 8-legged Inverse Kinematics (IK) physics.
- **🕸️ Web weaving:** Spins both round orb webs and cozy corner webs.
- **🐛 Bug hunting:** Catches wandering flies, moths, fireflies, and wasps, wraps them in silk, and stores them in its pantry.
- **💧 Rain & Dew physics:** Water droplets collect on the silk threads (or can be washed via the tray menu). When the web sags under their weight, the spider walks over and shakes them off.
- **🍃 Light breeze:** An occasional room breeze gently sways the webs.
- **🖱️ Click-through:** Lives behind your work and games. You can click right through the webs without anything getting blocked.

<p align="center">
  <img src="docs/rain.gif" alt="The spider shakes rain droplets off its corner web" width="520">
  <br>
  <em>Heavy with rain, the web sags, so the spider shakes the droplets off.</em>
</p>

---

## 🚀 How to Run

You can run SpiderDesk directly from the source code using the [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0):

```bash
# 1. Clone the repository
git clone https://github.com/eamre/spider-desk.git
cd spider-desk

# 2. Run directly
dotnet run
```

That's it! The spider will appear on your desktop, and its control icon will show up in the Windows System Tray (near the clock).

### (Optional) Build a standalone executable
If you want to compile a single portable binary for yourself:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```

The compiled executable will be located in `./publish/SpiderDesk.exe`.

> **Note:** SpiderDesk stores your chosen preferences (selected theme, spawn rates) in `%LocalAppData%\SpiderDesk\settings.json`. If enabled from the tray menu, it can also create an optional Windows startup shortcut.

---

## 🎮 Controls

Right-click the **Spider icon in the System Tray** (by the clock) to:
- 🎨 Change themes (*Black Widow, Frost Crystal, Forest Tarantula, Cyber Neon*)
- 🕸️ Tell it to weave a new web or rappel down on a thread
- 🌧️ Trigger rain on webs (spawn water droplets)
- 🐛 Spawn a few extra bugs
- ⏸️ Pause the simulation

**Mouse interactions:**
- Drag your mouse across a web to pluck the silk — the spider will run over to investigate!
- Move the cursor quickly towards the spider to startle it.

---

## 🛠️ Built with
- **C# / .NET 9**
- **WPF** (Windows Presentation Foundation)
