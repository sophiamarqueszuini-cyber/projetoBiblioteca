// Cria o "builder" da aplicação, responsável por configurar serviços (DI),
// configurações (appsettings.json) e o pipeline antes de a aplicação ser montada.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Registra os serviços de MVC: Controllers (lógica) + Views (Razor/.cshtml).
builder.Services.AddControllersWithViews();

// Sessao: e nela que guardamos quem esta logado
// Registra um cache em memória do próprio processo, usado internamente
// para armazenar os dados de sessão (não persiste se a aplicação reiniciar).
builder.Services.AddDistributedMemoryCache();
// Habilita o uso de Session (dados por usuário guardados no servidor,
// identificados por um cookie) e configura suas opções.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);   // desloga apos 30 min sem uso
    // Impede que o cookie de sessão seja acessado via JavaScript no navegador
    // (proteção contra ataques XSS que tentam roubar o cookie).
    options.Cookie.HttpOnly = true;
    // Marca o cookie como essencial, permitindo seu uso mesmo sem consentimento
    // de cookies não essenciais (útil quando há banner de LGPD/GDPR).
    options.Cookie.IsEssential = true;
});
// Registra o IHttpContextAccessor, que permite acessar o HttpContext
// (e portanto a Session) de fora de um Controller, se necessário.
builder.Services.AddHttpContextAccessor();

// Constrói de fato o objeto "app" (WebApplication) com tudo que foi configurado acima.
var app = builder.Build();

// Configure the HTTP request pipeline.
// Se NÃO estiver em ambiente de desenvolvimento (ou seja, em produção)...
if (!app.Environment.IsDevelopment())
{
    // ...usa uma página de erro genérica (não mostra stack trace ao usuário final).
    app.UseExceptionHandler("/Home/Error");
    // Adiciona o cabeçalho HSTS, forçando o navegador a sempre usar HTTPS.
    app.UseHsts();
}

// Redireciona automaticamente requisições HTTP para HTTPS.
app.UseHttpsRedirection();
// Habilita o sistema de roteamento (necessário para o MapControllerRoute abaixo funcionar).
app.UseRouting();

app.UseSession();          // precisa vir ANTES dos controllers

// Habilita o middleware de autorização (verifica permissões antes de executar as actions).
app.UseAuthorization();

// Mapeia os arquivos estáticos (wwwroot: css, js, imagens etc.) com cache/otimizações do .NET.
app.MapStaticAssets();

// Define a rota padrão da aplicação:
// /Controller/Action/id  (ex.: /Livros/Edit/5)
// Se não informado, usa Home/Index e o "id" é opcional.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Inicia a aplicação e fica escutando requisições HTTP.
app.Run();
