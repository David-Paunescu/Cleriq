import { RolSemnatarDispozitie, StatusActRedactional } from '../../shared/enums';
import { DispozitieDetalii } from './dispozitii.models';

export interface ActiuniDispozitie {
  // Redactare
  poateEditaContinut: boolean;
  poateRegenera: boolean;
  poateAtribuiNumar: boolean;
  poateSemna: boolean;
  poateDescarcaPdf: boolean;
  esteReadOnly: boolean;
  // Stări legale (antet)
  poatePublica: boolean;          // estePublicat false→true (Status >= Numerotat)
  poateDepublica: boolean;        // estePublicat true→false (liber, inclusiv Individual)
  poatePublicaMol: boolean;       // Status == Semnat și fără MOL
  poateAnulaMol: boolean;         // Admin + are MOL + fără comunicare la prefect
  poateInvalida: boolean;         // ne-invalidat
  poateAnulaInvalidare: boolean;  // Admin + invalidat
  // Semnatari / refuz contrasemnare
  poateRefuzaContrasemnare: boolean;  // Admin/Secretar + !Semnat + !refuz deja (înlocuiește poateGestionaSemnatari)
  // Variantă semnată (latch)
  poateIncarcaSemnat: boolean;    // Semnat + fără fișier (prima atașare, permisă chiar și post-circuit)
  poateInlocuiSemnat: boolean;    // Semnat + are fișier + neintrat în circuit (latch)
  poateStergeSemnat: boolean;     // Admin + are fișier + neintrat în circuit (latch)
  poateDescarcaSemnat: boolean;   // are fișier
  // Delete act întreg
  poateSterge: boolean;           // Admin + fără comunicări + (invalidat SAU (!Semnat && !Publicat))
}

// Oglinda gărzii de completitudine din DispozitiiController.Semneaza: exact 1 Emitent +
// (1 SecretarContrasemnatura SAU refuz motivat). Rândul de secretar soft-șters la refuz NU apare
// în `semnatari` (filtrul global îl exclude) → se numără corect.
export function semnatariCompletiDispozitie(d: DispozitieDetalii): boolean {
  const emitenti = d.semnatari.filter(s => s.rolSemnatar === RolSemnatarDispozitie.Emitent).length;
  const secretari = d.semnatari.filter(
    s => s.rolSemnatar === RolSemnatarDispozitie.SecretarContrasemnatura).length;

  if (emitenti !== 1) return false;

  const areContrasemnatura = secretari === 1;
  const areRefuzMotivat = d.contrasemnaturaRefuzata
    && (d.obiectieLegalitateSecretar?.trim().length ?? 0) > 0;
  return areContrasemnatura || areRefuzMotivat;
}

// Oglindă strictă a gărzilor pe status din DispozitiiController.
// ⚠ Două capcane de nume (aceleași ca la HCL):
//  - `esteSemnat` (DTO, bool) = are PDF semnat ATAȘAT ≠ `status === Semnat` (ciclu de viață).
//  - `poateIncarcaSemnat` NU verifică latch-ul: prima atașare e permisă și după intrarea în circuit
//    (gardă backend: freeze doar când există DEJA fișier). Doar Înlocuiește/Șterge verifică latch-ul.
export function actiuniPermise(
  d: DispozitieDetalii | null,
  esteAdminSauSecretar: boolean,
  esteAdmin: boolean
): ActiuniDispozitie {
  const status = d?.status ?? null;
  const esteSemnatStatus = status === StatusActRedactional.Semnat;   // ciclu de viață
  const esteDraft = status === StatusActRedactional.Draft;
  const esteNumerotat = status === StatusActRedactional.Numerotat;
  const celPutinNumerotat = esteNumerotat || esteSemnatStatus;       // Status >= Numerotat

  const estePublicat = d?.estePublicat ?? false;
  const areMol = d?.dataPublicareMol != null;
  const aIntratInCircuit = d?.aIntratInCircuit ?? false;            // latch: MOL sau comunicare prefect
  const areComunicari = (d?.comunicari?.length ?? 0) > 0;
  const areVariantaSemnata = d?.esteSemnat ?? false;               // fișier PDF semnat atașat
  const esteInvalidat = d?.dataInvalidare != null;
  const contrasemnaturaRefuzata = d?.contrasemnaturaRefuzata ?? false;

  return {
    poateEditaContinut: esteAdminSauSecretar && d != null && !esteSemnatStatus,
    poateRegenera: esteAdminSauSecretar && d != null && !esteSemnatStatus,
    poateAtribuiNumar: esteAdminSauSecretar && esteDraft,
    poateSemna: esteAdminSauSecretar && esteNumerotat && d != null && semnatariCompletiDispozitie(d),
    poateDescarcaPdf: d != null,
    esteReadOnly: !esteAdminSauSecretar,

    poatePublica: esteAdminSauSecretar && d != null && celPutinNumerotat && !estePublicat,
    poateDepublica: esteAdminSauSecretar && d != null && estePublicat,
    poatePublicaMol: esteAdminSauSecretar && esteSemnatStatus && !areMol,
    poateAnulaMol: esteAdmin && areMol && !areComunicari,
    poateInvalida: esteAdminSauSecretar && d != null && !esteInvalidat,
    poateAnulaInvalidare: esteAdmin && esteInvalidat,

    poateRefuzaContrasemnare:
      esteAdminSauSecretar && d != null && !esteSemnatStatus && !contrasemnaturaRefuzata,

    poateIncarcaSemnat: esteAdminSauSecretar && esteSemnatStatus && !areVariantaSemnata,
    poateInlocuiSemnat: esteAdminSauSecretar && esteSemnatStatus && areVariantaSemnata && !aIntratInCircuit,
    poateStergeSemnat: esteAdmin && areVariantaSemnata && !aIntratInCircuit,
    poateDescarcaSemnat: d != null && areVariantaSemnata,

    poateSterge: esteAdmin && !areComunicari && (esteInvalidat || (!esteSemnatStatus && !estePublicat))
  };
}

// === Comunicări prefect (tab self-contained) — mirror ComunicariDispozitiiPrefectController ===
export interface ActiuniComunicari {
  poateAdauga: boolean;   // [Admin,Secretar] + Status >= Numerotat (gard POST)
  poateEdita: boolean;    // [Admin,Secretar] (PUT răspuns)
  poateSterge: boolean;   // [Admin] (DELETE)
}

export function actiuniComunicari(
  status: StatusActRedactional | null,
  esteAdminSauSecretar: boolean,
  esteAdmin: boolean
): ActiuniComunicari {
  const celPutinNumerotat = status === StatusActRedactional.Numerotat
    || status === StatusActRedactional.Semnat;
  return {
    poateAdauga: esteAdminSauSecretar && celPutinNumerotat,
    poateEdita: esteAdminSauSecretar,
    poateSterge: esteAdmin
  };
}
