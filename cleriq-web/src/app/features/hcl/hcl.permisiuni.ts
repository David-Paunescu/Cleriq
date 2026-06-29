import { RolSemnatar, StatusHclRedactional } from '../../shared/enums';
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
  poateAnulaMol: boolean;         // Admin + are MOL
  poateInvalida: boolean;         // ne-invalidat
  poateAnulaInvalidare: boolean;  // Admin + invalidat
  // FE2 — semnatari
  poateGestionaSemnatari: boolean;// Status != Semnat
  // FE2 — variantă semnată (garda B)
  poateIncarcaSemnat: boolean;    // Semnat + fără fișier (prima atașare, permisă chiar și post-MOL)
  poateInlocuiSemnat: boolean;    // Semnat + are fișier + fără MOL
  poateStergeSemnat: boolean;     // Admin + are fișier + fără MOL
  poateDescarcaSemnat: boolean;   // are fișier
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
  const esteSemnat = status === StatusHclRedactional.Semnat;
  const esteDraft = status === StatusHclRedactional.Draft;
  const esteNumerotat = status === StatusHclRedactional.Numerotat;
  const celPutinNumerotat = esteNumerotat || esteSemnat;  // Status >= Numerotat

  const estePublicat = hcl?.estePublicat ?? false;
  const areMol = hcl?.dataPublicareMol != null;
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
    poateAnulaMol: esteAdmin && areMol,
    poateInvalida: esteAdminSauSecretar && hcl != null && !esteInvalidat,
    poateAnulaInvalidare: esteAdmin && esteInvalidat,

    poateGestionaSemnatari: esteAdminSauSecretar && hcl != null && !esteSemnat,

    poateIncarcaSemnat: esteAdminSauSecretar && esteSemnat && !areVariantaSemnata,
    poateInlocuiSemnat: esteAdminSauSecretar && esteSemnat && areVariantaSemnata && !areMol,
    poateStergeSemnat: esteAdmin && areVariantaSemnata && !areMol,
    poateDescarcaSemnat: hcl != null && areVariantaSemnata
  };
}
