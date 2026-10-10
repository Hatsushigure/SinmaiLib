# NetHttpClient 测试

运行：

```powershell
dotnet test tests/NetHttpClient.Tests/NetHttpClient.Tests.csproj
```

测试项目直接编译生产代码中的 `NetHttpClient.cs`、`CipherAes.cs` 和
`Singleton.cs`，无需构建依赖 Unity 的完整游戏项目，也无需安装原生
`amdaemon_api`。`Support/ExternalDependencies.cs` 仅替换 `Auth` 和
`OperationManager` 提供的运行环境配置与 Cookie 存储。

每个网络测试使用监听 `127.0.0.1` 随机端口的 Kestrel HTTP 服务，不访问实际
游戏服务器。测试覆盖请求/响应的 AES 与 zlib 处理、HTTP 方法和请求头、
Cookie、HTTP 错误与解码错误、超时和取消、重复请求状态重置、同步入口的
异步完成，以及资源释放。TLS 证书校验和原生认证交互不在这组测试的范围内。
