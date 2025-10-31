
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VentasWeb.Services;

public class HttpClientService
{
    /*
        // requiere paquete nuget Microsoft.Extensions.Http

        // agregar servicio HttpClient en Program.cs. Uno o varios, luego se los refiere con nombre


            // Add a user-agent default request header.
            builder.Services.AddHttpClient("api1", client => {
                client.BaseAddress = new Uri("https://api1.com/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-docs");
            });

            //  Obtener la URL base de la API desde la configuración. Agregar la clave de la API a los encabezados de solicitud
            builder.Services.AddHttpClient("api2", client => {
                client.BaseAddress = new Uri(config["ApiSettings:BaseUrl"]);                        
                client.DefaultRequestHeaders.Add("ApiKey", config["ApiSettings:ApiKey"]);
            });

            // Obtener la URL base de la API desde la configuración, con bearer y timeout
            builder.Services.AddHttpClient("api2", client => {
                client.BaseAddress = new Uri(config["ApiSettings:BaseUrl"]);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "TU_TOKEN_AQUI");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register pero mas seguro, usando un certificado
            builder.Services.AddHttpClient("ConCertificado")
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                var cert = new X509Certificate2("ruta/al/certificado.pfx", "clave");
                handler.ClientCertificates.Add(cert);
                return handler;
            });


            // Registrar el servicio HttpClientService
            builder.Services.AddSingleton<HttpClientService>();
            
            // tambien
            builder.Services.AddSingleton<HttpClientService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetService<ILogger<HttpClientService>>();
                var cache = sp.GetService<IMemoryCache>();
                return new HttpClientService(factory, logger, cache);
            });
        

            // Usage
            private readonly HttpClient _httpClient;
            var resultado = await _httpClient.GetAsync<MiModelo>("endpoint/valor", "api1");


    // uso multipart
    var content = new MultipartFormDataContent();
content.Add(new StringContent("123"), "Id");
content.Add(new StreamContent(fileStream), "File", "documento.pdf");

var resultado = await httpService.PostMultipartAsync<MyResponse>("upload", content, "api3");

    // uso cache
    var datos = await httpService.GetCachedAsync<List<Producto>>("productos", "default", TimeSpan.FromMinutes(10));
     */


    private readonly IHttpClientFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<HttpClientService>? _logger;
    private readonly IMemoryCache? _cache;

    public HttpClientService(IHttpClientFactory factory, ILogger<HttpClientService>? logger, IMemoryCache? cache)
    {
        _factory = factory;
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }


