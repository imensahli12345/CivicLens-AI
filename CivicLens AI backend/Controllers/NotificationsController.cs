using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CivicLens.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "CITOYEN")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public NotificationsController(AppDbContext db)
        {
            _db = db;
        }

        private async Task<Guid?> GetCurrentCitoyenIdAsync(CancellationToken ct)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return null;
            // We expect Role=Citoyen already, but keep it safe
            var exists = await _db.Citoyens.AsNoTracking().AnyAsync(c => c.Id == citoyenId, ct);
            return exists ? citoyenId : null;
        }

        [HttpGet("mes")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int? limit, CancellationToken ct)
        {
            var citoyenId = await GetCurrentCitoyenIdAsync(ct);
            if (citoyenId == null) return Unauthorized("Token invalide.");

            IQueryable<Notification> q = _db.Notifications
                .AsNoTracking()
                .Where(n => n.CitoyenId == citoyenId)
                .OrderByDescending(n => n.CreatedAt);

            if (limit.HasValue && limit.Value > 0)
                q = q.Take(limit.Value);

            var list = await q.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                RelatedEvenementId = n.RelatedEvenementId,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead
            }).ToListAsync(ct);

            return Ok(list);
        }

        [HttpGet("mes/unread-count")]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var citoyenId = await GetCurrentCitoyenIdAsync(ct);
            if (citoyenId == null) return Unauthorized("Token invalide.");

            var count = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.CitoyenId == citoyenId && !n.IsRead)
                .CountAsync(ct);

            return Ok(new { unreadCount = count });
        }

        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken ct)
        {
            var citoyenId = await GetCurrentCitoyenIdAsync(ct);
            if (citoyenId == null) return Unauthorized("Token invalide.");

            var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.CitoyenId == citoyenId, ct);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Notification marquée comme lue." });
        }

        [HttpPut("mes/mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
        {
            var citoyenId = await GetCurrentCitoyenIdAsync(ct);
            if (citoyenId == null) return Unauthorized("Token invalide.");

            var notifs = await _db.Notifications
                .Where(n => n.CitoyenId == citoyenId && !n.IsRead)
                .ToListAsync(ct);

            foreach (var n in notifs) n.IsRead = true;

            await _db.SaveChangesAsync(ct);
            return Ok(new { message = "Toutes les notifications sont marquées comme lues." });
        }
    }
}

