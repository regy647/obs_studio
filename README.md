# TestSonioxLocal - Real-Time Transcription & Translation System

## Audio Input Methods

This application supports **two different methods** for capturing audio and sending it to the Soniox API for real-time transcription and translation:

### Method 1: OBS Studio / System Audio Capture (Currently Active)
**How it works:**
- Captures **system audio output** (what's playing on your computer)
- Uses **WASAPI Loopback Capture** to intercept audio streams
- Perfect for **OBS Studio integration** - captures whatever audio OBS is processing
- **No browser microphone permissions** required
- **Higher quality audio** since it captures at system level

**Use cases:**
- Streaming with OBS Studio
- Transcribing video calls, webinars, or live streams
- Processing pre-recorded audio content
- Capturing audio from any application

### Method 2: Web Microphone (Currently Commented Out)
**How it works:**
- Captures audio directly from the **browser's microphone**
- Uses **Web Audio API** for real-time audio processing
- Converts audio to **PCM format** compatible with Soniox API
- Requires **browser microphone permissions**
- Processes audio in **real-time** as it's spoken

**Use cases:**
- Direct speech input from users
- Live transcription of conversations
- Web-based voice interfaces
- Mobile device compatibility

### Why We Switched Back to OBS Studio Method

**Original Implementation:** OBS Studio/System Audio Capture
**Later Added:** Web Microphone Support
**Current Choice:** Back to OBS Studio Method

**Reasons for the switch:**
1. **Better Audio Quality** - System-level capture provides higher fidelity
2. **OBS Integration** - Seamless integration with streaming workflows
3. **No Permission Issues** - No need for browser microphone permissions
4. **Flexibility** - Can capture any audio source, not just microphone
5. **Professional Use** - More suitable for streaming and broadcasting scenarios

### How to Switch Between Methods

**To use OBS Studio/System Audio (Current):**
- No changes needed - this is the active configuration
- Audio is automatically captured from system output

**To use Web Microphone (Currently disabled):**
1. Uncomment web microphone services in `Program.cs`
2. Uncomment `WebAudioController.cs` and `WebMicAudioService.cs`
3. Comment out the OBS Studio services
4. The frontend will automatically detect and use web microphone

## Overview
This document outlines the frontend changes made to the TestSonioxLocal application, focusing on the dual-container layout and real-time translation capabilities.

## Major Changes Made

### 1. Web-Based Microphone Implementation
- **Replaced OBS Studio integration** with direct browser microphone access
- **Implemented Web Audio API** for real-time audio capture and processing
- **Added PCM audio conversion** for Soniox API compatibility
- **Direct WebSocket connection** to Soniox API from the browser

### 2. Hamburger Menu System
- **Created animated hamburger menu** for all controls

### 3. Dual Container Layout
- **Split interface into two containers:**
  - **Transcription Container** (top) - Shows original speech
  - **Translation Container** (bottom) - Shows translations
- **Independent visibility controls** for each container
- **Smart layout management** - Single containers take full space

### 4. Language Selection System
- **Comprehensive language support** - 60+ languages including:
  - Slovenian, English, German, French, Spanish, Italian
  - Portuguese, Russian, Chinese, Japanese, Korean, Arabic
  - Hindi, Dutch, Swedish, Norwegian, Danish, Finnish
  - Polish, Czech, Slovak, Hungarian, Romanian, Bulgarian
  - Croatian, Serbian, Bosnian, Macedonian, Albanian, Greek
  - Turkish, Ukrainian, Belarusian, Estonian, Latvian, Lithuanian
  - Catalan, Basque, Galician, Welsh, Irish, Maltese
  - Icelandic, Faroese
- **Separate source and target language dropdowns**
- **Real-time language switching** without restarting

### 5. Container Visibility Controls
- **Two independent toggle switches:**
  - "Show Transcriptions" - Controls transcription container
  - "Show Translations" - Controls translation container
- **Four possible combinations:**
  - Both containers visible (default)
  - Only transcription container
  - Only translation container
  - Both containers hidden
- **Smart positioning** - Single containers take full height and width

### 6. Microphone Controls Integration
- **Moved microphone controls** into the hamburger menu
- **Three-button system:**
  - "Initialize Microphone" - Sets up audio capture
  - "Start Recording" - Begins transcription/translation
  - "Stop Recording" - Ends the session
- **Dynamic button visibility** based on microphone state

### 7. Speaker Diarization Support
- **Speaker identification** with "S1:", "S2:" labels
- **Smart speaker grouping** - Same speaker text flows together
- **Speaker label positioning** - Only shown for first sentence of each speaker
- **Clean text display** - No repetition or partial token issues

### 8. UI/UX Improvements
- **Reduced white space** around containers
- **Extended container width** to use more screen space
- **Fixed overlapping issues** between controls and content
- **Proper label positioning** for all container states
- **Consistent styling** across all elements

## Technical Implementation

### CSS Changes
- **Moved all inline styling** to CSS classes for easy customization
- **Responsive positioning** with absolute and fixed layouts
- **Smooth animations** for all interactive elements
- **Proper z-index management** to prevent overlapping

### JavaScript Features
- **Web Audio API integration** for microphone access
- **Real-time audio processing** with ScriptProcessorNode
- **PCM audio conversion** for Soniox API compatibility
- **SignalR integration** for real-time caption delivery
- **Dynamic UI updates** based on user interactions

### HTML Structure
- **Semantic container organization** with clear labels
- **Accessible form controls** for language selection
- **Proper button hierarchy** for microphone controls
- **Clean separation** between content and controls

## File Structure
```
TestSonioxLocal/
├── Pages/
│   └── Index.cshtml          # Main frontend page with all UI changes
├── Controllers/
│   └── CaptionsController.cs # API endpoint for Soniox API key
├── Hubs/
│   └── CaptionHub.cs         # SignalR hub for real-time communication
└── wwwroot/
    └── js/
        └── signalr/          # SignalR client library
```

## Usage Instructions

### 1. Starting the Application
- Run the application with HTTPS support
- Navigate to `https://localhost:50001`
- Allow microphone permissions when prompted

### 2. Using the Interface
1. **Click the hamburger menu** (☰) in the top-right corner
2. **Select languages** from the "From:" and "To:" dropdowns
3. **Choose container visibility** using the toggle switches
4. **Initialize microphone** by clicking the green button
5. **Start recording** to begin transcription and translation
6. **Stop recording** when finished

### 3. Container Management
- **Both containers** - Shows transcriptions and translations side by side
- **Transcription only** - Shows only original speech in full height
- **Translation only** - Shows only translations in full height
- **Both hidden** - Hides all content containers

## Browser Compatibility
- **Chrome/Chromium** - Full support
- **Firefox** - Full support
- **Safari** - Full support
- **Edge** - Full support

## Requirements
- **HTTPS connection** - Required for microphone access
- **Modern browser** - Web Audio API support
- **Microphone permissions** - User must allow microphone access
- **Internet connection** - For Soniox API communication

## Troubleshooting

### Common Issues
1. **Microphone not working** - Check HTTPS connection and permissions
2. **No transcriptions** - Verify microphone permissions and API key
3. **UI overlapping** - Ensure proper browser zoom level (100%)
4. **Language not changing** - Check API key and network connection

### Debug Information
- **Console logging** - Check browser console for detailed error messages
- **Network tab** - Monitor WebSocket connections to Soniox API
- **Audio permissions** - Verify microphone access in browser settings

## Future Enhancements
- **Custom styling options** - Theme selection and color customization
- **Export functionality** - Save transcriptions and translations
- **Keyboard shortcuts** - Quick access to common functions
- **Multi-language support** - Interface localization
- **Advanced audio settings** - Noise reduction and audio quality controls

## Support
For technical support or questions about the frontend implementation, refer to the browser console for detailed error messages and check the network connectivity to the Soniox API.
