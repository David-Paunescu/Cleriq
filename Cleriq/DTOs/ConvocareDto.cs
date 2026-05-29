using Cleriq.Models;

namespace Cleriq.DTOs;

// Status general derivat din statusurile pe canale (doar la afișare, NU în DB)
public enum StatusConvocare
{
    TotalSucces = 1,
    PartialSucces = 2,
    Esuata = 3,
    FaraCoordonate = 4,
    InCursDeTrimitere = 5
}

public record ConvocareDto(
    int Id,
    int SedintaId,
    int ConsilierId,
    string NumeCompletConsilier,
    StatusTrimitere? EmailStatus,
    DateTime? EmailTrimisLa,
    string? EmailDetalii,
    StatusTrimitere? SmsStatus,
    DateTime? SmsTrimisLa,
    string? SmsDetalii,
    StatusConvocare StatusGeneral,
    DateTime CreatLa);

public record RezultatConvocareDto(
    int TotalConsilieri,
    int TotalSucces,            
    int InCursDeTrimitere,      
    int FaraCoordonate,
    DateTime? ConvocareTrimisaLa);