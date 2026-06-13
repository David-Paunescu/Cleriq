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