import { StatusSedinta } from '../../shared/enums';

export interface ActiuniConvocare {
  poateTrimite: boolean;
  poateRetry: boolean;
  poateReseta: boolean;
  esteReadOnly: boolean;
}

export function actiuniPermise(
  status: StatusSedinta,
  esteAdminSauSecretar: boolean
): ActiuniConvocare {
  if (!esteAdminSauSecretar) {
    return {
      poateTrimite: false,
      poateRetry: false,
      poateReseta: false,
      esteReadOnly: true
    };
  }

  const blocateStatic = status === StatusSedinta.Finalizata
                     || status === StatusSedinta.Anulata;

  return {
    poateTrimite: status === StatusSedinta.Planificata,
    poateRetry: status === StatusSedinta.Convocata
              || status === StatusSedinta.InDesfasurare,
    poateReseta: status === StatusSedinta.Planificata
              || status === StatusSedinta.Convocata,
    esteReadOnly: blocateStatic
  };
}