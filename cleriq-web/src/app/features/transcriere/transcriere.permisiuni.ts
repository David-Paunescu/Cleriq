import { StatusTranscriere } from '../../shared/enums';
import { TranscriereContinut } from './transcriere.models';

export interface ActiuniTranscriere {
  poateIncarca: boolean;
  poateDescarcaAudio: boolean;
  poateRetry: boolean;
  poateEditaContinut: boolean;
  poateSterge: boolean;
  poatePublica: boolean;
  poateRetragePublicare: boolean;
  esteReadOnly: boolean;
}

export function actiuniPermise(
  status: StatusTranscriere | null,
  continut: TranscriereContinut | null,
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
      poatePublica: false,
      poateRetragePublicare: false,
      esteReadOnly: true
    };
  }

  const audioExista = status !== null;
  const editatNeVid = (continut?.continutEditat?.trim().length ?? 0) > 0;
  const estePublicat = (continut?.continutPublicat?.length ?? 0) > 0;

  return {
    poateIncarca: status === null || status === StatusTranscriere.Esuata,
    poateDescarcaAudio: audioExista,
    poateRetry: status === StatusTranscriere.Esuata,
    poateEditaContinut: status === StatusTranscriere.Finalizata,
    poateSterge: audioExista && esteAdmin,
    poatePublica: status === StatusTranscriere.Finalizata && editatNeVid,
    poateRetragePublicare: estePublicat && esteAdmin,
    esteReadOnly: false
  };
}