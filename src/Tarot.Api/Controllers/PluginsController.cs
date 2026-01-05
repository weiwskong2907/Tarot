using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PluginsController : ControllerBase
{
    private readonly IPluginManager _pluginManager;

    public PluginsController(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    [HttpGet]
    public IActionResult GetPlugins()
    {
        var plugins = _pluginManager.GetPlugins().Select(p => new
        {
            p.Name,
            p.Version,
            p.Description
        });
        return Ok(plugins);
    }
}
