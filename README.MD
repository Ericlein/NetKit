# DevOps Helper – Windows System Tray App

A lightweight Windows system tray application built with **C# (.NET + WPF)** to speed up common DevOps tasks.  
Initial features:  
- Run fast HTTP(S) requests against a domain (using `HttpClient`).  
- Check SSL certificate expiry natively in C# (no OpenSSL/WSL required).  

---

## 🚀 Plan & Setup

### 1. Tech Stack
- **C# (.NET 8)** – modern runtime.  
- **WPF** – flexible, stylable UI (borderless, rounded corners).  
- **NotifyIcon** (via WinForms interop) – system tray integration.  
- **HttpClient** – native HTTP(S) requests (faster & cleaner than `curl.exe`).  

---

### 2. Project Bootstrap
1. Create a new **WPF App (.NET)** in Visual Studio or via CLI:  
   ```bash
   dotnet new wpf -n msOps
---

### 3. System Tray & Hotkey
- Add tray icon context menu: **Open UI** | **Exit**.  
- Register a **global hotkey** (e.g. `Ctrl+Alt+Space`) → opens the UI instantly.  

---

### 4. Popup UI
- Create a **borderless WPF window** styled like a MacOS panel:  
  - Rounded corners, shadows.  
  - Input field for domain.  
  - Toggle for `http` / `https`.  
  - Buttons for **Curl** and **SSL Check**.  

---

### 5. Features
- **HTTP Request**  
  - Use `HttpClient` to send requests directly from C#.  
  - Capture and display headers or response.  

- **SSL Expiry Check**  
  - Use `TcpClient` + `SslStream` to fetch the server certificate.  
  - Display expiry date + remaining days.  

---

### 6. Quality of Life
- Persist last used domain (e.g. in `Properties.Settings`).  
- Optionally auto-run on Windows startup.  
- Error handling for bad domains / network issues.  

---

### 7. Polish
- Improve styling (minimal, clean, MacOS-like).  
- Add keyboard navigation.  
- Add tray notifications (e.g. SSL expiring soon).  

---

### 8. Future Extensions
- Add utilities like **ping** or **DNS lookup**.  
- Integrate with cloud CLIs (AWS, Azure, GCP).  
- Support “favorite domains” or quick actions.  