import { StatusSedinta } from '../../shared/enums';

export interface ActiuniPermise {
  poateEdita: boolean;
  poateSterge: boolean;
  poateIncepe: boolean;
  poateFinaliza: boolean;
  poateAnula: boolean;
}

export function actiuniPermise(status: StatusSedinta): ActiuniPermise {
  return {
    poateEdita: status === StatusSedinta.Planificata || status === StatusSedinta.Convocata,
    poateSterge: status !== StatusSedinta.Finalizata,
    poateIncepe: status === StatusSedinta.Convocata,
    poateFinaliza: status === StatusSedinta.InDesfasurare,
    poateAnula: status === StatusSedinta.Planificata || status === StatusSedinta.Convocata
  };
}