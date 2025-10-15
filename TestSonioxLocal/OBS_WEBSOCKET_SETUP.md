# 🚀 OBS WebSocket Setup Guide

## **The Elegant Solution: Automatic OBS Styling**

This app now supports **automatic OBS styling** via WebSocket API! When you change colors, sizes, or other settings in the web interface, it automatically updates your OBS Browser Source - no manual copying needed!

## **Setup Steps:**

### **1. Install OBS WebSocket Plugin**
1. Go to: https://github.com/obsproject/obs-websocket/releases
2. Download the latest `obs-websocket-[version]-Windows.zip`
3. Extract the `obs-websocket.dll` file
4. Copy it to your OBS Studio `plugins` folder:
   - Usually: `C:\Program Files\obs-studio\obs-plugins\64bit\`
5. Restart OBS Studio

### **2. Enable WebSocket Server**
1. In OBS Studio: **Tools** → **WebSocket Server Settings**
2. Check **"Enable WebSocket server"**
3. Set **Port**: `4455` (default)
4. Set **Password**: Choose a secure password (e.g., `myobs2024`)
5. Click **OK**

### **3. Configure Your App**
1. Open `appsettings.json`
2. Update the OBS WebSocket settings:
   ```json
   "OBSWebSocket": {
     "Host": "localhost",
     "Port": 4455,
     "Password": "myobs2024"
   }
   ```
   Replace `myobs2024` with your actual password.

### **4. Set Your Browser Source Name**
1. In your `Index.cshtml`, find this line:
   ```javascript
   sourceName: 'Browser Source', // Change this to your actual OBS source name
   ```
2. Change `'Browser Source'` to match your actual OBS Browser Source name

## **How It Works:**

1. **User changes styling** in the web interface (colors, sizes, etc.)
2. **App automatically sends WebSocket command** to OBS
3. **OBS updates Browser Source CSS** instantly
4. **Changes appear immediately** in OBS - no manual steps!

## **Features:**
- ✅ **Automatic CSS updates** - no copying/pasting
- ✅ **Real-time styling** - changes apply instantly
- ✅ **Works with HTTP and HTTPS**
- ✅ **Fallback support** - works even if OBS WebSocket is disabled
- ✅ **User-friendly** - just use the normal web interface

## **Testing:**

1. Start your app: `dotnet run --http`
2. Open: `http://localhost:50000`
3. Change text color to yellow
4. Watch OBS Browser Source update automatically! 🎉

## **Troubleshooting:**

**"OBS WebSocket not available"** in console:
- Check if OBS WebSocket plugin is installed
- Verify WebSocket server is enabled in OBS
- Confirm password in `appsettings.json` matches OBS settings
- Make sure OBS is running

**Styles not updating in OBS:**
- Check Browser Source name matches the one in your code
- Verify WebSocket connection in OBS Tools menu
- Look for error messages in browser console

## **Production Deployment:**
For production, update the `Host` setting to your server's IP address:
```json
"OBSWebSocket": {
  "Host": "192.168.1.100",  // Your server IP
  "Port": 4455,
  "Password": "your-production-password"
}
```

This is the **industry-standard solution** for OBS automation! 🎯
