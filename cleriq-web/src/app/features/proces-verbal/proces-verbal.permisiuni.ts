import { StatusProcesVerbal } from '../../shared/enums';

export interface ActiuniProcesVerbal {
  poateGenera: boolean;
  poateEdita: boolean;
  poateFinaliza: boolean;
  poateDescarcaPdf: boolean;
  poateIncarcaSemnat: boolean;
  poateDescarcaSemnat: boolean;
  poateStergeSemnat: boolean;
  esteReadOnly: boolean;
}

export function actiuniPermise(
  status: StatusProcesVerbal | null,
  areSemnat: boolean,
  esteAdmin: boolean,
  esteAdminSauSecretar: boolean
): ActiuniProcesVerbal {
  const esteDraft = status === StatusProcesVerbal.Draft;
  const esteFinalizat = status === StatusProcesVerbal.Finalizat;

  if (!esteAdminSauSecretar) {
    return {
      poateGenera: false,
      poateEdita: false,
      poateFinaliza: false,
      poateDescarcaPdf: status !== null,
      poateIncarcaSemnat: false,
      poateDescarcaSemnat: status !== null && areSemnat,
      poateStergeSemnat: false,
      esteReadOnly: true
    };
  }

  return {
    poateGenera: status === null || esteDraft,
    poateEdita: esteDraft,
    poateFinaliza: esteDraft,
    poateDescarcaPdf: status !== null,
    poateIncarcaSemnat: esteFinalizat,
    poateDescarcaSemnat: status !== null && areSemnat,
    poateStergeSemnat: esteFinalizat && areSemnat && esteAdmin,
    esteReadOnly: false
  };
}