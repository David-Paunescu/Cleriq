using Cleriq.Models;

namespace Cleriq.Services;

public interface IServiciuDispozitieConvocare
{
    // Best-effort + idempotent: creează dispoziția de convocare (Draft, Individual) legată de ședință,
    // o singură dată per ședință. Dacă nu se poate deriva primarul/secretarul la dată, NU aruncă —
    // convocarea reușește oricum (dispoziția o poate crea manual secretarul).
    Task CreeazaDacaLipsesteAsync(Sedinta sedinta, CancellationToken ct = default);
}
