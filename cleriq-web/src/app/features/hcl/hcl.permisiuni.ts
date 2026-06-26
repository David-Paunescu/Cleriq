import { RolSemnatar, StatusHclRedactional } from '../../shared/enums';
import { HclDetalii } from './hcl.models';

export interface ActiuniHcl {
  poateEditaContinut: boolean;
  poateRegenera: boolean;
  poateAtribuiNumar: boolean;
  poateSemna: boolean;
  poateDescarcaPdf: boolean;
  esteReadOnly: boolean;
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

// Oglindă strictă a gărzilor pe status din HclController.
// Precondițiile generării (președinte/secretar) NU sunt aici — nu sunt vizibile pe client,
// se tratează reactiv (snackbar din 400 la click în PuncteTab).
export function actiuniPermise(
  hcl: HclDetalii | null,
  esteAdminSauSecretar: boolean
): ActiuniHcl {
  const status = hcl?.status ?? null;
  const esteSemnat = status === StatusHclRedactional.Semnat;
  const esteDraft = status === StatusHclRedactional.Draft;
  const esteNumerotat = status === StatusHclRedactional.Numerotat;

  return {
    poateEditaContinut: esteAdminSauSecretar && hcl != null && !esteSemnat,
    poateRegenera: esteAdminSauSecretar && hcl != null && !esteSemnat,
    poateAtribuiNumar: esteAdminSauSecretar && esteDraft,
    poateSemna: esteAdminSauSecretar && esteNumerotat && hcl != null && semnatariCompleti(hcl),
    poateDescarcaPdf: hcl != null,
    esteReadOnly: !esteAdminSauSecretar
  };
}
