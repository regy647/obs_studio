# Protocol Selection Guide

## The Problem
- **HTTP mode**: Works in OBS Browser Source but styling changes don't persist
- **HTTPS mode**: Styling works perfectly but may not display in OBS Browser Source

## Solution: Dual Protocol Support

The application now supports both HTTP and HTTPS modes. Choose based on your needs:

### 🌐 HTTP Mode (OBS Compatible)
**Usage:**
```bash
dotnet run --http
# or
dotnet run -h
# or double-click run-http.bat
```

**Features:**
- ✅ Works reliably in OBS Browser Source
- ✅ Transcriptions and translations work perfectly
- ❌ Style changes (colors, sizes) may not persist in OBS
- ❌ localStorage may not work properly in OBS

**Best for:** When you need OBS Browser Source to work reliably

### 🔒 HTTPS Mode (Better Styling)
**Usage:**
```bash
dotnet run --https
# or
dotnet run -s
# or double-click run-https.bat
```

**Features:**
- ✅ All styling changes work perfectly
- ✅ localStorage persistence works
- ✅ CSS custom properties work reliably
- ❌ May not display in OBS Browser Source (browser security)

**Best for:** When you need full styling control and can use screen capture instead of Browser Source

## URLs
- **HTTP**: http://localhost:50000
- **HTTPS**: https://localhost:50001

## OBS Browser Source Setup
1. **For HTTP**: Use `http://localhost:50000`
2. **For HTTPS**: Use `https://localhost:50001` (may require accepting SSL certificate)

## Recommended Workflow
1. **Development/Testing**: Use HTTPS mode for full styling control
2. **OBS Recording**: Use HTTP mode for reliable OBS Browser Source
3. **Hybrid**: Use HTTPS for styling setup, then switch to HTTP for OBS

## Troubleshooting
- **HTTPS not working in OBS**: Accept the SSL certificate in your regular browser first
- **HTTP styling not persisting**: This is a known limitation of HTTP mode in OBS
- **Port conflicts**: Make sure no other applications are using ports 50000 or 50001
