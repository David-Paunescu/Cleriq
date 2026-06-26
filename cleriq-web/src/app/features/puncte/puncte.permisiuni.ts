import { RezultatPunct, TipPunct } from '../../shared/enums';
import { PunctOrdineZi } from './puncte.models';

export interface ActiuniPunct {
  poateEdita: boolean;
  poateSterge: boolean;
  poateInchideVot: boolean;
  poateAmana: boolean;
  poateRetrage: boolean;
}

export function actiuniPermise(punct: PunctOrdineZi): ActiuniPunct {
  const areRezultat = punct.rezultat != null;
  return {
    poateEdita: true,
    poateSterge: true,
    poateInchideVot: punct.necesitaVot && !areRezultat,
    poateAmana: !areRezultat,
    poateRetrage: !areRezultat
  };
}

// Buton „Generează HCL": punct ProiectHCL adoptat, fără HCL deja generat (hclId == null).
// Precondițiile backend (președinte/secretar) sunt reactive — snackbar din 400 la click.
export function poateGeneraHcl(punct: PunctOrdineZi): boolean {
  return punct.tip === TipPunct.ProiectHCL
    && punct.rezultat === RezultatPunct.Adoptat
    && punct.hclId == null;
}