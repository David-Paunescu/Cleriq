import { RolSemnatar, StatusActRedactional } from '../../shared/enums';
import { HclDetalii } from './hcl.models';

export interface ActiuniHcl {
  // FE1 — redactare
  poateEditaContinut: boolean;
  poateRegenera: boolean;
  poateAtribuiNumar: boolean;
  poateSemna: boolean;
  poateDescarcaPdf: boolean;
  esteReadOnly: boolean;
  // FE2 — stări legale (antet)
  poatePublica: boolean;          // estePublicat false→true (Status >= Numerotat)
  poateDepublica: boolean;        // estePublicat true→false
  poatePublicaMol: boolean;       // Status == Semnat și fără MOL
  poateAnulaMol: boolean;         // Admin + are MOL + fără comunicare la prefect
  poateInvalida: boolean;         // ne-invalidat
  poateAnulaInvalidare: boolean;  // Admin + invalidat
  // FE2 — semnatari
  poateGestionaSemnatari: boolean;// Status != Semnat
  // FE2 — variantă semnată (garda B)
  poateIncarcaSemnat: boolean;    // Semnat + fără fișier (prima atașare, permisă chiar și post-circuit)
  poateInlocuiSemnat: boolean;    // Semnat + are fișier + neintrat în circuit (latch)
  poateStergeSemnat: boolean;     // Admin + are fișier + neintrat în circuit (latch)
  poateDescarcaSemnat: boolean;   // are fișier
}

// === FE3 — Comunicări prefect (tab self-contained) ===
// Oglindă strictă a gărzilor din ComunicariHclPrefectController.
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

// === FE3 — Relații cu alte acte (tab self-contained) ===
// POST și DELETE sunt [Admin,Secretar]; ștergerea doar din sursă e o constrângere
// structurală (doar rândurile relatiiSursa primesc buton), oglindită de backend.
export interface ActiuniRelatii {
  poateAdauga: boolean;
  poateSterge: boolean;
}

export function actiuniRelatii(esteAdminSauSecretar: boolean): ActiuniRelatii {
  return {
    poateAdauga: esteAdminSauSecretar,
    poateSterge: esteAdminSauSecretar
  };
}

// === FE3 — Anexe (tab self-contained, prin DocumenteService) ===
// POST/PUT/DELETE pe documente sunt [Admin,Secretar]. Pe HCL semnat, NumarOrdinAnexa
// e imutabil (gard PUT) → blocăm tip + nr. la editare (mirror).
export interface ActiuniAnexe {
  poateAdauga: boolean;
  poateEdita: boolean;
  poateSterge: boolean;
  numarOrdinBlocat: boolean;
}

export function actiuniAnexe(
  status: StatusActRedactional | null,
  esteAdminSauSecretar: boolean
): ActiuniAnexe {
  return {
    poateAdauga: esteAdminSauSecretar,
    poateEdita: esteAdminSauSecretar,
    poateSterge: esteAdminSauSecretar,
    numarOrdinBlocat: status === StatusActRedactional.Semnat
  };
}

// Oglinda gărzii de completitudine din HclController.Semneaza (art. 140 alin. 2):
// exact un Secretar UAT + fie un președinte de ședință, fie ≥2 semnatari alternativi
// cu motivul lipsei semnăturii completat.
export function semnatariCompleti(hcl: HclDetalii): boolean {
  const secretari = hcl.semnatari.filter(s => s.rolSemnatar === RolSemnatar.SecretarUat).length;
  const presedinti = hcl.semnatari.filter(s => s.rolSemnatar === RolSemnatar.PresedinteSedinta).length;
  const alternativi = hcl.semnatari.filter(
    s => s.rolSemnatar === RolSemnatar.SemnatarAlternativArt140).length;

  if (secretari !== 1) return false;

  const arePresedinte = presedinti === 1;
  const areAlternativi = alternativi >= 2
    && (hcl.motivLipsaSemnaturaPresedinte?.trim().length ?? 0) > 0;
  return arePresedinte || areAlternativi;
}

// Oglindă strictă a gărzilor pe status din HclController + SemnatariHclController.
// Precondițiile generării (președinte/secretar) NU sunt aici — nu sunt vizibile pe client,
// se tratează reactiv (snackbar din 400 la click în PuncteTab).
export function actiuniPermise(
  hcl: HclDetalii | null,
  esteAdminSauSecretar: boolean,
  esteAdmin: boolean
): ActiuniHcl {
  const status = hcl?.status ?? null;
  const esteSemnat = status === StatusActRedactional.Semnat;
  const esteDraft = status === StatusActRedactional.Draft;
  const esteNumerotat = status === StatusActRedactional.Numerotat;
  const celPutinNumerotat = esteNumerotat || esteSemnat;  // Status >= Numerotat

  const estePublicat = hcl?.estePublicat ?? false;
  const areMol = hcl?.dataPublicareMol != null;
  const aIntratInCircuit = hcl?.aIntratInCircuit ?? false; // latch: MOL sau comunicare prefect
  const areComunicari = (hcl?.comunicari?.length ?? 0) > 0;
  const areVariantaSemnata = hcl?.esteSemnat ?? false;     // fișier PDF semnat atașat
  const esteInvalidat = hcl?.dataInvalidare != null;

  return {
    poateEditaContinut: esteAdminSauSecretar && hcl != null && !esteSemnat,
    poateRegenera: esteAdminSauSecretar && hcl != null && !esteSemnat,
    poateAtribuiNumar: esteAdminSauSecretar && esteDraft,
    poateSemna: esteAdminSauSecretar && esteNumerotat && hcl != null && semnatariCompleti(hcl),
    poateDescarcaPdf: hcl != null,
    esteReadOnly: !esteAdminSauSecretar,

    poatePublica: esteAdminSauSecretar && hcl != null && celPutinNumerotat && !estePublicat,
    poateDepublica: esteAdminSauSecretar && hcl != null && estePublicat,
    poatePublicaMol: esteAdminSauSecretar && esteSemnat && !areMol,
    poateAnulaMol: esteAdmin && areMol && !areComunicari,
    poateInvalida: esteAdminSauSecretar && hcl != null && !esteInvalidat,
    poateAnulaInvalidare: esteAdmin && esteInvalidat,

    poateGestionaSemnatari: esteAdminSauSecretar && hcl != null && !esteSemnat,

    poateIncarcaSemnat: esteAdminSauSecretar && esteSemnat && !areVariantaSemnata,
    poateInlocuiSemnat: esteAdminSauSecretar && esteSemnat && areVariantaSemnata && !aIntratInCircuit,
    poateStergeSemnat: esteAdmin && areVariantaSemnata && !aIntratInCircuit,
    poateDescarcaSemnat: hcl != null && areVariantaSemnata
  };
}
