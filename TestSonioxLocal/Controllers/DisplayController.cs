using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TestSonioxLocal.Hubs;
using TestSonioxLocal.Models;

namespace TestSonioxLocal.Controllers;

[Route("api/[controller]")]
public class DisplayController : ControllerBase
{
    private static UserSettings _userSettings = new UserSettings(); // In-memory storage
    private readonly IHubContext<CaptionHub, ICaptionClient> _hubContext;

    public DisplayController(IHubContext<CaptionHub, ICaptionClient> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpGet]
    public IActionResult GetDisplay()
    {
        // Generate HTML with embedded CSS based on user settings
        var html = GenerateStyledHTML(_userSettings);
        return Content(html, "text/html");
    }

    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] UserSettings settings)
    {
        _userSettings = settings;
        
        // Language settings are handled by SignalR hub (same as original system)
        return Ok(new { success = true, message = "Settings saved successfully" });
    }

    [HttpGet("settings")]
    public IActionResult GetSettings()
    {
        return Ok(_userSettings);
    }

    private string GenerateStyledHTML(UserSettings settings)
    {
        var transcriptionDisplay = settings.ShowTranscriptions ? "block" : "none";
        var translationDisplay = settings.ShowTranslations ? "block" : "none";
        var transcriptionLabelDisplay = settings.ShowTranscriptions ? "block" : "none";
        var translationLabelDisplay = settings.ShowTranslations ? "block" : "none";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>OBS Display</title>
    <style>
        :root {{
            --text-color: {settings.TextColor};
            --text-size-transcription: {settings.TextSize}px;
            --text-size-translation: {settings.TextSize + 4}px;
            --line-spacing: {settings.LineSpacing};
            --container-color: {settings.ContainerColor};
        }}

        html, body {{
            margin: 0;
            padding: 0;
            height: 100%;
            overflow: hidden;
            font-family: Arial, sans-serif;
        }}

        #transcriptionLabel {{
            position: absolute;
            top: 5px;
            left: 20px;
            font-size: 14px;
            font-weight: bold;
            padding: 5px 10px;
            background-color: rgba(0, 0, 0, 0.8);
            border-radius: 5px;
            z-index: 10;
            color: #ffffff;
            display: {transcriptionLabelDisplay};
        }}

        #translationLabel {{
            position: absolute;
            top: calc(40% + 40px);
            left: 20px;
            font-size: 14px;
            font-weight: bold;
            padding: 5px 10px;
            background-color: rgba(0, 0, 0, 0.8);
            border-radius: 5px;
            z-index: 10;
            color: #ffffff;
            display: {translationLabelDisplay};
            margin-top: 20px;
        }}

        #transcriptionContainer {{
            position: absolute;
            top: 30px;
            left: 50%;
            transform: translateX(-50%);
            height: 40%;
            width: 96%;
            padding: 10px;
            border: none;
            background-color: {settings.ContainerColor};
            color: {settings.TextColor};
            font-size: {settings.TextSize}px;
            line-height: {settings.LineSpacing};
            overflow-y: auto;
            margin-top: 5px;
            display: {transcriptionDisplay};
            scrollbar-width: none;
            -ms-overflow-style: none;
        }}

        #captionContainer {{
            position: absolute;
            top: calc(40% + 60px);
            bottom: 20px;
            left: 50%;
            transform: translateX(-50%);
            padding: 10px;
            border: none;
            background-color: {settings.ContainerColor};
            color: {settings.TextColor};
            font-size: {settings.TextSize + 4}px;
            line-height: {settings.LineSpacing};
            overflow-y: auto;
            width: 96%;
            margin-top: 30px;
            display: {translationDisplay};
            scrollbar-width: none;
            -ms-overflow-style: none;
        }}

        #transcriptionContainer::-webkit-scrollbar,
        #captionContainer::-webkit-scrollbar {{
            display: none;
        }}

        .caption-line {{
            margin: 5px 0;
        }}

        .speaker-label {{
            font-weight: bold;
            color: #FFC107;
            margin-right: 8px;
        }}

        .token {{
            color: {settings.TextColor};
        }}

        .transcription-line {{
            margin: 8px 0;
            padding: 5px;
            border-left: 3px solid #4CAF50;
            padding-left: 10px;
        }}
    </style>
</head>
<body>
    <div id='transcriptionLabel'>TRANSCRIPTIONS ({settings.SourceLanguage.ToUpper()})</div>
    <div id='translationLabel'>TRANSLATIONS ({settings.TargetLanguage.ToUpper()})</div>
    <div id='transcriptionContainer'></div>
    <div id='captionContainer'></div>
    
    <script src='/js/signalr/dist/browser/signalr.js'></script>
    <script>
        const transcriptionContainer = document.getElementById('transcriptionContainer');
        const translationContainer = document.getElementById('captionContainer');

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/caption-hub')
            .build();

        let sourceLanguage = '{settings.SourceLanguage}';
        let targetLanguage = '{settings.TargetLanguage}';

        connection.on('ReceiveCaption', function (caption, isFinal, speaker, isTranslation) {{
            console.log(`[${{isTranslation ? 'TRANSLATION' : 'TRANSCRIPTION'}}] [${{isFinal ? 'FINAL' : 'PARTIAL'}}] Speaker: '${{speaker}}' (type: ${{typeof speaker}}): ${{caption}}`);

            const targetContainer = isTranslation ? translationContainer : transcriptionContainer;
            const shortSpeaker = speaker ? 'S' + speaker.replace('Speaker ', '') : '';
            
            const lastElement = targetContainer.lastElementChild;
            const lastSpeaker = lastElement ? lastElement.getAttribute('data-speaker') : '';
            const needsNewLine = !lastElement || lastSpeaker !== shortSpeaker;
            
            if (needsNewLine) {{
                const lineDiv = document.createElement('div');
                lineDiv.className = 'caption-line';
                lineDiv.setAttribute('data-speaker', shortSpeaker);
                
                if (shortSpeaker) {{
                    const speakerSpan = document.createElement('span');
                    speakerSpan.className = 'speaker-label';
                    speakerSpan.textContent = shortSpeaker + ': ';
                    lineDiv.appendChild(speakerSpan);
                }}
            
                const textNode = document.createTextNode(caption);
                lineDiv.appendChild(textNode);
                
                targetContainer.appendChild(lineDiv);
            }} else {{
                if (isFinal) {{
                    const textNode = document.createTextNode(caption);
                    lastElement.appendChild(textNode);
                }}
            }}
            
            targetContainer.scrollTop = targetContainer.scrollHeight;
        }});

        connection.start().then(() => {{
            console.log('Connected to hub - ready for transcriptions and translations!');
            console.log('Language settings: {settings.SourceLanguage} → {settings.TargetLanguage}');
        }}).catch(err => console.error(err));
    </script>
</body>
</html>";
    }
}
