namespace Cleriq.Models;

// Contract minimal pentru actele administrative cu registru de numerotare propriu
// (HCL, Dispoziție). Permite serviciului generic ServiciuNumerotareActe să opereze
// peste orice astfel de act prin _context.Set<T>(). Proprietățile MAPATE (Numar,
// AnNumerotare, Status, Continut) se traduc în SQL pe tipul concret T la call-site
// (EF rezolvă proprietatea entității după nume).
public interface IActNumerotat : IEntitateCuTenant
{
    int? Numar { get; set; }
    int? AnNumerotare { get; set; }
    StatusActRedactional Status { get; set; }
    string? Continut { get; set; }

    // Data de la care se derivă anul de registru (DataAdoptare la HCL / DataEmitere la
    // Dispoziție). Implementatorii o expun ca proprietate computed [NotMapped] — folosită
    // DOAR in-memory (serviciul aplică .LaFusOrar(...).Year), niciodată într-un .Where SQL.
    DateTime DataReferintaNumerotare { get; }
}
