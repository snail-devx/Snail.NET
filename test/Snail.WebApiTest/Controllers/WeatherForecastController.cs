using Microsoft.AspNetCore.Mvc;
using Snail.Abstractions.ErrorCode.DataModels;
using Snail.WebApiTest.Components;
using Snail.WebApp.Attributes;

namespace Snail.WebApiTest.Controllers
{
    /// <summary>
    /// 测试基础类型，
    /// </summary>
    [Performance(Disabled = true)]
    public class WeatherForecastBaseController : ApiController
    {

    }

    [Route("[controller]")]
    [Log]
    public class WeatherForecastController : WeatherForecastBaseController
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Get")]
        [CustomContent]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetErrorCode")]
        [Error]
        public ErrorCodeDescriptor GetErrorCode()
        {
            return new ErrorCodeDescriptor("1", "dddddd");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetException")]
        [Error, Response(JsonResolver = WebApp.Enumerations.JsonResolverType.Default)]
        public Task GetException()
        {
            throw new NotSupportedException("大大大大大大大大大");
        }

        /// <summary>
        /// 测试 Post方法
        /// </summary>
        /// <returns></returns>
        [HttpPost("PostTest")]
        public Task PostTest()
        {
            return Task.CompletedTask;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CustomContentAttribute : ContentAttribute
    {
    }
}
