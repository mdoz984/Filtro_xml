using System.Text;
using ProcessadorNfce.Api;
using ProcessadorNfce.Dados;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string caminhoBanco = Path.Combine(builder.Environment.ContentRootPath, "processador_nfce.db");
builder.Services.AddSingleton(new RepositorioProcessamentos(caminhoBanco));

WebApplication app = builder.Build();

app.Services.GetRequiredService<RepositorioProcessamentos>().CriarTabelas();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapProcessamentos();

app.Run();
