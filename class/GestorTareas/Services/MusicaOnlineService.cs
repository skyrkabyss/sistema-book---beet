using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using GestorTareas.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GestorTareas.Services;
public sealed class MusicaOnlineService
{
    private readonly IHttpClientFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MusicaOnlineService> _log;
    private readonly string _youtubeKey;
    public bool YoutubeConfigurado => !string.IsNullOrWhiteSpace(_youtubeKey);
    public MusicaOnlineService(IHttpClientFactory factory, IMemoryCache cache, ILogger<MusicaOnlineService> log, IConfiguration config)
    {
        (_factory, _cache, _log) = (factory, cache, log);
        _youtubeKey = config["YouTube:ApiKey"] ?? "";
    }

    private static string Str(JsonElement e, string key)
        => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var v)
           && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    private static string Plain(string value)
        => WebUtility.HtmlDecode(Regex.Replace(value, "<[^>]+>", " ")).Trim();
    private static bool ValidUrl(string url, params string[] hosts)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
           && hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);

    public async Task<List<PistaEnLinea>> BuscarCommonsAsync(string buscar)
    {
        var key = "commons/" + buscar.ToLowerInvariant();
        if (_cache.TryGetValue(key, out List<PistaEnLinea>? cached) && cached is not null) return cached;
        var result = new List<PistaEnLinea>();
        try
        {
            var client = _factory.CreateClient("catalogos");
            // Wikimedia exige identificar a la aplicación; cambiar el contacto al publicar.
            using var req = new HttpRequestMessage(HttpMethod.Get,
                "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrnamespace=6&gsrlimit=25&gsrsearch=" +
                Uri.EscapeDataString("filetype:audio " + buscar) + "&prop=imageinfo&iiprop=url%7Cmime%7Cextmetadata&format=json&formatversion=2");
            req.Headers.UserAgent.ParseAdd("BiblioBeatAcademic/2.0 (https://example.org/bibliobeat; contacto: administrador del sitio)");
            using var response = await client.SendAsync(req);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (!json.RootElement.TryGetProperty("query", out var query) || !query.TryGetProperty("pages", out var pages)) return result;
            foreach (var page in pages.EnumerateArray())
            {
                var title = Str(page, "title");
                if (!title.StartsWith("File:", StringComparison.OrdinalIgnoreCase)) continue;
                if (!page.TryGetProperty("imageinfo", out var infos) || infos.GetArrayLength() == 0) continue;
                var info = infos[0];
                var url = Str(info, "url");
                var mime = Str(info, "mime");
                if (!mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
                    !ValidUrl(url, "upload.wikimedia.org") ||
                    !new[] { ".ogg", ".oga", ".mp3", ".wav", ".flac", ".webm", ".opus" }
                       .Any(ext => new Uri(url).AbsolutePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))) continue;
                if (!info.TryGetProperty("extmetadata", out var meta)) continue;
                string field(string name) => meta.TryGetProperty(name, out var e) ? Plain(Str(e, "value")) : "";
                var license = field("LicenseShortName");
                // Evitar contenido con cláusulas no comerciales o licencias no identificadas.
                if (!(license.Contains("CC BY", StringComparison.OrdinalIgnoreCase) ||
                      license.Contains("CC0", StringComparison.OrdinalIgnoreCase) ||
                      license.Contains("Public domain", StringComparison.OrdinalIgnoreCase) ||
                      license.Contains("Dominio público", StringComparison.OrdinalIgnoreCase) ||
                      license.Equals("PD", StringComparison.OrdinalIgnoreCase)) ||
                    license.Contains("NC", StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(new PistaEnLinea {
                    Fuente = "Commons", IdExterno = title,
                    Titulo = title[5..].Replace('_', ' ').Replace(".ogg", "", StringComparison.OrdinalIgnoreCase)
                        .Replace(".mp3", "", StringComparison.OrdinalIgnoreCase),
                    Artista = field("Artist") is var artist && artist.Length > 0 ? artist[..Math.Min(100, artist.Length)] : "Colaboradores de Wikimedia Commons",
                    UrlEscucha = url,
                    UrlOrigen = "https://commons.wikimedia.org/wiki/" + Uri.EscapeDataString(title.Replace(' ', '_')),
                    Licencia = license
                });
                if (result.Count >= 14) break;
            }
            _cache.Set(key, result, TimeSpan.FromMinutes(6));
        }
        catch (Exception ex) { _log.LogWarning(ex, "Wikimedia Commons no disponible"); }
        return result;
    }

    public async Task<List<PistaEnLinea>> BuscarAppleAsync(string buscar)
    {
        var key = "apple/" + buscar.ToLowerInvariant();
        if (_cache.TryGetValue(key, out List<PistaEnLinea>? cached) && cached is not null) return cached;
        var result = new List<PistaEnLinea>();
        try
        {
            var client = _factory.CreateClient("catalogos");
            var uri = "https://itunes.apple.com/search?media=music&entity=song&limit=16&country=us&term=" + Uri.EscapeDataString(buscar);
            using var stream = await client.GetStreamAsync(uri);
            using var json = await JsonDocument.ParseAsync(stream);
            foreach (var track in json.RootElement.GetProperty("results").EnumerateArray())
            {
                if (!track.TryGetProperty("trackId", out var id)) continue;
                var origin = Str(track, "trackViewUrl");
                if (!ValidUrl(origin, "music.apple.com", "itunes.apple.com")) continue;
                result.Add(new PistaEnLinea {
                    Fuente="Apple", IdExterno = id.ToString(), Titulo = Str(track,"trackName"),
                    Artista=Str(track,"artistName"), UrlOrigen=origin,
                    Licencia="Música comercial: reproducción en servicio oficial (no alojada por BiblioBeat)"
                });
            }
            _cache.Set(key, result, TimeSpan.FromMinutes(8));
        }
        catch (Exception ex) { _log.LogWarning(ex, "Catálogo de Apple no disponible"); }
        return result;
    }

    // YouTube Data API v3 oficial. No se descargan ni extraen pistas de YouTube;
    // el audio/video se escucha únicamente en el reproductor embebido de YouTube.
    public async Task<List<PistaEnLinea>> BuscarYouTubeAsync(string buscar)
    {
        var tracks = new List<PistaEnLinea>();
        if(!YoutubeConfigurado || string.IsNullOrWhiteSpace(buscar))return tracks;
        var key="yt/"+buscar.ToLowerInvariant();
        if(_cache.TryGetValue(key,out List<PistaEnLinea>? cached)&&cached is not null)return cached;
        try
        {
            var client=_factory.CreateClient("catalogos");
            var url="https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&videoEmbeddable=true&videoSyndicated=true&safeSearch=strict&maxResults=12&q="+
                Uri.EscapeDataString(buscar)+"&key="+Uri.EscapeDataString(_youtubeKey);
            using var stream=await client.GetStreamAsync(url);
            using var json=await JsonDocument.ParseAsync(stream);
            if(!json.RootElement.TryGetProperty("items",out var items))return tracks;
            foreach(var item in items.EnumerateArray())
            {
                if(!item.TryGetProperty("id",out var id)||!id.TryGetProperty("videoId",out var v))continue;
                var videoId=v.GetString()??"";
                if(!Regex.IsMatch(videoId,@"^[a-zA-Z0-9_-]{11}$"))continue;
                var snippet=item.GetProperty("snippet");
                tracks.Add(new PistaEnLinea{
                    Fuente="YouTube",IdExterno=videoId, Titulo=Plain(Str(snippet,"title")),
                    Artista=Plain(Str(snippet,"channelTitle")),
                    UrlOrigen="https://www.youtube.com/watch?v="+videoId,
                    Licencia="Reproducción mediante el reproductor oficial de YouTube"
                });
            }
            _cache.Set(key,tracks,TimeSpan.FromMinutes(8));
        }
        catch(Exception ex){_log.LogWarning("Consulta de YouTube no disponible ({Tipo})",ex.GetType().Name);}
        return tracks;
    }

    public async Task<PistaEnLinea?> ObtenerAsync(string fuente, string id)
    {
        if(fuente=="YouTube" && YoutubeConfigurado && Regex.IsMatch(id,@"^[a-zA-Z0-9_-]{11}$"))
        {
            try
            {
                var client=_factory.CreateClient("catalogos");
                using var stream=await client.GetStreamAsync(
                    "https://www.googleapis.com/youtube/v3/videos?part=snippet,status&id="+Uri.EscapeDataString(id)+
                    "&key="+Uri.EscapeDataString(_youtubeKey));
                using var json=await JsonDocument.ParseAsync(stream);
                if(!json.RootElement.TryGetProperty("items",out var items)||items.GetArrayLength()==0)return null;
                var video=items[0];
                var status=video.GetProperty("status");
                if(!status.TryGetProperty("embeddable",out var embeddable)||embeddable.ValueKind!=JsonValueKind.True)return null;
                var info=video.GetProperty("snippet");
                return new PistaEnLinea {Fuente="YouTube",IdExterno=id,
                    Titulo=Plain(Str(info,"title")),Artista=Plain(Str(info,"channelTitle")),
                    UrlOrigen="https://www.youtube.com/watch?v="+id,
                    Licencia="Reproducción en reproductor oficial de YouTube"};
            }
            catch(Exception ex){_log.LogWarning("Error verificando video YouTube ({Tipo})",ex.GetType().Name);return null;}
        }
        if (fuente == "Apple" && long.TryParse(id, out var idNum) && idNum > 0)
        {
            try
            {
                var client = _factory.CreateClient("catalogos");
                using var stream = await client.GetStreamAsync("https://itunes.apple.com/lookup?entity=song&id=" + idNum);
                using var json = await JsonDocument.ParseAsync(stream);
                if (!json.RootElement.TryGetProperty("results", out var tracks) || tracks.GetArrayLength()==0) return null;
                var t=tracks[0]; var origin=Str(t,"trackViewUrl");
                if (!ValidUrl(origin,"itunes.apple.com","music.apple.com") || Str(t,"trackName").Length==0) return null;
                return new PistaEnLinea { Fuente="Apple", IdExterno=idNum.ToString(), Titulo=Str(t,"trackName"),
                    Artista=Str(t,"artistName"), UrlOrigen=origin,
                    Licencia="Reproducción externa autorizada por el servicio oficial" };
            }
            catch (Exception ex) { _log.LogWarning(ex,"Error al verificar Apple ID"); return null; }
        }
        if (fuente == "Commons" && id.StartsWith("File:", StringComparison.OrdinalIgnoreCase) && id.Length < 256)
        {
            // Verificamos de nuevo el archivo original con el proveedor. No confiamos en URLs del POST.
            try
            {
                var client=_factory.CreateClient("catalogos");
                using var req=new HttpRequestMessage(HttpMethod.Get,
                    "https://commons.wikimedia.org/w/api.php?action=query&titles=" + Uri.EscapeDataString(id) +
                    "&prop=imageinfo&iiprop=url%7Cmime%7Cextmetadata&format=json&formatversion=2");
                req.Headers.UserAgent.ParseAdd("BiblioBeatAcademic/2.0 (https://example.org/bibliobeat; contacto: administrador del sitio)");
                using var res=await client.SendAsync(req);res.EnsureSuccessStatusCode();
                using var json=JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                var pages=json.RootElement.GetProperty("query").GetProperty("pages");
                var page=pages[0];var info=page.GetProperty("imageinfo")[0];
                var url=Str(info,"url");var mime=Str(info,"mime");
                if (!mime.StartsWith("audio/",StringComparison.OrdinalIgnoreCase)||
                    !ValidUrl(url,"upload.wikimedia.org")) return null;
                var meta=info.GetProperty("extmetadata");
                string field(string name) => meta.TryGetProperty(name,out var el) ? Plain(Str(el,"value")) : "";
                var lic=field("LicenseShortName");
                if (!(lic.Contains("CC BY",StringComparison.OrdinalIgnoreCase) ||
                      lic.Contains("CC0",StringComparison.OrdinalIgnoreCase) ||
                      lic.Contains("Public domain",StringComparison.OrdinalIgnoreCase) ||
                      lic.Equals("PD",StringComparison.OrdinalIgnoreCase)) ||
                     lic.Contains("NC",StringComparison.OrdinalIgnoreCase)) return null;
                return new PistaEnLinea {
                    Fuente="Commons",IdExterno=id,Titulo=id[5..].Replace('_',' '),
                    Artista=field("Artist") is var a && a.Length>0 ? a[..Math.Min(a.Length,100)] : "Wikimedia Commons",
                    UrlEscucha=url,UrlOrigen="https://commons.wikimedia.org/wiki/"+Uri.EscapeDataString(id.Replace(' ','_')),
                    Licencia=lic
                };
            }
            catch(Exception ex){_log.LogWarning(ex,"Error al verificar archivo Commons");}
        }
        return null;
    }
}
