
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

using System;
using System.Text.Json.Serialization;
using VentasWeb;
using VentasWeb.Models;
using VentasWeb.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddHttpClient("ProductecaLCA", c =>
{
    c.BaseAddress = new Uri("https://consultascda.casadelaudio.com/producteca/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("duco-rest");
});

builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
));

builder.Services.AddOutputCache();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddSingleton<HttpClientService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetService<ILogger<HttpClientService>>();
    return new HttpClientService(factory, logger, null);
});

var app = builder.Build();

app.UseCors(builder =>
{
    builder
        .AllowAnyOrigin() // Permite solicitudes desde cualquier origen
        .AllowAnyMethod() // Permite cualquier método HTTP (GET, POST, PUT, DELETE, etc.)
        .AllowAnyHeader(); // Permite cualquier encabezado en la solicitud
});


app.UseStaticFiles(); // Agregar middleware para servir archivos estáticos desde la carpeta wwwroot

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/health", () =>
{
    return "ok";
});

app.MapGet("/articulos-excluidos", async (AppDBContext db) =>
{
    var artExcluidos = await db.ArticulosExcluidos.Select(a => a.SKU).ToListAsync();
    return Results.Ok(artExcluidos);

})
    .CacheOutput(c => c
    .Expire(TimeSpan.FromDays(1))
    .Tag("articulosexcluidos-get")
)
    .WithTags("ObtenerDatos");


app.MapGet("/sucursales-excluidas", async (AppDBContext db) =>
{
    var sucExcluidas = await db.SucursalesExcluidas.Select(a => a.Numero).ToListAsync();
    return Results.Ok(sucExcluidas);

})
    .CacheOutput(c => c
    .Expire(TimeSpan.FromDays(1))
    .Tag("sucursalesexcluidas-get")
)
    .WithTags("ObtenerDatos");

app.MapGet("/familias-excluidas", async (AppDBContext db) =>
{
    var famExcluidas = await db.FamiliasExcluidas.Select(a => a.Codigo).ToListAsync();
    return Results.Ok(famExcluidas);

})
    .CacheOutput(c => c
    .Expire(TimeSpan.FromDays(1))
    .Tag("familiasexcluidas-get")
)
    .WithTags("ObtenerDatos");


app.MapGet("/precios-forzados", async (AppDBContext db) =>
{
    var preForzados = await db.PreciosForzados.ToListAsync();
    return Results.Ok(preForzados);

})
    .CacheOutput(c => c
    .Expire(TimeSpan.FromDays(1))
    .Tag("preciosforzados-get")
)
    .WithTags("ObtenerDatos");


app.MapPost("/subir/{tipo}", async (string tipo, HttpContext context, AppDBContext db, IOutputCacheStore outputCache) =>
{
    try
    {
        var files = context.Request.Form.Files;
        if (files.Count == 0)
            return Results.BadRequest("No se recibió ningún archivo");

        // Validar tipo
        var tiposValidos = new[] { "PF", "AE", "FE", "SE" };
        if (!tiposValidos.Contains(tipo.ToUpper()))
            return Results.BadRequest($"Tipo '{tipo}' no es válido");

        foreach (var file in files)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            string? linea;
            bool primera = true;
            string[] lineas = Array.Empty<string>();
            while ((linea = await reader.ReadLineAsync()) != null)
            {
                if (primera) // saltar cabecera
                {
                    primera = false;
                    continue;
                }
                lineas = [.. lineas, linea];
            }

            if (lineas.Length == 0) return Results.BadRequest("El archivo no contiene datos");

            switch (tipo.ToUpper())
            {
                case "PF":
                    var pf = new PrecioForzado();
                    await pf.Guardar(db, lineas);
                    await outputCache.EvictByTagAsync("preciosforzados-get", default); //matar el memory cache
                    break;
                case "AE":
                    var ae = new ArticuloExcluido();
                    await ae.Guardar(db, lineas);
                    await outputCache.EvictByTagAsync("articulosexcluidos-get", default); //matar el memory cache
                    break;
                case "FE":
                    var fe = new FamiliaExcluida();
                    await fe.Guardar(db, lineas);
                    await outputCache.EvictByTagAsync("familiasexcluidas-get", default); //matar el memory cache
                    break;
                case "SE":
                    var se = new SucursalExcluida();
                    await se.Guardar(db, lineas);
                    await outputCache.EvictByTagAsync("sucursalesexcluidas-get", default); //matar el memory cache
                    break;
            }
        }

        return Results.Ok($"Archivos procesados correctamente para tipo {tipo}");
    }
    catch (Exception ex)
    {
        var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
        return Results.BadRequest(msg);
    }
});


app.MapPost("/establecerProducteca/", async (HttpContext context, IOutputCacheStore outputCache, HttpClientService http) =>
{
    try
    {
        var files = context.Request.Form.Files;
        if (files.Count != 1)
            return Results.BadRequest("Se debe recibir UN archivo");

        using var reader = new StreamReader(files[0].OpenReadStream());
        string? linea;
        bool primera = true;
        string[] lineas = [];
        while ((linea = await reader.ReadLineAsync()) != null)
        {
            if (primera) // saltar cabecera
            {
                primera = false;
                continue;
            }
            lineas = [.. lineas, linea];
        }

        if (lineas.Length == 0) return Results.BadRequest("El archivo no contiene datos");

        var estPtec = new EstadoProducteca();
        var res = await estPtec.Procesar(http, lineas);

        return res.Count == 0 ? Results.Ok() : Results.BadRequest(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();
