//  测试自定义的webapi程序
using Snail.Abstractions.Dependency.Extensions;
using Snail.Abstractions.Logging.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.WebApp.Components;

var app1 = new Snail.WebApp.WebApplication(args);
app1.OnRegister += services =>
{
};
app1.OnController += (mvc, services) =>
{
    mvc.AddMvcOptions(options =>
    {
        options.Filters.AddService<ActionBaseFilter>();
    });
};
app1.OnBuild += (builder, _) =>
{
};

///** 针对控制器注册多次Options，会都执行
// app1.OnController += builder =>
// {
//     builder.AddMvcOptions(options =>
//     {
//         options.AllowEmptyInputInBodyModelBinding = true;
//         options.Filters.Clear();
//         //options.Filters.AddService(typeof(WebApplication));
//     });
//     builder.AddMvcOptions(options =>
//     {

//     });
// };
// app1.OnController += builder =>
// {
//     builder.AddMvcOptions(options =>
//     {

//     });
// };
// */

Snail.Abstractions.Logging.ILogger? logger = null;
app1.OnBuilded += (app, _) =>
{
    app.MapGet("/health", (HttpRequest request) =>
    {
        Results.Ok();
    });
};
app1.OnRun += di =>
{
    logger = di.Resolve<Snail.Abstractions.Logging.ILogger>();
    logger?.Trace("程序启动了。。", DateTime.Now.AsDateTimeString());
};
app1.OnStop += async () =>
{
    logger?.Trace("程序准备停止了", DateTime.Now.AsDateTimeString());
    await Task.Delay(TimeSpan.FromSeconds(2));
    logger?.Trace("程序停止了", DateTime.Now.AsDateTimeString());

    await Task.Delay(TimeSpan.FromMinutes(1));
    string x = 11.ToString();
};

app1.Run();

//var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers(options =>
//{
//}).AddControllersAsServices()
//.AddJsonOptions(options =>
//{
//    // 关闭属性名转换，保持原样（PascalCase）
//    options.JsonSerializerOptions.PropertyNamingPolicy = null;
//    // 如果有字典键也想保持原样，同样关闭
//    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
//});

////builder.Services.AddKeyedScoped

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//app.UseAuthorization();

//app.MapControllers();

//app.Run();



////public class XXX
////{

////}