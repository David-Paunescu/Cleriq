export interface ActiuniDocument {
  poateAdauga: boolean;
  poateEdita: boolean;
  poateSterge: boolean;
  poateSchimbaVizibilitate: boolean;
}

export function actiuniPermise(esteAdminSauSecretar: boolean): ActiuniDocument {
  return {
    poateAdauga: esteAdminSauSecretar,
    poateEdita: esteAdminSauSecretar,
    poateSterge: esteAdminSauSecretar,
    poateSchimbaVizibilitate: esteAdminSauSecretar
  };
}