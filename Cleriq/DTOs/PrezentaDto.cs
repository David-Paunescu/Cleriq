using Cleriq.Models;

namespace Cleriq.DTOs;

public record SetarePrezentaDto(int ConsilierId, StatusPrezenta Status, DateTime? OraSosire);

public record PrezentaDto(int ConsilierId, string NumeCompletConsilier, StatusPrezenta Status, DateTime? OraSosire);

public record CvorumDto(int TotalConsilieriActivi, int Prezenti, int CvorumNecesar, bool CvorumIntrunit);