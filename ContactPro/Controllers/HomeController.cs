using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ContactPro.Models;

namespace ContactPro.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    // Custom Error handling
    //Add custom Route. String matches the string in program.cs
    [Route("/Home/HandleError/{code:int}")]
    public IActionResult HandleError(int code)
    {
        var customError = new CustomError();

        customError.code = code;

        if (code == 404)
        {
            customError.message =
                "The page you are looking for might have been removed or had its name changed or is temporary unavailable";
        }
        else
        {
            customError.message = "Sorry, something went wrong";
        }

        return View("~/Views/Shared/CustomError.cshtml", customError);
        
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}