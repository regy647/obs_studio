using Microsoft.AspNetCore.Mvc;

namespace TestSonioxLocal.Controllers;

public class StylingController : Controller
{
    [HttpGet]
    public IActionResult Index(
        string? textColor = "#ffffff",
        string? textSize = "24",
        string? lineSpacing = "1.5",
        string? containerColor = "rgba(0, 0, 0, 0.7)",
        string? transcriptionVisible = "true",
        string? translationVisible = "true")
    {
        // Pass styling parameters to the view
        ViewBag.TextColor = textColor;
        ViewBag.TextSize = textSize;
        ViewBag.LineSpacing = lineSpacing;
        ViewBag.ContainerColor = containerColor;
        ViewBag.TranscriptionVisible = transcriptionVisible == "true";
        ViewBag.TranslationVisible = translationVisible == "true";
        
        return View("~/Pages/Index.cshtml");
    }
}
