using Microsoft.AspNetCore.Mvc;

namespace OpenReservation.ScheduleServices.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public sealed class HealthController: ControllerBase
{
    [HttpGet]
    public IActionResult Live() => Ok();

    [HttpGet]
    public IActionResult Ready() => Ok();
}