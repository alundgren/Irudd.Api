# Requirements

- This was built against dotnet6. It may or may not work against other versions.

# Usage
Create a new webapi project using

>> dotnet new webapi

Change Program.cs to:

```csharp
using Irudd.Api;

var app = ApiApplication.Create();

app.Run();
```