    public static string BuildQueryString(Dictionary<string, object?> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return string.Empty;

        var items = parameters
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value is not null)
            .SelectMany(kv =>
            {
                if (kv.Value is IEnumerable<object> list && kv.Value is not string)
                {
                    return list.Select(v => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(v?.ToString() ?? "")}");
                }

                return new[] { $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value.ToString()!)}" };
            });

        return "?" + string.Join("&", items);
    }


    public async Task<T?> GetAsync<T>(string url, string clientName, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        var client = _factory.CreateClient(clientName);
        _logger?.LogDebug("GET → {ClientName}: {Url}", clientName, url);

        if (timeout.HasValue) client.Timeout = timeout.Value;
        ApplyHeaders(client, headers);

        var response = await client.GetAsync(url);

        return await HandleResponse<T>(response);
    }

    public async Task<T?> GetCachedAsync<T>(string url, string clientName, TimeSpan cacheDuration, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        if (_cache != null && _cache.TryGetValue(url, out T? cachedResult))
        {
            _logger?.LogDebug("GETCache → {ClientName}: {Url}", clientName, url);
            return cachedResult;
        }

        var result = await GetAsync<T>(url, clientName, headers, timeout);

        _cache?.Set(url, result, cacheDuration);

        return result;
    }

    public async Task<T?> PutAsync<T>(string url, object body, string clientName, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        var client = _factory.CreateClient(clientName);
        if (timeout.HasValue) client.Timeout = timeout.Value;
        ApplyHeaders(client, headers);

        var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);

        _logger?.LogDebug("PUT → {ClientName}: {Url} Body: {Body}", clientName, url, jsonBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.PutAsync(url, content);
        return await HandleResponse<T>(response);
    }

    public async Task<T?> PostAsync<T>(string url, object body, string clientName, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        var client = _factory.CreateClient(clientName);

        if (timeout.HasValue) client.Timeout = timeout.Value;
        ApplyHeaders(client, headers);

        var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
        _logger?.LogDebug("POST → {ClientName}: {Url} Body: {Body}", clientName, url, jsonBody);

        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        return await HandleResponse<T>(response);
    }

    public async Task PostAsync(string url, object body, string clientName, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        var client = _factory.CreateClient(clientName);
        if (timeout.HasValue) client.Timeout = timeout.Value;
        ApplyHeaders(client, headers);

        var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
        _logger?.LogDebug("POSTNoResult → {ClientName}: {Url} Body: {Body}", clientName, url, jsonBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        _logger?.LogDebug("POST (sin retorno) ok: {Body}", response);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger?.LogError("POST (sin retorno) falló: {Status} {Body}", response.StatusCode, err);
            throw new HttpRequestException($"POST falló con status {response.StatusCode}");
        }
    }

    public async Task<T?> PostMultipartAsync<T>(string url, MultipartFormDataContent formData, string clientName, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        var client = _factory.CreateClient(clientName);
        if (timeout.HasValue) client.Timeout = timeout.Value;
        ApplyHeaders(client, headers);

        var response = await client.PostAsync(url, formData);
        return await HandleResponse<T>(response);
    }

    public async Task<JsonDocument?> PostAsJsonAsync(string url, object body, string clientName,  Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        // obtiene valor bruto como JsonDocument
        //var valor = json?.RootElement.GetProperty("result").GetString();
        var client = _factory.CreateClient(clientName);
        if (timeout.HasValue) client.Timeout = timeout.Value;
        ApplyHeaders(client, headers);
        var jsonBody = JsonSerializer.Serialize(body, _jsonOptions);
        _logger?.LogDebug("POST → {ClientName}: {Url} Body: {Body}", clientName, url, jsonBody);

        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError("HTTP Error ({StatusCode}): {Content}", response.StatusCode, responseText);
            throw new HttpRequestException($"Error HTTP: {response.StatusCode}");
        }

        return JsonDocument.Parse(responseText);
    }

    public async Task DeleteAsync(string url, string clientName, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        var client = _factory.CreateClient(clientName);
        if (timeout.HasValue) client.Timeout = timeout.Value;
        ApplyHeaders(client, headers);

        _logger?.LogDebug("DELETE → {ClientName}: {Url}", clientName, url);
        var response = await client.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger?.LogError("DELETE falló: {Status} {Body}", response.StatusCode, err);
            throw new HttpRequestException($"DELETE falló con status {response.StatusCode}");
        }
    }


    public async Task<List<T>> GetOffsetPagedAsync<T>(string baseUrl, string clientName,  int pageSize = 50,  string fromParam = "from",  string toParam = "to",
        int startItem = 0, Dictionary<string, string>? extraParams = null, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {

        /*
         * Ejemplo de uso:
         * Solicita from=0&to=200, luego from=201&to=400, y así sucesivamente hasta que la API ya no devuelva resultados.
         
         var logs = await _httpClientService.GetOffsetPagedAsync<LogItem>(
    baseUrl: "logs",
    clientName: "api1",
    pageSize: 200,
    extraParams: new Dictionary<string, string>
    {
        { "nivel", "error" }
    }
);
         */

        var results = new List<T>();
        var from = startItem;
        var to = startItem + pageSize;
        bool hasMore = true;

        while (hasMore)
        {
            var query = new Dictionary<string, object?>
        {
            { fromParam, from },
            { toParam, to }
        };

            if (extraParams != null)
            {
                foreach (var kv in extraParams)
                    query[kv.Key] = kv.Value;
            }

            var url = baseUrl + BuildQueryString(query);
            var pageData = await GetAsync<List<T>>(url, clientName, headers, timeout);

            if (pageData != null && pageData.Count > 0)
            {
                results.AddRange(pageData);
                from += pageSize + 1;
                to += pageSize;
            }
            else
            {
                hasMore = false;
            }
        }

        return results;
    }

    public async Task<List<T>> GetPagedAsync<T>(string baseUrl, string clientName,  int pageSize = 50,  string pageParam = "page",  string sizeParam = "pageSize",
            int startPage = 1,  Dictionary<string, string>? extraParams = null,  Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
    {
        /*
         * Ejemplo de uso:
         * Va pidiendo automáticamente page=1, page=2, etc., hasta que la API devuelva una lista vacía.
         * 
        var usuarios = await _httpClientService.GetPagedAsync<Usuario>(
            baseUrl: "users",
            clientName: "api1",
            pageSize: 100,
            extraParams: new Dictionary<string, string>
            {
                { "activo", "true" }   // ejemplo: filtros adicionales
            }
        );
        */


        var results = new List<T>();
        var page = startPage;
        bool hasMore = true;

        while (hasMore)
        {
            var query = new Dictionary<string, object?>
        {
            { pageParam, page },
            { sizeParam, pageSize }
        };

            if (extraParams != null)
            {
                foreach (var kv in extraParams)
                    query[kv.Key] = kv.Value;
            }

            var url = baseUrl + BuildQueryString(query);
            var pageData = await GetAsync<List<T>>(url, clientName, headers, timeout);

            if (pageData != null && pageData.Count > 0)
            {
                results.AddRange(pageData);
                page++;
            }
            else
            {
                hasMore = false;
            }
        }

        return results;
    }








    private async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError("HTTP Error ({StatusCode}): {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Error HTTP: {response.StatusCode}");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error al deserializar JSON: {Content}", content);
            throw;
        }
    }

    private void ApplyHeaders(HttpClient client, Dictionary<string, string>? headers)
    {
        if (headers == null) return;

        foreach (var header in headers)
        {
            // Authorization se trata especialmente para evitar errores
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", header.Value);
            }
            else
            {
                client.DefaultRequestHeaders.Remove(header.Key);
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}
