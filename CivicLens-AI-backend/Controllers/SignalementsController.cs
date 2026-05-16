using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CivicLens.API.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace CivicLens.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    /*[Authorize(Roles = "CITOYEN")]*/
    public class SignalementsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private const long MaxImageBytes = 5 * 1024 * 1024;

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IAIService _aiService;
        private readonly string _uploadsRoot;
        private readonly List<string> _uploadsReadRoots;

        public SignalementsController(AppDbContext db, IWebHostEnvironment env, IAIService aiService, IConfiguration config)
        {
            _db = db;
            _env = env;
            _aiService = aiService;
            _uploadsRoot = config["UploadsRoot"] ?? Path.Combine(_env.ContentRootPath, "uploads");
            _uploadsReadRoots = new List<string> { _uploadsRoot, Path.Combine(_env.ContentRootPath, "uploads") }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateSignalementDto dto, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var citoyenInfo = await _db.Citoyens
                .AsNoTracking()
                .Where(c => c.Id == citoyenId)
                .Select(c => new { c.Delegation })
                .FirstOrDefaultAsync(ct);
            if (citoyenInfo is null)
                return Unauthorized("Citoyen introuvable.");

            var citoyenDelegation = citoyenInfo.Delegation;

            var images = dto.Images?.Where(f => f is not null && f.Length > 0).ToList() ?? [];
            if (images.Count > 3)
                return BadRequest("Vous pouvez envoyer au maximum 3 images.");

            foreach (var file in images)
            {
                if (file.Length > MaxImageBytes)
                    return BadRequest("Une image dépasse la taille maximale autorisée (5MB).");

                if (!AllowedContentTypes.Contains(file.ContentType))
                    return BadRequest("Format image non supporté. Formats autorisés: jpeg, png, webp.");

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
                    return BadRequest("Extension image non supportée. Extensions autorisées: .jpg, .jpeg, .png, .webp.");
            }

            if (!double.TryParse(dto.Latitude.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) || lat < -90 || lat > 90)
                return BadRequest("Latitude invalide.");
                
            if (!double.TryParse(dto.Longitude.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lon) || lon < -180 || lon > 180)
                return BadRequest("Longitude invalide.");

            var writtenFiles = new List<string>();

            var aiResult = await _aiService.AnalyzeSignalementAsync(dto.Description.Trim(), lat, lon, images);
            if (aiResult == null)
                return StatusCode(500, "Erreur de communication avec le service d'Intelligence Artificielle.");

            Enum.TryParse<Domaine>(aiResult.Domain, true, out var domaine);
            Enum.TryParse<Gravite>(aiResult.Gravity, true, out var gravite);
            Enum.TryParse<Priorite>(aiResult.Priority, true, out var priorite);

            var limitDate = DateTime.UtcNow.AddDays(-30);
            var existingReports = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.Statut != StatutSignalement.REJETE && s.Domaine == domaine && s.DateCreation > limitDate)
                .Select(s => new ExistingReportDto
                {
                    Id = s.Id,
                    Description = s.Description,
                    Lat = s.Latitude ?? 0,
                    Lon = s.Longitude ?? 0,
                    Vote_Count = s.Votes.Count()
                })
                .ToListAsync(ct);

            var duplicateCheck = await _aiService.CheckDuplicateAsync(dto.Description.Trim(), lat, lon, aiResult.Domain, existingReports);
            if (duplicateCheck != null && duplicateCheck.Is_Duplicate)
            {
                var originalId = duplicateCheck.Original_Report_Id;
                var origin = Request.Headers.Origin.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(origin))
                {
                    var referer = Request.Headers.Referer.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                    {
                        origin = refererUri.GetLeftPart(UriPartial.Authority);
                    }
                }
                origin ??= "http://localhost:4200";

                var link = originalId.HasValue ? $"{origin}/dashboard/signalement/{originalId.Value}" : null;
                var message = originalId.HasValue
                    ? $"Un signalement similaire ou identique existe déjà. Vous pouvez le consulter et voter ici : {link}"
                    : "Un signalement similaire ou identique existe déjà. Merci pour votre engagement !";

                return Conflict(new { message, originalId, link });
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var signalement = new Signalement
                {
                    Description = dto.Description.Trim(),
                    Latitude = lat,
                    Longitude = lon,
                    CitoyenId = citoyenId,
                    OrganisationId = null,
                    Statut = StatutSignalement.EN_ATTENTE,
                    Gravite = gravite,
                    Priorite = priorite,
                    Domaine = domaine,
                    Adresse = aiResult.Address != null ? System.Text.Json.JsonSerializer.Serialize(aiResult.Address) : null,
                    ScoreAI = aiResult.Score,
                    DateCreation = DateTime.UtcNow
                };

                _db.Signalements.Add(signalement);
                await _db.SaveChangesAsync(ct);

                var folder = Path.Combine(_uploadsRoot, "signalements", signalement.Id.ToString());
                Directory.CreateDirectory(folder);

                foreach (var file in images)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var physicalPath = Path.Combine(folder, fileName);

                    await using (var stream = System.IO.File.Create(physicalPath))
                    {
                        await file.CopyToAsync(stream, ct);
                    }

                    writtenFiles.Add(physicalPath);

                    var url = $"/uploads/signalements/{signalement.Id}/{fileName}";
                    _db.ImagesSignalement.Add(new ImageSignalement
                    {
                        SignalementId = signalement.Id,
                        UrlImage = url,
                        DateAjout = DateTime.UtcNow
                    });
                }

                _db.HistoriquesStatut.Add(new HistoriqueStatut
                {
                    SignalementId = signalement.Id,
                    Statuts = new List<StatutSignalement> { StatutSignalement.EN_ATTENTE },
                    IdModificateurs = new List<Guid> { citoyenId },
                    DateChangement = signalement.DateCreation
                });

                if (!string.IsNullOrWhiteSpace(citoyenDelegation))
                {
                    var recipientIds = await _db.Citoyens
                        .AsNoTracking()
                        .Where(c => c.Delegation == citoyenDelegation && c.Id != citoyenId)
                        .Select(c => c.Id)
                        .ToListAsync(ct);

                    foreach (var recipientId in recipientIds)
                    {
                        _db.Notifications.Add(new Notification
                        {
                            CitoyenId = recipientId,
                            Type = "NOUVEAU_SIGNALEMENT_DELEGATION",
                            RelatedSignalementId = signalement.Id,
                            Message = $"Nouveau signalement dans votre délégation : Signalement #{signalement.Id}.",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = signalement.Id },
                    new
                    {
                        signalement.Id,
                        signalement.Description,
                        signalement.Latitude,
                        signalement.Longitude,
                        signalement.Statut,
                        Images = await _db.ImagesSignalement
                            .Where(i => i.SignalementId == signalement.Id)
                            .Select(i => i.UrlImage)
                            .ToListAsync(ct)
                    });
            }
            catch
            {
                foreach (var path in writtenFiles)
                {
                    try { System.IO.File.Delete(path); } catch { }
                }

                await tx.RollbackAsync(ct);
                throw;
            }
        }

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
        {
            var signalement = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    Id = s.Id,
                    Titre = $"Signalement #{s.Id}",
                    Description = s.Description,
                    CreatedAt = s.DateCreation,
                    Statut = s.Statut.ToString(),
                    Gravite = s.Gravite.ToString(),
                    Priorite = s.Priorite.ToString(),
                    Categorie = s.Domaine.ToString(),
                    ScoreAI = s.ScoreAI,
                    Adresse = s.Adresse,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    CitoyenId = s.CitoyenId,
                    OrganisationId = s.OrganisationId,
                    Votes = s.Votes.Count()
                })
                .FirstOrDefaultAsync(ct);

            if (signalement is null)
                return NotFound();

            var images = await _db.ImagesSignalement
                .AsNoTracking()
                .Where(i => i.SignalementId == id)
                .Select(i => i.UrlImage)
                .ToListAsync(ct);

            var historique = await _db.HistoriquesStatut
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.SignalementId == id, ct);

            var historiqueDto = historique == null
                ? null
                : new
                {
                    historique.Id,
                    historique.DateChangement,
                    Statuts = historique.Statuts.Select(s => s.ToString()).ToList(),
                    IdModificateurs = historique.IdModificateurs
                };

            return Ok(new { signalement, images, historique = historiqueDto });
        }

        [HttpGet("{id:long}/pdf")]
        public async Task<IActionResult> DownloadPdf([FromRoute] long id, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var signalement = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.Description,
                    CreatedAt = s.DateCreation,
                    Statut = s.Statut.ToString(),
                    Gravite = s.Gravite.ToString(),
                    Priorite = s.Priorite.ToString(),
                    Categorie = s.Domaine.ToString(),
                    s.Adresse,
                    s.Latitude,
                    s.Longitude,
                    s.CitoyenId,
                    Votes = s.Votes.Count()
                })
                .FirstOrDefaultAsync(ct);

            if (signalement is null)
                return NotFound("Signalement introuvable.");

            if (signalement.CitoyenId != citoyenId)
                return Forbid();

            var imageUrls = await _db.ImagesSignalement
                .AsNoTracking()
                .Where(i => i.SignalementId == id)
                .OrderBy(i => i.Id)
                .Select(i => i.UrlImage)
                .ToListAsync(ct);

            var images = new List<byte[]>();
            foreach (var url in imageUrls)
            {
                var bytes = await TryReadUploadsFileAsync(url, ct);
                if (bytes is not null && bytes.Length > 0)
                    images.Add(bytes);
                if (images.Count >= 3)
                    break;
            }

            var title = $"Signalement #{signalement.Id}";
            var dateText = signalement.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            var coordsText = $"{signalement.Latitude:0.######}, {signalement.Longitude:0.######}";
            var adresseText = FormatAdresse(signalement.Adresse);
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
                                    col.Item().PaddingTop(2).Text("Rapport de signalement").FontSize(10).FontColor(Colors.White);
                                    col.Item().PaddingTop(6).Text(title).FontSize(22).FontColor(Colors.White).Bold();
                                });

                                row.ConstantItem(170).AlignRight().Column(col =>
                                {
                                    col.Item().AlignRight().Text(dateText).FontSize(10).FontColor(Colors.White);
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
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Coordonnées", coordsText, "#6366f1"));
                            });

                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Catégorie", signalement.Categorie ?? "—", "#3b82f6"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Gravité", signalement.Gravite ?? "—", "#f59e0b"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Priorité", signalement.Priorite ?? "—", "#ef4444"));
                                row.ConstantItem(10);
                                row.RelativeItem().Element(e => BuildInfoCard(e, "Votes", signalement.Votes.ToString(), "#64748b"));
                            });

                            content.Item().Element(e => BuildSection(e, "Description", signalement.Description ?? "—", "#0ea5e9"));

                            content.Item().Element(card =>
                            {
                                card.Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Background(Colors.White)
                                    .Padding(10)
                                    .Column(col =>
                                    {
                                        col.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text("Images").FontSize(12).FontColor("#0f172a").SemiBold();
                                            r.ConstantItem(80).AlignRight().Text($"{images.Count}/3").FontSize(9).FontColor(Colors.Grey.Darken1);
                                        });
                                        col.Item().PaddingTop(6).LineHorizontal(2).LineColor("#0ea5e9");

                                        if (images.Count == 0)
                                        {
                                            col.Item().PaddingTop(10).Text("Aucune image attachée.").FontSize(10).FontColor(Colors.Grey.Darken2);
                                            return;
                                        }

                                        col.Item().PaddingTop(10).Row(row =>
                                        {
                                            row.Spacing(10);

                                            for (var i = 0; i < images.Count; i++)
                                            {
                                                row.RelativeItem()
                                                    .Height(150)
                                                    .Border(1)
                                                    .BorderColor(Colors.Grey.Lighten2)
                                                    .Background(Colors.Grey.Lighten5)
                                                    .Padding(4)
                                                    .AlignCenter()
                                                    .AlignMiddle()
                                                    .Image(images[i])
                                                    .FitArea();
                                            }

                                            for (var i = images.Count; i < 3; i++)
                                            {
                                                row.RelativeItem()
                                                    .Height(150)
                                                    .Border(1)
                                                    .BorderColor(Colors.Grey.Lighten2)
                                                    .Background(Colors.Grey.Lighten5);
                                            }
                                        });
                                    });
                            });
                        });

                    page.Footer()
                        .Background("#f8fafc")
                        .PaddingBottom(8)
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("CivicLens • ").FontColor(Colors.Grey.Darken2);
                            text.Span($"Signalement #{signalement.Id} • ").FontColor(Colors.Grey.Darken2);
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontColor(Colors.Grey.Darken2);
                        });
                });
            }).GeneratePdf();

            var fileName = $"signalement_{signalement.Id}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
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

        private static void BuildSection(IContainer container, string title, string body, string accentHex)
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
                    col.Item().PaddingTop(10).MaxHeight(100).Text(string.IsNullOrWhiteSpace(body) ? "—" : body).FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.25f);
                });
        }

        private static void BuildStatusPill(IContainer container, string value, string colorHex)
        {
            container
                .Background(colorHex)
                .PaddingHorizontal(12)
                .PaddingVertical(6)
                .Text(value)
                .FontSize(10)
                .FontColor(Colors.White)
                .SemiBold();
        }

        private static string GetStatusColor(string statut)
        {
            return statut switch
            {
                "RESOLU" => "#16a34a",
                "REJETE" => "#dc2626",
                "EN_ATTENTE" => "#f59e0b",
                "ENVOYEE" => "#2563eb",
                "EN_COURS_DETUDE" => "#9333ea",
                "OUVERTE" => "#0ea5e9",
                _ => "#64748b"
            };
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

        [HttpGet("mes/stats")]
        public async Task<IActionResult> GetMyStats(CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized();

            var signalements = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.CitoyenId == citoyenId)
                .ToListAsync(ct);

            var stats = new
            {
                Total = signalements.Count,
                EnAttente = signalements.Count(s => s.Statut == StatutSignalement.EN_ATTENTE),
                EnCours = signalements.Count(s => s.Statut == StatutSignalement.EN_COURS_DETUDE || s.Statut == StatutSignalement.OUVERTE || s.Statut == StatutSignalement.ENVOYEE),
                Resolus = signalements.Count(s => s.Statut == StatutSignalement.RESOLU),
                Rejetes = signalements.Count(s => s.Statut == StatutSignalement.REJETE)
            };

            return Ok(stats);
        }

        [HttpGet("mes")]
        public async Task<IActionResult> GetMySignalements(CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized();

            var signalements = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.CitoyenId == citoyenId)
                .OrderByDescending(s => s.DateCreation)
                .Select(s => new
                {
                    Id = s.Id,
                    Titre = $"Signalement #{s.Id}",
                    Description = s.Description,
                    CreatedAt = s.DateCreation,
                    Statut = s.Statut.ToString(),
                    Gravite = s.Gravite.ToString(),
                    Priorite = s.Priorite.ToString(),
                    Categorie = s.Domaine.ToString(),
                    ScoreAI = s.ScoreAI,
                    Adresse = s.Adresse,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Votes = s.Votes.Count(),
                    aVote = s.Votes.Any(v => v.CitoyenId == citoyenId),
                    ImageUrl = _db.ImagesSignalement.Where(img => img.SignalementId == s.Id).Select(img => img.UrlImage).FirstOrDefault()
                })
                .ToListAsync(ct);

            return Ok(signalements);
        }

        [HttpGet("citoyen/{citoyenId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCitoyen([FromRoute] Guid citoyenId, CancellationToken ct)
        {
            var signalements = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.CitoyenId == citoyenId)
                .OrderByDescending(s => s.DateCreation)
                .Select(s => new
                {
                    Id = s.Id,
                    Titre = $"Signalement #{s.Id}",
                    Description = s.Description,
                    CreatedAt = s.DateCreation,
                    Statut = s.Statut.ToString(),
                    Gravite = s.Gravite.ToString(),
                    Priorite = s.Priorite.ToString(),
                    Categorie = s.Domaine.ToString(),
                    ScoreAI = s.ScoreAI,
                    Adresse = s.Adresse,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Votes = s.Votes.Count(),
                    ImageUrl = _db.ImagesSignalement.Where(img => img.SignalementId == s.Id).Select(img => img.UrlImage).FirstOrDefault()
                })
                .ToListAsync(ct);

            return Ok(signalements);
        }

        [HttpGet("delegation")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDelegation([FromQuery] string nomDelegation, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(nomDelegation))
                return BadRequest("Le nom de la délégation est requis.");

            var searchPattern = $"%{nomDelegation.Trim()}%";

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            Guid.TryParse(userIdStr, out var currentUserId);

            var signalements = await _db.Signalements
                .FromSqlInterpolated($"SELECT * FROM \"Signalements\" WHERE \"Adresse\" IS NOT NULL AND (\"Adresse\"::jsonb ->> 'city' ILIKE {searchPattern} OR \"Adresse\"::jsonb ->> 'town' ILIKE {searchPattern} OR \"Adresse\"::jsonb ->> 'village' ILIKE {searchPattern} OR \"Adresse\"::jsonb ->> 'county' ILIKE {searchPattern} OR \"Adresse\"::jsonb ->> 'state' ILIKE {searchPattern})")
                .AsNoTracking()
                .OrderByDescending(s => s.DateCreation)
                .Select(s => new
                {
                    Id = s.Id,
                    CitoyenId = s.CitoyenId,
                    Titre = $"Signalement #{s.Id}", // Fallback since Titre was removed
                    Description = s.Description,
                    CreatedAt = s.DateCreation,
                    Statut = s.Statut.ToString(),
                    Gravite = s.Gravite.ToString(),
                    Priorite = s.Priorite.ToString(),
                    Categorie = s.Domaine.ToString(), // Maps to frontend categorie
                    ScoreAI = s.ScoreAI,
                    Adresse = s.Adresse,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Votes = s.Votes.Count(),
                    aVote = currentUserId != Guid.Empty && s.Votes.Any(v => v.CitoyenId == currentUserId),
                    ImageUrl = _db.ImagesSignalement.Where(img => img.SignalementId == s.Id).Select(img => img.UrlImage).FirstOrDefault()
                })
                .ToListAsync(ct);

            return Ok(signalements);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
        {
            var signalement = await _db.Signalements.FindAsync([id], ct);
            if (signalement == null)
                return NotFound("Signalement introuvable.");

            // Optionnel: vérifier si c'est bien le créateur qui supprime
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (Guid.TryParse(userIdStr, out var citoyenId) && signalement.CitoyenId != citoyenId)
            {
                return Forbid("Vous ne pouvez supprimer que vos propres signalements.");
            }

            if (signalement.Statut != StatutSignalement.EN_ATTENTE)
            {
                return BadRequest("Vous ne pouvez supprimer qu'un signalement en attente.");
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // 1. Supprimer tout l'historique de statut
                var historiques = await _db.HistoriquesStatut.Where(h => h.SignalementId == id).ToListAsync(ct);
                _db.HistoriquesStatut.RemoveRange(historiques);

                // 2. Supprimer les images (en base de données)
                var images = await _db.ImagesSignalement.Where(i => i.SignalementId == id).ToListAsync(ct);
                _db.ImagesSignalement.RemoveRange(images);

                // 3. Supprimer le dossier contenant les images physiques
                var folder = Path.Combine(_env.ContentRootPath, "uploads", "signalements", id.ToString());
                if (Directory.Exists(folder))
                {
                    try { Directory.Delete(folder, true); } catch { /* Ignorer les erreurs d'I/O disque */ }
                }

                // 4. Supprimer le signalement lui-même
                _db.Signalements.Remove(signalement);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return Ok(new { message = "Signalement, historique et images supprimés avec succès." });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                return StatusCode(500, new { message = "Erreur lors de la suppression.", detail = ex.Message });
            }
        }

        [HttpPost("{id:long}/vote")]
        public async Task<IActionResult> Vote([FromRoute] long id, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var signalement = await _db.Signalements.FindAsync(new object[] { id }, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable.");

            if (signalement.CitoyenId == citoyenId)
                return BadRequest("Vous ne pouvez pas voter pour votre propre signalement.");

            var existingVote = await _db.Votes
                .FirstOrDefaultAsync(v => v.CitoyenId == citoyenId && v.SignalementId == id, ct);
            
            if (existingVote != null)
                return BadRequest("Vous avez déjà voté pour ce signalement.");

            var vote = new Vote { CitoyenId = citoyenId, SignalementId = id };
            _db.Votes.Add(vote);
            await _db.SaveChangesAsync(ct);

            var voteCount = await _db.Votes.CountAsync(v => v.SignalementId == id, ct);

            string prioriteLevel = signalement.Priorite.HasValue ? signalement.Priorite.Value.ToString() : "NORMALE";
            string graviteLevel = signalement.Gravite.HasValue ? signalement.Gravite.Value.ToString() : "MOYENNE";
            string domaineStr = signalement.Domaine.HasValue ? signalement.Domaine.Value.ToString() : "AUTRE";

            var aiResult = await _aiService.UpdatePriorityAsync(
                graviteLevel,
                voteCount,
                domaineStr,
                signalement.Latitude ?? 0,
                signalement.Longitude ?? 0
            );

            if (aiResult != null)
            {
                if (Enum.TryParse<Priorite>(aiResult.Priority, true, out var newPriorite))
                {
                    signalement.Priorite = newPriorite;
                }
                signalement.ScoreAI = aiResult.Score;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new
            {
                Id = signalement.Id,
                Titre = $"Signalement #{signalement.Id}",
                Description = signalement.Description,
                CreatedAt = signalement.DateCreation,
                Statut = signalement.Statut.ToString(),
                Gravite = signalement.Gravite.ToString(),
                Priorite = signalement.Priorite.ToString(),
                Categorie = signalement.Domaine.ToString(),
                ScoreAI = signalement.ScoreAI,
                Adresse = signalement.Adresse,
                Latitude = signalement.Latitude,
                Longitude = signalement.Longitude,
                Votes = voteCount,
                aVote = true
            });
        }
    }
}
