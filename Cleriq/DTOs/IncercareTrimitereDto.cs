using Cleriq.Models;

namespace Cleriq.DTOs;

public record IncercareTrimitereDto(
    int Id,
    CanalNotificare Canal,
    StatusIncercare Status,
    string? Destinatar,
    string? Detalii,
    DateTime CreatLa);