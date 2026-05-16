using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace CivicLens.API.Controllers
{
    [Route("api/bureau")]
    [ApiController]
    //[Authorize(Roles = "BUREAUDECONTROLE")]
    public class BureauDeControleController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IBureauDeControleService _bureauService;
        private readonly ISignalementManagementService _signalementManagementService;
        private readonly IHistoriqueService _historiqueService;
        private readonly IOrganisationService _organisationService;
        private readonly List<string> _uploadsReadRoots;

        public BureauDeControleController(
            IWebHostEnvironment env,
            IBureauDeControleService bureauService,
            ISignalementManagementService signalementManagementService,
            IHistoriqueService historiqueService,
            IOrganisationService organisationService,
            IConfiguration config)
        {
            _env = env;
            _bureauService = bureauService;
            _signalementManagementService = signalementManagementService;
            _historiqueService = historiqueService;
            _organisationService = organisationService;

            var configuredUploadsRoot = config["UploadsRoot"];
            var contentRootUploadsRoot = Path.Combine(_env.ContentRootPath, "uploads");
            var primaryUploadsRoot = !string.IsNullOrWhiteSpace(configuredUploadsRoot)
                ? configuredUploadsRoot
                : contentRootUploadsRoot;
            _uploadsReadRoots = new List<string> { primaryUploadsRoot, contentRootUploadsRoot }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        private async Task<BureauDeControle?> GetCurrentBureauAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                if (!_env.IsDevelopment())
                    return null;

                return await _bureauService.GetFirstBureauAsync(ct);
            }

            return await _bureauService.GetBureauByIdAsync(userId.Value, ct);
        }

        [HttpGet("signalements/tous")]
        public async Task<IActionResult> GetAllSignalements(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var signalements = await _signalementManagementService.GetAllSignalementsAsync(null, bureau.Delegation, ct);
            return Ok(signalements);
        }

        [HttpGet("signalements")]
        public async Task<IActionResult> GetSignalements([FromQuery] SignalementFilterDto filter, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalements = await _signalementManagementService.GetSignalementsAsync(null, bureau.Delegation, filter, ct);
                return Ok(signalements);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("signalements/{id:long}")]
        public async Task<IActionResult> GetSignalementById([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var signalement = await _signalementManagementService.GetSignalementByIdAsync(id, null, bureau.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre delegation.");

            return Ok(signalement);
        }

        [HttpGet("signalements/{id:long}/historique")]
        public async Task<IActionResult> GetHistorique([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var signalement = await _signalementManagementService.GetSignalementByIdAsync(id, null, bureau.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre delegation.");

            var historiques = await _historiqueService.GetExpandedBySignalementIdAsync(id, ct);
            return Ok(historiques);
        }

        [HttpGet("signalements/{id:long}/pdf")]
        public async Task<IActionResult> DownloadPdf([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var signalement = await _signalementManagementService.GetSignalementByIdAsync(id, null, bureau.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre delegation.");

            var imageBytes = new List<byte[]>();
            foreach (var url in signalement.Images ?? new List<string>())
            {
                var bytes = await TryReadUploadsFileAsync(url, ct);
                if (bytes is { Length: > 0 })
                    imageBytes.Add(bytes);
                if (imageBytes.Count >= 3)
                    break;
            }

            var historiqueSingle = await _historiqueService.GetSingleBySignalementIdAsync(id, ct);
            var historiqueEntries = await _historiqueService.GetExpandedBySignalementIdAsync(id, ct);
            var transitions = BuildTransitions(historiqueEntries);

            var title = $"Signalement #{signalement.Id}";
            var createdAt = signalement.DateCreation.ToString("dd/MM/yyyy HH:mm");
            var updatedAt = (historiqueSingle?.DateChangement ?? signalement.DateCreation).ToString("dd/MM/yyyy HH:mm");
            var adresseText = FormatAdresse(signalement.Adresse);
            var resolutionText = signalement.DateResolution.HasValue ? signalement.DateResolution.Value.ToString("dd/MM/yyyy HH:mm") : "—";
            var statutColor = GetStatusColor(signalement.Statut);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10).FontColor("#0f172a"));

                    page.Header().Column(header =>
                    {
                        header.Item()
                            .Background("#0ea5e9")
                            .PaddingHorizontal(16)
                            .PaddingVertical(14)
                            .Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("CivicLens").FontSize(16).FontColor(Colors.White).SemiBold();
                                    col.Item().PaddingTop(2).Text("Rapport Bureau de contrôle").FontSize(10).FontColor(Colors.White);
                                    col.Item().PaddingTop(6).Text(title).FontSize(22).FontColor(Colors.White).Bold();
                                    col.Item().PaddingTop(4).Text($"Matricule: {bureau.Matricule} • Délégation: {bureau.Delegation}").FontSize(10).FontColor(Colors.White);
                                });

                                row.ConstantItem(190).AlignRight().Column(col =>
                                {
                                    col.Item().AlignRight().Text($"Créé: {createdAt}").FontSize(9).FontColor(Colors.White);
                                    col.Item().AlignRight().Text($"MAJ: {updatedAt}").FontSize(9).FontColor(Colors.White);
                                    col.Item().PaddingTop(8).AlignRight().Element(e => BuildStatusPill(e, signalement.Statut, statutColor));
                                });
                            });
                    });

                    page.Content()
                        .Background("#f8fafc")
                        .Padding(14)
                        .Column(content =>
                        {
                            content.Spacing(10);

                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Adresse", adresseText, "#0ea5e9"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Date résolution", resolutionText, "#6366f1"));
                            });

                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Domaine", signalement.Domaine ?? "—", "#3b82f6"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Gravité", signalement.Gravite ?? "—", "#f59e0b"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Priorité", signalement.Priorite ?? "—", "#ef4444"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Organisation", string.IsNullOrWhiteSpace(signalement.OrganisationNom) ? "—" : signalement.OrganisationNom, "#64748b"));
                            });

                            content.Item().Element(e => BuildSection(e, "Description (Citoyen)", signalement.Description ?? "—", "#0ea5e9", 90));

                            if (!string.IsNullOrWhiteSpace(signalement.DescriptionService))
                            {
                                content.Item().Element(e => BuildSection(e, "Note Bureau/Service", signalement.DescriptionService!, "#6366f1", 70));
                            }

                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Element(e => BuildHistoryCard(e, transitions, "#0ea5e9"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildImagesCard(e, imageBytes, "#0ea5e9"));
                            });
                        });

                    page.Footer()
                        .Background("#f8fafc")
                        .PaddingBottom(8)
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("CivicLens • ").FontColor(Colors.Grey.Darken2);
                            text.Span($"Bureau • {bureau.Matricule} • ").FontColor(Colors.Grey.Darken2);
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontColor(Colors.Grey.Darken2);
                        });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"signalement_{signalement.Id}_rapport.pdf");
        }

        private async Task<byte[]?> TryReadUploadsFileAsync(string? urlOrPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath))
                return null;

            var normalized = urlOrPath.Trim();
            var queryIdx = normalized.IndexOf('?', StringComparison.Ordinal);
            if (queryIdx >= 0)
                normalized = normalized.Substring(0, queryIdx);

            if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var idx = normalized.IndexOf("/uploads/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    normalized = normalized.Substring(idx + "/uploads/".Length);
            }
            else if (normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("/uploads/".Length);
            }
            else if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("uploads/".Length);
            }

            normalized = Uri.UnescapeDataString(normalized);
            normalized = normalized.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            foreach (var root in _uploadsReadRoots)
            {
                var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
                var rootFull = Path.GetFullPath(root);
                if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!System.IO.File.Exists(fullPath))
                    continue;

                return await System.IO.File.ReadAllBytesAsync(fullPath, ct);
            }

            return null;
        }

        private static List<(string action, string by)> BuildTransitions(List<HistoriqueEntryDto> entries)
        {
            var result = new List<(string action, string by)>();
            if (entries == null || entries.Count == 0)
                return result;

            for (var i = 1; i < entries.Count; i++)
            {
                var from = FormatStatut(entries[i - 1].Statut);
                var to = FormatStatut(entries[i].Statut);
                var by = string.IsNullOrWhiteSpace(entries[i].Modificateur) ? "—" : entries[i].Modificateur!;
                result.Add(($"{from} → {to}", by));
            }

            if (result.Count == 0)
            {
                var by = string.IsNullOrWhiteSpace(entries[0].Modificateur) ? "—" : entries[0].Modificateur!;
                result.Add(($"Création ({FormatStatut(entries[0].Statut)})", by));
            }

            return result;
        }

        private static void BuildInfoCard(IContainer container, string label, string value, string accentHex)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.White)
                .Row(row =>
                {
                    row.ConstantItem(4).Background(accentHex);
                    row.RelativeItem().Padding(10).Column(col =>
                    {
                        col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(11).FontColor("#0f172a").SemiBold();
                    });
                });
        }

        private static void BuildSection(IContainer container, string title, string body, string accentHex, float maxHeight)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.White)
                .Padding(10)
                .Column(col =>
                {
                    col.Item().Text(title).FontSize(12).FontColor("#0f172a").SemiBold();
                    col.Item().PaddingTop(6).LineHorizontal(2).LineColor(accentHex);
                    col.Item().PaddingTop(10).MaxHeight(maxHeight).Text(string.IsNullOrWhiteSpace(body) ? "—" : body).FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.25f);
                });
        }

        private static void BuildStatusPill(IContainer container, string value, string colorHex)
        {
            container
                .Background(colorHex)
                .PaddingHorizontal(12)
                .PaddingVertical(6)
                .Text(FormatStatut(value))
                .FontSize(10)
                .FontColor(Colors.White)
                .SemiBold();
        }

        private static void BuildHistoryCard(IContainer container, List<(string action, string by)> transitions, string accentHex)
        {
            var rows = transitions ?? new List<(string action, string by)>();
            var visible = rows.Take(6).ToList();
            var remaining = Math.Max(0, rows.Count - visible.Count);

            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.White)
                .Padding(10)
                .Column(col =>
                {
                    col.Item().Text("Historique").FontSize(12).FontColor("#0f172a").SemiBold();
                    col.Item().PaddingTop(6).LineHorizontal(2).LineColor(accentHex);

                    if (visible.Count == 0)
                    {
                        col.Item().PaddingTop(10).Text("Aucune action enregistrée.").FontSize(10).FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Action").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold();
                            header.Cell().Text("Modificateur").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold();
                        });

                        foreach (var (action, by) in visible)
                        {
                            table.Cell().PaddingTop(4).Text(action).FontSize(9).FontColor("#0f172a");
                            table.Cell().PaddingTop(4).Text(by).FontSize(9).FontColor(Colors.Grey.Darken2);
                        }
                    });

                    if (remaining > 0)
                    {
                        col.Item().PaddingTop(6).Text($"+{remaining} autres...").FontSize(9).FontColor(Colors.Grey.Darken2);
                    }
                });
        }

        private static void BuildImagesCard(IContainer container, List<byte[]> images, string accentHex)
        {
            var list = images ?? new List<byte[]>();

            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.White)
                .Padding(10)
                .Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Images").FontSize(12).FontColor("#0f172a").SemiBold();
                        r.ConstantItem(80).AlignRight().Text($"{list.Count}/3").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().PaddingTop(6).LineHorizontal(2).LineColor(accentHex);

                    if (list.Count == 0)
                    {
                        col.Item().PaddingTop(10).Text("Aucune image attachée.").FontSize(10).FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.Spacing(8);

                        for (var i = 0; i < list.Count; i++)
                        {
                            row.RelativeItem()
                                .Height(110)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Background(Colors.Grey.Lighten5)
                                .Padding(4)
                                .AlignCenter()
                                .AlignMiddle()
                                .Image(list[i])
                                .FitArea();
                        }

                        for (var i = list.Count; i < 3; i++)
                        {
                            row.RelativeItem()
                                .Height(110)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Background(Colors.Grey.Lighten5);
                        }
                    });
                });
        }

        private static string GetStatusColor(string? statut)
        {
            return (statut ?? string.Empty) switch
            {
                "RESOLU" => "#16a34a",
                "RESOLUE" => "#16a34a",
                "REJETEE" => "#dc2626",
                "REJETE" => "#dc2626",
                "EN_ATTENTE" => "#f59e0b",
                "ENVOYEE" => "#2563eb",
                "OUVERTE" => "#0ea5e9",
                "EN_COURS_DETUDE" => "#9333ea",
                "EN_COURS" => "#9333ea",
                _ => "#64748b"
            };
        }

        private static string FormatStatut(string? statut)
        {
            return (statut ?? string.Empty) switch
            {
                "EN_ATTENTE" => "En attente",
                "OUVERTE" => "Ouverte",
                "ENVOYEE" => "Envoyée",
                "EN_COURS_DETUDE" => "En cours d'étude",
                "EN_COURS" => "En cours",
                "RESOLU" => "Résolu",
                "RESOLUE" => "Résolu",
                "REJETEE" => "Rejetée",
                "REJETE" => "Rejetée",
                _ => string.IsNullOrWhiteSpace(statut) ? "—" : statut!
            };
        }

        private static string FormatAdresse(string? adresseRaw)
        {
            if (string.IsNullOrWhiteSpace(adresseRaw))
                return "—";

            var trimmed = adresseRaw.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) && !trimmed.StartsWith("[", StringComparison.Ordinal))
                return trimmed;

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;

                static string GetString(JsonElement element, string name)
                {
                    if (element.ValueKind != JsonValueKind.Object)
                        return string.Empty;
                    if (!element.TryGetProperty(name, out var prop))
                        return string.Empty;
                    return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? string.Empty : prop.ToString();
                }

                var road = GetString(root, "road");
                var neighbourhood = GetString(root, "neighbourhood");
                var city = GetString(root, "city");
                var town = GetString(root, "town");
                var village = GetString(root, "village");
                var county = GetString(root, "county");
                var state = GetString(root, "state");

                var locality = !string.IsNullOrWhiteSpace(city) ? city
                    : !string.IsNullOrWhiteSpace(town) ? town
                    : !string.IsNullOrWhiteSpace(village) ? village
                    : !string.IsNullOrWhiteSpace(county) ? county
                    : string.Empty;

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(road)) parts.Add(road);
                if (!string.IsNullOrWhiteSpace(neighbourhood)) parts.Add(neighbourhood);
                if (!string.IsNullOrWhiteSpace(locality)) parts.Add(locality);
                if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

                return parts.Count > 0 ? string.Join(", ", parts) : trimmed;
            }
            catch
            {
                return trimmed;
            }
        }

        [HttpPut("signalements/{id:long}/ouvrir")]
        public async Task<IActionResult> OuvrirSignalement([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.OuvrirSignalementAsync(id, null, bureau.Delegation, bureau.Id, ct);
                return Ok(new { message = "Signalement ouvert avec succes.", signalementId = signalement.Id, statut = signalement.Statut.ToString() });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("signalements/{id:long}/envoyer")]
        public async Task<IActionResult> EnvoyerSignalement([FromRoute] long id, [FromBody] SendSignalementDto dto, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.EnvoyerSignalementAsync(id, null, bureau.Delegation, bureau.Id, dto.OrganisationId, dto.DescriptionSupplementaire, ct);
                var organisation = await _organisationService.GetByIdAsync(dto.OrganisationId, ct);
                return Ok(new
                {
                    message = "Signalement envoye avec succes a l'organisation.",
                    signalementId = signalement.Id,
                    statut = signalement.Statut.ToString(),
                    organisationId = signalement.OrganisationId,
                    organisationNom = organisation?.NomOrganisation
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("signalements/{id:long}/rejeter")]
        public async Task<IActionResult> RejeterSignalement([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.RejeterSignalementAsync(id, null, bureau.Delegation, bureau.Id, ct);
                return Ok(new { message = "Signalement rejete.", signalementId = signalement.Id, statut = signalement.Statut.ToString() });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var stats = await _signalementManagementService.GetStatsAsync(null, bureau.Delegation, ct);
            return Ok(stats);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            return Ok(new
            {
                id = bureau.Id,
                email = bureau.Email,
                region = bureau.Region,
                delegation = bureau.Delegation,
                matricule = bureau.Matricule,
                //statut = bureau.StatutCompte.ToString()
            });
        }

        [HttpGet("services")]
        public async Task<IActionResult> GetServices(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var services = await _bureauService.GetServicesAsync(bureau.Id, ct);
            return Ok(new
            {
                bureauId = bureau.Id,
                bureauEmail = bureau.Email,
                services = services.Select(service => new
                {
                    id = service.Id,
                    nom = service.Nom,
                    email = service.Email,
                    domaine = service.Domaine.ToString(),
                    statut = service.StatutCompte.ToString()
                })
            });
        }
[HttpPut("services/{serviceId:guid}/activer")]
public async Task<IActionResult> ActivateService([FromRoute] Guid serviceId, CancellationToken ct)
{
    var bureau = await GetCurrentBureauAsync(ct);
    if (bureau == null)
        return Unauthorized("Bureau de controle introuvable pour ce token.");

    try
    {
        await _bureauService.ActivateServiceAsync(bureau.Id, serviceId, ct);
        return Ok("Ce service est activé.");
    }
    catch (KeyNotFoundException)
    {
        return NotFound("Service introuvable pour ce bureau.");
    }
    catch (InvalidOperationException)
    {
        // Status was not changed (e.g. already active, or wrong bureau)
        return NotFound("Le statut n'a pas pu être modifié, veuillez réessayer.");
    }
}

[HttpPut("services/{serviceId:guid}/desactiver")]
public async Task<IActionResult> DeactivateService([FromRoute] Guid serviceId, CancellationToken ct)
{
    var bureau = await GetCurrentBureauAsync(ct);
    if (bureau == null)
        return Unauthorized("Bureau de controle introuvable pour ce token.");

    try
    {
        await _bureauService.DeactivateServiceAsync(bureau.Id, serviceId, ct);
        return Ok("Ce service est désactivé.");
    }
    catch (KeyNotFoundException)
    {
        return NotFound("Service introuvable pour ce bureau.");
    }
    catch (InvalidOperationException)
    {
        return NotFound("Le statut n'a pas pu être modifié, veuillez réessayer.");
    }
}

[HttpPut("services/{serviceId:guid}/password")]
public async Task<IActionResult> UpdateServicePassword([FromRoute] Guid serviceId, [FromBody] UpdateServicePasswordDto dto, CancellationToken ct)
{
    var bureau = await GetCurrentBureauAsync(ct);
    if (bureau == null)
        return Unauthorized("Bureau de controle introuvable pour ce token.");

    var next = (dto?.NouveauMotDePasse ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(next) || next.Length < 6)
        return BadRequest("Nouveau mot de passe invalide (min 6 caractères).");

    var bureauPwd = (dto?.MotDePasseBureau ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(bureauPwd))
        return BadRequest("Mot de passe du bureau requis.");

    if (!BCrypt.Net.BCrypt.Verify(bureauPwd, bureau.MotDePasseHash))
        return BadRequest("Mot de passe du bureau incorrect.");

    try
    {
        await _bureauService.UpdateServicePasswordAsync(bureau.Id, serviceId, next, ct);
        return Ok("Mot de passe du service mis à jour.");
    }
    catch (KeyNotFoundException)
    {
        return NotFound("Service introuvable pour ce bureau.");
    }
}
        [HttpGet("organisations")]
        public async Task<IActionResult> GetOrganisations(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var organisations = await _organisationService.GetListAsync(null, ct);
            return Ok(organisations.Select(o => new
            {
                id = o.Id,
                nomOrganisation = o.NomOrganisation,
                domaineOrganisation = o.DomaineOrganisation.ToString(),
                typeOrganisation = o.TypeOrganisation.ToString(),
                region = o.Region,
                //delegation = o.Delegation,
                email = o.Email
            }));
        }

        public class UpdateServicePasswordDto
        {
            public string NouveauMotDePasse { get; set; } = string.Empty;
            public string MotDePasseBureau { get; set; } = string.Empty;
        }
    }
}
