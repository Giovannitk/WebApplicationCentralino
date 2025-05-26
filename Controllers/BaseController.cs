using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplicationCentralino.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
    }
} 