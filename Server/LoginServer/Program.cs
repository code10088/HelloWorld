// 测试用登录服
// 实现客户端 AccountInfo.cs 使用的两个接口：
//   POST /auth/login    body: {"platform":0,"authCode":"1","deviceId":"...","clientVersion":"1.2.0"}
//   POST /auth/refresh  body: {"refreshToken":"..."}
// 返回的 host/port 就是客户端随后 TCP 连接的游戏服地址。
//
// 注意：客户端 HttpManager 用 HttpWebResponse.ContentLength 读响应，
// 所以这里必须显式写 Content-Length（下面的 WriteJson 已处理）。
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 只监听本机，由 nginx 负责 443/TLS
var listenUrl = Environment.GetEnvironmentVariable("LOGIN_LISTEN") ?? "http://127.0.0.1:5080";
builder.WebHost.UseUrls(listenUrl);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

var app = builder.Build();

var gameHost = Environment.GetEnvironmentVariable("GAME_HOST") ?? "112.126.84.17";
var gamePort = Environment.GetEnvironmentVariable("GAME_PORT") ?? "8088";
var serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "test-server-1";
var userId = Environment.GetEnvironmentVariable("USER_ID") ?? "test-user-1";

app.MapGet("/health", () => Results.Text("ok", "text/plain", Encoding.UTF8));

app.MapPost("/auth/login", async (HttpContext ctx) =>
{
    var body = await ReadBodyAsync(ctx.Request);
    app.Logger.LogInformation("login body: {Body}", body);

    // 测试服不校验 authCode，固定返回成功
    await WriteJsonAsync(ctx.Response, new
    {
        code = 1,
        accessToken = "test-access-token",
        refreshToken = "test-refresh-token",
        userId,
        serverId,
        host = gameHost,
        port = gamePort,        // LoginResponse.port 是 string，必须是字符串
    });
});

app.MapPost("/auth/refresh", async (HttpContext ctx) =>
{
    var body = await ReadBodyAsync(ctx.Request);
    app.Logger.LogInformation("refresh body: {Body}", body);

    await WriteJsonAsync(ctx.Response, new
    {
        code = 1,
        accessToken = "test-access-token",
        refreshToken = "test-refresh-token",
        userId,
        serverId,
        host = gameHost,
        port = gamePort,
    });
});

app.Logger.LogInformation("登录服已启动: {Listen}, 下发游戏服地址 {Host}:{Port}", listenUrl, gameHost, gamePort);
app.Run();

static async Task<string> ReadBodyAsync(HttpRequest request)
{
    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();
    return body.Length > 4096 ? body[..4096] : body;
}

static async Task WriteJsonAsync(HttpResponse response, object value)
{
    var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
    response.StatusCode = StatusCodes.Status200OK;
    response.ContentType = "application/json; charset=utf-8";
    response.ContentLength = bytes.Length;   // 客户端依赖这个头
    await response.Body.WriteAsync(bytes);
}