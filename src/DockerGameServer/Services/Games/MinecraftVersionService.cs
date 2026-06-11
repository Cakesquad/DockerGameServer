using System.Text.Json;
using System.Xml.Linq;

namespace DockerGameServer.Services.Games
{
    public enum MinecraftVersionType
    {
        Release,
        Snapshot
    }

    public sealed record MinecraftCombinedVersion(
        string Version,
        MinecraftVersionType Type,
        bool SupportsFabric,
        bool SupportsNeoForge
    );


    public sealed record MinecraftVersionInfo(
        string Id,
        string Type,          // release / snapshot
        DateTime ReleaseTime
    );

    public sealed record FabricVersionInfo(
        string Version,
        bool Stable
    );

    public sealed class MinecraftVersionService
    {
        private readonly HttpClient _http;

        private static readonly Uri MojangManifestUrl =
            new("https://piston-meta.mojang.com/mc/game/version_manifest.json");

        private static readonly Uri FabricGameVersionsUrl =
            new("https://meta.fabricmc.net/v2/versions/game");

        private static readonly Uri NeoForgeMetadataUrl =
            new("https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml");

        public MinecraftVersionService(HttpClient http)
        {
            _http = http;
        }

        // ------------------------------------------------------------
        // VANILLA: release + snapshot (ingen server-check)
        // ------------------------------------------------------------
        public async Task<IReadOnlyList<MinecraftVersionInfo>> GetVanillaVersionsAsync(
            CancellationToken ct = default)
        {
            var json = await _http.GetStringAsync(MojangManifestUrl, ct);
            using var doc = JsonDocument.Parse(json);

            var versions = doc.RootElement.GetProperty("versions").EnumerateArray();

            return versions
                .Where(v =>
                {
                    var type = v.GetProperty("type").GetString()!;
                    return type == "release" || type == "snapshot";
                })
                .Select(v => new MinecraftVersionInfo(
                    Id: v.GetProperty("id").GetString()!,
                    Type: v.GetProperty("type").GetString()!,
                    ReleaseTime: v.GetProperty("releaseTime").GetDateTime()
                ))
                .OrderByDescending(v => v.ReleaseTime)
                .ToList();
        }

        // ------------------------------------------------------------
        // FABRIC SUPPORTED VERSIONS
        // ------------------------------------------------------------
        public async Task<IReadOnlyList<FabricVersionInfo>> GetFabricVersionsAsync(
            CancellationToken ct = default)
        {
            var json = await _http.GetStringAsync(FabricGameVersionsUrl, ct);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .EnumerateArray()
                .Select(e => new FabricVersionInfo(
                    Version: e.GetProperty("version").GetString()!,
                    Stable: e.GetProperty("stable").GetBoolean()
                ))
                .ToList();
        }

        // ------------------------------------------------------------
        // NEOFORGE SUPPORTED VERSIONS
        // ------------------------------------------------------------
        public async Task<IReadOnlyList<string>> GetNeoForgeVersionsAsync(
            CancellationToken ct = default)
        {
            var xml = await _http.GetStringAsync(NeoForgeMetadataUrl, ct);
            var doc = XDocument.Parse(xml);

            var versions = doc
                .Root?
                .Element("versioning")?
                .Element("versions")?
                .Elements("version")
                .Select(x => x.Value)
                .ToList() ?? new List<string>();

            return versions;
        }

        // ------------------------------------------------------------
        // COMMON SUPPORTED VERSIONS (Vanilla + Fabric + NeoForge)
        // ------------------------------------------------------------
        public async Task<IReadOnlyList<string>> GetCommonVersionsAsync(
            CancellationToken ct = default)
        {
            var vanilla = await GetVanillaVersionsAsync(ct);
            var fabric = await GetFabricVersionsAsync(ct);
            var neoforge = await GetNeoForgeVersionsAsync(ct);

            var vanillaIds = vanilla.Select(v => v.Id).ToHashSet();
            var fabricIds = fabric.Select(f => f.Version).ToHashSet();

            return vanillaIds
                .Intersect(fabricIds)
                .Intersect(neoforge)
                .OrderByDescending(v => v)
                .ToList();
        }

        public async Task<IReadOnlyList<MinecraftCombinedVersion>> GetCombinedVersionsAsync(
    CancellationToken ct = default)
        {
            var vanilla = await GetVanillaVersionsAsync(ct);
            var fabric = await GetFabricVersionsAsync(ct);
            var neoforgeRaw = await GetNeoForgeVersionsAsync(ct);

            var fabricSet = fabric
                .Select(f => f.Version)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var neoforgeKeys = neoforgeRaw
                .Select(GetNeoForgeKeyFromVersion)
                .Where(k => k is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new List<MinecraftCombinedVersion>();

            foreach (var v in vanilla)
            {
                var type = v.Type == "release"
                    ? MinecraftVersionType.Release
                    : MinecraftVersionType.Snapshot;

                var mcKey = GetMinecraftKeyFromVersion(v.Id);

                var supportsFabric = fabricSet.Contains(v.Id);
                var supportsNeoForge = mcKey is not null && neoforgeKeys.Contains(mcKey);

                result.Add(new MinecraftCombinedVersion(
                    Version: v.Id,
                    Type: type,
                    SupportsFabric: supportsFabric,
                    SupportsNeoForge: supportsNeoForge
                ));
            }

            return result
                .OrderByDescending(v => v.Version, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? GetMinecraftKeyFromVersion(string mcVersion)
        {
            var parts = mcVersion.Split('.');
            if (parts.Length < 3)
                return null;

            // Sidste to tal: minor.patch
            return $"{parts[1]}.{parts[2]}";
        }

        private static string? GetNeoForgeKeyFromVersion(string nfVersion)
        {
            var parts = nfVersion.Split('.');
            if (parts.Length < 2)
                return null;

            // Første to tal: major.minor
            return $"{parts[0]}.{parts[1]}";
        }
    }
}
