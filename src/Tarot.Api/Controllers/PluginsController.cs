using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PluginsController(IPluginManager pluginManager) : ControllerBase
{
    private readonly IPluginManager _pluginManager = pluginManager;

    [HttpGet]
    public IActionResult GetPlugins() =>
        Ok(_pluginManager.GetPlugins().Select(p => new
        {
            p.Name,
            p.Version,
            p.Description
        }));
}
