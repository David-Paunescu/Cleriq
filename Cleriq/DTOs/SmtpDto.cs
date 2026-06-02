using Cleriq.Models;

namespace Cleriq.DTOs;

public record ConfigSmtpDto(
    string? Host,
    int? Port,
    string? Utilizator,
    string? EmailFrom,
    string? NumeFrom,
    SmtpSecuritate Securitate,
    bool ParolaSetata);

public record SetareSmtpDto(
    string Host,
    int Port,
    string Utilizator,
    string? Parola,
    string EmailFrom,
    string? NumeFrom,
    SmtpSecuritate Securitate);

public record TestareSmtpDto(string EmailDestinatar);

public record RezultatTestareSmtpDto(bool Succes, string? Detalii);