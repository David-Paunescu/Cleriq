import { Persoana } from './persoane.models';

export interface ActiuniPermisePersoana {
  poateSterge: boolean;
}

export function actiuniPermise(persoana: Persoana): ActiuniPermisePersoana {
  return {
    poateSterge: !persoana.areMandate
  };
}