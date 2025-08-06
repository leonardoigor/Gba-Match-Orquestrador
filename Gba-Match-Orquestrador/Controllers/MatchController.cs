using Gba_Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gba_Match_Orquestrador.Controllers;

[ApiController]
[Route("matches")]
public class MatchController : ControllerBase
{
    private readonly IGbaMatchKubernetesService _service;

    public MatchController(IGbaMatchKubernetesService service)
    {
        _service = service;
    }


    [HttpPost("match")]
    public async Task<IActionResult> Create()
    {
        var Id=Guid.NewGuid();
        var match = await _service.CreateMatchAsync(Id);
        return Ok(match);
    }

    [HttpDelete("match/{podName}")]
    public async Task<IActionResult> Delete(string podName)
    {
        await _service.DeleteMatchAsync(podName);
        return NoContent();
    }

    [HttpDelete("all")]
    public async Task<IActionResult> DeleteAll()
    {
        await _service.DeleteAllMatchesAsync();
        return NoContent();
    }
}