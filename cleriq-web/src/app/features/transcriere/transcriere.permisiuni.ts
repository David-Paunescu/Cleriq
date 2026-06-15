import { StatusTranscriere } from '../../shared/enums';

export interface ActiuniTranscriere {
  poateIncarca: boolean;
  poateDescarcaAudio: boolean;
  poateRetry: boolean;
  poateEditaContinut: boolean;
  poateSterge: boolean;
  esteReadOnly: boolean;
}

export function actiuniPermise(
  status: StatusTranscriere | null,
  esteAdmin: boolean,
  esteAdminSauSecretar: boolean
): ActiuniTranscriere {
  if (!esteAdminSauSecretar) {
    return {
      poateIncarca: false,
      poateDescarcaAudio: false,
      poateRetry: false,
      poateEditaContinut: false,
      poateSterge: false,
      esteReadOnly: true
    };
  }

  const audioExista = status !== null;

  return {
    poateIncarca: status === null || status === StatusTranscriere.Esuata,
    poateDescarcaAudio: audioExista,
    poateRetry: status === StatusTranscriere.Esuata,
    poateEditaContinut: status === StatusTranscriere.Finalizata,
    poateSterge: audioExista && esteAdmin,
    esteReadOnly: false
  };
}