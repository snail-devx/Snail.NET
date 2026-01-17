using Microsoft.AspNetCore.Mvc;
using Snail.WebApp.Attributes;

namespace Snail.WebApiTest.Components
{
    /// <summary>
    /// 
    /// </summary>
    [Log, Error, Auth, Response, Content, Performance, Action(Tag = "API_X")]
    public abstract class ApiController : ControllerBase
    {

    }
}
