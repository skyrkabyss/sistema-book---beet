using System.Net;
using System.Text;
using System.Text.Json;
using GestorTareas.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GestorTareas.Services;
public sealed class LibrosOnlineService
{
    private readonly IHttpClientFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LibrosOnlineService> _logger;
    public LibrosOnlineService(IHttpClientFactory factory, IMemoryCache cache, ILogger<LibrosOnlineService> logger)
        => (_factory,_cache,_logger)=(factory,cache,logger);
    static string Str(JsonElement element, string key) => element.TryGetProperty(key,out var v) && v.ValueKind==JsonValueKind.String ? v.GetString()??"" : "";
    private static string? TextoUrl(JsonElement formats)
    {
        // Gutenberg suele incluir varias codificaciones. Preferir UTF-8 en texto plano.
        return formats.EnumerateObject()
            .Where(x => x.Name.StartsWith("text/plain",StringComparison.OrdinalIgnoreCase) && x.Value.ValueKind==JsonValueKind.String)
            .OrderByDescending(x => x.Name.Contains("utf-8",StringComparison.OrdinalIgnoreCase))
            .Select(x=>x.Value.GetString()).FirstOrDefault(IsGutenbergUrl);
    }
    private static bool IsGutenbergUrl(string? url)
        => Uri.TryCreate(url,UriKind.Absolute,out var u) && u.Scheme=="https" &&
           (u.Host=="www.gutenberg.org" || u.Host=="gutenberg.org") &&
           u.AbsolutePath.StartsWith("/",StringComparison.Ordinal);
    private static LibroEnLinea Parse(JsonElement element)
    {
        var id=element.GetProperty("id").GetInt32();
        var autor=element.TryGetProperty("authors",out var authors) && authors.GetArrayLength()>0 ? Str(authors[0],"name") : "Autor no especificado";
        var formats=element.GetProperty("formats");
        var portada=formats.TryGetProperty("image/jpeg",out var cover) ? cover.GetString()??"" : "";
        var libre=element.TryGetProperty("copyright",out var copyright) && copyright.ValueKind==JsonValueKind.False;
        return new LibroEnLinea { Id=id,Titulo=Str(element,"title"),Autor=autor,
            Idioma=element.TryGetProperty("languages",out var languages)&&languages.GetArrayLength()>0 ? languages[0].GetString()??"" : "",
            Portada=portada,Pagina=$"https://www.gutenberg.org/ebooks/{id}",TextoUrl=TextoUrl(formats),Libre=libre };
    }
    public async Task<List<LibroEnLinea>> BuscarAsync(string buscar, string idioma)
    {
        var key=$"gutenberg/{idioma}/{buscar.ToLowerInvariant()}";
        if (_cache.TryGetValue(key,out List<LibroEnLinea>? cached) && cached is not null) return cached;
        var result=new List<LibroEnLinea>();
        try
        {
            var url="https://gutendex.com/books?"+(idioma=="todos"?"":"languages="+Uri.EscapeDataString(idioma)+"&")+
                (string.IsNullOrWhiteSpace(buscar)?"":"search="+Uri.EscapeDataString(buscar)+"&")+"copyright=false";
            using var stream=await _factory.CreateClient("libros").GetStreamAsync(url);
            using var json=await JsonDocument.ParseAsync(stream);
            result=json.RootElement.GetProperty("results").EnumerateArray().Select(Parse)
                .Where(b=>b.Libre).Take(24).ToList();
            _cache.Set(key,result,TimeSpan.FromMinutes(12));
        }
        catch(Exception ex){_logger.LogWarning(ex,"No se pudo consultar Gutendex");}
        return result;
    }
    public async Task<LibroEnLinea?> ObtenerAsync(int id)
    {
        if(id<1 || id>99999999) return null;
        try
        {
            using var stream=await _factory.CreateClient("libros").GetStreamAsync($"https://gutendex.com/books/{id}");
            using var doc=await JsonDocument.ParseAsync(stream);
            var book=Parse(doc.RootElement);return book.Libre?book:null;
        }
        catch(Exception ex){_logger.LogWarning(ex,"Libro libre {Id} no disponible",id);return null;}
    }
    public async Task<string?> LeerTextoAsync(LibroEnLinea libro)
    {
        if(!libro.Libre || !IsGutenbergUrl(libro.TextoUrl)) return null;
        try
        {
            // No seguir redirecciones a otros dominios (protege el lector contra SSRF).
            using var handler=new HttpClientHandler { AllowAutoRedirect=false };
            using var client=new HttpClient(handler) { Timeout=TimeSpan.FromSeconds(25) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BiblioBeatAcademic/2.0 (https://example.org/bibliobeat)");
            using var response=await client.GetAsync(libro.TextoUrl,HttpCompletionOption.ResponseHeadersRead);
            if(!response.IsSuccessStatusCode ||
               response.Content.Headers.ContentLength is long size && size>7_000_000) return null;
            await using var input=await response.Content.ReadAsStreamAsync();
            using var output=new MemoryStream();
            var buffer=new byte[16384];int read;
            while((read=await input.ReadAsync(buffer))>0)
            {
                if(output.Length+read>7_000_000) return null;
                await output.WriteAsync(buffer.AsMemory(0,read));
            }
            var text=Encoding.UTF8.GetString(output.ToArray());
            if (text.Contains('\uFFFD'))
                text=Encoding.Latin1.GetString(output.ToArray());
            return text;
        }
        catch(Exception ex){_logger.LogWarning(ex,"No se pudo abrir el texto Gutenberg");return null;}
    }
}
